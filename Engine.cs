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

    public interface IFrameModifier
    {
        void Apply(List<ArtObject> children, ArtTypes.Vector2 frameSize);
    }

    // Entry Point
    public static class Engine
    {
        // Public References
        public static HighPrecisionLimiter HighPrecisionLimiter { get => Art.Instance._precisionLimiter; private set { Art.Instance._precisionLimiter = value; } }

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
        internal HighPrecisionLimiter _precisionLimiter = new HighPrecisionLimiter();

        // Private References
        private IArt art;

        // --- Performance Monitor Circular Buffers (Logic & Render) ---
        private float[] _updateTimeHistory = new float[150];
        private float[] _drawTimeHistory = new float[150];
        private float[] _inputTimeHistory = new float[150];
        private int _historyIndex = 0;
        private System.Diagnostics.Stopwatch _updateTimer = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _drawTimer = new System.Diagnostics.Stopwatch();
        private float _lastRecordedUpdateMs = 0f;
        private long _lastInputTimestamp = 0;

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
                e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };
            Content.RootDirectory = ".";
            IsMouseVisible = true;

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
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

            RealTimeInputEngine.Start();

            art.Setup();
            base.Initialize();
        }

        // Standart FNA Game Loop
        protected override void Update(GameTime gameTime)
        {
            _updateTimer.Restart();

            // 1. Keep your performance monitoring metrics
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

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 2. Core Subsystem Updates
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

            // 3. Game Logic Execution
            art.Update(dt);
            base.Update(gameTime);

            _updateTimer.Stop();
            _lastRecordedUpdateMs = (float)_updateTimer.Elapsed.TotalMilliseconds;

            long currentInputTimestamp = RealTimeInputEngine.LatestTimestampMs;
            float currentInputDeltaMs = _lastInputTimestamp == 0 ? 1.11f : (float)(currentInputTimestamp - _lastInputTimestamp);
            _lastInputTimestamp = currentInputTimestamp;

            float prevDrawMs = (float)_drawTimer.Elapsed.TotalMilliseconds;

            if (GraphicsHelper.ShowPerformanceTelemetry)
            {
                _inputTimeHistory[_historyIndex] = currentInputDeltaMs;
                _updateTimeHistory[_historyIndex] = _lastRecordedUpdateMs;
                _drawTimeHistory[_historyIndex] = prevDrawMs;
                _historyIndex = (_historyIndex + 1) % 150;
            }

            // 4. Force your high-precision limiter to run at the absolute end of the cycle
            _precisionLimiter.Wait();
        }

        protected override void Draw(GameTime gameTime)
        {
            _drawTimer.Restart();
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

                // If performance telemetry is enabled, draw the graphs
                if (GraphicsHelper.ShowPerformanceTelemetry)
                {
                    DrawPerformanceGraph();
                }
                else
                {
                    // Draw FPS / Polling Rate Counter in the bottom-left
                    string counterText = $"FPS: {_currentFps:0} | Logic: {_currentUps:0} | Input: {RealTimeInputEngine.CurrentHz:0}Hz";
                    FontHelper.DrawTextPro(
                        "gsans",
                        counterText,
                        new ArtTypes.Vector2(20f, GraphicsHelper.ScreenHeight - 35f),
                        new ArtTypes.Vector2(0f, 0f),
                        0f,
                        15f, // scale
                        new ArtTypes.Color(255, 255, 255, 255) // Slightly transparent white
                    );
                }

            GraphicsHelper.CloseBatch();

            base.Draw(gameTime);
            _drawTimer.Stop();
        }

        private void DrawPerformanceGraph()
        {
            float graphWidth = 300f;
            float graphHeight = 60f;
            float graphX = 20f;

            // --- 1. INPUT POLLING / LATENCY GRAPH (Top) ---
            float inputGraphY = GraphicsHelper.ScreenHeight - 280f;
            GraphicsHelper.DrawRectangle(graphX, inputGraphY, graphWidth, graphHeight, new ArtTypes.Color(15, 15, 15, 180));

            float inputMaxMs = 2.5f; // 2.5ms full scale
            // Grid at 1.11ms (900 Hz Background Thread Target)
            float inputGridY900 = (inputGraphY + graphHeight) - Math.Clamp((1.111f / inputMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, inputGridY900, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 40));
            // Grid at 2.22ms (450 Hz Jitter Threshold)
            float inputGridY450 = (inputGraphY + graphHeight) - Math.Clamp((2.222f / inputMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, inputGridY450, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 25));

            // --- 2. DRAW / RENDER GRAPH (Middle) ---
            float drawGraphY = GraphicsHelper.ScreenHeight - 210f;
            GraphicsHelper.DrawRectangle(graphX, drawGraphY, graphWidth, graphHeight, new ArtTypes.Color(15, 15, 15, 180));

            float drawMaxMs = 8.33f; // 120 FPS full scale
            float drawGridY400 = (drawGraphY + graphHeight) - Math.Clamp((2.5f / drawMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, drawGridY400, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 30));
            float drawGridY200 = (drawGraphY + graphHeight) - Math.Clamp((5.0f / drawMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, drawGridY200, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 30));

            // --- 3. UPDATE / LOGIC GRAPH (Bottom) ---
            float updateGraphY = GraphicsHelper.ScreenHeight - 140f;
            GraphicsHelper.DrawRectangle(graphX, updateGraphY, graphWidth, graphHeight, new ArtTypes.Color(15, 15, 15, 180));

            float updateMaxMs = 2.5f; // 2.5ms full scale
            float updateGridY1200 = (updateGraphY + graphHeight) - Math.Clamp((0.833f / updateMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, updateGridY1200, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 30));
            float updateGridY600 = (updateGraphY + graphHeight) - Math.Clamp((1.666f / updateMaxMs) * graphHeight, 0f, graphHeight);
            GraphicsHelper.DrawRectangle(graphX, updateGridY600, graphWidth, 1f, new ArtTypes.Color(255, 255, 255, 30));

            // Render raw historical bars
            int index = _historyIndex;
            for (int i = 0; i < 150; i++)
            {
                float inputMs = _inputTimeHistory[index];
                float drawMs = _drawTimeHistory[index];
                float updateMs = _updateTimeHistory[index];
                index = (index + 1) % 150;

                float barX = graphX + (i * 2f);

                // A. Draw Input Graph Bar (Purple/Magenta theme for hardware processing)
                float inputBarHeight = Math.Clamp((inputMs / inputMaxMs) * graphHeight, 1f, graphHeight);
                float inputBarY = (inputGraphY + graphHeight) - inputBarHeight;
                ArtTypes.Color inputColor = inputMs < 1.15f ? new ArtTypes.Color(168, 85, 247, 220) :  // Vibrant Purple (Perfect 900Hz execution)
                                   inputMs < 2.23f ? new ArtTypes.Color(250, 204, 21, 220) :  // Yellow (Minor Jitter)
                                                     new ArtTypes.Color(248, 113, 113, 255); // Red (Thread Stall)
                GraphicsHelper.DrawRectangle(barX, inputBarY, 2f, inputBarHeight, inputColor);

                // B. Draw Render Graph Bar
                float drawBarHeight = Math.Clamp((drawMs / drawMaxMs) * graphHeight, 1f, graphHeight);
                float drawBarY = (drawGraphY + graphHeight) - drawBarHeight;
                ArtTypes.Color drawColor = drawMs < 2.6f ? new ArtTypes.Color(74, 222, 128, 220) :
                                  drawMs < 5.0f ? new ArtTypes.Color(250, 204, 21, 220) :
                                                  new ArtTypes.Color(248, 113, 113, 255);
                GraphicsHelper.DrawRectangle(barX, drawBarY, 2f, drawBarHeight, drawColor);

                // C. Draw Update Graph Bar
                float updateBarHeight = Math.Clamp((updateMs / updateMaxMs) * graphHeight, 1f, graphHeight);
                float updateBarY = (updateGraphY + graphHeight) - updateBarHeight;
                ArtTypes.Color updateColor = updateMs < 0.84f ? new ArtTypes.Color(96, 165, 250, 220) :
                                    updateMs < 1.67f ? new ArtTypes.Color(250, 204, 21, 220) :
                                                       new ArtTypes.Color(248, 113, 113, 255);
                GraphicsHelper.DrawRectangle(barX, updateBarY, 2f, updateBarHeight, updateColor);
            }

            // Draw outlines/borders
            DrawGraphOutline(graphX, inputGraphY, graphWidth, graphHeight);
            DrawGraphOutline(graphX, drawGraphY, graphWidth, graphHeight);
            DrawGraphOutline(graphX, updateGraphY, graphWidth, graphHeight);

            // Technical text labels
            FontHelper.DrawTextPro("gsans_bold", "INPUT POLLING / LATENCY (ms)", new ArtTypes.Vector2(graphX + 5f, inputGraphY + 5f), new ArtTypes.Vector2(0f, 0f), 0f, 9.7f, new ArtTypes.Color(220, 220, 220, 180));
            FontHelper.DrawTextPro("gsans_bold", "DRAW / RENDER (ms)", new ArtTypes.Vector2(graphX + 5f, drawGraphY + 5f), new ArtTypes.Vector2(0f, 0f), 0f, 9.7f, new ArtTypes.Color(220, 220, 220, 180));
            FontHelper.DrawTextPro("gsans_bold", "UPDATE / LOGIC (ms)", new ArtTypes.Vector2(graphX + 5f, updateGraphY + 5f), new ArtTypes.Vector2(0f, 0f), 0f, 9.7f, new ArtTypes.Color(220, 220, 220, 180));

            // Also draw the numerical summary text at the bottom left
            string counterText = $"FPS: {_currentFps:0} | Logic: {_currentUps:0} | Input: {RealTimeInputEngine.CurrentHz:0}Hz";
            FontHelper.DrawTextPro(
                "gsans",
                counterText,
                new ArtTypes.Vector2(20f, GraphicsHelper.ScreenHeight - 35f),
                new ArtTypes.Vector2(0f, 0f),
                0f,
                15f, // scale
                new ArtTypes.Color(255, 255, 255, 255)
            );
        }

        private void DrawGraphOutline(float x, float y, float w, float h)
        {
            GraphicsHelper.DrawRectangle(x, y, w, 1f, new ArtTypes.Color(80, 80, 80, 120));
            GraphicsHelper.DrawRectangle(x, y + h - 1f, w, 1f, new ArtTypes.Color(80, 80, 80, 120));
            GraphicsHelper.DrawRectangle(x, y, 1f, h, new ArtTypes.Color(80, 80, 80, 120));
            GraphicsHelper.DrawRectangle(x + w - 1f, y, 1f, h, new ArtTypes.Color(80, 80, 80, 120));
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            RealTimeInputEngine.Stop();
            SpriteHelper.UnloadImages();
            AudioHelper.AudioCleanup();
            base.OnExiting(sender, args);
        }
    }
}