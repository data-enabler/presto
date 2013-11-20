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
            get { return paletteTexture; }
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

        public PrestoSprite(Texture2D cMap, Texture2D hMap, Texture2D palette) {
            initSprite(cMap, hMap, palette);
        }

        public PrestoSprite(string cMapPath, string hMapPath, string palettePath) {
            System.IO.FileStream cStream = new System.IO.FileStream(cMapPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.FileStream hStream = new System.IO.FileStream(hMapPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.FileStream pStream = new System.IO.FileStream(palettePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            initSprite(Texture2D.FromStream(game.GraphicsDevice, cStream),
                       Texture2D.FromStream(game.GraphicsDevice, hStream),
                       Texture2D.FromStream(game.GraphicsDevice, pStream));
            cStream.Close();
            hStream.Close();
            pStream.Close();
        }

        private void initSprite(Texture2D cmap, Texture2D hmap, Texture2D palette) {
            cMapTexture = cmap;
            hMapTexture = hmap;
            paletteTexture = palette;

            // Convert height map to normal map
            int hw = hMapTexture.Width;
            int hh = hMapTexture.Height;
            Color[] hMapColors = new Color[hw * hh];

            // Round up texture dimensions to power of 2
            int hDim = 1 << (int)Math.Ceiling(Math.Log(Math.Max(hw, hh), 2.0));
            effect2NMapTexture = new Texture2D(game.GraphicsDevice, hDim, hDim);
            Color[] effect2Normals = new Color[hDim * hDim];

            hMapTexture.GetData<Color>(hMapColors);

            int[] offsets = { -1, 1, -hw, hw }; // left, right, down, up
            del[] toEdge = {
                (pix, imgWidth, imgHeight) => pix % imgWidth,
                (pix, imgWidth, imgHeight) => imgWidth - 1 - (pix % imgWidth),
                (pix, imgWidth, imgHeight) => pix / imgWidth,
                (pix, imgWidth, imgHeight) => imgHeight - 1 - (pix / imgWidth)
            };
            for (int y = 0; y < hh; y++) {
                for (int x = 0; x < hw; x++) {
                    int origPix = y * hw + x;
                    Color orig = hMapColors[origPix];
                    int[] dists = new int[4]; // left, right, down, up
                    float[] diffs = new float[4]; // left, right, down, up
                    int k;

                    for (int dir = 0; dir < 4; dir++) {
                        int pix = origPix;
                        for (k = 0; k < toEdge[dir](pix, hw, hh); k++) {
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

                    int pixDst = y * hDim + x;
                    effect2Normals[pixDst].R = (byte)Math.Round(n.X * 127 + 127);
                    effect2Normals[pixDst].G = (byte)Math.Round(n.Y * 127 + 127);
                    effect2Normals[pixDst].B = (byte)Math.Round(n.Z * 127 + 127);
                    effect2Normals[pixDst].A = 255;
                }
            }
            effect2NMapTexture.SetData<Color>(effect2Normals);

            // Create color index map, used to get pallete coordinates
            int cw = cMapTexture.Width;
            int ch = cMapTexture.Height;
            Color[] cMapColors = new Color[cw * ch];

            // Round up texture dimensions to power of 2
            int cDim = 1 << (int)Math.Ceiling(Math.Log(Math.Max(cw, ch), 2.0));
            pMapTexture = new Texture2D(game.GraphicsDevice, cDim, cDim);
            Color[] pMapColors = new Color[cDim * cDim];

            int pw = paletteTexture.Width;
            int ph = paletteTexture.Height;
            Color[] paletteColors = new Color[pw * ph];

            // Round up texture dimensions to power of 2
            int pDim = 1 << (int)Math.Ceiling(Math.Log(Math.Max(pw, ph), 2.0));
            adjustedPaletteTexture = new Texture2D(game.GraphicsDevice, pDim, pDim);
            Color[] apColors = new Color[pDim * pDim];

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
                    apColors[y * pDim + x - 1] = c;
                }
                x--;
                colorToPMapIndex[paletteColors[y * pw]] = new Color((y + 0.5f) / pDim, (x - 1.0f) / pDim, 0.5f / pDim);
            }

            for (int y = 0; y < ch; y++) {
                for (int x = 0; x < cw; x++) {
                    int pixSrc = y * cw + x;
                    int pixDst = y * cDim + x;
                    if (colorToPMapIndex.ContainsKey(cMapColors[pixSrc])) {
                        pMapColors[pixDst] = colorToPMapIndex[cMapColors[pixSrc]];
                    }
                }
            }

            pMapTexture.SetData<Color>(pMapColors);
            adjustedPaletteTexture.SetData<Color>(apColors);

            using (System.IO.Stream stream = System.IO.File.Create("pMap.png")) {
                pMapTexture.SaveAsPng(stream, cDim, cDim);
            }
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
