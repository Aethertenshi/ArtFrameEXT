using ManagedBass.Fx;
using Microsoft.Xna.Framework;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace ArtFrame.ArtTypes
{
    public enum AnchorX { Left, Center, Right }
    public enum AnchorY { Top, Center, Bottom }
    public enum ObjectFit { Fill, Contain, None, Cover }

    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public Color(string hex)
        {
            hex = hex.TrimStart('#');
            R = Convert.ToByte(hex.Substring(0, 2), 16);
            G = Convert.ToByte(hex.Substring(2, 2), 16);
            B = Convert.ToByte(hex.Substring(4, 2), 16);
            A = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
        }

        public static Color LerpColor(Color a, Color b, float t)
        {
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t)
            );
        }
        public static Color Fade(Color color, float alpha) => new Color(color.R, color.G, color.B, (byte)Math.Clamp(alpha * 255f, 0, 255));

        public static Color operator *(Color a, byte b) => new Color(a.R, a.G, a.B, (byte)Math.Clamp(a.A * b, 0, 255));
        public static Color operator *(Color a, float b) => new Color(a.R, a.G, a.B, (byte)Math.Clamp(a.A * b, 0, 255));

        public static Color White => new Color(255, 255, 255);
        public static Color Black => new Color(0, 0, 0);
        public static Color Blue => new Color(0, 0, 255);

        public static implicit operator Microsoft.Xna.Framework.Color(Color color)
        {
            return new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
        }
        public static implicit operator Color(Microsoft.Xna.Framework.Color c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }
    }

    public class ArtMathHelper()
    {
        public static float Clamp(float value, float min, float max) => MathHelper.Clamp(value, min, max);
        public static float Lerp(float start, float end, float amount) => MathHelper.Lerp(start, end, amount);
    }

    public struct UDim2
    {
        public float ScaleX, ScaleY;
        public float OffsetX, OffsetY;

        public UDim2(float scaleX, float scaleY)
        {
            ScaleX = scaleX; ScaleY = scaleY;
            OffsetX = 0; OffsetY = 0;
        }
        public UDim2(float scaleX, float scaleY, float offsetX, float offsetY)
        {
            ScaleX = scaleX; ScaleY = scaleY;
            OffsetX = offsetX; OffsetY = offsetY;
        }

        public override string ToString()
        {
            return $"UDim2(ScaleX: {ScaleX}, ScaleY: {ScaleY}, OffsetX: {OffsetX}, OffsetY: {OffsetY})";
        }
        public Vector2 Resolve(Vector2 parentSize) => new Vector2(
            parentSize.X * ScaleX + OffsetX,
            parentSize.Y * ScaleY + OffsetY
        );

        // Operator Overloads
        public static UDim2 operator +(UDim2 a, UDim2 b) => new UDim2(a.ScaleX + b.ScaleX, a.ScaleY + b.ScaleY, a.OffsetX + b.OffsetX, a.OffsetY + b.OffsetY);
        public static UDim2 operator -(UDim2 a, UDim2 b) => new UDim2(a.ScaleX - b.ScaleX, a.ScaleY - b.ScaleY, a.OffsetX - b.OffsetX, a.OffsetY - b.OffsetY);
        public static UDim2 operator *(UDim2 a, float scalar) => new UDim2(a.ScaleX * scalar, a.ScaleY * scalar, a.OffsetX * scalar, a.OffsetY * scalar);
        public static UDim2 operator /(UDim2 a, float scalar) => new UDim2(a.ScaleX / scalar, a.ScaleY / scalar, a.OffsetX / scalar, a.OffsetY / scalar);

        // Convenience shorthands
        public static UDim2 Lerp(UDim2 a, UDim2 b, float t) => new UDim2(
            MathHelper.Lerp(a.ScaleX, b.ScaleX, t),
            MathHelper.Lerp(a.ScaleY, b.ScaleY, t),
            MathHelper.Lerp(a.OffsetX, b.OffsetX, t),
            MathHelper.Lerp(a.OffsetY, b.OffsetY, t)
        );
        public static UDim2 FromOffset(float x, float y) => new UDim2(0, 0, x, y);
        public static UDim2 FromScale(float x, float y) => new UDim2(x, y, 0, 0);
    }

    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        // Operator Overloads
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float scalar) => new Vector2(a.X * scalar, a.Y * scalar);
        public static Vector2 operator /(Vector2 a, float scalar) => new Vector2(a.X / scalar, a.Y / scalar);
        public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2 a, Vector2 b) => a.X != b.X || a.Y != b.Y;


        // Methods
        public float Length() => (float)Math.Sqrt(X * X + Y * Y);
        public Vector2 Normalize()
        {
            float length = Length();
            return length > 0 ? this / length : new Vector2(0, 0);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
        public static float Distance(Vector2 value1, Vector2 value2)
        {
            float diffX = value1.X - value2.X;
            float diffY = value1.Y - value2.Y;

            // Math.Sqrt requires a double, so we cast the final result back to float
            return (float)Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        public static Vector2 Lerp(Vector2 v1, Vector2 v2, float amount) => Microsoft.Xna.Framework.Vector2.Lerp(v1, v2, amount);

        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1f, 1f);
        public static Vector2 UnitX => new Vector2(1f, 0f);
        public static Vector2 UnitY => new Vector2(0f, 1f);

        public static implicit operator Microsoft.Xna.Framework.Vector2(Vector2 t)
        {
            return new Microsoft.Xna.Framework.Vector2(t.X, t.Y);
        }
        public static implicit operator Vector2(Microsoft.Xna.Framework.Vector2 t)
        {
            return new Vector2(t.X, t.Y);
        }
    }

    public struct Image
    {
        internal Microsoft.Xna.Framework.Graphics.Texture2D xnaTexture;

        public int Width => xnaTexture.Width;
        public int Height => xnaTexture.Height;

        public static implicit operator Microsoft.Xna.Framework.Graphics.Texture2D(Image t)
        {
            return t.xnaTexture;
        }
        public static implicit operator Image(Microsoft.Xna.Framework.Graphics.Texture2D t)
        {
            return new Image { xnaTexture = t };
        }
    }

    public struct Texture2D
    {
        internal Microsoft.Xna.Framework.Graphics.Texture2D xnaTexture;

        public int Width => xnaTexture.Width;
        public int Height => xnaTexture.Height;

        public static Texture2D CreateSinglePixel(Color color)
        {
            var tex = new Microsoft.Xna.Framework.Graphics.Texture2D(Art.Instance.GraphicsDevice, 1, 1);

            // Map your custom Color to XNA Color if they are different
            tex.SetData(new[] { (Microsoft.Xna.Framework.Color)color });

            return new Texture2D { xnaTexture = tex };
        }

        public static implicit operator Microsoft.Xna.Framework.Graphics.Texture2D(Texture2D t)
        {
            return t.xnaTexture;
        }
        public static implicit operator Texture2D(Microsoft.Xna.Framework.Graphics.Texture2D t)
        {
            return new Texture2D { xnaTexture = t };
        }
    }

    public struct Rectangle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float Top => Y;
        public float Left => X;
        public float Bottom => Y + Height;
        public float Right => X + Width;

        public Vector2 Center => new Vector2(X + Width / 2f, Y + Height / 2f);
        public Rectangle(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }
        public bool Contains(float x, float y)
        {
            return x >= Left && x <= Right && y >= Top && y <= Bottom;
        }
        public bool Contains(Microsoft.Xna.Framework.Point point)
        {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }
        public static Rectangle Intersect(Rectangle value1, Rectangle value2)
        {
            float newLeft = Math.Max(value1.X, value2.X);
            float newTop = Math.Max(value1.Y, value2.Y);
            float newRight = Math.Min(value1.X + value1.Width, value2.X + value2.Width);
            float newBottom = Math.Min(value1.Y + value1.Height, value2.Y + value2.Height);

            if (newRight > newLeft && newBottom > newTop)
            {
                return new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop);
            }
            return new Rectangle(0, 0, 0, 0);
        }

        public static implicit operator Microsoft.Xna.Framework.Rectangle(Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
        public static implicit operator Rectangle(Microsoft.Xna.Framework.Rectangle r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }

        public static Rectangle Empty => new Rectangle(0, 0, 0, 0);
    }
}
