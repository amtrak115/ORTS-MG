﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Common.Diagnostics;
using FreeTrainSimulator.Common.Info;
using FreeTrainSimulator.Graphics;
using FreeTrainSimulator.Graphics.Xna;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.ActivityRunner.Viewer3D;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Orts.ActivityRunner.Processes
{
    internal sealed class RenderProcess : IDisposable
    {
        public const int ShadowMapCountMaximum = 4;

        public Point DisplaySize { get; private set; }

        private readonly Profiler profiler;

        private readonly GameHost game;
        private Viewport viewport;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly Form windowForm;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool syncing;
        private Point windowPosition;
        private System.Drawing.Size windowSize;
        private Screen currentScreen;
        private ScreenMode currentScreenMode;
        private bool toggleScreenRequested;

        private readonly Action onClientSizeChanged;

        private readonly GraphicsDeviceManager graphicsDeviceManager;

        private RenderFrame CurrentFrame;   // a frame contains a list of primitives to draw at a specified time
        private RenderFrame NextFrame;      // we prepare the next frame in the background while the current one is rendering,

        public bool IsMouseVisible { get; set; }  // handles cross thread issues by signalling RenderProcess of a change
        public Cursor ActualCursor { get; set; } = Cursors.Default;

        public ref readonly Viewport Viewport => ref viewport;

        // Diagnostic information
        public int[] PrimitiveCount { get; private set; }
        public int[] PrimitivePerFrame { get; private set; }
        public int[] ShadowPrimitiveCount { get; private set; }
        public int[] ShadowPrimitivePerFrame { get; private set; }

        // Dynamic shadow map setup.
        public static int ShadowMapCount { get; private set; } = -1; // number of shadow maps
        public static int[] ShadowMapDistance; // distance of shadow map center from camera
        public static int[] ShadowMapDiameter; // diameter of shadow map
        public static float[] ShadowMapLimit; // diameter of shadow map far edge from camera
        private bool disposedValue;

        internal RenderProcess(GameHost gameHost)
        {
            this.game = gameHost;
            windowForm = (Form)Control.FromHandle(gameHost.Window.Handle);

            profiler = new Profiler("Render");
            profiler.SetThread();
            Profiler.ProfilingData[ProcessType.Render] = profiler;
            gameHost.Window.Title = RuntimeInfo.ApplicationName;

            LoadSettings();

            if (!string.IsNullOrEmpty(gameHost.UserSettings.Language))
            {
                try
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(gameHost.UserSettings.Language);
                }
                catch (CultureNotFoundException) { }
            }

            PrimitiveCount = new int[EnumExtension.GetLength<RenderPrimitiveSequence>()];
            PrimitivePerFrame = new int[EnumExtension.GetLength<RenderPrimitiveSequence>()];

            // Run the game initially at 10FPS fixed-time-step. Do not change this! It affects the loading performance.
            gameHost.IsFixedTimeStep = true;
            gameHost.TargetElapsedTime = TimeSpan.FromMilliseconds(100);
            gameHost.InactiveSleepTime = TimeSpan.FromMilliseconds(100);

            graphicsDeviceManager = new GraphicsDeviceManager(gameHost)
            {
                // Set up the rest of the graphics according to the settings.
                SynchronizeWithVerticalRetrace = gameHost.UserSettings.VerticalSync,
                PreferredBackBufferFormat = SurfaceFormat.Color,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = gameHost.UserSettings.MultiSamplingCount > 0,
                IsFullScreen = gameHost.UserSettings.ScreenMode == ScreenMode.BorderlessFullscreen,
            };
            graphicsDeviceManager.PreparingDeviceSettings += GraphicsPreparingDeviceSettings;

            gameHost.Window.Position = windowPosition;
            SetScreenMode(currentScreenMode);
            RenderPrimitive.SetGraphicsDevice(gameHost.GraphicsDevice);

            // using reflection to be able to trigger ClientSizeChanged event manually as this is not 
            // reliably raised otherwise with the resize functionality below in SetScreenMode
            MethodInfo m = gameHost.Window.GetType().GetMethod("OnClientSizeChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            onClientSizeChanged = (Action)Delegate.CreateDelegate(typeof(Action), gameHost.Window, m);

            windowForm.LocationChanged += WindowForm_LocationChanged;
            windowForm.ClientSizeChanged += WindowForm_ClientSizeChanged;
            FreeTrainSimulator.Common.Info.SystemInfo.SetGraphicAdapterInformation(graphicsDeviceManager.GraphicsDevice.Adapter.Description);
        }

        private void WindowForm_LocationChanged(object sender, EventArgs e)
        {
            WindowForm_ClientSizeChanged(sender, e);
        }

        private void WindowForm_ClientSizeChanged(object sender, EventArgs e)
        {
            if (syncing)
                return;
            if (currentScreenMode == ScreenMode.Windowed)
                windowSize = new System.Drawing.Size(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height);
            //originally, following code would be in Window.LocationChanged handler, but seems to be more reliable here for MG version 3.7.1
            if (currentScreenMode == ScreenMode.Windowed)
                windowPosition = game.Window.Position;
            // if (fullscreen) gameWindow is moved to different screen we may need to refit for different screen resolution
            Screen newScreen = Screen.FromControl(windowForm);
            (newScreen, currentScreen) = (currentScreen, newScreen);
            if (newScreen.DeviceName != currentScreen.DeviceName && currentScreenMode != ScreenMode.Windowed)
            {
                SetScreenMode(currentScreenMode);
                //reset Window position to center on new screen
                windowPosition = new Point(
                    currentScreen.WorkingArea.Left + (currentScreen.WorkingArea.Size.Width - windowSize.Width) / 2,
                    currentScreen.WorkingArea.Top + (currentScreen.WorkingArea.Size.Height - windowSize.Height) / 2);
            }
        }

        private void GraphicsPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.GraphicsProfile = GraphicsProfile.HiDef;
            // This stops ResolveBackBuffer() clearing the back buffer.
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            e.GraphicsDeviceInformation.PresentationParameters.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = game.UserSettings.MultiSamplingCount;
        }

        internal void Start()
        {
            DisplaySize = game.GraphicsDevice.Viewport.Bounds.Size;

            ShadowMapCount = game.UserSettings.ShadowMapCount;
            if (!game.UserSettings.DynamicShadows || ShadowMapCount < 0)
                ShadowMapCount = 0;
            else if (ShadowMapCount > ShadowMapCountMaximum)
                ShadowMapCount = ShadowMapCountMaximum;
            if (ShadowMapCount < 1)
                game.UserSettings.DynamicShadows = false;

            ShadowMapDistance = new int[ShadowMapCount];
            ShadowMapDiameter = new int[ShadowMapCount];
            ShadowMapLimit = new float[ShadowMapCount];

            ShadowPrimitiveCount = new int[ShadowMapCount];
            ShadowPrimitivePerFrame = new int[ShadowMapCount];

            InitializeShadowMapLocations();

            CurrentFrame = new RenderFrame(game);
            NextFrame = new RenderFrame(game);
        }

        private void InitializeShadowMapLocations()
        {
            float ratio = (float)DisplaySize.X / DisplaySize.Y;
            float fov = MathHelper.ToRadians(game.UserSettings.FieldOfView);
            float n = 0.5f;
            float f = game.UserSettings.ViewingDistance / 2f;

            var m = (float)ShadowMapCount;
            var LastC = n;
            for (var shadowMapIndex = 0; shadowMapIndex < ShadowMapCount; shadowMapIndex++)
            {
                //     Clog  = split distance i using logarithmic splitting
                //         i
                // Cuniform  = split distance i using uniform splitting
                //         i
                //         n = near view plane
                //         f = far view plane
                //         m = number of splits
                //
                //                   i/m
                //     Clog  = n(f/n)
                //         i
                // Cuniform  = n+(f-n)i/m
                //         i

                // Calculate the two Cs and average them to get a good balance.
                var i = (float)(shadowMapIndex + 1);
                var Clog = n * (float)Math.Pow(f / n, i / m);
                var Cuniform = n + (f - n) * i / m;
                var C = (3 * Clog + Cuniform) / 4;

                // This shadow map goes from LastC to C; calculate the correct center and diameter for the sphere from the view frustum.
                var height1 = (float)Math.Tan(fov / 2) * LastC;
                var height2 = (float)Math.Tan(fov / 2) * C;
                var width1 = height1 * ratio;
                var width2 = height2 * ratio;
                var corner1 = new Vector3(height1, width1, LastC);
                var corner2 = new Vector3(height2, width2, C);
                var cornerCenter = (corner1 + corner2) / 2;
                var length = cornerCenter.Length();
                cornerCenter.Normalize();
                var center = length / Vector3.Dot(cornerCenter, Vector3.UnitZ);
                var diameter = 2 * (float)Math.Sqrt(height2 * height2 + width2 * width2 + (C - center) * (C - center));

                ShadowMapDistance[shadowMapIndex] = (int)center;
                ShadowMapDiameter[shadowMapIndex] = (int)diameter;
                ShadowMapLimit[shadowMapIndex] = C;
                LastC = C;
            }
        }

        internal void Initialize()
        {
            SetScreenMode(currentScreenMode);
            viewport = game.GraphicsDevice.Viewport;
        }

        internal void Update(GameTime gameTime)
        {
            if (IsMouseVisible != game.IsMouseVisible)
                game.IsMouseVisible = IsMouseVisible;

            Cursor.Current = ActualCursor;

            if (toggleScreenRequested)
            {
                SetScreenMode(currentScreenMode.Next());
                toggleScreenRequested = false;
                viewport = game.GraphicsDevice.Viewport;
            }

            game.UpdaterProcess.WaitForComplection();

            // Swap frames and start the next update (non-threaded updater does the whole update).
            (CurrentFrame, NextFrame) = (NextFrame, CurrentFrame);
            game.UpdaterProcess.TriggerUpdate(NextFrame, gameTime);
            game.SystemProcess.TriggerUpdate(gameTime);
        }

        private void LoadSettings()
        {
            currentScreenMode = game.UserSettings.ScreenMode;
            currentScreen = game.UserSettings.WindowScreen >= 0 && game.UserSettings.WindowScreen < Screen.AllScreens.Length ? Screen.AllScreens[game.UserSettings.WindowScreen] : Screen.PrimaryScreen;

            windowSize.Width = game.UserSettings.WindowSettings[WindowSetting.Size].X;
            windowSize.Height = game.UserSettings.WindowSettings[WindowSetting.Size].Y;

            windowPosition = game.UserSettings.WindowSettings[WindowSetting.Location].ToPoint();
            windowPosition = new Point(
                currentScreen.WorkingArea.Left + windowPosition.X * (currentScreen.WorkingArea.Size.Width - windowSize.Width) / 100,
                currentScreen.WorkingArea.Top + windowPosition.Y * (currentScreen.WorkingArea.Size.Height - windowSize.Height) / 100);
        }

        private void SaveSettings()
        {
            /// Settings which should be persisted in the model, need to be configured also in <see cref="FreeTrainSimulator.Models.Shim.ProfileSettingsExtensions.UpdateRuntimeUserSettingsModel"/>
            game.UserSettings.WindowSettings[WindowSetting.Size] = (windowSize.Width, windowSize.Height);
            game.UserSettings.WindowSettings[WindowSetting.Location] = (
                (int)Math.Max(0, Math.Round(100f * (windowPosition.X - currentScreen.Bounds.Left) / (currentScreen.WorkingArea.Width - windowSize.Width))),
                (int)Math.Max(0, Math.Round(100.0 * (windowPosition.Y - currentScreen.Bounds.Top) / (currentScreen.WorkingArea.Height - windowSize.Height))));
            game.UserSettings.WindowScreen = Screen.AllScreens.ToList().IndexOf(currentScreen);
        }

        private void SetScreenMode(ScreenMode targetMode)
        {
            syncing = true;
            windowForm.Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (graphicsDeviceManager.IsFullScreen)
                    graphicsDeviceManager.ToggleFullScreen();
                switch (targetMode)
                {
                    case ScreenMode.Windowed:
                        if (targetMode != currentScreenMode)
                            game.Window.Position = windowPosition;
                        windowForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                        windowForm.Size = windowSize;
                        graphicsDeviceManager.PreferredBackBufferWidth = windowSize.Width;
                        graphicsDeviceManager.PreferredBackBufferHeight = windowSize.Height;
                        graphicsDeviceManager.ApplyChanges();
                        break;
                    case ScreenMode.WindowedFullscreen:
                        graphicsDeviceManager.PreferredBackBufferWidth = windowSize.Width;
                        graphicsDeviceManager.PreferredBackBufferHeight = windowSize.Height;
                        windowForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                        game.Window.Position = new Point(currentScreen.WorkingArea.Location.X, currentScreen.WorkingArea.Location.Y);
                        graphicsDeviceManager.ApplyChanges();
                        if (!graphicsDeviceManager.IsFullScreen)
                            graphicsDeviceManager.ToggleFullScreen();
                        break;
                    case ScreenMode.BorderlessFullscreen:
                        graphicsDeviceManager.PreferredBackBufferWidth = currentScreen.Bounds.Width;
                        graphicsDeviceManager.PreferredBackBufferHeight = currentScreen.Bounds.Height;
                        graphicsDeviceManager.ApplyChanges();
                        windowForm.FormBorderStyle = FormBorderStyle.None;
                        game.Window.Position = new Point(currentScreen.Bounds.X, currentScreen.Bounds.Y);
                        graphicsDeviceManager.ApplyChanges();
                        break;
                }
            });
            currentScreenMode = targetMode;
            onClientSizeChanged?.Invoke();
            syncing = false;
        }

        internal void BeginDraw()
        {
            if (game.State == null)
                return;

            profiler.Start();

            CurrentFrame.IsScreenChanged = DisplaySize != game.GraphicsDevice.Viewport.Bounds.Size;
            if (CurrentFrame.IsScreenChanged)
            {
                DisplaySize = game.GraphicsDevice.Viewport.Bounds.Size;
                InitializeShadowMapLocations();
            }

            game.State.BeginRender(CurrentFrame);
        }

        internal void Draw(GameTime gameTime)
        {
            try
            {
                CurrentFrame.Draw(gameTime);
            }
            catch (Exception error) when (!Debugger.IsAttached)
            {
                game.ProcessReportError(error);
            }
        }

        internal void EndDraw()
        {
            if (game.State == null)
                return;

            game.State.EndRender(CurrentFrame);

            Array.Copy(PrimitiveCount, PrimitivePerFrame, PrimitiveCount.Length);
            Array.Copy(ShadowPrimitiveCount, ShadowPrimitivePerFrame, ShadowMapCount);

            profiler.Stop();
            (game.SystemInfo[DiagnosticInfo.GpuMetric] as GraphicMetrics).CurrentMetrics = game.GraphicsDevice.Metrics;
        }

        internal void Stop()
        {
            SaveSettings();
        }

        public void ToggleFullScreen()
        {
            toggleScreenRequested = true;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    graphicsDeviceManager?.Dispose();
                    // TODO: dispose managed state (managed objects)
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
