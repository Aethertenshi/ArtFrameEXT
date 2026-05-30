using ArtFrame.ArtTypes;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using static ArtFrame.GraphicsHelper;
namespace ArtFrame
{
    // Helper Class and Struct
    public class MtsdfFont
    {
        public Microsoft.Xna.Framework.Graphics.Texture2D Texture { get; internal set; }
        public float DistanceRange { get; internal set; }
        public float EmSize { get; internal set; }
        public Dictionary<char, MtsdfGlyph> Glyphs { get; internal set; } = new();

        public MtsdfGlyph GetGlyph(char c) => Glyphs.TryGetValue(c, out var g) ? g : Glyphs['?'];
    }
    public struct MtsdfGlyph
    {
        public float Advance;
        public Microsoft.Xna.Framework.Vector4 AtlasBounds; // X=Left, Y=Bottom, Z=Right, W=Top
        public Microsoft.Xna.Framework.Vector4 PlaneBounds; // X=Left, Y=Bottom, Z=Right, W=Top
    }
    public class AtlasData
    {
        public AtlasMetrics Metrics { get; set; }
        public List<GlyphData> Glyphs { get; set; }
    }

    public class AtlasMetrics
    {
        public float EmSize { get; set; }
        public float LineHeight { get; set; }
        public float Ascent { get; set; }
        public float Descent { get; set; }
        public float DistanceRange { get; set; } // This is your 'pxrange' from the command
    }

    public class GlyphData
    {
        public int Unicode { get; set; }
        public float Advance { get; set; }
        public PlaneBounds PlaneBounds { get; set; } // The "Vector" bounds
        public AtlasBounds AtlasBounds { get; set; } // The Texture UV bounds
    }
    public record PlaneBounds(float Left, float Bottom, float Right, float Top);
    public record AtlasBounds(float Left, float Bottom, float Right, float Top);

    // Main Method
    public static class FontHelper
    {
        // Static Reference and Variables
        private static Art instance => Art.Instance;
        private static GraphicsDevice graphicsDevice => instance.graphicsDevice;
        private static Effect _fontShader;
        private static Dictionary<string, MtsdfFont> _fonts = new();

        // Rendering Methods
        internal static void DrawTextPro(
           string fontName,
           string text,
           Vector2 position,
           Vector2 origin,     // screen pixels, pass MeasureText()/2 to center
           float rotation,
           float scale,
           Color color,
           float strokeWidth = 0f,
           Color? strokeColor = null)
        {
            if (!_fonts.TryGetValue(fontName, out var font)) return;


            CloseBatch();
            SetEffectParameters(fontName);
            StartBatch(_fontShader);

            if (strokeWidth > 0f && strokeColor.HasValue)
            {
                float r = Microsoft.Xna.Framework.MathHelper.ToRadians(rotation);
                Vector2[] offsets = {
            new(-strokeWidth,  0),           new(strokeWidth,  0),
            new(0,            -strokeWidth),  new(0,            strokeWidth),
            new(-strokeWidth, -strokeWidth),  new(strokeWidth, -strokeWidth),
            new(-strokeWidth,  strokeWidth),  new(strokeWidth,  strokeWidth)
        };
                foreach (var off in offsets)
                    DrawMtsdfString(font, text, position + off, strokeColor.Value, r, origin, scale, SpriteEffects.None, 0f);
            }

            DrawMtsdfString(font, text, position, color, Microsoft.Xna.Framework.MathHelper.ToRadians(rotation), origin, scale, SpriteEffects.None, 0f);

            CloseBatch();
            StartBatch();
        }

        public static (Vector2 offset, Vector2 size) MeasureTextBounds(string fontName, string text, float scale = 1f)
        {
            if (!_fonts.TryGetValue(fontName, out var font) || string.IsNullOrEmpty(text))
                return (Vector2.Zero, Vector2.Zero);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            float curX = 0f, curY = 0f;
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

                if (font.Glyphs.TryGetValue(c, out var glyph))
                {
                    if (glyph.PlaneBounds != Microsoft.Xna.Framework.Vector4.Zero)
                    {
                        hasGlyphs = true;

                        // Calculate exact visual boundaries for this specific character
                        float left = curX + glyph.PlaneBounds.X - padding;
                        float right = curX + glyph.PlaneBounds.Z + padding;
                        float top = curY - glyph.PlaneBounds.W - padding;
                        float bottom = curY - glyph.PlaneBounds.Y + padding;

                        // Expand our bounding box to include this character
                        if (left < minX) minX = left;
                        if (right > maxX) maxX = right;
                        if (top < minY) minY = top;
                        if (bottom > maxY) maxY = bottom;
                    }
                    curX += glyph.Advance;
                }
                else if (c == ' ')
                {
                    curX += 0.25f;
                }
            }

            if (!hasGlyphs) return (Vector2.Zero, Vector2.Zero);

            Vector2 offset = new Vector2(minX * scale, minY * scale);
            Vector2 size = new Vector2((maxX - minX) * scale, (maxY - minY) * scale);

            return (offset, size);
        }

        public static Vector2 MeasureText(string fontName, string text, float scale = 1f)
        {
            if (!_fonts.TryGetValue(fontName, out var font)) return Vector2.Zero;
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;

            float maxX = 0f, curX = 0f;
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

                if (font.Glyphs.TryGetValue(c, out var glyph))
                {
                    curX += glyph.Advance;
                }
                else if (c == ' ')
                {
                    curX += 0.25f;
                }
            }

            return new Vector2(Math.Max(maxX, curX), lines * 1f) * scale;
        }

        // Framework Backend
        internal static void LoadFontShader()
        {
            byte[] bytecode = File.ReadAllBytes("shaders/atlas.fxb");
            _fontShader = new Effect(graphicsDevice, bytecode);
        }

        public static void LoadAtlasFont(string fontName, string jsonPath, string texturePath)
        {
            _fonts.Add(fontName, LoadMtsdfFont(jsonPath, texturePath));
        }

        internal static void SetEffectParameters(string fontName)
        {
            var font = _fonts[fontName];
            _fontShader?.Parameters["atlasSize"].SetValue(new Vector2(font.Texture.Width, font.Texture.Height));
            _fontShader?.Parameters["pxRange"].SetValue(font.DistanceRange);
        }

        internal static void DrawMtsdfString(
            MtsdfFont font, string text, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale,
            SpriteEffects effects, float layerDepth)
        {
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            Vector2 cursor = Vector2.Zero;

            float textureScale = scale / font.EmSize;
            float padding = font.DistanceRange / font.EmSize;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    cursor.X = 0f;
                    cursor.Y += 1f;
                    continue;
                }

                var glyph = font.GetGlyph(c);

                if (glyph.AtlasBounds != Microsoft.Xna.Framework.Vector4.Zero)
                {
                    Vector2 localPos = new Vector2(
                        cursor.X + glyph.PlaneBounds.X - padding,
                        cursor.Y - glyph.PlaneBounds.W - padding
                    );

                    Vector2 offset = (localPos * scale) - origin;
                    Vector2 drawPos = position + new Vector2(
                        offset.X * cos - offset.Y * sin,
                        offset.X * sin + offset.Y * cos
                    );

                    Rectangle src = new Rectangle(
                        (int)glyph.AtlasBounds.X,
                        font.Texture.Height - (int)glyph.AtlasBounds.W,
                        (int)(glyph.AtlasBounds.Z - glyph.AtlasBounds.X),
                        (int)(glyph.AtlasBounds.W - glyph.AtlasBounds.Y)
                    );

                    instance.spriteBatch.Draw(font.Texture, drawPos, src, color,
                        rotation, Vector2.Zero, textureScale, effects, layerDepth);
                }

                cursor.X += glyph.Advance;
            }
        }

        internal static MtsdfFont LoadMtsdfFont(string jsonPath, string texturePath)
        {
            var jsonContent = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var font = new MtsdfFont
            {
                Texture = Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(graphicsDevice, File.OpenRead(texturePath)),
                DistanceRange = root.GetProperty("atlas").GetProperty("distanceRange").GetSingle(),
                EmSize = root.GetProperty("atlas").GetProperty("size").GetSingle()
            };

            foreach (var glyphElement in root.GetProperty("glyphs").EnumerateArray())
            {
                char c = (char)glyphElement.GetProperty("unicode").GetInt32();
                var glyph = new MtsdfGlyph { Advance = glyphElement.GetProperty("advance").GetSingle() };

                if (glyphElement.TryGetProperty("atlasBounds", out var ab))
                    glyph.AtlasBounds = new Microsoft.Xna.Framework.Vector4(ab.GetProperty("left").GetSingle(), ab.GetProperty("bottom").GetSingle(), ab.GetProperty("right").GetSingle(), ab.GetProperty("top").GetSingle());

                if (glyphElement.TryGetProperty("planeBounds", out var pb))
                    glyph.PlaneBounds = new Microsoft.Xna.Framework.Vector4(pb.GetProperty("left").GetSingle(), pb.GetProperty("bottom").GetSingle(), pb.GetProperty("right").GetSingle(), pb.GetProperty("top").GetSingle());

                font.Glyphs[c] = glyph;
            }

            return font;
        }
    }
}