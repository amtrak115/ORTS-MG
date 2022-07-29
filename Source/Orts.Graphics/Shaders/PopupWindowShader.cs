﻿
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orts.Graphics.Shaders
{
    public class PopupWindowShader : EffectShader
    {
#pragma warning disable CA1044 // Properties should not be write only
        public Color GlassColor
        {
            set => Parameters["GlassColor"].SetValue(new Vector3(value.R / 255f, value.G / 255f, value.B / 255f));
        }

        public float Opacity
        {
            get => Parameters["Opacity"].GetValueSingle();
            set => Parameters["Opacity"].SetValue(value);
        }

        public Texture2D WindowTexture
        {
            set => Parameters["WindowTexture"].SetValue(value);
        }
#pragma warning restore CA1044 // Properties should not be write only

        public void SetMatrix(in Matrix w, ref Matrix wvp)
        {
            World = w;
            WorldViewProjection = wvp;
        }

        public PopupWindowShader(GraphicsDevice graphicsDevice): 
            base(graphicsDevice, "PopupWindow")
        {
            CurrentTechnique = Techniques["PopupWindow"];
        }

        public override void SetState(EffectShader previousShader)
        {
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            base.SetState(previousShader);
        }

        public override void ResetState()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            base.ResetState();
        }
    }
}
