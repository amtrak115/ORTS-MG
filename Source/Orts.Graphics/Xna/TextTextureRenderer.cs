﻿using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework.Graphics;

namespace Orts.Graphics.Xna
{
    public class OutlineRenderOptions : IDisposable
    {
        public static OutlineRenderOptions Default { get; } = new OutlineRenderOptions(2.0f, Color.Black, Color.White);
        public static OutlineRenderOptions DefaultTransparent { get; } = new OutlineRenderOptions(1.0f, Color.Black, Color.Transparent);

        internal readonly Pen Pen;
        internal readonly Brush FillBrush;

        private bool disposedValue;

        public float OutlineWidth => Pen.Width;
        public Color OutlineColor => Pen.Color;

        public OutlineRenderOptions(float width, Color outlineColor, Color fillColor)
        {
            Pen = new Pen(outlineColor, width)
            {
                LineJoin = LineJoin.Round
            };
            FillBrush = new SolidBrush(fillColor);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Pen?.Dispose();
                    FillBrush?.Dispose();
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
    public class TextTextureRenderer : IDisposable
    {
        private readonly Texture2D emptyTexture;
        private readonly Bitmap measureBitmap;
        private readonly Microsoft.Xna.Framework.Game game;
        private readonly ConcurrentQueue<(System.Drawing.Graphics, StringFormat)> measureGraphicsHolder = new ConcurrentQueue<(System.Drawing.Graphics, StringFormat)>();
        private readonly ConcurrentQueue<Brush> whiteBrushHolder = new ConcurrentQueue<Brush>();
        private bool disposedValue;

        private TextTextureRenderer(Microsoft.Xna.Framework.Game game)
        {
            this.game = game;
            emptyTexture = new Texture2D(game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            measureBitmap = new Bitmap(1, 1);
        }

        public static TextTextureRenderer Instance(Microsoft.Xna.Framework.Game game)
        {
            if (null == game)
                throw new ArgumentNullException(nameof(game));

            TextTextureRenderer instance;
            if ((instance = game.Services.GetService<TextTextureRenderer>()) == null)
            {
                instance = new TextTextureRenderer(game);
                game.Services.AddService(instance);
            }
            return instance;
        }

        public Size Measure(string text, Font font, OutlineRenderOptions outlineOptions = null)
        {
            if (!measureGraphicsHolder.TryDequeue(out (System.Drawing.Graphics measureGraphics, StringFormat formatHolder) measureContainer))
            {
                measureContainer.measureGraphics = System.Drawing.Graphics.FromImage(measureBitmap);
                measureContainer.measureGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                measureContainer.formatHolder = new StringFormat(StringFormat.GenericDefault);
            }

            Size size = Size.Empty;

            if (!string.IsNullOrEmpty(text) && font != null)
            {
                measureContainer.formatHolder.SetMeasurableCharacterRanges(new CharacterRange[] { new CharacterRange(0, text.Length) });
                Region[] ranges = measureContainer.measureGraphics.MeasureCharacterRanges(text, font, new RectangleF(0, 0, text.Length * font.Height, text.Length * font.Height), measureContainer.formatHolder);
                SizeF actual = ranges[0].GetBounds(measureContainer.measureGraphics).Size;
                int padding = (int)Math.Ceiling(font.Size * 0.2);
                if (outlineOptions != null && outlineOptions.OutlineWidth > 1)
                    padding = (int)(padding * outlineOptions.OutlineWidth);
                size = new Size((int)Math.Ceiling(actual.Width + 2 * padding), (int)Math.Ceiling(actual.Height + padding / 2));
            }
            measureGraphicsHolder.Enqueue(measureContainer);
            return size;
        }

        public Texture2D Resize(string text, Font font, OutlineRenderOptions outlineOptions = null)
        {
            Size size = Measure(text, font, outlineOptions);
            return (size.Width == 0 || size.Height == 0) ? emptyTexture : new Texture2D(game.GraphicsDevice, size.Width, size.Height, false, SurfaceFormat.Color);
        }

        public void RenderText(string text, Font font, Texture2D texture, OutlineRenderOptions outlineOptions = null)
        {
            if (null == texture)
                throw new ArgumentNullException(nameof(texture));
            if (texture == emptyTexture || (texture.Width == 1 && texture.Height == 1))
                return;
            if (null == font)
                throw new ArgumentNullException(nameof(font));

            // Create the final bitmap
            using (Bitmap bmpSurface = new Bitmap(texture.Width, texture.Height))
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmpSurface))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.High;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                    // Draw the text to the clean bitmap
                    graphics.Clear(Color.Transparent);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    if (!whiteBrushHolder.TryDequeue(out Brush whiteBrush))
                        whiteBrush = new SolidBrush(Color.White);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    if (outlineOptions != null)
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            path.AddString(text, font.FontFamily, (int)font.Style, graphics.DpiY * font.SizeInPoints / 72, Point.Empty, null);
                            graphics.DrawPath(outlineOptions.Pen, path);
                            graphics.FillPath(outlineOptions.FillBrush, path);
                        }
                    }
                    else
                    {
                        graphics.DrawString(text, font, whiteBrush, Point.Empty);
                    }
                    whiteBrushHolder.Enqueue(whiteBrush);
                    BitmapData bmd = bmpSurface.LockBits(new Rectangle(0, 0, bmpSurface.Width, bmpSurface.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    int bufferSize = bmd.Height * bmd.Stride;
                    //create data buffer 
                    byte[] bytes = new byte[bufferSize];
                    // copy bitmap data into buffer
                    Marshal.Copy(bmd.Scan0, bytes, 0, bytes.Length);

                    // copy our buffer to the texture
                    texture.SetData(bytes);
                    // unlock the bitmap data
                    bmpSurface.UnlockBits(bmd);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    while (measureGraphicsHolder.TryDequeue(out (System.Drawing.Graphics measureGraphics, StringFormat formatHolder) measureContainer))
                    {
                        measureContainer.measureGraphics?.Dispose();
                        measureContainer.formatHolder?.Dispose();
                    }
                    while (whiteBrushHolder.TryDequeue(out Brush brush))
                        brush?.Dispose();

                    emptyTexture?.Dispose();
                    measureBitmap?.Dispose();
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
