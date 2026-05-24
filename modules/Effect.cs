using ArtFrame.UserInterface;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using ArtFrame.ArtTypes;

namespace ArtFrame
{
    public static class EffectsHelper
    {
        // Initialize empty immediately to prevent NullReference on the Dictionary itself
        public static Dictionary<string, Effect> LoaddedEffects { get; set; } = new Dictionary<string, Effect>();

        public static void SetupEffects()
        {
            string shaderPath = Path.Combine(Art.Instance.Content.RootDirectory, "shaders/gaussian.fxb");

            if (!File.Exists(shaderPath))
            {
                throw new FileNotFoundException($"Could not find the raw compiled shader binary at: {shaderPath}");
            }

            // Read the compiled raw byte array directly from disk
            byte[] shaderCode = File.ReadAllBytes(shaderPath);

            // Pass the raw byte array directly to the Graphics Device
            LoaddedEffects["gaussian"] = new Effect(Art.Instance.graphicsDevice, shaderCode);
        }
    }
}

namespace ArtFrame.Effects
{
    /// <summary>
    /// Base interface for all custom shader wrappers.
    /// </summary>
    /// 

    public interface IArtEffect : IDisposable
    {
        /// <summary>
        /// Takes the raw rendered children, applies the shader passes, and draws to the screen.
        /// </summary>
        /// <param name="content">The texture containing the pre-rendered children.</param>
        /// <param name="destination">Where on the screen it should be drawn.</param>
        /// <param name="tint">The master color/alpha tint.</param>
        void DrawEffect(Texture2D content, Rectangle destination, Color tint);
    }

    public class GaussianBlurEffect : IArtEffect
    {
        private Effect _shader;
        private RenderTarget2D? _pass1Target;

        // Developer-friendly properties!
        public float BlurAmount { get; set; } = 2f;

        // Use a property to fetch the shader lazily
        private Effect Shader
        {
            get
            {
                if (_shader == null)
                {
                    if (!EffectsHelper.LoaddedEffects.ContainsKey("gaussian"))
                    {
                        throw new InvalidOperationException("EffectsHelper has not loaded the 'gaussian' shader yet. Make sure SetupEffects() was called.");
                    }
                    _shader = EffectsHelper.LoaddedEffects["gaussian"];
                }
                return _shader;
            }
        }

        public void DrawEffect(Texture2D content, Rectangle destination, Color tint)
        {
            var gd = Art.Instance.graphicsDevice;
            var sb = Art.Instance.spriteBatch;

            // 1. Maintain the intermediate RenderTarget for the 2-pass blur
            if (_pass1Target == null || _pass1Target.Width != content.Width || _pass1Target.Height != content.Height)
            {
                _pass1Target?.Dispose();
                _pass1Target = new RenderTarget2D(gd, content.Width, content.Height, false, SurfaceFormat.Color, DepthFormat.None);
            }

            // Standardize shader parameters (assuming standard MonoGame Blur implementation)
            Shader.Parameters["BlurAmount"]?.SetValue(BlurAmount);

            // --- PASS 1: Horizontal Blur ---
            GraphicsHelper.CloseBatch();
            var previousTargets = gd.GetRenderTargets(); // Save screen
            gd.SetRenderTarget(_pass1Target);
            gd.Clear(Color.Transparent);

            // Tell shader to blur horizontally based on texture width
            Shader.Parameters["TexelSize"]?.SetValue(new Vector2(1f / content.Width, 0));

            GraphicsHelper.StartBatch(Shader);
            sb.Draw(content, new Rectangle(0, 0, content.Width, content.Height), Color.White);
            GraphicsHelper.CloseBatch();

            // --- PASS 2: Vertical Blur ---
            gd.SetRenderTargets(previousTargets); // Restore screen

            // Tell shader to blur vertically based on texture height
            Shader.Parameters["TexelSize"]?.SetValue(new Vector2(0, 1f / content.Height));

            GraphicsHelper.StartBatch(Shader);
            // Draw the result of Pass 1 onto the screen, applying Pass 2!
            sb.Draw(_pass1Target, destination, tint);
            // Note: We don't CloseBatch() here because EffectFrame's Draw loop expects it to remain open!

            GraphicsHelper.CloseBatch();
            GraphicsHelper.StartBatch(null);
        }

        public void Dispose()
        {
            _pass1Target?.Dispose();
        }
    }
}