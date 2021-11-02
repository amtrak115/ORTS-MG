﻿using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orts.View.Xna
{
    /// <summary>
    /// Renders text string to a Texture2D
    /// The texture is kept and reused when updating the text
    /// This class should be used where text updates infrequently
    /// </summary>
    public abstract class DrawableTextComponent : TextureContentComponent
    {
        private protected Font font;

        private protected readonly Brush whiteBrush = new SolidBrush(System.Drawing.Color.White);

        protected DrawableTextComponent(Game game, Font font, Microsoft.Xna.Framework.Color color, Vector2 position) :
            base(game, color, position)
        {
            this.font = font;
        }

        protected virtual void InitializeSize(string text)
        {
            using (Bitmap measureBitmap = new Bitmap(1, 1))
            {
                using (Graphics measureGraphics = Graphics.FromImage(measureBitmap))
                {
                    Resize(measureGraphics.MeasureString(text, font).ToSize());
                }
            }
        }

        protected virtual void Resize(Size size)
        {
            Texture2D current = texture;
            texture = new Texture2D(Game.GraphicsDevice, size.Width, size.Height, false, SurfaceFormat.Bgra32);
            current?.Dispose();
        }

        protected virtual void DrawString(string text)
        {
            // Create the final bitmap
            using (Bitmap bmpSurface = new Bitmap(texture.Width, texture.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpSurface))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

                    // Draw the text to the clean bitmap
                    g.Clear(System.Drawing.Color.Transparent);
                    g.DrawString(text, font, whiteBrush, PointF.Empty);

                    BitmapData bmd = bmpSurface.LockBits(new System.Drawing.Rectangle(0, 0, bmpSurface.Width, bmpSurface.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                whiteBrush?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Renders text string to a Texture2D
    /// The texture and graphics resources are kept and reused when updating the text
    /// This class should be used where text updates very frequently
    /// </summary>
    public abstract class QuickRepeatableDrawableTextComponent : DrawableTextComponent
    {
        // Create the final bitmap
        private protected Bitmap bmpSurface;
        private protected Graphics g;

        protected QuickRepeatableDrawableTextComponent(Game game, Font font, Microsoft.Xna.Framework.Color color, Vector2 position) :
            base(game, font, color, position)
        {

        }

        protected override void Resize(Size size)
        {
            base.Resize(size);
            Bitmap currentSurface = bmpSurface;
            Graphics currentGraphics = g;
            bmpSurface = new Bitmap(texture.Width, texture.Height);
            g = Graphics.FromImage(bmpSurface);
            currentGraphics?.Dispose();
            currentSurface?.Dispose();
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        }

        protected override void DrawString(string text)
        {
            // Draw the text to the clean bitmap
            g.Clear(System.Drawing.Color.Transparent);
            g.DrawString(text, font, whiteBrush, PointF.Empty);

            BitmapData bmd = bmpSurface.LockBits(new System.Drawing.Rectangle(0, 0, bmpSurface.Width, bmpSurface.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                g?.Dispose();
                bmpSurface?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
