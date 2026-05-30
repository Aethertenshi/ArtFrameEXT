using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static ArtFrame.EffectsHelper;
using static ArtFrame.InputHelper;

namespace ArtFrame
{
    // Interfaces
    public interface IArt
    {
        void Setup();
        void Update(float dt) { }
        void ManualDraw(float dt) { }
    }
    public interface IArtHelper
    {
        void Update(float dt) { }
        void Draw(float dt) { }
    }
    
    // Parent Class
    public class ArtObject
    {
        public ArtTypes.UDim2 position { get; set; }
        public ArtTypes.UDim2 size { get; set; }
        public ArtTypes.AnchorX anchorX { get; set; }
        public ArtTypes.AnchorY anchorY { get; set; }

        public virtual void Draw(float dt, ArtTypes.Vector2 parentSize, ArtTypes.Vector2 parentOrigin) { }
        public virtual void Update(float dt) { }

        public ArtTypes.Vector2 GetResolvedSize(ArtTypes.Vector2 parentSize) => size.Resolve(parentSize);
    }

    //public interface IArtObject
    //{
    //    // Interface Variables
    //    ArtTypes.UDim2 position { get; set; }
    //    ArtTypes.UDim2 size { get; set; }
    //    ArtTypes.AnchorX anchorX { get; set; }
    //    ArtTypes.AnchorY anchorY { get; set; }

    //    // Interface Methods
    //    void Draw(float dt, ArtTypes.Vector2 parentSize, ArtTypes.Vector2 parentOrigin);
    //    void Update(float dt) { }

    //    // 
    //    ArtTypes.Vector2 GetResolvedSize(ArtTypes.Vector2 parentSize) => size.Resolve(parentSize);
    //}
    public interface IFrameModifier
    {
        void Apply(List<ArtObject> children, ArtTypes.Vector2 frameSize);
    }

    // Entry Point
    public static class Engine
    {
        public static void Run<T>() where T : IArt, new()
        {
            var userLogic = new T();
            using (var game = new Art(userLogic))
            {
                game.Run();
            }
        }

        public static void Exit()
        {
            Art.Instance?.Exit();
        }
    }

    internal class Art : Game
    {
        // Internal References
        internal static Art Instance { get; private set; }
        internal GraphicsDeviceManager graphics { get; private set; }
        internal SpriteBatch spriteBatch { get; private set; }
        internal GraphicsDevice graphicsDevice { get; private set; }
        internal Texture2D? pixel { get; private set; } = null;
        // Private References
        private IArt art;

        // Timing Counters (FPS / UPS / Polling Rate)
        private int _updateCount = 0;
        private int _drawCount = 0;
        private float _counterElapsed = 0f;
        private float _currentFps = 0f;
        private float _currentUps = 0f;

        // Draw Suppression Timing
        private System.Reflection.FieldInfo? _accumulatorField;
        private double _drawAccumulator = 0.0;

        // Text Input
        private int _textInputRefCount = 0;

        internal void RegisterTextInput(Action<char> textInput)
        {
            if (_textInputRefCount == 0)
                Microsoft.Xna.Framework.Input.TextInputEXT.StartTextInput();
            _textInputRefCount++;
            Microsoft.Xna.Framework.Input.TextInputEXT.TextInput += textInput;
        }

        internal void unRegisterTextInput(Action<char> textInput)
        {
            Microsoft.Xna.Framework.Input.TextInputEXT.TextInput -= textInput;
            _textInputRefCount = Math.Max(0, _textInputRefCount - 1);
            if (_textInputRefCount == 0)
                Microsoft.Xna.Framework.Input.TextInputEXT.StopTextInput();
        }

        // Constructor
        public Art(IArt art)
        {
            Instance = this;
            this.art = art;
            graphics = new GraphicsDeviceManager(this);
            graphics.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };
            Content.RootDirectory = ".";
            IsMouseVisible = true;

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        // Protected Methods
        protected override void Initialize()
        {
            _accumulatorField = typeof(Game).GetField("accumulator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            graphicsDevice = GraphicsDevice; // ← moved here, now valid
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = ArtTypes.Texture2D.CreateSinglePixel(Color.White);
            AudioHelper.UseAudioEngine();
            FontHelper.LoadFontShader();
            
            // Loading Basic Effects
            SetupEffects();

            art.Setup();
            base.Initialize();
        }

        // Standart FNA Game Loop
        protected override void Update(GameTime gameTime)
        {
            _updateCount++;
            _counterElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_counterElapsed >= 0.25f)
            {
                _currentUps = _updateCount / _counterElapsed;
                _currentFps = _drawCount / _counterElapsed;
                _updateCount = 0;
                _drawCount = 0;
                _counterElapsed = 0f;
            }

            // Draw Suppression (using reflection to safely detect the final update step of the frame)
            bool isLastUpdate = true;
            if (IsFixedTimeStep && _accumulatorField != null)
            {
                TimeSpan accumulatorValue = (TimeSpan)_accumulatorField.GetValue(this)!;
                isLastUpdate = accumulatorValue < TargetElapsedTime;
            }

            if (isLastUpdate)
            {
                if (GraphicsHelper._targetDrawTime > 0f)
                {
                    _drawAccumulator += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_drawAccumulator > 0.5)
                    {
                        _drawAccumulator = 0.0;
                    }

                    if (_drawAccumulator < GraphicsHelper._targetDrawTime)
                    {
                        SuppressDraw();
                    }
                    else
                    {
                        _drawAccumulator -= GraphicsHelper._targetDrawTime;
                    }
                }
            }
            else
            {
                // Suppress intermediate updates to prevent double-suppression bugs
                SuppressDraw();
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            InputManager.Update();

            // Object Pool Update
            if (SpriteHelper.objectPool.Count > 0)
            {
                foreach (var obj in SpriteHelper.objectPool)
                    obj.Update(dt);
            }

            // Helper Pool Update
            if (RythmHelper.helperPool.Count > 0)
            {
                foreach (var helper in RythmHelper.helperPool)
                    helper?.Update(dt);
            }


            // Tween Pool Update
            if (TweenHelper.tweenPool.Count > 0)
            {
                foreach (var tween in TweenHelper.tweenPool)
                    tween.Update(dt);
            }

            // Update Logic
            art.Update(dt);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _drawCount++;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            GraphicsHelper.StartBatch(null);
                GraphicsDevice.Clear(Color.Black);

                if (SpriteHelper.objectPool.Count > 0)
                {
                    foreach (var obj in SpriteHelper.objectPool)
                        obj.Draw(dt, new ArtTypes.Vector2(GraphicsHelper.ScreenWidth, GraphicsHelper.ScreenHeight), Vector2.Zero);
                }

                art.ManualDraw(dt);
                
                // Helper Pool Update
                if (RythmHelper.helperPool.Count > 0)
                {
                    foreach (var helper in RythmHelper.helperPool)
                        helper.Draw(dt);
                }

                // Draw FPS / Polling Rate Counter in the bottom-left
                string counterText = $"FPS: {_currentFps:0} | Polling Rate: {_currentUps:0}";
                FontHelper.DrawTextPro(
                    "gsans",
                    counterText,
                    new ArtTypes.Vector2(20f, GraphicsHelper.ScreenHeight - 35f),
                    new ArtTypes.Vector2(0f, 0f),
                    0f,
                    15f, // scale
                    new ArtTypes.Color(255, 255, 255, 255) // Slightly transparent white
                );

            GraphicsHelper.CloseBatch();

            base.Draw(gameTime);
        }
        protected override void OnExiting(object sender, EventArgs args)
        {
            SpriteHelper.UnloadImages();
            AudioHelper.AudioCleanup();
            base.OnExiting(sender, args);
        }
    }
}