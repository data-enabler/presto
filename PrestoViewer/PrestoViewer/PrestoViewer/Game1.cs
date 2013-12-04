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

        Texture2D cMapTexture, hMapTexture, paletteTexture;
        PrestoSprite sprite;

        // File loading and watching
        string cMapPath, hMapPath, palettePath;
        FileSystemWatcher cWatcher, hWatcher, pWatcher;
        FileSystemEventHandler cHandler, hHandler, pHandler;
        bool filesProvided = false;

        // Drag and Drop 
        System.Windows.Forms.Form form;
        Rectangle cMapDrop, hMapDrop, paletteDrop;
        const int DROP_BOX_SIZE = 150;

        // Tooltips
        string tooltip = "";
        Rectangle pMapHover, nMapHover;
        const string CMAP_TEXT = "Base Color Map";
        const string HMAP_TEXT = "Height Map";
        const string PALETTE_TEXT = "Color Palette";
        const string PMAP_TEXT = "Palette Map\n(For Internal Use)";
        const string NMAP_TEXT = "Normal Map\n(For Internal Use)";
        bool viewHelp = false;
        const string HELP_TEXT = "To replace the Color Map, Height Map, or Color Palette,\n" +
            "simply drag and drop files into the viewer.\n\n" +
            "Controls:\n" +
            "Mouse Scroll or Arrow Up/Down: Zoom In/Out\n" +
            "Right-Click: Lock/Unlock Light Direction\n" +
            "Space: Toggle Background Color\n" +
            "?: Toggle Help";

        // Input
        KeyboardState prevKeyState = Keyboard.GetState();
        MouseState prevMouseState = Mouse.GetState();

        // Zoom
        const float BASE_SCALE = 4.0f;
        float scale = BASE_SCALE;
        int keyboardZoom = 0;

        // Light Dir
        Vector3 lightDir = Vector3.UnitZ;
        bool lightLock = false;

        // Colors
        Color[] colors = { Color.AntiqueWhite, new Color(0.2f, 0.2f, 0.2f) };
        bool invertColor = false;

        // Misc
        Texture2D WHITE_BOX;

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
                        System.Console.WriteLine("Using palette " + palettePath);
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

            cHandler = new FileSystemEventHandler(OnCMapChange);
            hHandler = new FileSystemEventHandler(OnHMapChange);
            pHandler = new FileSystemEventHandler(OnPaletteChange);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            this.IsMouseVisible = true;

            int w = graphics.GraphicsDevice.Viewport.Width;
            int h = graphics.GraphicsDevice.Viewport.Height;

            cMapPos = new Vector2(w * 0.15f, h * 0.25f);
            pMapPos = new Vector2(w * 0.15f, h * 0.75f);
            hMapPos = new Vector2(w * 0.85f, h * 0.25f);
            nMapPos = new Vector2(w * 0.85f, h * 0.75f);
            palettePos = new Vector2(w * 0.5f, h * 0.95f);
            spritePos = new Vector2(w * 0.5f, h * 0.5f);

            int hw = DROP_BOX_SIZE / 2;
            cMapDrop = new Rectangle((int)cMapPos.X - hw, (int)cMapPos.Y - hw, DROP_BOX_SIZE, DROP_BOX_SIZE);
            hMapDrop = new Rectangle((int)hMapPos.X - hw, (int)hMapPos.Y - hw, DROP_BOX_SIZE, DROP_BOX_SIZE);
            paletteDrop = new Rectangle((int)palettePos.X - hw, (int)palettePos.Y - hw, DROP_BOX_SIZE, DROP_BOX_SIZE);
            pMapHover = new Rectangle((int)pMapPos.X - hw, (int)pMapPos.Y - hw, DROP_BOX_SIZE, DROP_BOX_SIZE);
            nMapHover = new Rectangle((int)nMapPos.X - hw, (int)nMapPos.Y - hw, DROP_BOX_SIZE, DROP_BOX_SIZE);

            form = System.Windows.Forms.Form.FromHandle(Window.Handle) as System.Windows.Forms.Form;
            if (form != null) {
                form.AllowDrop = true;
                form.DragOver += new System.Windows.Forms.DragEventHandler(OnFileDragOver);
                form.DragDrop += new System.Windows.Forms.DragEventHandler(OnFileDragDrop);
            }

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("SpriteFont1");

            WHITE_BOX = new Texture2D(GraphicsDevice, 1, 1);
            WHITE_BOX.SetData(new[] { Color.White });

            PrestoSprite.init(this);
            if (filesProvided) {
                // Load textures
                cMapTexture = LoadTextureFromFile(cMapPath);
                hMapTexture = LoadTextureFromFile(hMapPath);
                paletteTexture = LoadTextureFromFile(palettePath);

                // Add Watcher on files
                cWatcher = WatchFile(cMapPath, cHandler);
                hWatcher = WatchFile(hMapPath, hHandler);
                pWatcher = WatchFile(palettePath, pHandler);
            } else {
                cMapTexture = Content.Load<Texture2D>("cmap");
                hMapTexture = Content.Load<Texture2D>("hmap");
                paletteTexture = Content.Load<Texture2D>("palette");
            }
            ReloadSprite();
        }

        private void ReloadSprite() {
            sprite = new PrestoSprite(cMapTexture, hMapTexture, paletteTexture);
        }

        private Texture2D LoadTextureFromFile(string path) {
            Texture2D tex;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                tex = Texture2D.FromStream(GraphicsDevice, stream);
            }
            return tex;
        }

        private static FileSystemWatcher WatchFile(string path, FileSystemEventHandler handler) {
            path = Path.GetFullPath(path);
            FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += handler;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private void OnCMapChange(object sender, FileSystemEventArgs e) {
            Console.WriteLine("CMap change detected");
            try {
                cMapTexture = LoadTextureFromFile(cMapPath);
                Console.WriteLine("CMap updated");
                ReloadSprite();
            } catch (System.IO.IOException) {
                Console.WriteLine("Error loading file {0}", cMapPath);
            }
        }

        private void OnHMapChange(object sender, FileSystemEventArgs e) {
            Console.WriteLine("HMap change detected");
            try {
                hMapTexture = LoadTextureFromFile(hMapPath);
                Console.WriteLine("HMap updated");
                ReloadSprite();
            } catch (System.IO.IOException) {
                Console.WriteLine("Error loading file {0}", hMapPath);
            }
        }

        private void OnPaletteChange(object sender, FileSystemEventArgs e) {
            Console.WriteLine("Palette change detected");
            try {
                paletteTexture = LoadTextureFromFile(palettePath);
                Console.WriteLine("Palette updated");
                ReloadSprite();
            } catch (System.IO.IOException) {
                Console.WriteLine("Error loading file {0}", palettePath);
            }
        }

        private void OnFileDragOver(object sender, System.Windows.Forms.DragEventArgs e) {
            System.Drawing.Point p = form.PointToClient(new System.Drawing.Point(e.X, e.Y));
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                if (cMapDrop.Contains(p.X, p.Y)) {
                    e.Effect = System.Windows.Forms.DragDropEffects.Link;
                } else if (hMapDrop.Contains(p.X, p.Y)) {
                    e.Effect = System.Windows.Forms.DragDropEffects.Link;
                } else if (paletteDrop.Contains(p.X, p.Y)) {
                    e.Effect = System.Windows.Forms.DragDropEffects.Link;
                } else {
                    e.Effect = System.Windows.Forms.DragDropEffects.None;
                }
            } else {
                e.Effect = System.Windows.Forms.DragDropEffects.None;
            }
        }

        private void OnFileDragDrop(object sender, System.Windows.Forms.DragEventArgs e) {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                if (files != null) {
                    // Ignore files other than the first
                    System.Drawing.Point p = form.PointToClient(new System.Drawing.Point(e.X, e.Y));
                    if (cMapDrop.Contains(p.X, p.Y)) {
                        if (cWatcher != null) {
                            cWatcher.EnableRaisingEvents = false;
                            cWatcher.Dispose(); 
                        }
                        cMapPath = files[0];
                        try {
                            Console.WriteLine("Setting CMap: {0}", files[0]);
                            cMapTexture = LoadTextureFromFile(files[0]);
                            ReloadSprite();
                        } catch (System.IO.IOException) {
                            Console.WriteLine("Error loading file {0}", files[0]);
                        }
                        cWatcher = WatchFile(files[0], cHandler);
                    } else if (hMapDrop.Contains(p.X, p.Y)) {
                        if (hWatcher != null) {
                            hWatcher.EnableRaisingEvents = false;
                            hWatcher.Dispose();
                        }
                        hMapPath = files[0];
                        try {
                            Console.WriteLine("Setting HMap: {0}", files[0]);
                            hMapTexture = LoadTextureFromFile(files[0]);
                            ReloadSprite();
                        } catch (System.IO.IOException) {
                            Console.WriteLine("Error loading file {0}", files[0]);
                        }
                        hWatcher = WatchFile(files[0], hHandler);
                    } else if (paletteDrop.Contains(p.X, p.Y)) {
                        if (pWatcher != null) {
                            pWatcher.EnableRaisingEvents = false;
                            pWatcher.Dispose();
                        }
                        palettePath = files[0];
                        try {
                            Console.WriteLine("Setting Palette: {0}", files[0]);
                            paletteTexture = LoadTextureFromFile(files[0]);
                            ReloadSprite();
                        } catch (System.IO.IOException) {
                            Console.WriteLine("Error loading file {0}", files[0]);
                        }
                        pWatcher = WatchFile(files[0], pHandler);
                    }
                }
            }
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
            KeyboardState currKeyState = Keyboard.GetState();
            MouseState currMouseState = Mouse.GetState();
            int x = currMouseState.X;
            int y = currMouseState.Y;

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) {
                this.Exit();
            }

            // Change Shader
            //if (currKeyState.IsKeyDown(Keys.D1)) {
            //    sprite.Shader = PrestoSprite.ShadingVersion.Ver1;
            //}
            //if (currKeyState.IsKeyDown(Keys.D2)) {
            //    sprite.Shader = PrestoSprite.ShadingVersion.Ver2;
            //}

            // Zoom
            if (currKeyState.IsKeyDown(Keys.Up) && !prevKeyState.IsKeyDown(Keys.Up)) {
                keyboardZoom--;
            }
            if (currKeyState.IsKeyDown(Keys.Down) && !prevKeyState.IsKeyDown(Keys.Down)) {
                keyboardZoom++;
            }
            int zoomValue = currMouseState.ScrollWheelValue / -120;
            scale = BASE_SCALE / (float) Math.Pow(2.0, zoomValue + keyboardZoom);

            // Tooltips
            if (cMapDrop.Contains(x, y)) {
                tooltip = CMAP_TEXT;
            } else if (hMapDrop.Contains(x, y)) {
                tooltip = HMAP_TEXT;
            } else if (paletteDrop.Contains(x, y)) {
                tooltip = PALETTE_TEXT;
            } else if (pMapHover.Contains(x, y)) {
                tooltip = PMAP_TEXT;
            } else if (nMapHover.Contains(x, y)) {
                tooltip = NMAP_TEXT;
            } else {
                tooltip = "";
            }

            // Light Lock
            if (currMouseState.RightButton == ButtonState.Pressed &&
                !(prevMouseState.RightButton == ButtonState.Pressed)) {
                lightLock = !lightLock;
            }

            // Update Light Dir
            if (!lightLock) {
                lightDir = new Vector3(currMouseState.X - spritePos.X, currMouseState.Y - spritePos.Y, 128.0f);
                lightDir.Normalize();
            }

            // Update Background Color
            if (currKeyState.IsKeyDown(Keys.Space) && !prevKeyState.IsKeyDown(Keys.Space)) {
                invertColor = !invertColor;
            }

            // Toggle Help
            if (currKeyState.IsKeyDown(Keys.OemQuestion) && !prevKeyState.IsKeyDown(Keys.OemQuestion)) {
                viewHelp = !viewHelp;
            }

            prevKeyState = currKeyState;
            prevMouseState = currMouseState;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;
            MouseState ms = Mouse.GetState();

            GraphicsDevice.Clear(colors[invertColor ? 1 : 0]);
            Color textColor = colors[invertColor ? 0 : 1];

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

            Vector2 cMapOrigin = new Vector2(sprite.CMap.Width / 2.0f, sprite.CMap.Height / 2.0f);
            spriteBatch.Draw(sprite.CMap, cMapPos, null, Color.White, 0.0f, cMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 pMapOrigin = new Vector2(sprite.CMap.Width / 2.0f, sprite.CMap.Height / 2.0f);
            spriteBatch.Draw(sprite.PMap, pMapPos, null, Color.White, 0.0f, pMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 hMapOrigin = new Vector2(sprite.HMap.Width / 2.0f, sprite.HMap.Height / 2.0f);
            spriteBatch.Draw(sprite.HMap, hMapPos, null, Color.White, 0.0f, hMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 nMapOrigin = new Vector2(sprite.HMap.Width / 2.0f, sprite.HMap.Height / 2.0f);
            spriteBatch.Draw(sprite.NMap, nMapPos, null, Color.White, 0.0f, nMapOrigin, scale, SpriteEffects.None, 0.0f);

            Vector2 paletteOrigin = new Vector2(sprite.Palette.Width / 2.0f, sprite.Palette.Height / 2.0f);
            spriteBatch.Draw(sprite.Palette, palettePos, null, Color.White, 0.0f, paletteOrigin, 3.0f * scale, SpriteEffects.None, 0.0f);

            spriteBatch.DrawString(font, "Light: " + lightDir, Vector2.Zero, textColor);
            if (lightLock) {
                spriteBatch.DrawString(font, "Light direction locked, right-click to unlock", new Vector2(0, font.LineSpacing), textColor);
            }
            spriteBatch.DrawString(font, "Press ? For Help", new Vector2(0, h - font.LineSpacing), textColor);

            Vector2 spriteOrigin = new Vector2(sprite.CMap.Width / 2.0f, sprite.CMap.Height / 2.0f);
            sprite.draw(spriteBatch, spritePos, spriteOrigin, scale, lightDir);

            spriteBatch.End();

            // Draw Tooltip
            if (viewHelp) {
                int margin = 80;
                spriteBatch.Begin();
                Rectangle tooltipBackground = new Rectangle(margin / 2, margin / 2, w - margin, h - margin);
                spriteBatch.Draw(WHITE_BOX, tooltipBackground, new Color(0.1f, 0.1f, 0.1f, 0.90f));
                spriteBatch.DrawString(font, HELP_TEXT, new Vector2(margin, margin), Color.AntiqueWhite);
                spriteBatch.End();
            } else if (tooltip != "") {
                spriteBatch.Begin();
                Vector2 tooltipSize = font.MeasureString(tooltip);
                Rectangle tooltipBackground = new Rectangle(ms.X - 20, ms.Y + 20, (int)tooltipSize.X + 4, (int)tooltipSize.Y + 4);
                spriteBatch.Draw(WHITE_BOX, tooltipBackground, new Color(0.2f, 0.2f, 0.2f, 0.85f));
                spriteBatch.DrawString(font, tooltip, new Vector2(ms.X - 18, ms.Y + 22), Color.AntiqueWhite);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
