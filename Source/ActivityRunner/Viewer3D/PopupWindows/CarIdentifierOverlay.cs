﻿using System;
using System.Collections.Generic;
using System.Linq;

using FreeTrainSimulator.Common;
using FreeTrainSimulator.Common.Input;
using FreeTrainSimulator.Common.Position;
using FreeTrainSimulator.Graphics.Window;
using FreeTrainSimulator.Graphics.Window.Controls;
using FreeTrainSimulator.Graphics.Window.Controls.Layout;
using FreeTrainSimulator.Graphics.Xna;
using FreeTrainSimulator.Models.Settings;

using GetText;

using Microsoft.Xna.Framework;

using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;

namespace Orts.ActivityRunner.Viewer3D.PopupWindows
{
    internal sealed partial class CarIdentifierOverlay : OverlayBase
    {

        private enum ViewMode
        {
            Cars,
            Trains,
        }

        private readonly UserCommandController<UserCommand> userCommandController;
        private readonly Viewer viewer;
        private readonly ProfileUserSettingsModel userSettings;
        private ViewMode viewMode;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private ControlLayout controlLayout;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly ResourceGameComponent<Label3DOverlay, int> labelCache;
        private readonly List<Label3DOverlay> labelList = new List<Label3DOverlay>();
        private readonly CameraViewProjectionHolder cameraViewProjection;

        public CarIdentifierOverlay(WindowManager owner, ProfileUserSettingsModel userSettings, Viewer viewer, Catalog catalog = null) :
            base(owner, catalog ?? CatalogManager.Catalog)
        {
            ArgumentNullException.ThrowIfNull(viewer);
            this.userSettings = userSettings;
            userCommandController = viewer.UserCommandController;
            this.viewer = viewer;
            ZOrder = -5;

            labelCache = Owner.Game.Components.OfType<ResourceGameComponent<Label3DOverlay, int>>().FirstOrDefault() ?? new ResourceGameComponent<Label3DOverlay, int>(Owner.Game);
            cameraViewProjection = new CameraViewProjectionHolder(viewer);
        }

        protected override ControlLayout Layout(ControlLayout layout, float headerScaling = 1)
        {
            return controlLayout = base.Layout(layout, headerScaling);
        }

        protected override void Update(GameTime gameTime, bool shouldUpdate)
        {
            if (shouldUpdate)
            {
                ref readonly WorldLocation cameraLocation = ref viewer.Camera.CameraWorldLocation;
                labelList.Clear();
                foreach (Train train in Simulator.Instance.Trains)
                {
                    Tile firstCarDelta = train.FirstCar.WorldPosition.Tile - cameraLocation.Tile;
                    Tile lastCarDelta = train.LastCar.WorldPosition.Tile - cameraLocation.Tile;

                    //only consider trains which are within 1 tile max distance from current camera position
                    if ((Math.Abs(firstCarDelta.X) < 2 && Math.Abs(firstCarDelta.Z) < 2) || ((Math.Abs(lastCarDelta.X) < 2 && Math.Abs(lastCarDelta.Z) < 2)))
                    {
                        switch (viewMode)
                        {
                            case ViewMode.Cars:
                                foreach (TrainCar car in train.Cars)
                                {
                                    labelList.Add(labelCache.Get(car.GetHashCode(), () => new Label3DOverlay(this, car.CarID, LabelType.Car, car.CarHeightM, car, cameraViewProjection)));
                                }
                                break;
                            case ViewMode.Trains:
                                labelList.Add(labelCache.Get(HashCode.Combine(train.GetHashCode(), train.FirstCar.GetHashCode()), () => new Label3DOverlay(this, train.Name, LabelType.Car, train.FirstCar.CarHeightM, train.FirstCar, cameraViewProjection)));
                                if (train.Cars.Count > 2)
                                    labelList.Add(labelCache.Get(HashCode.Combine(train.GetHashCode(), train.LastCar.GetHashCode()), () => new Label3DOverlay(this, train.Name, LabelType.Car, train.LastCar.CarHeightM, train.LastCar, cameraViewProjection)));
                                break;
                        }
                    }
                }
                controlLayout.Controls.Clear();
                foreach (Label3DOverlay item in labelList)
                    controlLayout.Controls.Add(item);
            }
            base.Update(gameTime, shouldUpdate);
        }

        public override bool Open()
        {
            ChangeMode();
            userCommandController.AddEvent(UserCommand.DisplayCarLabels, KeyEventType.KeyPressed, TabAction, true);
            return base.Open();
        }

        public override bool Close()
        {
            userCommandController.RemoveEvent(UserCommand.DisplayCarLabels, KeyEventType.KeyPressed, TabAction);
            Simulator.Instance.Confirmer.Information(Catalog.GetString("Train and car labels hidden."));
            return base.Close();
        }

        protected override void Initialize()
        {
            if (EnumExtension.GetValue(userSettings.PopupSettings[ViewerWindowType.CarIdentifierOverlay], out viewMode))
            {
                ChangeMode();
            }
            base.Initialize();
        }

        private void TabAction(UserCommandArgs args)
        {
            if (args is ModifiableKeyCommandArgs keyCommandArgs && (keyCommandArgs.AdditionalModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
            {
                viewMode = viewMode.Next();
                userSettings.PopupSettings[ViewerWindowType.CarIdentifierOverlay] = viewMode.ToString();
                ChangeMode();
            }
        }

        private void ChangeMode()
        {
            switch (viewMode)
            {
                case ViewMode.Trains:
                    Simulator.Instance.Confirmer.Information(Catalog.GetString("Train labels visible."));
                    break;
                case ViewMode.Cars:
                    Simulator.Instance.Confirmer.Information(Catalog.GetString("Car labels visible."));
                    break;
            }
        }
    }
}
