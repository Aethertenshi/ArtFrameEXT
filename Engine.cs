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
    }

    internal class Art : Game
    {
        // Internal References
        internal static Art Instance { get; private set; }
        internal GraphicsDeviceManager graphics { get; private set; }
        internal SpriteBatch spriteBatch { get; private set; }
        internal GraphicsDevice graphicsDevice { get; private set; }
        internal Texture2D pixel { get; private set; }

        // Private References
        private IArt art;

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
        }

        // Protected Methods
        protected override void Initialize()
        {
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
            float dt = (float)TargetElapsedTime.TotalSeconds;

            // Draw Suppression
            InputManager.Update();
            GraphicsHelper._currentDrawTime += dt;
            if (GraphicsHelper._currentDrawTime < GraphicsHelper._targetDrawTime)
                SuppressDraw();
            else
                GraphicsHelper._currentDrawTime -= GraphicsHelper._targetDrawTime;

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
                    helper.Update(dt);
            }


            // Tween Pool Update
            if (TweenHelper.tweenPool.Count > 0)
            {
                foreach (var tween in TweenHelper.tweenPool)
                    tween.Update(dt);
            }

            if (Keyboard.IsKeyPressed(Keys.Escape)) Exit();

            // Update Logic
            art.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            float dt = (float)TargetElapsedTime.TotalSeconds;

            GraphicsHelper.StartBatch(null);
                GraphicsDevice.Clear(Color.Black);
                art.ManualDraw((float)gameTime.ElapsedGameTime.TotalSeconds);

                if (SpriteHelper.objectPool.Count > 0)
                {
                    foreach (var obj in SpriteHelper.objectPool)
                        obj.Draw(dt, new ArtTypes.Vector2(GraphicsHelper.ScreenWidth, GraphicsHelper.ScreenHeight), Vector2.Zero);
                }

                // Helper Pool Update
                if (RythmHelper.helperPool.Count > 0)
                {
                    foreach (var helper in RythmHelper.helperPool)
                        helper.Draw(dt);
                }
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