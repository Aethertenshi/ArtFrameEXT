using ArtFrame.ArtTypes;
using ArtFrame.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArtFrame
{
    public static class TweenHelper
    {
        internal static List<Tweener> tweenPool = new();

        public static Tweener AddTween(Tweener tweener)
        {
            tweenPool.Add(tweener);
            return tweener;
        }
        public static void Remove(Tweener tweener)
        {
            tweenPool.Remove(tweener);
        }
    }
    public static class TextureHelper
    {
        public static ArtTypes.Color GetAverageColor(Image? texture, int sampleStep = 10)
        {
            if (texture == null) return ArtTypes.Color.Black;

            // Grab the actual struct/value safely
            Image img = texture.Value;

            // Create an array to hold the pixel data
            Microsoft.Xna.Framework.Color[] pixels = new Microsoft.Xna.Framework.Color[img.Width * img.Height];

            // FIX: Wrap the cast in parentheses so it casts BEFORE calling GetData
            ((Microsoft.Xna.Framework.Graphics.Texture2D)img).GetData(pixels);

            long r = 0;
            long g = 0;
            long b = 0;
            int count = 0;

            // Loop through the pixels, skipping by 'sampleStep' for performance
            for (int i = 0; i < pixels.Length; i += sampleStep)
            {
                // Skip fully transparent pixels so they don't muddy the average with black/white
                if (pixels[i].A == 0) continue;

                r += pixels[i].R;
                g += pixels[i].G;
                b += pixels[i].B;
                count++;
            }

            if (count == 0) return ArtTypes.Color.Black;

            // Calculate the average and return your custom Color struct
            return new ArtTypes.Color(
                (byte)(r / count),
                (byte)(g / count),
                (byte)(b / count)
            );
        }
    }
    public static class SpriteHelper
    {
        // Static Reference
        private static Art instance => Art.Instance;
        private static GraphicsDevice graphicsDevice => instance.graphicsDevice;

        // Internal References
        internal static List<ArtObject> objectPool = new();
        internal static Dictionary<string, Image> imagePool = new();

        // Binder Methods
        public class DrawBinder : ArtObject
        {
            public Action<float>? onDraw;
            public Action<float>? onUpdate;

            public override void Update(float dt)
            {
                onUpdate?.Invoke(dt);
            }
            public override void Draw(float dt, ArtTypes.Vector2 v1, ArtTypes.Vector2 v2)
            {
                onDraw?.Invoke(dt);
            }
        }

        // Object Methods
        public static void Add(ArtObject obj)
        {
            objectPool.Add(obj);
        }
        public static void Remove(ArtObject obj)
        {
            objectPool.Remove(obj);
        }

        // Image Methods
        public static Image LoadImage(string imageName)
        {
            if (instance == null || instance.GraphicsDevice == null)
                throw new InvalidOperationException("Art manager must be initialized before loading images.");

            if (imagePool.ContainsKey(imageName))
                return imagePool[imageName];

            throw new Exception($"Texture '{imageName}' not found. Make sure to call UseTexture with the path at least once before using it.");
        }
        public static Image LoadImage(string imageName, string imagePath)
        {
            if (instance == null || instance.GraphicsDevice == null)
                throw new InvalidOperationException("Art manager must be initialized before loading images.");

            if (imagePool.ContainsKey(imageName))
                return imagePool[imageName];

            using FileStream stream = File.OpenRead(imagePath);
            imagePool.Add(imageName, Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(graphicsDevice, stream));

            return imagePool[imageName];
        }
        public static void UnloadImages()
        {
            foreach (Image image in imagePool.Values) image.xnaTexture.Dispose();
            imagePool.Clear();
        }
    }
    public static class GraphicsHelper
    {
        // Static References
        private static Art instance => Art.Instance;
        private static GraphicsDeviceManager graphics => instance.graphics;
        private static GameWindow window => instance.Window;
        private static GraphicsDevice graphicsDevice => instance.graphicsDevice;

        public static RasterizerState? CurrentRasterizerState = null;

        // Frame Rate Control
        internal static float _targetDrawTime = 1.0f / 60.0f;
        internal static float _currentDrawTime = 0.0f;
        internal static BasicEffect _basicEffect = new BasicEffect(instance.graphicsDevice)
        {
            VertexColorEnabled = true,
            View = Matrix.Identity,
            World = Matrix.Identity
        };

        // SpriteBatch Control
        internal static bool _spriteBatchOpen = false;

        // Public Variables
        public static float ScreenHeight => graphicsDevice.Viewport.Height;
        public static float ScreenWidth => graphicsDevice.Viewport.Width;

        // Window Configuration
        public static void ConfigureWindow(int width, int height, string title = "ArtFramework", bool fullscreen = false)
        {
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.IsFullScreen = fullscreen;
            graphics.ApplyChanges();
            window.Title = title;
        }
        public static void SetInputFramerate(int framerate)
        {
            instance.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / framerate);
            instance.IsFixedTimeStep = true;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();
        }
        public static void SetFrameRate(int fps)
        {
            _targetDrawTime = 1.0f / fps;
        }
        public static void SetVSyncMode()
        {
            instance.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = true;
            _targetDrawTime = 0f;
            graphics.ApplyChanges();
        }
        public static void SetPerformanceMode(int pollingRate, int fps)
        {
            instance.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / pollingRate);
            instance.IsFixedTimeStep = true;
            graphics.SynchronizeWithVerticalRetrace = false;
            _targetDrawTime = 1.0f / fps;
            graphics.ApplyChanges();
        }

        // Draw Helpers
        public static ArtTypes.Vector2 GetAnchorOffset(AnchorX anchorX, AnchorY anchorY, ArtTypes.Vector2 size)
        {
            // 1. Calculate X Offset independently
            float offsetX = 0;
            if (anchorX == AnchorX.Center) offsetX = size.X / 2f;
            else if (anchorX == AnchorX.Right) offsetX = size.X;

            // 2. Calculate Y Offset independently
            float offsetY = 0;
            if (anchorY == AnchorY.Center) offsetY = size.Y / 2f;
            else if (anchorY == AnchorY.Bottom) offsetY = size.Y;

            return new ArtTypes.Vector2(offsetX, offsetY);
        }

        // SpriteBatch Helpers
        internal static void StartBatch(Effect? effect = null)
        {
            if (_spriteBatchOpen) instance.spriteBatch.End();
            instance.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, CurrentRasterizerState, effect);
            _spriteBatchOpen = true;
        }
        internal static void CloseBatch()
        {
            if (!_spriteBatchOpen) return;
            instance.spriteBatch.End();
            _spriteBatchOpen = false;
        }

        // Primitive Functions
        internal static void DrawRectangle(float x, float y, float width, float height, ArtTypes.Color color) => instance.spriteBatch.Draw(instance.pixel, new ArtTypes.Rectangle(x, y, width, height), color);
        internal static void DrawRectanglePro(ArtTypes.Vector2 position, ArtTypes.Vector2 size, ArtTypes.Vector2 origin, float rotation, ArtTypes.Color color)
        {
            // Because we are stretching a 1x1 pixel to 'size', 
            // the origin needs to be normalized (0.0 to 1.0) relative to the 1x1 texture.
            ArtTypes.Vector2 normalizedOrigin = new ArtTypes.Vector2(
                size.X == 0 ? 0 : origin.X / size.X,
                size.Y == 0 ? 0 : origin.Y / size.Y
            );

            instance.spriteBatch.Draw(
                instance.pixel,
                position,
                null, // source rectangle
                color,
                rotation,
                normalizedOrigin,
                size, // use size as scale since the texture is 1x1
                SpriteEffects.None,
                0f
            );
        }
        public static void DrawRing(ArtTypes.Vector2 center, float innerRadius, float outerRadius, float startAngle, float endAngle, int segments, ArtTypes.Color color)
        {
            CloseBatch();

            float startRad = MathHelper.ToRadians(startAngle);
            float endRad = MathHelper.ToRadians(endAngle);
            float step = (endRad - startRad) / segments;

            var verts = new VertexPositionColor[segments * 6];
            int idx = 0;

            for (int i = 0; i < segments; i++)
            {
                float a0 = startRad + i * step;
                float a1 = a0 + step;

                var io = new Vector3(center.X + MathF.Cos(a0) * innerRadius, center.Y + MathF.Sin(a0) * innerRadius, 0);
                var oo = new Vector3(center.X + MathF.Cos(a0) * outerRadius, center.Y + MathF.Sin(a0) * outerRadius, 0);
                var i1 = new Vector3(center.X + MathF.Cos(a1) * innerRadius, center.Y + MathF.Sin(a1) * innerRadius, 0);
                var o1 = new Vector3(center.X + MathF.Cos(a1) * outerRadius, center.Y + MathF.Sin(a1) * outerRadius, 0);

                // First triangle
                verts[idx++] = new VertexPositionColor(oo, color);
                verts[idx++] = new VertexPositionColor(io, color);
                verts[idx++] = new VertexPositionColor(o1, color);

                // Second triangle
                verts[idx++] = new VertexPositionColor(o1, color);
                verts[idx++] = new VertexPositionColor(io, color);
                verts[idx++] = new VertexPositionColor(i1, color);
            }

            // 1. Crucial Effect Setups
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, ScreenWidth, ScreenHeight, 0, 0, 1);
            _basicEffect.VertexColorEnabled = true; // MUST be true to render the 'color' parameter

            // 2. Crucial State Management 
            // Save the old state so we don't break whatever SpriteBatch expects later
            RasterizerState oldRasterizerState = instance.graphicsDevice.RasterizerState;

            // Disable culling so winding order (CW vs CCW) doesn't matter
            instance.graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                instance.graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, segments * 2);
            }

            // 3. Restore the state
            instance.graphicsDevice.RasterizerState = oldRasterizerState;

            StartBatch();
        }
    }
}