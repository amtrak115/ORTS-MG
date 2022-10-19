﻿using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orts.Graphics.Xna
{
    public class TextTextureResourceHolder : ResourceGameComponent<Texture2D>
    {
        private readonly TextTextureRenderer textRenderer;

        public Texture2D EmptyTexture { get; }

        private TextTextureResourceHolder(Game game, int sweepInterval) : base(game)
        {
            SweepInterval = sweepInterval;
            EmptyTexture = new Texture2D(game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            textRenderer = TextTextureRenderer.Instance(game) ?? throw new InvalidOperationException("TextTextureRenderer not found");
        }

        public static TextTextureResourceHolder Instance(Game game)
        {
            if (null == game)
                throw new ArgumentNullException(nameof(game));

            TextTextureResourceHolder instance;
            if ((instance = game.Components.OfType<TextTextureResourceHolder>().FirstOrDefault()) == null)
            {
                instance = new TextTextureResourceHolder(game, 30);
            }
            return instance;
        }

        public Texture2D PrepareResource(string text, System.Drawing.Font font)
        {
            int identifier = HashCode.Combine(text, font);
            if (!currentResources.TryGetValue(identifier, out Texture2D texture))
            {
                if (previousResources.TryRemove(identifier, out texture))
                {
                    if (!currentResources.TryAdd(identifier, texture))
                        Trace.TraceInformation($"Texture Resource '{text}' already added.");
                }
                else
                {
                    texture = textRenderer.Resize(text, font);
                    textRenderer.RenderText(text, font, texture);
                    if (!currentResources.TryAdd(identifier, texture))
                    {
                        texture.Dispose();
                        if (!currentResources.TryGetValue(identifier, out texture))
                            Trace.TraceError($"Texture Resource '{text}' not found. Retrying.");
                    }
                }
            }
            return texture;
        }
    }
}
