using System;
using System.Collections.Generic;
using System.IO;
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
        Vector2 cMapPos;
        Vector2 hMapPos;
        Vector2 nMapPos;
        Vector2 pMapPos;
        Vector2 palettePos;
        Vector2 spritePos;
        string cMapPath, hMapPath, palettePath;
        FileSystemWatcher cWatcher, hWatcher, pWatcher;
        bool filesProvided = false;
        PrestoSprite sprite;

        const float BASE_SCALE = 4.0f;
        float scale = BASE_SCALE;
        int keyboardZoom = 0;
        KeyboardState prevKeyState = Keyboard.GetState();

        public Game1(string[] args) {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            bool cmap = false, hmap = false, palette = false;
            for (int i = 0; i < args.Length - 1; i++) {
                switch (args[i]) {
                    case "-c":
                        cmap = true;
                        cMapPath = args[++i];
                        System.Console.WriteLine("Using color map " + cMapPath);
                        break;
                    case "-h":
                        hmap = true;
                        hMapPath = args[++i];
                        System.Console.WriteLine("Using height map " + hMapPath);
                        break;
                    case "-p":
                        palette = true;
                        palettePath = args[++i];
                        System.Console.WriteLine("Using palette" + palettePath);
                        break;
                    default:
                        System.Console.WriteLine("[Warning] Unsupported argument: " + args[i]);
                        break;
                }
            }

            filesProvided = cmap && hmap && palette;
            if (!filesProvided && (cmap || hmap || palette)) {
                System.Console.WriteLine("[Warning] Not all required files were provided");
            }
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

            cMapPos = new Vector2(w * 0.15f, h * 0.25f);
            pMapPos = new Vector2(w * 0.15f, h * 0.75f);
            hMapPos = new Vector2(w * 0.85f, h * 0.25f);
            nMapPos = new Vector2(w * 0.85f, h * 0.75f);
            palettePos = new Vector2(w * 0.5f, h * 0.95f);
            spritePos = new Vector2(w * 0.5f, h * 0.5f);

            PrestoSprite.init(this);
            if (filesProvided) {
                sprite = new PrestoSprite(cMapPath, hMapPath, palettePath);
                cWatcher = new System.IO.FileSystemWatcher(Directory.GetCurrentDirectory(), cMapPath);
                hWatcher = new System.IO.FileSystemWatcher(Directory.GetCurrentDirectory(), hMapPath);
                pWatcher = new System.IO.FileSystemWatcher(Directory.GetCurrentDirectory(), palettePath);
                cWatcher.NotifyFilter = NotifyFilters.LastWrite;
                hWatcher.NotifyFilter = NotifyFilters.LastWrite;
                pWatcher.NotifyFilter = NotifyFilters.LastWrite;
                cWatcher.Changed += new FileSystemEventHandler(OnFileChange);
                hWatcher.Changed += new FileSystemEventHandler(OnFileChange);
                pWatcher.Changed += new FileSystemEventHandler(OnFileChange);
                cWatcher.EnableRaisingEvents = true;
                hWatcher.EnableRaisingEvents = true;
                pWatcher.EnableRaisingEvents = true;
            } else {
                sprite = new PrestoSprite();
            }
        }

        private void OnFileChange(object sender, FileSystemEventArgs e) {
            sprite = new PrestoSprite(cMapPath, hMapPath, palettePath);
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
                sprite.Shader = PrestoSprite.ShadingVersion.Ver1;
            }
            if (currKeyState.IsKeyDown(Keys.D2)) {
                sprite.Shader = PrestoSprite.ShadingVersion.Ver2;
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

            Vector2 cMapOrigin = new Vector2(sprite.CMap.Width / 2.0f, sprite.CMap.Height / 2.0f);
            spriteBatch.Draw(sprite.CMap, cMapPos, null, Color.White, 0.0f, cMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 pMapOrigin = new Vector2(sprite.PMap.Width / 2.0f, sprite.PMap.Height / 2.0f);
            spriteBatch.Draw(sprite.PMap, pMapPos, null, Color.White, 0.0f, pMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 hMapOrigin = new Vector2(sprite.HMap.Width / 2.0f, sprite.HMap.Height / 2.0f);
            spriteBatch.Draw(sprite.HMap, hMapPos, null, Color.White, 0.0f, hMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 nMapOrigin = new Vector2(sprite.NMap.Width / 2.0f, sprite.NMap.Height / 2.0f);
            spriteBatch.Draw(sprite.NMap, nMapPos, null, Color.White, 0.0f, nMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 paletteOrigin = new Vector2(sprite.Palette.Width / 2.0f, sprite.Palette.Height / 2.0f);
            spriteBatch.Draw(sprite.Palette, palettePos, null, Color.White, 0.0f, paletteOrigin, 3.0f * scale, SpriteEffects.None, 0.0f);

            Vector3 lightDir = new Vector3(Mouse.GetState().X - spritePos.X, Mouse.GetState().Y - spritePos.Y, 128.0f);
            lightDir.Normalize();
            spriteBatch.DrawString(font, "Light: " + lightDir, Vector2.Zero, Color.Black);

            Vector2 spriteOrigin = new Vector2(sprite.CMap.Width / 2.0f, sprite.CMap.Height / 2.0f);

            sprite.draw(spriteBatch, spritePos, spriteOrigin, scale, lightDir);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
