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
        Texture2D paletteTexture;
        Vector2 cMapPos;
        Vector2 hMapPos;
        Vector2 palettePos;
        Vector2 spritePos;
        Effect[] effect = new Effect[2];

        const float BASE_SCALE = 4.0f;
        float scale = BASE_SCALE;
        int keyboardZoom = 0;
        KeyboardState prevKeyState = Keyboard.GetState();

        enum ShadingVersion { Ver1, Ver2 };
        ShadingVersion version = ShadingVersion.Ver1;

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
            cMapPos = new Vector2(w * 0.15f, h * 0.5f);

            hMapTexture = Content.Load<Texture2D>("hmap");
            hMapPos = new Vector2(w * 0.85f, h * 0.5f);

            paletteTexture = Content.Load<Texture2D>("palette");
            palettePos = new Vector2(w * 0.5f, h * 0.95f);

            spritePos = new Vector2(w * 0.5f, h * 0.5f);

            effect[0] = Content.Load<Effect>("Effect1");
            //effect[1] = Content.Load<Effect>("Effect2");
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

            Vector2 hMapOrigin = new Vector2(hMapTexture.Width / 2.0f, hMapTexture.Height / 2.0f);
            spriteBatch.Draw(hMapTexture, hMapPos, null, Color.White, 0.0f, hMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 paletteOrigin = new Vector2(paletteTexture.Width / 2.0f, paletteTexture.Height / 2.0f);
            spriteBatch.Draw(paletteTexture, palettePos, null, Color.White, 0.0f, paletteOrigin, 3.0f * scale, SpriteEffects.None, 0.0f);

            Vector3 lightDir = new Vector3(Mouse.GetState().X - spritePos.X, Mouse.GetState().Y - spritePos.Y, 64.0f);
            lightDir.Normalize();
            spriteBatch.DrawString(font, "Light: " + lightDir, Vector2.Zero, Color.Black);

            Effect currentEffect = effect[(int) version];
            switch (version) {
                case ShadingVersion.Ver1:
                    break;
                case ShadingVersion.Ver2:
                    break;
            }
            currentEffect.Parameters["hMap"].SetValue(hMapTexture);
            currentEffect.Parameters["palette"].SetValue(paletteTexture);
            currentEffect.Parameters["lightDir"].SetValue(lightDir);
            currentEffect.Parameters["texWidth"].SetValue(cMapTexture.Width);
            currentEffect.Parameters["texHeight"].SetValue(cMapTexture.Height);
            currentEffect.CurrentTechnique.Passes[0].Apply();

            Vector2 spriteOrigin = new Vector2(cMapTexture.Width / 2.0f, cMapTexture.Height / 2.0f);
            spriteBatch.Draw(cMapTexture, spritePos, null, Color.White, 0.0f, spriteOrigin, scale, SpriteEffects.None, 0.0f);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
