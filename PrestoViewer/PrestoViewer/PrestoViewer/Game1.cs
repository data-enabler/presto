using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace PrestoViewer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        Texture2D cMapTexture;
        Texture2D hMapTexture;
        // Colors are replaced with index into palette. R = y, G = size of color range
        Texture2D pMapTexture;
        Texture2D paletteTexture;
        // Leftmost pixels removed, size adjusted to power of 2
        Texture2D adjustedPaletteTexture;
        Vector2 cMapPos;
        Vector2 hMapPos;
        Vector2 nMapPos;
        Vector2 pMapPos;
        Vector2 palettePos;
        Vector2 spritePos;
        Effect[] effect = new Effect[2];
        Texture2D effect2NMapTexture;
        delegate int del(int pix, int imgWidth, int imgHeight);

        const float BASE_SCALE = 4.0f;
        float scale = BASE_SCALE;
        int keyboardZoom = 0;
        KeyboardState prevKeyState = Keyboard.GetState();

        enum ShadingVersion { Ver1, Ver2 };
        ShadingVersion version = ShadingVersion.Ver2;

        public Game1() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            this.IsMouseVisible = true;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            int w = graphics.GraphicsDevice.Viewport.Width;
            int h = graphics.GraphicsDevice.Viewport.Height;

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("SpriteFont1");

            cMapTexture = Content.Load<Texture2D>("cmap");
            cMapPos = new Vector2(w * 0.15f, h * 0.25f);
            pMapPos = new Vector2(w * 0.15f, h * 0.75f);

            hMapTexture = Content.Load<Texture2D>("hmap");
            hMapPos = new Vector2(w * 0.85f, h * 0.25f);
            nMapPos = new Vector2(w * 0.85f, h * 0.75f);

            paletteTexture = Content.Load<Texture2D>("palette");
            palettePos = new Vector2(w * 0.5f, h * 0.95f);

            spritePos = new Vector2(w * 0.5f, h * 0.5f);

            effect[0] = Content.Load<Effect>("Effect1");
            effect[1] = Content.Load<Effect>("Effect2");

            // Normal map for Effect 2
            int width = hMapTexture.Width;
            int height = hMapTexture.Height;
            effect2NMapTexture = new Texture2D(graphics.GraphicsDevice, width, height);
            Color[] effect2Normals = new Color[width * height];
            Color[] hMapColors = new Color[width * height];
            hMapTexture.GetData<Color>(hMapColors);
            int[] offsets = { -1, 1, -width, width }; // left, right, down, up
            del[] toEdge = {
                (pix, imgWidth, imgHeight) => pix % imgWidth,
                (pix, imgWidth, imgHeight) => imgWidth - 1 - (pix % imgWidth),
                (pix, imgWidth, imgHeight) => pix / imgWidth,
                (pix, imgWidth, imgHeight) => imgHeight - 1 - (pix / imgWidth)
            };
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int origPix = y * width + x;
                    Color orig = hMapColors[origPix];
                    int[] dists = new int[4]; // left, right, down, up
                    float[] diffs = new float[4]; // left, right, down, up
                    int k;

                    for (int dir = 0; dir < 4; dir++) {
                        int pix = origPix;
                        for (k = 0; k < toEdge[dir](pix, width, height); k++) {
                            pix += offsets[dir];
                            Color c = hMapColors[pix];
                            if (!orig.Equals(c)) {
                                if (c.A > 0) {
                                    diffs[dir] = (orig.R - c.R) / 255.0f;
                                }
                                break;
                            }
                        }
                        dists[dir] = k + 1;
                    }

                    Vector3 n = new Vector3(diffs[1] / dists[1] - diffs[0] / dists[0], diffs[3] / dists[3] - diffs[2] / dists[2], 0.2f);
                    n.Normalize();
                    effect2Normals[y * width + x].R = (byte) Math.Round(n.X * 127 + 127);
                    effect2Normals[y * width + x].G = (byte) Math.Round(n.Y * 127 + 127);
                    effect2Normals[y * width + x].B = (byte) Math.Round(n.Z * 127 + 127);
                    effect2Normals[y * width + x].A = 255;
                }
            }
            effect2NMapTexture.SetData<Color>(effect2Normals);

            // Pallete Map
            int cw = cMapTexture.Width;
            int ch = cMapTexture.Height;
            int pw = paletteTexture.Width;
            int ph = paletteTexture.Height;
            pMapTexture = new Texture2D(graphics.GraphicsDevice, cw, ch);
            int dim = 1 << (int) Math.Ceiling(Math.Log(Math.Max(pw, ph), 2.0));
            adjustedPaletteTexture = new Texture2D(graphics.GraphicsDevice, dim, dim);
            Color[] cMapColors = new Color[cw * ch];
            Color[] pMapColors = new Color[cw * ch];
            Color[] paletteColors = new Color[pw * ph];
            Color[] apColors = new Color[dim * dim];
            cMapTexture.GetData<Color>(cMapColors);
            paletteTexture.GetData<Color>(paletteColors);
            Dictionary<Color, Color> colorToPMapIndex = new Dictionary<Color, Color>();

            for (int y = 0; y < ph; y++) {
                int x;
                for (x = 1; x < pw; x++) {
                    Color c = paletteColors[y * pw + x];
                    if (c.A == 0) {
                        break;
                    }
                    apColors[y * dim + x - 1] = c;
                }
                colorToPMapIndex[paletteColors[y * pw]] = new Color((y + 0.5f) / dim, (x - 1.0f) / dim, 0.0f);
            }

            for (int y = 0; y < ch; y++) {
                for (int x = 0; x < cw; x++) {
                    int pix = y * cw + x;
                    if (colorToPMapIndex.ContainsKey(cMapColors[pix])) {
                        pMapColors[pix] = colorToPMapIndex[cMapColors[pix]];
                    }
                }
            }

            pMapTexture.SetData<Color>(pMapColors);
            adjustedPaletteTexture.SetData<Color>(apColors);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) {
                this.Exit();
            }

            KeyboardState currKeyState = Keyboard.GetState();
            if (currKeyState.IsKeyDown(Keys.D1)) {
                version = ShadingVersion.Ver1;
            }
            if (currKeyState.IsKeyDown(Keys.D2)) {
                version = ShadingVersion.Ver2;
            }
            if (currKeyState.IsKeyDown(Keys.Up) && !prevKeyState.IsKeyDown(Keys.Up)) {
                keyboardZoom--;
            }
            if (currKeyState.IsKeyDown(Keys.Down) && !prevKeyState.IsKeyDown(Keys.Down)) {
                keyboardZoom++;
            }
            prevKeyState = currKeyState;

            int zoomValue = Mouse.GetState().ScrollWheelValue / -120;

            scale = BASE_SCALE / (float) Math.Pow(2.0, zoomValue + keyboardZoom);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.AntiqueWhite);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

            Vector2 cMapOrigin = new Vector2(cMapTexture.Width / 2.0f, cMapTexture.Height / 2.0f);
            spriteBatch.Draw(cMapTexture, cMapPos, null, Color.White, 0.0f, cMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 pMapOrigin = new Vector2(pMapTexture.Width / 2.0f, pMapTexture.Height / 2.0f);
            spriteBatch.Draw(pMapTexture, pMapPos, null, Color.White, 0.0f, pMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 hMapOrigin = new Vector2(hMapTexture.Width / 2.0f, hMapTexture.Height / 2.0f);
            spriteBatch.Draw(hMapTexture, hMapPos, null, Color.White, 0.0f, hMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 nMapOrigin = new Vector2(effect2NMapTexture.Width / 2.0f, effect2NMapTexture.Height / 2.0f);
            spriteBatch.Draw(effect2NMapTexture, nMapPos, null, Color.White, 0.0f, nMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 paletteOrigin = new Vector2(paletteTexture.Width / 2.0f, paletteTexture.Height / 2.0f);
            spriteBatch.Draw(paletteTexture, palettePos, null, Color.White, 0.0f, paletteOrigin, 3.0f * scale, SpriteEffects.None, 0.0f);

            Vector3 lightDir = new Vector3(Mouse.GetState().X - spritePos.X, Mouse.GetState().Y - spritePos.Y, 64.0f);
            lightDir.Normalize();
            spriteBatch.DrawString(font, "Light: " + lightDir, Vector2.Zero, Color.Black);

            Vector2 spriteOrigin = new Vector2(cMapTexture.Width / 2.0f, cMapTexture.Height / 2.0f);

            Effect currentEffect = effect[(int) version];
            currentEffect.Parameters["lightDir"].SetValue(lightDir);
            switch (version) {
                case ShadingVersion.Ver1:
                    currentEffect.Parameters["palette"].SetValue(paletteTexture);
                    currentEffect.Parameters["hMap"].SetValue(hMapTexture);
                    currentEffect.Parameters["texWidth"].SetValue(cMapTexture.Width);
                    currentEffect.Parameters["texHeight"].SetValue(cMapTexture.Height);
                    currentEffect.CurrentTechnique.Passes[0].Apply();
                    spriteBatch.Draw(cMapTexture, spritePos, null, Color.White, 0.0f, spriteOrigin, scale, SpriteEffects.None, 0.0f);
                    break;
                case ShadingVersion.Ver2:
                    currentEffect.Parameters["palette"].SetValue(adjustedPaletteTexture);
                    currentEffect.Parameters["nMap"].SetValue(effect2NMapTexture);
                    currentEffect.CurrentTechnique.Passes[0].Apply();
                    spriteBatch.Draw(pMapTexture, spritePos, null, Color.White, 0.0f, spriteOrigin, scale, SpriteEffects.None, 0.0f);
                    break;
            }


            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
