using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Art2Core.SdlBindings;

namespace Art2Core.Modules
{
    public struct GlyphInfo
    {
        public float U1;
        public float V1;
        public float U2;
        public float V2;
        public int Width;
        public int Height;
        public int OffsetX;
        public int OffsetY;
        public int Advance;
    }

    public class GlyphAtlas
    {
        public IntPtr Texture;
        public int AtlasWidth;
        public int AtlasHeight;
        public float LineHeight;
        public float Ascent;
        public Dictionary<char, GlyphInfo> Glyphs = new();
    }

    /// <summary>
    /// Manages high-performance loading, measuring, and drawing of TrueType Fonts using SDL3_ttf with pre-rendered Glyph Atlases.
    /// </summary>
    public static class Fonts
    {
        private static readonly Dictionary<string, string> _fontPaths = new(StringComparer.OrdinalIgnoreCase);
        
        // Cache of pre-built glyph atlases by (fontName, size)
        private static readonly Dictionary<(string fontName, float size), GlyphAtlas> _atlases = new();

        /// <summary>
        /// Registers a TrueType font path under a specific name.
        /// </summary>
        /// <param name="fontName">The registered name of the font.</param>
        /// <param name="fontPath">The filesystem path to the TTF font file.</param>
        public static void LoadFont(string fontName, string fontPath)
        {
            if (string.IsNullOrEmpty(fontName)) throw new ArgumentNullException(nameof(fontName));
            if (string.IsNullOrEmpty(fontPath)) throw new ArgumentNullException(nameof(fontPath));

            if (!File.Exists(fontPath))
            {
                throw new FileNotFoundException($"Font file not found: {fontPath}");
            }

            _fontPaths[fontName] = fontPath;
        }

        /// <summary>
        /// Kept for backwards compatibility. Registers a font from the specified TTF path.
        /// </summary>
        public static void LoadAtlasFont(string fontName, string jsonPath, string texturePath)
        {
            Console.WriteLine($"[Fonts] LoadAtlasFont called for '{fontName}' (json: {jsonPath}, texture: {texturePath}).");
            
            // Try to find a TTF file in the same folder or with the same name.
            string baseDir = Path.GetDirectoryName(jsonPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(jsonPath);
            string ttfPath = Path.Combine(baseDir, baseName + ".ttf");

            if (File.Exists(ttfPath))
            {
                LoadFont(fontName, ttfPath);
            }
            else
            {
                string localArial = Path.Combine(baseDir, "arial.ttf");
                if (File.Exists(localArial))
                {
                    LoadFont(fontName, localArial);
                }
                else
                {
                    string systemFont = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    if (File.Exists(systemFont))
                    {
                        LoadFont(fontName, systemFont);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Could not load font '{fontName}': TTF fallback not found.");
                    }
                }
            }
        }

        /// <summary>
        /// Measures the size of a text string based on the glyph atlas.
        /// </summary>
        public static (float Width, float Height) MeasureText(string fontName, string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return (0f, 0f);

            var atlas = GetAtlas(fontName, scale);
            float maxX = 0f;
            float curX = 0f;
            int lines = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    if (curX > maxX) maxX = curX;
                    curX = 0f;
                    lines++;
                    continue;
                }

                char lookupChar = c;
                if (!atlas.Glyphs.ContainsKey(lookupChar))
                {
                    lookupChar = '?';
                }

                if (atlas.Glyphs.TryGetValue(lookupChar, out var glyph))
                {
                    curX += glyph.Advance;
                }
            }

            if (curX > maxX) maxX = curX;

            return (maxX, lines * atlas.LineHeight);
        }

        /// <summary>
        /// Measures the precise spatial boundaries of the text.
        /// </summary>
        public static ((float X, float Y) Offset, (float Width, float Height) Size) MeasureTextBounds(string fontName, string text, float scale = 1f)
        {
            var size = MeasureText(fontName, text, scale);
            return ((0f, 0f), size);
        }

        /// <summary>
        /// Draws a text string at the specified coordinates using a pre-rendered Glyph Atlas.
        /// </summary>
        public static void DrawText(string fontName, string text, float x, float y, float scale, SDL_FColor color)
        {
            if (string.IsNullOrEmpty(text)) return;

            var atlas = GetAtlas(fontName, scale);

            float cx = x;
            float cy = y + atlas.Ascent; // Align base of text from the top-left coordinate system

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    cx = x;
                    cy += atlas.LineHeight;
                    continue;
                }

                char lookupChar = c;
                if (!atlas.Glyphs.ContainsKey(lookupChar))
                {
                    lookupChar = '?';
                }

                if (atlas.Glyphs.TryGetValue(lookupChar, out var glyph))
                {
                    float drawX = cx + glyph.OffsetX;
                    float drawY = cy - glyph.OffsetY;
                    float drawW = glyph.Width;
                    float drawH = glyph.Height;

                    Renderer.DrawTextureQuad(atlas.Texture, drawX, drawY, drawW, drawH, glyph.U1, glyph.V1, glyph.U2, glyph.V2, color);

                    cx += glyph.Advance;
                }
            }
        }

        private static GlyphAtlas GetAtlas(string fontName, float size)
        {
            // Standardize size key
            float roundedSize = MathF.Round(size);
            if (roundedSize < 1) roundedSize = 1;

            var key = (fontName.ToLowerInvariant(), roundedSize);
            if (_atlases.TryGetValue(key, out var atlas))
            {
                return atlas;
            }

            if (!_fontPaths.TryGetValue(fontName, out var path))
            {
                string systemFont = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                if (File.Exists(systemFont))
                {
                    _fontPaths[fontName] = systemFont;
                    path = systemFont;
                }
                else
                {
                    throw new KeyNotFoundException($"Font '{fontName}' has not been registered and no system fallback font was found.");
                }
            }

            IntPtr fontPtr = SdlTtf.TTF_OpenFont(path, roundedSize);
            if (fontPtr == IntPtr.Zero)
            {
                throw new Exception($"Failed to open font '{fontName}' from '{path}' at size {roundedSize}");
            }

            try
            {
                atlas = CreateGlyphAtlas(fontPtr, roundedSize);
                _atlases[key] = atlas;
                return atlas;
            }
            finally
            {
                SdlTtf.TTF_CloseFont(fontPtr);
            }
        }

        private static GlyphAtlas CreateGlyphAtlas(IntPtr fontPtr, float size)
        {
            int fontHeight = SdlTtf.TTF_GetFontHeight(fontPtr);
            int fontAscent = SdlTtf.TTF_GetFontAscent(fontPtr);

            int atlasWidth = 512;
            int atlasHeight = 512;

            // Render a test character to query the native pixel format returned by SDL3_ttf
            uint format = 0x16362004u; // Default to SDL_PIXELFORMAT_ARGB8888
            IntPtr testSurfPtr = SdlTtf.TTF_RenderGlyph_Blended(fontPtr, 'A', new SDL_Color(255, 255, 255, 255));
            if (testSurfPtr != IntPtr.Zero)
            {
                SDL_Surface surf = Marshal.PtrToStructure<SDL_Surface>(testSurfPtr);
                format = surf.format;
                Renderer.SDL_DestroySurface(testSurfPtr);
            }

            IntPtr atlasSurfacePtr = Renderer.SDL_CreateSurface(atlasWidth, atlasHeight, format);
            if (atlasSurfacePtr == IntPtr.Zero)
            {
                throw new Exception("Failed to create blank surface for font glyph atlas.");
            }

            int currentX = 0;
            int currentY = 0;
            int rowHeight = 0;
            int padding = 2; // Prevent bleeding

            var glyphs = new Dictionary<char, GlyphInfo>();

            // Pack printable ASCII range (32 to 126)
            for (uint ch = 32; ch <= 126; ch++)
            {
                IntPtr glyphSurfPtr = SdlTtf.TTF_RenderGlyph_Blended(fontPtr, ch, new SDL_Color(255, 255, 255, 255));
                if (glyphSurfPtr == IntPtr.Zero) continue;

                SDL_Surface glyphSurf = Marshal.PtrToStructure<SDL_Surface>(glyphSurfPtr);
                int gw = glyphSurf.w;
                int gh = glyphSurf.h;

                if (currentX + gw + padding > atlasWidth)
                {
                    currentX = 0;
                    currentY += rowHeight + padding;
                    rowHeight = 0;
                }

                if (currentY + gh + padding > atlasHeight)
                {
                    Console.WriteLine("[Fonts] Warning: glyph atlas surface is full.");
                    Renderer.SDL_DestroySurface(glyphSurfPtr);
                    break;
                }

                var dstRect = new SDL_Rect(currentX, currentY, gw, gh);
                Renderer.SDL_BlitSurface(glyphSurfPtr, IntPtr.Zero, atlasSurfacePtr, ref dstRect);

                SdlTtf.TTF_GetGlyphMetrics(fontPtr, ch, out int minx, out int maxx, out int miny, out int maxy, out int advance);

                var info = new GlyphInfo
                {
                    U1 = (float)currentX / (float)atlasWidth,
                    V1 = (float)currentY / (float)atlasHeight,
                    U2 = (float)(currentX + gw) / (float)atlasWidth,
                    V2 = (float)(currentY + gh) / (float)atlasHeight,
                    Width = gw,
                    Height = gh,
                    OffsetX = 0,
                    OffsetY = fontAscent,
                    Advance = advance
                };
                glyphs[(char)ch] = info;

                currentX += gw + padding;
                if (gh > rowHeight) rowHeight = gh;

                Renderer.SDL_DestroySurface(glyphSurfPtr);
            }

            IntPtr texturePtr = Renderer.SDL_CreateTextureFromSurface(Renderer.Pointer, atlasSurfacePtr);
            Renderer.SDL_DestroySurface(atlasSurfacePtr);

            if (texturePtr == IntPtr.Zero)
            {
                throw new Exception("Failed to upload glyph atlas surface to GPU texture.");
            }

            return new GlyphAtlas
            {
                Texture = texturePtr,
                AtlasWidth = atlasWidth,
                AtlasHeight = atlasHeight,
                LineHeight = fontHeight,
                Ascent = fontAscent,
                Glyphs = glyphs
            };
        }

        /// <summary>
        /// Clears all loaded atlases and textures.
        /// </summary>
        public static void Shutdown()
        {
            foreach (var atlas in _atlases.Values)
            {
                if (atlas.Texture != IntPtr.Zero)
                {
                    Renderer.DestroyTexture(atlas.Texture);
                }
            }
            _atlases.Clear();
            _fontPaths.Clear();
        }
    }
}
