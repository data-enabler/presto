using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace PrestoViewer {
    class PrestoSprite {
        static Effect[] effect = new Effect[2];

        Texture2D cMapTexture;
        public Texture2D CMap {
            get { return cMapTexture; }
        }
        // Colors are replaced with index into palette. R = y, G = size of color range
        Texture2D pMapTexture;
        public Texture2D PMap {
            get { return pMapTexture; }
        }
        Texture2D hMapTexture;
        public Texture2D HMap {
            get { return hMapTexture; }
        }
        Texture2D effect2NMapTexture;
        public Texture2D NMap {
            get { return effect2NMapTexture; }
        }
        Texture2D paletteTexture;
        public Texture2D Palette {
            get { return adjustedPaletteTexture; }
        }
        // Leftmost pixels removed, size adjusted to power of 2
        Texture2D adjustedPaletteTexture;
        delegate int del(int pix, int imgWidth, int imgHeight);

        public enum ShadingVersion { Ver1, Ver2 };
        ShadingVersion version = ShadingVersion.Ver2;
        public ShadingVersion Shader {
            get { return version; }
            set { version = value; }
        }

        static Game game;
        public static void init(Game game) {
            PrestoSprite.game = game;
            effect[0] = game.Content.Load<Effect>("Effect1");
            effect[1] = game.Content.Load<Effect>("Effect2");
        }

        public PrestoSprite(string cmap, string hmap, string palette) {
            cMapTexture = game.Content.Load<Texture2D>(cmap);
            hMapTexture = game.Content.Load<Texture2D>(hmap);
            paletteTexture = game.Content.Load<Texture2D>(palette);

            // Normal map for Effect 2
            int width = hMapTexture.Width;
            int height = hMapTexture.Height;
            effect2NMapTexture = new Texture2D(game.GraphicsDevice, width, height);
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
                    effect2Normals[y * width + x].R = (byte)Math.Round(n.X * 127 + 127);
                    effect2Normals[y * width + x].G = (byte)Math.Round(n.Y * 127 + 127);
                    effect2Normals[y * width + x].B = (byte)Math.Round(n.Z * 127 + 127);
                    effect2Normals[y * width + x].A = 255;
                }
            }
            effect2NMapTexture.SetData<Color>(effect2Normals);

            // Pallete Map
            int cw = cMapTexture.Width;
            int ch = cMapTexture.Height;
            int pw = paletteTexture.Width;
            int ph = paletteTexture.Height;
            pMapTexture = new Texture2D(game.GraphicsDevice, cw, ch);
            int dim = 1 << (int)Math.Ceiling(Math.Log(Math.Max(pw, ph), 2.0));
            adjustedPaletteTexture = new Texture2D(game.GraphicsDevice, dim, dim);
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

        public void draw(SpriteBatch spriteBatch, Vector2 spritePos, Vector2 spriteOrigin, float scale, Vector3 lightDir) {
            Effect currentEffect = effect[(int)version];
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
        }
    }
}
