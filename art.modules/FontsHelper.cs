using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ArtFrameCore.SdlBindings;

namespace ArtFrameCore.Modules
{
    /// <summary>
    /// Represents the precise boundaries of a glyph in an atlas or plane coordinate system.
    /// </summary>
    public struct GlyphBounds
    {
        /// <summary>The left edge of the bounding box.</summary>
        public float Left;
        /// <summary>The bottom edge of the bounding box.</summary>
        public float Bottom;
        /// <summary>The right edge of the bounding box.</summary>
        public float Right;
        /// <summary>The top edge of the bounding box.</summary>
        public float Top;

        /// <summary>
        /// Constructs a new GlyphBounds bounding box.
        /// </summary>
        public GlyphBounds(float left, float bottom, float right, float top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }
    }

    /// <summary>
    /// Represents structural layout data for a single Multi-channel Signed Distance Field (MSDF) character.
    /// </summary>
    public struct MtsdfGlyph
    {
        /// <summary>The horizontal advance distance for the character.</summary>
        public float Advance;
        /// <summary>The texture coordinates of the glyph in the atlas.</summary>
        public GlyphBounds AtlasBounds;
        /// <summary>The local vector space layout boundary of the glyph.</summary>
        public GlyphBounds PlaneBounds;
        /// <summary>Indicates if this glyph has texture atlas boundaries.</summary>
        public bool HasAtlasBounds;
        /// <summary>Indicates if this glyph has layout bounds in plane space.</summary>
        public bool HasPlaneBounds;
    }

    /// <summary>
    /// Represents a Multi-channel Signed Distance Field (MTSDF) Font asset loaded in memory.
    /// </summary>
    public class MtsdfFont
    {
        /// <summary>Gets the native SDL3 texture pointer for the font atlas.</summary>
        public IntPtr Texture { get; internal set; }

        /// <summary>Gets the width of the font atlas texture in pixels.</summary>
        public int TextureWidth { get; internal set; }

        /// <summary>Gets the height of the font atlas texture in pixels.</summary>
        public int TextureHeight { get; internal set; }

        /// <summary>Gets the distance range used during the distance field generation.</summary>
        public float DistanceRange { get; internal set; }

        /// <summary>Gets the baseline em size used to scale the font.</summary>
        public float EmSize { get; internal set; }

        // High-speed direct-lookup array for standard ASCII characters (0-255)
        private readonly MtsdfGlyph[] _asciiGlyphs = new MtsdfGlyph[256];

        // Dictionary fallback for extended/unicode characters (Chinese, Japanese, etc.)
        private readonly Dictionary<char, MtsdfGlyph> _extendedGlyphs = new Dictionary<char, MtsdfGlyph>();

        /// <summary>
        /// Retrieves glyph layout metrics for a specific character with O(1) array fast-path lookup.
        /// </summary>
        public MtsdfGlyph GetGlyph(char c)
        {
            if (c < 256)
            {
                return _asciiGlyphs[c];
            }
            if (_extendedGlyphs.TryGetValue(c, out var glyph))
            {
                return glyph;
            }
            return _asciiGlyphs['?']; // Default fallback character
        }

        /// <summary>
        /// Assigns a glyph to the lookup cache.
        /// </summary>
        internal void SetGlyph(char c, MtsdfGlyph glyph)
        {
            if (c < 256)
            {
                _asciiGlyphs[c] = glyph;
            }
            else
            {
                _extendedGlyphs[c] = glyph;
            }
        }
    }

    /// <summary>
    /// Manages high-performance loading, measuring, and drawing of Multi-channel Signed Distance Field (MTSDF) fonts.
    /// </summary>
    public static class Fonts
    {
        private static readonly Dictionary<string, MtsdfFont> _fonts = new Dictionary<string, MtsdfFont>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads an atlas font from metadata JSON and texture files.
        /// </summary>
        /// <param name="fontName">The unique name to register this font as.</param>
        /// <param name="jsonPath">The path to the font atlas metadata JSON file.</param>
        /// <param name="texturePath">The path to the font atlas texture image file.</param>
        public static void LoadAtlasFont(string fontName, string jsonPath, string texturePath)
        {
            if (string.IsNullOrEmpty(fontName)) throw new ArgumentNullException(nameof(fontName));
            if (string.IsNullOrEmpty(jsonPath)) throw new ArgumentNullException(nameof(jsonPath));
            if (string.IsNullOrEmpty(texturePath)) throw new ArgumentNullException(nameof(texturePath));

            var font = LoadMtsdfFont(jsonPath, texturePath);
            _fonts[fontName] = font;
        }

        /// <summary>
        /// Measures the size of a text string based on character advances and line counts.
        /// </summary>
        /// <param name="fontName">The registered name of the font to use.</param>
        /// <param name="text">The string to measure.</param>
        /// <param name="scale">The rendering scale factor.</param>
        /// <returns>A tuple representing the width and height of the measured text.</returns>
        public static (float Width, float Height) MeasureText(string fontName, string text, float scale = 1f)
        {
            if (!_fonts.TryGetValue(fontName, out var font) || string.IsNullOrEmpty(text))
                return (0f, 0f);

            float maxX = 0f;
            float curX = 0f;
            int lines = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    maxX = Math.Max(maxX, curX);
                    curX = 0f;
                    lines++;
                    continue;
                }

                var glyph = font.GetGlyph(c);
                curX += glyph.Advance;
            }

            return (Math.Max(maxX, curX) * scale, lines * scale);
        }

        /// <summary>
        /// Measures the precise spatial boundaries of the glyph geometry.
        /// </summary>
        /// <param name="fontName">The registered name of the font.</param>
        /// <param name="text">The string to measure.</param>
        /// <param name="scale">The rendering scale factor.</param>
        /// <returns>A tuple of coordinates representing layout offset and bounding dimensions.</returns>
        public static ((float X, float Y) Offset, (float Width, float Height) Size) MeasureTextBounds(string fontName, string text, float scale = 1f)
        {
            if (!_fonts.TryGetValue(fontName, out var font) || string.IsNullOrEmpty(text))
                return ((0f, 0f), (0f, 0f));

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            float curX = 0f;
            float curY = 0f;
            float padding = font.DistanceRange / font.EmSize;
            bool hasGlyphs = false;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    curX = 0f;
                    curY += 1f;
                    continue;
                }

                var glyph = font.GetGlyph(c);

                if (glyph.HasPlaneBounds)
                {
                    hasGlyphs = true;

                    float left = curX + glyph.PlaneBounds.Left - padding;
                    float right = curX + glyph.PlaneBounds.Right + padding;
                    float top = curY - glyph.PlaneBounds.Top - padding;
                    float bottom = curY - glyph.PlaneBounds.Bottom + padding;

                    if (left < minX) minX = left;
                    if (right > maxX) maxX = right;
                    if (top < minY) minY = top;
                    if (bottom > maxY) maxY = bottom;
                }

                curX += glyph.Advance;
            }

            if (!hasGlyphs) return ((0f, 0f), (0f, 0f));

            return ((minX * scale, minY * scale), ((maxX - minX) * scale, (maxY - minY) * scale));
        }

        /// <summary>
        /// Draws a text string at the specified coordinates using hardware-accelerated batch rendering.
        /// </summary>
        /// <param name="fontName">The registered name of the font to use.</param>
        /// <param name="text">The text string to draw.</param>
        /// <param name="x">The screen X coordinate.</param>
        /// <param name="y">The screen Y coordinate.</param>
        /// <param name="scale">The rendering scale factor.</param>
        /// <param name="color">The drawing color for the text.</param>
        public static void DrawText(string fontName, string text, float x, float y, float scale, SDL_FColor color)
        {
            if (!_fonts.TryGetValue(fontName, out var font) || string.IsNullOrEmpty(text))
                return;

            float cursorX = 0f;
            float cursorY = 0f;
            float padding = font.DistanceRange / font.EmSize;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    cursorX = 0f;
                    cursorY += 1f;
                    continue;
                }

                var glyph = font.GetGlyph(c);

                if (glyph.HasAtlasBounds)
                {
                    float left = cursorX + glyph.PlaneBounds.Left - padding;
                    float top = cursorY - glyph.PlaneBounds.Top - padding;
                    float width = glyph.PlaneBounds.Right - glyph.PlaneBounds.Left + (padding * 2f);
                    float height = glyph.PlaneBounds.Top - glyph.PlaneBounds.Bottom + (padding * 2f);

                    float drawX = x + (left * scale);
                    float drawY = y + (top * scale);
                    float drawW = width * scale;
                    float drawH = height * scale;

                    // Calculate UV coordinates mapped onto the font atlas
                    float u1 = glyph.AtlasBounds.Left / font.TextureWidth;
                    float v1 = (font.TextureHeight - glyph.AtlasBounds.Top) / font.TextureHeight;
                    float u2 = glyph.AtlasBounds.Right / font.TextureWidth;
                    float v2 = (font.TextureHeight - glyph.AtlasBounds.Bottom) / font.TextureHeight;

                    Renderer.DrawTextureQuad(font.Texture, drawX, drawY, drawW, drawH, u1, v1, u2, v2, color);
                }

                cursorX += glyph.Advance;
            }
        }

        /// <summary>
        /// Internally parses high-performance font atlas JSON metadata and loads the SDL texture.
        /// </summary>
        private static MtsdfFont LoadMtsdfFont(string jsonPath, string texturePath)
        {
            var jsonContent = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            IntPtr renderer = Renderer.Pointer;
            if (renderer == IntPtr.Zero)
            {
                throw new InvalidOperationException("[ArtFrameCore] Cannot load font: SDL renderer is not initialized.");
            }

            IntPtr texture = SdlImage.LoadTexture(renderer, texturePath);
            if (texture == IntPtr.Zero)
            {
                throw new FileNotFoundException($"[ArtFrameCore] Failed to load font texture from '{texturePath}'");
            }

            // In SDL3, we query texture dimensions directly
            int w = 512, h = 512; // Standard fallback sizes
            // We can query using standard SDL3 call if needed, but since MSDF JSON typically lists texture size, let's read it or use a default:
            var atlasElement = root.GetProperty("atlas");
            if (atlasElement.TryGetProperty("width", out var widthProp)) w = widthProp.GetInt32();
            if (atlasElement.TryGetProperty("height", out var heightProp)) h = heightProp.GetInt32();

            var font = new MtsdfFont
            {
                Texture = texture,
                TextureWidth = w,
                TextureHeight = h,
                DistanceRange = atlasElement.GetProperty("distanceRange").GetSingle(),
                EmSize = atlasElement.GetProperty("size").GetSingle()
            };

            // Prime direct lookup table with empty fallback glyphs
            for (int i = 0; i < 256; i++)
            {
                font.SetGlyph((char)i, new MtsdfGlyph { Advance = 0.25f });
            }

            foreach (var glyphElement in root.GetProperty("glyphs").EnumerateArray())
            {
                char c = (char)glyphElement.GetProperty("unicode").GetInt32();
                var glyph = new MtsdfGlyph 
                { 
                    Advance = glyphElement.GetProperty("advance").GetSingle() 
                };

                if (glyphElement.TryGetProperty("atlasBounds", out var ab))
                {
                    glyph.HasAtlasBounds = true;
                    glyph.AtlasBounds = new GlyphBounds(
                        ab.GetProperty("left").GetSingle(),
                        ab.GetProperty("bottom").GetSingle(),
                        ab.GetProperty("right").GetSingle(),
                        ab.GetProperty("top").GetSingle()
                    );
                }

                if (glyphElement.TryGetProperty("planeBounds", out var pb))
                {
                    glyph.HasPlaneBounds = true;
                    glyph.PlaneBounds = new GlyphBounds(
                        pb.GetProperty("left").GetSingle(),
                        pb.GetProperty("bottom").GetSingle(),
                        pb.GetProperty("right").GetSingle(),
                        pb.GetProperty("top").GetSingle()
                    );
                }

                font.SetGlyph(c, glyph);
            }

            return font;
        }
    }
}
