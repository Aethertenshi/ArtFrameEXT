using ArtFrame;
using ArtFrame.ArtTypes;
using ArtFrame.Easings;
using ArtFrame.UIModifier;

using static ArtFrame.InputHelper;

namespace ArtFrame.UserInterface
{

    // Frames
    public class Frame : ArtObject
    {
        public float rotation { get; set; } = 0f;
        public float alpha { get; set; } = 1f;
        public Color color { get; set; } = Color.White;

        public List<ArtObject> children { get; set; } = new List<ArtObject>();
        public List<IFrameModifier> modifiers { get; set; } = new();

        public Action<Frame, float>? onUpdate { get; set; }

        private static Texture2D? _sharedPixel;
        private Texture2D pixel => _sharedPixel ??= Texture2D.CreateSinglePixel(Color.White);

        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);
            foreach (var child in children)
                child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin; // offset by parent's top-left

            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 origin = new Vector2(
                (anchorOffset.X / resolvedSize.X) * pixel.Width,
                (anchorOffset.Y / resolvedSize.Y) * pixel.Height
            );

            Rectangle destRect = new Rectangle(
                (int)resolvedPos.X,
                (int)resolvedPos.Y,
                (int)resolvedSize.X,
                (int)resolvedSize.Y
            );

            Art.Instance.spriteBatch.Draw(
                texture: pixel,
                destinationRectangle: destRect,
                sourceRectangle: null,
                color: color * alpha,
                rotation: rotation,
                origin: origin,
                effects: Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
                layerDepth: 0f
            );

            // Pass this frame's top-left corner as the origin for children
            Vector2 frameTopLeft = resolvedPos - anchorOffset;
            foreach (var mod in modifiers)
                mod.Apply(children, resolvedSize);

            foreach (var child in children)
                child.Draw(dt, resolvedSize, frameTopLeft);
        }
    }

    public class ImageFrame : ArtObject
    {
        public float rotation { get; set; } = 0f;
        public ObjectFit fit { get; set; } = ObjectFit.None;
        public float alpha { get; set; } = 1f;
        public Image texture { get; set; } = new Image();
        public Color color { get; set; } = Color.White;

        public List<ArtObject> children { get; set; } = new List<ArtObject>();
        public List<IFrameModifier> modifiers { get; set; } = new();

        public Action<ImageFrame, float>? onUpdate { get; set; }

        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);
            foreach (var child in children)
                child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize);
            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);

            Vector2 screenTopLeft = parentOrigin + resolvedPos - anchorOffset;
            Vector2 objectCenter = screenTopLeft + resolvedSize / 2f;   // rotation pivot

            Color tint = new Color(color.R, color.G, color.B, (byte)(alpha * 255f));
            float radians = Microsoft.Xna.Framework.MathHelper.ToRadians(rotation);

            if (fit == ObjectFit.Cover)
            {
                Rectangle srcRect = ComputeCoverSrc(new Rectangle(0, 0, (int)resolvedSize.X, (int)resolvedSize.Y));
                Vector2 pivot = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
                Vector2 scale = new Vector2(resolvedSize.X / srcRect.Width, resolvedSize.Y / srcRect.Height);
                Art.Instance.spriteBatch.Draw(texture, objectCenter, srcRect, tint, radians, pivot, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }
            else
            {
                Rectangle destRect = ComputeDestRect(screenTopLeft, resolvedSize);
                Vector2 pivot = new Vector2(texture.Width / 2f, texture.Height / 2f);
                Vector2 scale = new Vector2((float)destRect.Width / texture.Width, (float)destRect.Height / texture.Height);
                Art.Instance.spriteBatch.Draw(texture, objectCenter, null, tint, radians, pivot, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }

            // ✅ Pass screenTopLeft, not frameTopLeft
            foreach (var mod in modifiers) mod.Apply(children, resolvedSize);
            foreach (var child in children) child.Draw(dt, resolvedSize, screenTopLeft);
        }

        private Rectangle ComputeCoverSrc(Rectangle targetRect)
        {
            float targetAspect = (float)targetRect.Width / targetRect.Height;
            float imageAspect = (float)texture.Width / texture.Height;

            float srcX = 0f, srcY = 0f;
            float srcW = texture.Width, srcH = texture.Height;

            if (imageAspect > targetAspect)
            {
                srcW = texture.Height * targetAspect;
                srcX = (texture.Width - srcW) / 2f;
            }
            else
            {
                srcH = texture.Width / targetAspect;
                srcY = (texture.Height - srcH) / 2f;
            }

            return new Rectangle((int)srcX, (int)srcY, (int)srcW, (int)srcH);
        }

        private Rectangle ComputeDestRect(Vector2 origin, Vector2 resolvedSize)
        {
            switch (fit)
            {
                case ObjectFit.Contain:
                    float scale = Math.Min(resolvedSize.X / texture.Width, resolvedSize.Y / texture.Height);
                    int containW = (int)(texture.Width * scale);
                    int containH = (int)(texture.Height * scale);
                    int containX = (int)(origin.X + (resolvedSize.X - containW) / 2f);
                    int containY = (int)(origin.Y + (resolvedSize.Y - containH) / 2f);
                    return new Rectangle(containX, containY, containW, containH);

                case ObjectFit.None:
                    int noneX = (int)(origin.X + (resolvedSize.X - texture.Width) / 2f);
                    int noneY = (int)(origin.Y + (resolvedSize.Y - texture.Height) / 2f);
                    return new Rectangle(noneX, noneY, texture.Width, texture.Height);

                case ObjectFit.Fill:
                default:
                    return new Rectangle((int)origin.X, (int)origin.Y, (int)resolvedSize.X, (int)resolvedSize.Y);
            }
        }
    }

    public class TextFrame : ArtObject
    {
        public AnchorX textAnchorX { get; set; } = AnchorX.Left;
        public AnchorY textAnchorY { get; set; } = AnchorY.Top;
        public float alpha { get; set; } = 1f;
        public float rotation { get; set; } = 0f;

        public string fontName { get; set; } = "";
        public string text { get; set; } = "";
        public float scale { get; set; } = 1f;
        public Color color { get; set; } = Color.White;
        public float strokeWidth { get; set; } = 0f;
        public Color? strokeColor { get; set; } = null;

        // --- NEW BACKGROUND PROPERTIES ---
        public Color? backgroundColor { get; set; } = null;
        public float backgroundAlpha { get; set; } = 1f;
        public float backgroundPadding { get; set; } = 0f;
        public float? backgroundStroke { get; set; } = null;
        public Color backgroundStrokeColor { get; set; } = Color.White;
        // ---------------------------------

        public List<IFrameModifier> modifiers { get; set; } = new();

        public Action<TextFrame, float>? onUpdate { get; set; }

        public override void Update(float dt) => onUpdate?.Invoke(this,dt);

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;
            Vector2 resolvedSize = size.Resolve(parentSize);

            // Get the exact visual bounds and rendering offset
            var (visualOffset, measuredText) = FontHelper.MeasureTextBounds(fontName, text, scale * 10);

            Vector2 bounds = new Vector2(
                resolvedSize.X <= 0 ? measuredText.X : resolvedSize.X,
                resolvedSize.Y <= 0 ? measuredText.Y : resolvedSize.Y
            );

            // Frame anchor — positions the frame relative to parent
            Vector2 frameAnchor = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, bounds);

            // --- DRAW BACKGROUND ---
            if (backgroundStroke.HasValue)
            {
                float bgWidth = bounds.X + (backgroundPadding * 2) + backgroundStroke.Value;
                float bgHeight = bounds.Y + (backgroundPadding * 2) + backgroundStroke.Value;

                float bgX = resolvedPos.X - frameAnchor.X - backgroundPadding - (backgroundStroke.Value / 2);
                float bgY = resolvedPos.Y - frameAnchor.Y - backgroundPadding - (backgroundStroke.Value / 2);

                Color bgColor = backgroundStrokeColor * (backgroundAlpha * alpha);

                GraphicsHelper.DrawRectangle(bgX, bgY, bgWidth, bgHeight, bgColor);
            }
            if (backgroundColor.HasValue)
            {
                float bgWidth = bounds.X + (backgroundPadding * 2);
                float bgHeight = bounds.Y + (backgroundPadding * 2);

                float bgX = resolvedPos.X - frameAnchor.X - backgroundPadding;
                float bgY = resolvedPos.Y - frameAnchor.Y - backgroundPadding;

                Color bgColor = backgroundColor.Value * (backgroundAlpha * alpha);

                GraphicsHelper.DrawRectangle(bgX, bgY, bgWidth, bgHeight, bgColor);
            }
            // -----------------------

            Vector2 drawPos = resolvedPos - frameAnchor;

            // Text anchor — aligns text within the frame bounds
            Vector2 textOrigin = GraphicsHelper.GetAnchorOffset(textAnchorX, textAnchorY, measuredText);

            // Shift drawPos so text lands at the right spot inside the frame
            Vector2 textAnchorShift = GraphicsHelper.GetAnchorOffset(textAnchorX, textAnchorY, bounds);
            drawPos += textAnchorShift;

            // CRITICAL FIX: Add the visual offset to the origin. 
            // This shifts the text so its exact visual center perfectly matches the box center.
            visualOffset += new Vector2(0, -2.5f);
            textOrigin += visualOffset;

            FontHelper.DrawTextPro(
                fontName, text, drawPos, textOrigin, rotation, scale * 10,
                color * alpha,
                strokeWidth,
                strokeColor.HasValue ? strokeColor.Value * alpha : null
            );
        }
    }

    // Buttons
    public class Button : ArtObject
    {
        // — Uniform properties —
        public float alpha { get; set; } = 1f;
        public float rotation { get; set; } = 0f;

        // — Button-specific —
        public Color color { get; set; } = Color.White;
        public Color hoverColor { get; set; } = Color.Black;
        public Color pressedColor { get; set; } = Color.Black;

        // — State —
        public bool IsHovered { get; private set; } = false;
        public bool IsPressed { get; private set; } = false;

        public List<IFrameModifier> modifiers { get; set; } = new();
        public List<ArtObject> children { get; set; } = new List<ArtObject>();

        // — Events —
        public Action<Button>? onUpdate { get; set; }
        public Action<Button>? onClick { get; set; }
        public Action<Button>? onHoverEnter { get; set; }
        public Action<Button>? onHoverExit { get; set; }

        private Rectangle _hitbox;
        private static Texture2D? _sharedPixel;
        private Texture2D _pixel => _sharedPixel ??= Texture2D.CreateSinglePixel(Color.White);

        public override void Update(float dt)
        {
            bool wasHovered = IsHovered;
            IsHovered = _hitbox.Contains(Mouse.Position.X, Mouse.Position.Y);
            IsPressed = IsHovered && Mouse.LeftDown();

            if (IsHovered && !wasHovered) onHoverEnter?.Invoke(this);
            if (!IsHovered && wasHovered) onHoverExit?.Invoke(this);

            // Trigger onClick on release inside hitbox
            if (IsHovered && Mouse.LeftClicked())
            {
                onClick?.Invoke(this);
            }

            foreach (var child in children)
                child.Update(dt);

            onUpdate?.Invoke(this);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;

            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 origin = new Vector2(
                (anchorOffset.X / resolvedSize.X) * _pixel.Width,
                (anchorOffset.Y / resolvedSize.Y) * _pixel.Height
            );

            _hitbox = new Rectangle(
                (int)(resolvedPos.X - anchorOffset.X),
                (int)(resolvedPos.Y - anchorOffset.Y),
                (int)resolvedSize.X,
                (int)resolvedSize.Y
            );

            Color drawColor = IsPressed ? pressedColor : IsHovered ? hoverColor : color;

            Art.Instance.spriteBatch.Draw(
                texture: _pixel,
                destinationRectangle: new Rectangle((int)resolvedPos.X, (int)resolvedPos.Y, (int)resolvedSize.X, (int)resolvedSize.Y),
                sourceRectangle: null,
                color: drawColor * alpha,
                rotation: rotation,
                origin: origin,
                effects: Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
                layerDepth: 0f
            );

            // Pass this frame's top-left corner as the origin for children
            Vector2 frameTopLeft = resolvedPos - anchorOffset;
            foreach (var mod in modifiers)
                mod.Apply(children, resolvedSize);

            foreach (var child in children)
                child.Draw(dt, resolvedSize, frameTopLeft);
        }
    }

    public class ImageButton : ArtObject
    {
        // — Uniform properties —
        public float alpha { get; set; } = 1f;
        public float rotation { get; set; } = 0f;

        // — ImageButton-specific —
        public Color color { get; set; } = Color.White;
        public Image texture { get; set; } = new Image();
        public Image? hoverImage { get; set; } = null;
        public Image? pressedImage { get; set; } = null;
        public ObjectFit fit { get; set; } = ObjectFit.Fill;
        public float hoverAlpha { get; set; } = 1f;
        public float pressedAlpha { get; set; } = 0.7f;

        // — State —
        public bool IsHovered { get; private set; } = false;
        public bool IsPressed { get; private set; } = false;

        public List<ArtObject> children { get; set; } = new List<ArtObject>();
        public List<IFrameModifier> modifiers { get; set; } = new();

        // — Events —
        public Action<ImageButton, float>? onUpdate { get; set; }
        public Action<ImageButton>? onClick { get; set; }
        public Action<ImageButton>? onHoverEnter { get; set; }
        public Action<ImageButton>? onHoverExit { get; set; }

        private Rectangle _hitbox;

        public override void Update(float dt)
        {
            bool wasHovered = IsHovered;
            IsHovered = _hitbox.Contains(Mouse.Position.X, Mouse.Position.Y);
            IsPressed = IsHovered && Mouse.LeftDown();

            if (IsHovered && !wasHovered) onHoverEnter?.Invoke(this);
            if (!IsHovered && wasHovered) onHoverExit?.Invoke(this);

            if (IsHovered && Mouse.LeftClicked())
            {
                onClick?.Invoke(this);
            }

            onUpdate?.Invoke(this, dt);
            foreach (var child in children)
                child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;

            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 topLeft = resolvedPos - anchorOffset;
            Vector2 objectCenter = topLeft + resolvedSize / 2f; // pivot for rotation and scaling

            _hitbox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)resolvedSize.X, (int)resolvedSize.Y);

            Image activeTex = IsPressed ? (pressedImage ?? texture)
                              : IsHovered ? (hoverImage ?? texture)
                              : texture;

            // Fix alpha override: multiply hovered/pressed modifiers by the base alpha
            float activeAlpha = alpha * (IsPressed ? pressedAlpha
                              : IsHovered ? hoverAlpha
                              : 1f);

            Color tint = new Color(color.R, color.G, color.B, (byte)(activeAlpha * 255f));
            float radians = Microsoft.Xna.Framework.MathHelper.ToRadians(rotation);

            if (fit == ObjectFit.Cover)
            {
                Rectangle srcRect = ComputeCoverSrc(new Rectangle(0, 0, (int)resolvedSize.X, (int)resolvedSize.Y), activeTex);
                Vector2 pivot = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
                Vector2 scale = new Vector2(resolvedSize.X / srcRect.Width, resolvedSize.Y / srcRect.Height);
                Art.Instance.spriteBatch.Draw(activeTex, objectCenter, srcRect, tint, radians, pivot, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }
            else
            {
                Rectangle destRect = ComputeDestRect(topLeft, resolvedSize, activeTex);
                Vector2 pivot = new Vector2(activeTex.Width / 2f, activeTex.Height / 2f);
                Vector2 scale = new Vector2((float)destRect.Width / activeTex.Width, (float)destRect.Height / activeTex.Height);
                Art.Instance.spriteBatch.Draw(activeTex, objectCenter, null, tint, radians, pivot, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            }

            // Pass this frame's top-left corner as the origin for children
            Vector2 frameTopLeft = resolvedPos - anchorOffset;
            foreach (var mod in modifiers)
                mod.Apply(children, resolvedSize);

            foreach (var child in children)
                child.Draw(dt, resolvedSize, frameTopLeft);
        }


        // Same as ImageFrame — just takes tex as parameter now
        private Rectangle ComputeCoverSrc(Rectangle targetRect, Image tex)
        {
            float targetAspect = (float)targetRect.Width / targetRect.Height;
            float imageAspect = (float)tex.Width / tex.Height;
            float srcX = 0f, srcY = 0f, srcW = tex.Width, srcH = tex.Height;
            if (imageAspect > targetAspect) { srcW = tex.Height * targetAspect; srcX = (tex.Width - srcW) / 2f; }
            else { srcH = tex.Width / targetAspect; srcY = (tex.Height - srcH) / 2f; }
            return new Rectangle((int)srcX, (int)srcY, (int)srcW, (int)srcH);
        }

        private Rectangle ComputeDestRect(Vector2 origin, Vector2 resolvedSize, Image tex)
        {
            switch (fit)
            {
                case ObjectFit.Contain:
                    float s = Math.Min(resolvedSize.X / tex.Width, resolvedSize.Y / tex.Height);
                    int cW = (int)(tex.Width * s), cH = (int)(tex.Height * s);
                    int cX = (int)(origin.X + (resolvedSize.X - cW) / 2f);
                    int cY = (int)(origin.Y + (resolvedSize.Y - cH) / 2f);
                    return new Rectangle(cX, cY, cW, cH);
                case ObjectFit.None:
                    return new Rectangle(
                        (int)(origin.X + (resolvedSize.X - tex.Width) / 2f),
                        (int)(origin.Y + (resolvedSize.Y - tex.Height) / 2f),
                        tex.Width, tex.Height);
                case ObjectFit.Fill:
                default:
                    return new Rectangle((int)origin.X, (int)origin.Y, (int)resolvedSize.X, (int)resolvedSize.Y);
            }
        }
    }

    // In-Testing
    public class SliderFrame : ArtObject
    {
        // ── Values ────────────────────────────────────────────────────────────
        public float minValue { get; set; } = 0f;
        public float maxValue { get; set; } = 1f;
        public float defaultValue { get; set; } = 0.5f;

        private float _currentValue = 0.5f;
        public float currentValue
        {
            get => _currentValue;
            set => _currentValue = Math.Clamp(value, minValue, maxValue);
        }

        /// <summary>Standard .NET format string, e.g. "0" for integers, "0.##" for floats.</summary>
        public string valueFormat { get; set; } = "0.##";

        // ── Text (left label area) ────────────────────────────────────────────
        public string title { get; set; } = "Slider";
        public string fontName { get; set; } = "";
        public float fontScale { get; set; } = 1f;
        public Color textColor { get; set; } = Color.White;

        /// <summary>Fixed pixel width reserved for the label column.</summary>
        public float textWidth { get; set; } = 150f;

        // ── Visuals ───────────────────────────────────────────────────────────
        public float alpha { get; set; } = 1f;
        public Color trackColor { get; set; } = new Color(50, 50, 60, 255);
        public Color fillColor { get; set; } = new Color(138, 43, 226, 255);
        public Color resetBtnColor { get; set; } = new Color(80, 80, 90, 255);
        public Color resetBtnHoverColor { get; set; } = new Color(220, 220, 230, 255);
        public Color handleColor { get; set; } = new Color(200, 180, 255, 255); // Brighter accent cap
        public float handleWidth { get; set; } = 12f;
        public float resetButtonWidth { get; set; } = 35f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fires every frame the slider value changes while dragging — ideal for sound ticks.</summary>
        public Action<SliderFrame>? onSlide { get; set; } = null;

        /// <summary>Fires once when dragging ends or the reset tween completes.</summary>
        public Action<SliderFrame>? onValueChanges { get; set; } = null;

        /// <summary>Fires every Update tick regardless of interaction.</summary>
        public Action<SliderFrame, float>? onUpdate { get; set; }

        // ── Internal state ────────────────────────────────────────────────────
        private bool _isDragging = false;
        private bool _isResetHovered = false;

        // The reset tween — animates currentValue back to defaultValue smoothly.
        private readonly Tweener _resetTween = new Tweener();

        // Hitboxes written by Draw, read by Update next tick.
        // One-frame lag on startup is imperceptible for UI.
        private Rectangle _trackHitbox;
        private Rectangle _resetHitbox;

        private static Texture2D? _sharedPixel;
        private Texture2D _pixel => _sharedPixel ??= Texture2D.CreateSinglePixel(Color.White);

        // ── Layout ───────────────────────────────────────────────────────────
        // KEY FIX: The reset button column is *always* reserved regardless of
        // visibility. Without this, trackW changes the moment you move away from
        // defaultValue, which re-maps the mouse position mid-drag and causes
        // the value to spike before settling.
        private readonly record struct Layout(
            Vector2 TopLeft,
            Vector2 ResolvedSize,
            float TrackX,
            float TrackY,
            float TrackH,
            float TrackW,   // constant; never depends on showReset
            float ResetX,
            bool ShowReset
        );

        private Layout ComputeLayout(Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;
            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 topLeft = resolvedPos - anchorOffset;

            // Reserve reset column at all times so the track never changes width.
            float gap = 6f;
            float trackX = topLeft.X + textWidth;
            float trackW = resolvedSize.X - textWidth - (resetButtonWidth + gap);
            float trackH = resolvedSize.Y * 0.68f;
            float trackY = topLeft.Y + (resolvedSize.Y - trackH) / 2f;
            float resetX = trackX + trackW + gap;

            bool showReset = Math.Abs(currentValue - defaultValue) > 0.001f
                          || _resetTween.IsPlaying;

            return new Layout(topLeft, resolvedSize, trackX, trackY, trackH, trackW, resetX, showReset);
        }

        // ── Update ────────────────────────────────────────────────────────────
        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);

            // 1. Advance the reset tween (fires before any input so value is fresh this frame)
            bool wasTweening = _resetTween.IsPlaying;
            _resetTween.Update(dt);

            if (_resetTween.IsPlaying)
            {
                currentValue = _resetTween.CurrentValue;
            }
            else if (wasTweening)
            {
                // Tween just finished — snap clean and fire the confirmation event
                currentValue = defaultValue;
                onValueChanges?.Invoke(this);
            }

            // 2. Reset button input
            bool showReset = Math.Abs(currentValue - defaultValue) > 0.001f
                          || _resetTween.IsPlaying;

            if (showReset)
            {
                _isResetHovered = _resetHitbox.Contains(Mouse.Position.X, Mouse.Position.Y);

                if (_isResetHovered && Mouse.LeftClicked())
                {
                    _isDragging = false;
                    // Kick off the tween from wherever the slider currently sits
                    _resetTween.Start(0.35f, currentValue, defaultValue, Easing.Cubic, Direction.Out);
                    return;
                }
            }
            else
            {
                _isResetHovered = false;
            }

            // 3. Drag input — skip while the reset tween is running
            if (_resetTween.IsPlaying) return;

            bool trackHovered = _trackHitbox.Contains(Mouse.Position.X, Mouse.Position.Y);

            if (trackHovered && Mouse.LeftClicked())
                _isDragging = true;

            if (!Mouse.LeftDown() && _isDragging)
            {
                _isDragging = false;
                onValueChanges?.Invoke(this);
            }

            if (_isDragging)
            {
                float prev = currentValue;
                float normalized = (Mouse.Position.X - _trackHitbox.X) / (float)_trackHitbox.Width;
                normalized = Math.Clamp(normalized, 0f, 1f);
                currentValue = minValue + normalized * (maxValue - minValue);

                if (Math.Abs(currentValue - prev) > 0.0001f)
                    onSlide?.Invoke(this);
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Layout lo = ComputeLayout(parentSize, parentOrigin);

            // Write hitboxes so Update can read them next tick
            _trackHitbox = new Rectangle((int)lo.TrackX, (int)lo.TrackY, (int)lo.TrackW, (int)lo.TrackH);
            _resetHitbox = lo.ShowReset
                ? new Rectangle((int)lo.ResetX, (int)lo.TrackY, (int)resetButtonWidth, (int)lo.TrackH)
                : Rectangle.Empty;

            // 1. Label — title + live value
            string labelText = $"{title}\n{currentValue.ToString(valueFormat)}";
            var (visualOffset, measuredText) = FontHelper.MeasureTextBounds(fontName, labelText, fontScale * 10);

            Vector2 labelPos = lo.TopLeft + new Vector2(0f, lo.ResolvedSize.Y / 2f);
            Vector2 labelOrigin = new Vector2(0f, measuredText.Y / 2f) + visualOffset + new Vector2(0f, -2.5f);

            FontHelper.DrawTextPro(
                fontName, labelText,
                labelPos, labelOrigin,
                rotation: 0f, scale: fontScale * 10,
                textColor * alpha,
                strokeWidth: 0f, strokeColor: null
            );

            // 2. Track background
            Art.Instance.spriteBatch.Draw(_pixel, _trackHitbox, trackColor * alpha);

            // 3. Fill
            float fillNormalized = (maxValue - minValue) > 0f
                ? (currentValue - minValue) / (maxValue - minValue)
                : 0f;
            int fillW = (int)(lo.TrackW * fillNormalized);

            if (fillW > 0)
            {
                Rectangle fillRect = new Rectangle((int)lo.TrackX, (int)lo.TrackY, fillW, (int)lo.TrackH);
                Art.Instance.spriteBatch.Draw(_pixel, fillRect, fillColor * alpha);
            }

            // 4. ── DRAW HANDLE (White/Light Accent Handle from osu!) ──────────
            float handleCenterX = lo.TrackX + (lo.TrackW * fillNormalized);
            // Clamp handle to completely stay inside the layout bounds of the track
            float handleDrawX = Math.Clamp(handleCenterX - (handleWidth / 2f), lo.TrackX, lo.TrackX + lo.TrackW - handleWidth);

            // Render handle full track height (or make it taller if you want it to pop out)
            Rectangle handleRect = new Rectangle(
                (int)handleDrawX,
                (int)lo.TrackY,
                (int)handleWidth,
                (int)lo.TrackH
            );
            Art.Instance.spriteBatch.Draw(_pixel, handleRect, handleColor * alpha);

            // 5. Reset button — fades in/out with its own alpha driven by how far
            //    currentValue is from defaultValue, so it never pops on/off harshly.
            if (lo.ShowReset)
            {
                float dist = Math.Abs(currentValue - defaultValue) / (maxValue - minValue);
                float buttonAlpha = Math.Clamp(dist / 0.02f, 0f, 1f); // fade over 2% of range
                Color btnColor = _isResetHovered ? resetBtnHoverColor : resetBtnColor;

                Art.Instance.spriteBatch.Draw(_pixel, _resetHitbox, (resetBtnColor * 0.4f) * (alpha * buttonAlpha));

                string resetGlyph = "#";
                var (rVisualOffset, rMeasured) = FontHelper.MeasureTextBounds(fontName, resetGlyph, fontScale * 10);

                Vector2 resetCenter = new Vector2(
                    lo.ResetX + resetButtonWidth / 2f,
                    lo.TopLeft.Y + lo.ResolvedSize.Y / 2f
                );
                Vector2 resetOrigin = new Vector2(rMeasured.X / 2f, rMeasured.Y / 2f)
                                    + rVisualOffset + new Vector2(0f, -2.5f);

                FontHelper.DrawTextPro(
                    fontName, resetGlyph,
                    resetCenter, resetOrigin,
                    rotation: 0f, scale: fontScale * 10,
                    btnColor * (alpha * buttonAlpha),
                    strokeWidth: 0f, strokeColor: null
                );
            }
        }
    }

    public class ScrollingFrame : ArtObject
    {
        // — Uniform properties —
        public float alpha { get; set; } = 1f;
        public float rotation { get; set; } = 0f;
        public Color color { get; set; } = Color.White;

        // — ScrollingFrame-specific —
        public ClipMode clipMode { get; set; } = ClipMode.Clip;
        public Axis scrollDirection { get; set; } = Axis.Vertical;
        public float scrollSensitivity { get; set; } = 40f;
        public bool showScrollbar { get; set; } = true;
        public float scrollbarWidth { get; set; } = 6f;
        public Color scrollbarColor { get; set; } = new Color(180, 180, 180, 200);
        public Color scrollbarTrackColor { get; set; } = new Color(60, 60, 60, 180);
        public float smoothing { get; set; } = 0f; // 0 = instant, higher = smoother (e.g. 8f)

        public List<IFrameModifier> modifiers { get; set; } = new();
        public List<ArtObject> children { get; set; } = new();
        public Action<ScrollingFrame, float>? onUpdate { get; set; }
        public Action<ScrollingFrame, float>? onPostLayout { get; set; }


        // — Read-only state —
        public float ScrollOffset { get; private set; } = 0f;
        public float ContentSize { get; private set; } = 0f; // auto-computed
        public Vector2 ParentSize { get; private set; } = Vector2.Zero;

        private float _targetOffset = 0f;
        private Texture2D _pixel = Texture2D.CreateSinglePixel(Color.White);
        private Rectangle _hitbox;

        // — Scissor rasterizer (one static instance is fine) —
        private static readonly Microsoft.Xna.Framework.Graphics.RasterizerState _scissorState = new Microsoft.Xna.Framework.Graphics.RasterizerState
        {
            ScissorTestEnable = true
        };

        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);

            if (_hitbox.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                int scrollDelta = Mouse.CurrentScrollWheelValue
                                - Mouse.LastScrollWheelValue;

                _targetOffset -= scrollDelta * (scrollSensitivity / 120f);
                _targetOffset = Math.Clamp(_targetOffset, 0f, Math.Max(0f, ContentSize));
            }

            if (smoothing <= 0f)
            {
                ScrollOffset = _targetOffset;
            }
            else
            {
                ScrollOffset = Microsoft.Xna.Framework.MathHelper.Lerp(ScrollOffset, _targetOffset, dt * smoothing);

                // Snap to target when close enough to avoid infinite creep
                if (Math.Abs(ScrollOffset - _targetOffset) < 0.5f)
                    ScrollOffset = _targetOffset;
            }

            foreach (var child in children) child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;

            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 topLeft = resolvedPos - anchorOffset;

            _hitbox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)resolvedSize.X, (int)resolvedSize.Y);
            ParentSize = parentSize;

            // — Draw background —
            Vector2 pivotInPixel = new Vector2(
                (anchorOffset.X / resolvedSize.X) * _pixel.Width,
                (anchorOffset.Y / resolvedSize.Y) * _pixel.Height
            );
            Art.Instance.spriteBatch.Draw(
                texture: _pixel,
                destinationRectangle: new Rectangle((int)resolvedPos.X, (int)resolvedPos.Y, (int)resolvedSize.X, (int)resolvedSize.Y),
                sourceRectangle: null,
                color: color * alpha,
                rotation: rotation,
                origin: pivotInPixel,
                effects: Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
                layerDepth: 0f
            );

            // — Apply modifiers —
            foreach (var mod in modifiers)
                mod.Apply(children, resolvedSize);

            onPostLayout?.Invoke(this, dt);

            // — Compute content size (for scroll clamping and scrollbar) —
            ContentSize = ComputeContentSize(resolvedSize) - (scrollDirection == Axis.Vertical ? resolvedSize.Y : resolvedSize.X);

            // — Scroll offset shifts the child origin —
            Vector2 scrollShift = scrollDirection == Axis.Vertical
                ? new Vector2(0, -ScrollOffset)
                : new Vector2(-ScrollOffset, 0);

            Vector2 childOrigin = topLeft + scrollShift;

            // — Draw children (clipped or not) —
            if (clipMode == ClipMode.Clip)
            {
                DrawClipped(dt, resolvedSize, childOrigin, topLeft);
            }
            else
            {
                foreach (var child in children)
                    child.Draw(dt, resolvedSize, childOrigin);
            }

            // — Draw scrollbar on top —
            if (showScrollbar && ContentSize > 0f)
                DrawScrollbar(topLeft, resolvedSize);
        }

        private void DrawClipped(float dt, Vector2 resolvedSize, Vector2 childOrigin, Vector2 topLeft)
        {
            var gd = Art.Instance.graphicsDevice;

            Rectangle scissor = new Rectangle(
                (int)topLeft.X,
                (int)topLeft.Y,
                (int)resolvedSize.X,
                (int)resolvedSize.Y
            );

            // Clamp scissor to current scissor so nested ScrollingFrames stack correctly
            if (gd.RasterizerState.ScissorTestEnable)
                scissor = Rectangle.Intersect(scissor, gd.ScissorRectangle);

            gd.ScissorRectangle = scissor;

            GraphicsHelper.CurrentRasterizerState = _scissorState;
            GraphicsHelper.StartBatch(null);

            foreach (var child in children)
                child.Draw(dt, resolvedSize, childOrigin);

            GraphicsHelper.CurrentRasterizerState = null;
            GraphicsHelper.StartBatch(null);
        }

        private void DrawScrollbar(Vector2 topLeft, Vector2 resolvedSize)
        {
            var sb = Art.Instance.spriteBatch;

            if (scrollDirection == Axis.Vertical)
            {
                float trackH = resolvedSize.Y;
                float thumbH = Math.Max(20f, trackH * (resolvedSize.Y / (resolvedSize.Y + ContentSize)));
                float thumbY = (ScrollOffset / ContentSize) * (trackH - thumbH);

                // Track
                sb.Draw(_pixel, new Rectangle(
                    (int)(topLeft.X + resolvedSize.X - scrollbarWidth),
                    (int)topLeft.Y,
                    (int)scrollbarWidth,
                    (int)trackH), scrollbarTrackColor);

                // Thumb
                sb.Draw(_pixel, new Rectangle(
                    (int)(topLeft.X + resolvedSize.X - scrollbarWidth),
                    (int)(topLeft.Y + thumbY),
                    (int)scrollbarWidth,
                    (int)thumbH), scrollbarColor);
            }
            else
            {
                float trackW = resolvedSize.X;
                float thumbW = Math.Max(20f, trackW * (resolvedSize.X / (resolvedSize.X + ContentSize)));
                float thumbX = (ScrollOffset / ContentSize) * (trackW - thumbW);

                sb.Draw(_pixel, new Rectangle(
                    (int)topLeft.X,
                    (int)(topLeft.Y + resolvedSize.Y - scrollbarWidth),
                    (int)trackW,
                    (int)scrollbarWidth), scrollbarTrackColor);

                sb.Draw(_pixel, new Rectangle(
                    (int)(topLeft.X + thumbX),
                    (int)(topLeft.Y + resolvedSize.Y - scrollbarWidth),
                    (int)thumbW,
                    (int)scrollbarWidth), scrollbarColor);
            }
        }

        private float ComputeContentSize(Vector2 resolvedSize)
        {
            if (!children.Any()) return 0f;

            if (scrollDirection == Axis.Vertical)
                return children.Max(c => c.position.Resolve(resolvedSize).Y + c.GetResolvedSize(resolvedSize).Y);
            else
                return children.Max(c => c.position.Resolve(resolvedSize).X + c.GetResolvedSize(resolvedSize).X);
        }

        //public Vector2 GetResolvedSize(Vector2 parentSize) => size.Resolve(parentSize);
    }

    public class GridTransitionRadial : ArtObject
    {
        private int _tileSize;
        private Color _color;
        private bool _fadeOut;
        private bool _reverseWave;
        private Tweener _tween = new Tweener();

        public bool IsPlaying => _tween.IsPlaying;
        public void SetValue(float value) => _tween.SetValue(value);

        public GridTransitionRadial(Color color, bool fadeOut = true, bool reverseWave = false, int tileSize = 50)
        {
            _tileSize = tileSize <= 0 ? 50 : tileSize;
            _color = color;
            _fadeOut = fadeOut;
            _reverseWave = reverseWave; // false = Center Out, true = Edges In
        }

        public void Play(float duration, Easing easing = Easing.Cubic, Direction direction = Direction.Out)
        {
            _tween.Start(duration, 0f, 1f, easing, direction);
        }

        public override void Update(float dt)
        {
            _tween.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            if (!_fadeOut && _tween.CurrentValue <= 0f) return;
            if (_fadeOut && _tween.CurrentValue >= 1f) return;

            float transitionProgress = _tween.CurrentValue;
            float screenWidth = GraphicsHelper.ScreenWidth;
            float screenHeight = GraphicsHelper.ScreenHeight;

            int cols = (int)screenWidth / _tileSize + ((int)screenWidth % _tileSize != 0 ? 1 : 0);
            int rows = (int)screenHeight / _tileSize + ((int)screenHeight % _tileSize != 0 ? 1 : 0);

            // Find the center of the grid
            float centerX = (cols - 1) / 2f;
            float centerY = (rows - 1) / 2f;

            // The maximum distance is from the center to any corner (e.g., x=0, y=0)
            float maxDist = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
            if (maxDist == 0f) maxDist = 1f; // Prevent division by zero if grid is 1x1

            float fadeSpread = 0.4f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // Calculate distance from center
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    // Normalize distance from 0 to 1
                    float normalizedDist = dist / maxDist;

                    // Scale the delay so that the final tile finishes exactly when transitionProgress reaches 1.0
                    float delayStart = (_reverseWave ? (1f - normalizedDist) : normalizedDist) * (1f - fadeSpread);

                    float tileLocalProgress = Math.Clamp((transitionProgress - delayStart) / fadeSpread, 0f, 1f);
                    float tileAlpha = _fadeOut ? 1f - tileLocalProgress : tileLocalProgress;

                    if (tileAlpha > 0f)
                    {
                        byte alphaByte = (byte)(255 * tileAlpha);
                        Color drawColor = new Color(_color.R, _color.G, _color.B, alphaByte);
                        GraphicsHelper.DrawRectangle(x * _tileSize, y * _tileSize, _tileSize, _tileSize, drawColor);
                    }
                }
            }
        }
    }

    public class GridTransitionHorizontal : ArtObject
    {

        private int _tileSize;
        private Color _color;
        private bool _fadeOut;
        private bool _reverseWave;
        private Tweener _tween = new Tweener();

        public bool IsPlaying => _tween.IsPlaying;
        public void SetValue(float value) => _tween.SetValue(value);

        public GridTransitionHorizontal(Color color, bool fadeOut = true, bool reverseWave = false, int tileSize = 50)
        {
            _tileSize = tileSize <= 0 ? 50 : tileSize;
            _color = color;
            _fadeOut = fadeOut;
            _reverseWave = reverseWave;
        }

        public void Play(float duration, Easing easing = Easing.Cubic, Direction direction = Direction.Out)
        {
            _tween.Start(duration, 0f, 1f, easing, direction);
        }

        public override void Update(float dt)
        {
            _tween.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            if (!_fadeOut && _tween.CurrentValue <= 0f) return;
            if (_fadeOut && _tween.CurrentValue >= 1f) return;


            float transitionProgress = _tween.CurrentValue;
            float screenWidth = GraphicsHelper.ScreenWidth;
            float screenHeight = GraphicsHelper.ScreenHeight;

            int cols = (int)screenWidth / _tileSize + ((int)screenWidth % _tileSize != 0 ? 1 : 0);
            int rows = (int)screenHeight / _tileSize + ((int)screenHeight % _tileSize != 0 ? 1 : 0);

            float maxDist = (cols - 1) + (rows - 1);
            float fadeSpread = 0.4f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    float distX = _reverseWave ? x : (cols - 1) - x;
                    float delayStart = distX / maxDist;

                    float tileLocalProgress = Math.Clamp((transitionProgress - delayStart) / fadeSpread, 0f, 1f);
                    float tileAlpha = _fadeOut ? 1f - tileLocalProgress : tileLocalProgress;

                    if (tileAlpha > 0f)
                    {
                        byte alphaByte = (byte)(255 * tileAlpha);
                        Color drawColor = new Color(_color.R, _color.G, _color.B, alphaByte);
                        GraphicsHelper.DrawRectangle(x * _tileSize, y * _tileSize, _tileSize, _tileSize, drawColor);
                    }
                }
            }
        }
    }

    // In Development
    public class TextBoxFrame : ArtObject
    {
        // ── Content ───────────────────────────────────────────────────────────
        private string _currentText = "";
        public string currentText
        {
            get => _currentText;
            set
            {
                _currentText = value ?? "";
                // Clamp cursor in case text was set programmatically
                _cursorIndex = Math.Clamp(_cursorIndex, 0, _currentText.Length);
            }
        }

        /// <summary>Shown when the box is empty and unfocused.</summary>
        public string placeholder { get; set; } = "Type here...";

        /// <summary>-1 = unlimited.</summary>
        public int maxLength { get; set; } = -1;

        // ── Text styling ──────────────────────────────────────────────────────
        public string fontName { get; set; } = "";
        public float fontScale { get; set; } = 1f;
        public Color textColor { get; set; } = Color.White;
        public Color placeholderColor { get; set; } = new Color(150, 150, 160, 255);

        // ── Visuals ───────────────────────────────────────────────────────────
        public float alpha { get; set; } = 1f;
        public float padding { get; set; } = 10f;        // inner horizontal & vertical padding
        public Color backgroundColor { get; set; } = new Color(30, 30, 40, 255);
        public Color focusedColor { get; set; } = new Color(45, 45, 60, 255);
        public Color borderColor { get; set; } = new Color(80, 80, 95, 255);
        public Color focusedBorderColor { get; set; } = new Color(138, 43, 226, 255);
        public float borderWidth { get; set; } = 2f;
        public Color cursorColor { get; set; } = Color.White;
        public float cursorWidth { get; set; } = 2f;
        public float cursorBlinkRate { get; set; } = 0.53f;      // seconds per blink half-cycle

        // ── State ─────────────────────────────────────────────────────────────
        public bool isFocused { get; private set; } = false;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fires when the player presses Enter.</summary>
        public Action<TextBoxFrame>? onEnter { get; set; } = null;

        /// <summary>Fires when focus is lost (click-outside or Enter).</summary>
        public Action<TextBoxFrame>? onFocusLost { get; set; } = null;

        /// <summary>Fires whenever the text content changes.</summary>
        public Action<TextBoxFrame>? onTextChanged { get; set; } = null;

        /// <summary>Fires every Update tick regardless of interaction.</summary>
        public Action<TextBoxFrame, float>? onUpdate { get; set; }

        // ── Internal state ────────────────────────────────────────────────────
        private int _cursorIndex = 0;      // insertion point within _currentText
        private float _cursorTimer = 0f;
        private bool _cursorVisible = true;

        // Key-repeat state for Backspace / Delete / arrow keys
        private Keys? _heldKey = null;
        private float _heldKeyTimer = 0f;
        private const float RepeatDelay = 0.40f;   // seconds before repeat starts
        private const float RepeatRate = 0.05f;   // seconds between repeats

        private Rectangle _hitbox;
        private readonly Texture2D _pixel = Texture2D.CreateSinglePixel(Color.White);

        // ── Focus management ──────────────────────────────────────────────────
        public void Focus()
        {
            if (isFocused) return;
            isFocused = true;
            _cursorIndex = _currentText.Length;  // place cursor at end
            _cursorTimer = 0f;
            _cursorVisible = true;
            Art.Instance.RegisterTextInput(OnTextInput);
        }

        public void Defocus()
        {
            if (!isFocused) return;
            isFocused = false;
            Art.Instance.unRegisterTextInput(OnTextInput);
            onFocusLost?.Invoke(this);
        }

        // ── TextInput event handler ───────────────────────────────────────────
        // FNA fires this for every printable character, correctly handling
        // keyboard layout, shift, caps-lock, IME, etc. We skip control chars
        // that we handle ourselves (backspace, enter).
        private void OnTextInput(char c)
        {

            // Skip control characters — handled via key polling below
            if (c == '\b' || c == '\r' || c == '\n' || c == '\x1b') return;

            // Respect maxLength
            if (maxLength >= 0 && _currentText.Length >= maxLength) return;

            _currentText = _currentText.Insert(_cursorIndex, c.ToString());
            _cursorIndex++;
            ResetCursorBlink();
            onTextChanged?.Invoke(this);
        }

        // ── Update ────────────────────────────────────────────────────────────
        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);

            // 1. Click to focus / click-outside to defocus
            if (Mouse.LeftClicked())
            {
                bool clickedInside = _hitbox.Contains(Mouse.Position.X, Mouse.Position.Y);
                if (clickedInside && !isFocused)
                    Focus();
                else if (!clickedInside && isFocused)
                    Defocus();
            }

            if (!isFocused) return;

            // 2. Cursor blink
            _cursorTimer += dt;
            if (_cursorTimer >= cursorBlinkRate)
            {
                _cursorTimer -= cursorBlinkRate;
                _cursorVisible = !_cursorVisible;
            }

            // 3. Special key handling with repeat

            // Which key is currently actionable?
            Keys? actionKey = null;
            if (Keyboard.IsKeyDown(Keys.Back)) actionKey = Keys.Back;
            else if (Keyboard.IsKeyDown(Keys.Delete)) actionKey = Keys.Delete;
            else if (Keyboard.IsKeyDown(Keys.Left)) actionKey = Keys.Left;
            else if (Keyboard.IsKeyDown(Keys.Right)) actionKey = Keys.Right;
            else if (Keyboard.IsKeyDown(Keys.Enter)) actionKey = Keys.Enter;
            else if (Keyboard.IsKeyDown(Keys.Home)) actionKey = Keys.Home;
            else if (Keyboard.IsKeyDown(Keys.End)) actionKey = Keys.End;

            if (actionKey.HasValue)
            {
                if (_heldKey != actionKey)
                {
                    // New key — fire immediately
                    _heldKey = actionKey;
                    _heldKeyTimer = 0f;
                    ProcessSpecialKey(actionKey.Value);
                }
                else
                {
                    // Same key held — wait for repeat
                    _heldKeyTimer += dt;
                    float threshold = _heldKeyTimer < RepeatDelay + RepeatRate
                        ? RepeatDelay
                        : RepeatRate;

                    if (_heldKeyTimer >= threshold)
                    {
                        _heldKeyTimer -= RepeatRate;
                        ProcessSpecialKey(actionKey.Value);
                    }
                }
            }
            else
            {
                _heldKey = null;
                _heldKeyTimer = 0f;
            }
        }

        private void ProcessSpecialKey(Keys key)
        {
            switch (key)
            {
                case Keys.Back:
                    if (_cursorIndex > 0)
                    {
                        _currentText = _currentText.Remove(_cursorIndex - 1, 1);
                        _cursorIndex--;
                        ResetCursorBlink();
                        onTextChanged?.Invoke(this);
                    }
                    break;

                case Keys.Delete:
                    if (_cursorIndex < _currentText.Length)
                    {
                        _currentText = _currentText.Remove(_cursorIndex, 1);
                        ResetCursorBlink();
                        onTextChanged?.Invoke(this);
                    }
                    break;

                case Keys.Left:
                    if (_cursorIndex > 0)
                    {
                        _cursorIndex--;
                        ResetCursorBlink();
                    }
                    break;

                case Keys.Right:
                    if (_cursorIndex < _currentText.Length)
                    {
                        _cursorIndex++;
                        ResetCursorBlink();
                    }
                    break;

                case Keys.Home:
                    _cursorIndex = 0;
                    ResetCursorBlink();
                    break;

                case Keys.End:
                    _cursorIndex = _currentText.Length;
                    ResetCursorBlink();
                    break;

                case Keys.Enter:
                    onEnter?.Invoke(this);
                    Defocus();
                    break;
            }
        }

        private void ResetCursorBlink()
        {
            _cursorTimer = 0f;
            _cursorVisible = true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;
            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 topLeft = resolvedPos - anchorOffset;

            // Write hitbox for Update next tick
            _hitbox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)resolvedSize.X, (int)resolvedSize.Y);

            // 1. Border (drawn as a slightly larger rect behind the background)
            if (borderWidth > 0f)
            {
                Color bColor = isFocused ? focusedBorderColor : borderColor;
                Rectangle borderRect = new Rectangle(
                    (int)(topLeft.X - borderWidth),
                    (int)(topLeft.Y - borderWidth),
                    (int)(resolvedSize.X + borderWidth * 2),
                    (int)(resolvedSize.Y + borderWidth * 2)
                );
                Art.Instance.spriteBatch.Draw(_pixel, borderRect, bColor * alpha);
            }

            // 2. Background
            Color bgColor = isFocused ? focusedColor : backgroundColor;
            Art.Instance.spriteBatch.Draw(_pixel, _hitbox, bgColor * alpha);

            // 3. Text (or placeholder)
            bool showPlaceholder = _currentText.Length == 0 && !isFocused;
            string displayText = showPlaceholder ? placeholder : _currentText;
            Color displayColor = showPlaceholder ? placeholderColor : textColor;

            float textX = topLeft.X + padding;
            float textY = topLeft.Y + resolvedSize.Y / 2f;

            if (displayText.Length > 0)
            {
                var (visualOffset, measuredText) = FontHelper.MeasureTextBounds(fontName, displayText, fontScale * 10);
                Vector2 textOrigin = new Vector2(0f, measuredText.Y / 2f) + visualOffset + new Vector2(0f, -2.5f);

                FontHelper.DrawTextPro(
                    fontName, displayText,
                    new Vector2(textX, textY), textOrigin,
                    rotation: 0f, scale: fontScale * 10,
                    displayColor * alpha,
                    strokeWidth: 0f, strokeColor: null
                );
            }

            // 4. Cursor — only when focused and blinking on
            if (isFocused && _cursorVisible)
            {
                // Measure width of text up to cursor index to find X position
                float cursorX = textX;
                if (_cursorIndex > 0)
                {
                    string textBeforeCursor = _currentText[.._cursorIndex];
                    var (_, beforeSize) = FontHelper.MeasureTextBounds(fontName, textBeforeCursor, fontScale * 10);
                    cursorX += beforeSize.X;
                }

                int cursorH = (int)(resolvedSize.Y * 0.6f);
                int cursorY = (int)(topLeft.Y + (resolvedSize.Y - cursorH) / 2f);

                Rectangle cursorRect = new Rectangle(
                    (int)cursorX, cursorY,
                    (int)cursorWidth, cursorH
                );

                Art.Instance.spriteBatch.Draw(_pixel, cursorRect, cursorColor * alpha);
            }
        }
    }

    public class EffectFrame : ArtObject
    {
        public float alpha { get; set; } = 1f;
        public Color color { get; set; } = Color.White;
        public bool BypassEffect { get; set; } = false;

        // --- Shader Injection ---
        public Effects.IArtEffect? Effect { get; set; }

        public List<ArtObject> children { get; set; } = new();
        public Action<EffectFrame, float>? onUpdate { get; set; }

        private Microsoft.Xna.Framework.Graphics.RenderTarget2D? _renderTarget;

        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);
            foreach (var child in children) child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;
            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);

            // ✅ THE BYPASS CHECK GOES HERE!
            // If bypassed, draw children directly to the screen (razor-sharp) and exit early.
            if (BypassEffect)
            {
                Vector2 frameTopLeft = resolvedPos - anchorOffset;
                foreach (var child in children) child.Draw(dt, resolvedSize, frameTopLeft);

                return; // CRITICAL: Exit the method so the RenderTarget code below doesn't run!
            }

            int width = (int)Math.Max(1, resolvedSize.X);
            int height = (int)Math.Max(1, resolvedSize.Y);

            // 1. Manage the Render Target Size Dynamically
            var gd = Art.Instance.graphicsDevice;
            if (_renderTarget == null || _renderTarget.Width != width || _renderTarget.Height != height)
            {
                _renderTarget?.Dispose();
                _renderTarget = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(
                    gd, width, height, false, gd.PresentationParameters.BackBufferFormat, Microsoft.Xna.Framework.Graphics.DepthFormat.None, 0, Microsoft.Xna.Framework.Graphics.RenderTargetUsage.PreserveContents);
            }

            // 2. Pause the current drawing loop and save the previous target (in case Effects are nested!)
            GraphicsHelper.CloseBatch();
            var previousRenderTargets = gd.GetRenderTargets();

            // 3. Redirect GPU to our internal canvas and clear it to transparent
            gd.SetRenderTarget(_renderTarget);
            gd.Clear(Microsoft.Xna.Framework.Color.Transparent);

            // 4. Draw all children to this internal canvas
            // We pass Vector2.Zero as parentOrigin because inside this RenderTarget, the top-left is strictly 0,0!
            GraphicsHelper.StartBatch(null);
            Vector2 innerFrameTopLeft = new Vector2(0, 0);
            foreach (var child in children)
            {
                child.Draw(dt, resolvedSize, innerFrameTopLeft);
            }
            GraphicsHelper.CloseBatch();

            // 5. Restore the GPU to the previous canvas (usually the backbuffer/screen)
            gd.SetRenderTargets(previousRenderTargets);
            GraphicsHelper.StartBatch(null);

            // 6. Apply the Effect!
            Microsoft.Xna.Framework.Rectangle destRect = new Microsoft.Xna.Framework.Rectangle(
                (int)(resolvedPos.X - anchorOffset.X),
                (int)(resolvedPos.Y - anchorOffset.Y),
                width, height);

            Microsoft.Xna.Framework.Color drawColor = new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, (byte)(alpha * 255f));

            if (Effect != null)
            {
                // Let the wrapper handle the complex shader passes
                Effect.DrawEffect(_renderTarget, destRect, drawColor);
            }
            else
            {
                // Fallback: Just draw it normally if no shader is assigned
                Art.Instance.spriteBatch.Draw(_renderTarget, destRect, drawColor);
            }
        }
    }

    public class CircleFrame : ArtObject
    {
        // — Uniform properties —
        public float alpha { get; set; } = 1f;
        public float rotation { get; set; } = 0f;
        public Color color { get; set; } = Color.White;

        // — Circle-specific properties —
        /// <summary>Set to 0 for a solid circle, or > 0 for a hollow ring.</summary>
        public float innerRadius { get; set; } = 0f;

        /// <summary>Starting angle in degrees.</summary>
        public float startAngle { get; set; } = 0f;

        /// <summary>Ending angle in degrees. Animating this creates pie-chart or spinner effects.</summary>
        public float endAngle { get; set; } = 360f;

        /// <summary>Higher values create a smoother edge but use more vertices.</summary>
        public int segments { get; set; } = 64;

        public List<ArtObject> children { get; set; } = new List<ArtObject>();
        public List<IFrameModifier> modifiers { get; set; } = new();

        public Action<CircleFrame, float>? onUpdate { get; set; }

        public override void Update(float dt)
        {
            onUpdate?.Invoke(this, dt);
            foreach (var child in children)
                child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            Vector2 resolvedSize = size.Resolve(parentSize);
            Vector2 resolvedPos = position.Resolve(parentSize) + parentOrigin;

            // Offset the drawing position based on the anchor settings
            Vector2 anchorOffset = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 frameTopLeft = resolvedPos - anchorOffset;

            // The mathematical center of the allocated UI box
            Vector2 center = frameTopLeft + (resolvedSize / 2f);

            // Derive outer radius from the shortest side to ensure it always fits inside its bounding box
            float outerRadius = Math.Min(resolvedSize.X, resolvedSize.Y) / 2f;

            // Prevent inverted or overflowing geometry
            float safeInnerRadius = Math.Clamp(innerRadius, 0f, outerRadius);

            // Draw using the primitive GraphicsHelper backend 
            GraphicsHelper.DrawRing(
                center,
                safeInnerRadius,
                outerRadius,
                startAngle + rotation,
                endAngle + rotation,
                segments,
                color * alpha
            );

            // Pass this frame's top-left corner as the origin for any nested UI children
            foreach (var mod in modifiers)
                mod.Apply(children, resolvedSize);

            foreach (var child in children)
                child.Draw(dt, resolvedSize, frameTopLeft);
        }
    }
    // Development Only
    public class Trail : ArtObject
    {
        private struct TrailPoint
        {
            public Vector2 Position;
            public float TimeAdded;

            public TrailPoint(Vector2 position, float time)
            {
                Position = position;
                TimeAdded = time;
            }
        }

        // --- Configuration ---
        public float MenuAlpha { get; set; }
        public float LoopDuration { get; set; } // Time (seconds) to draw one complete heart loop
        public float Persistence { get; set; } // Time (seconds) the trail segment exists before vanishing
        public float Scale { get; set; } = 35f; // Physical size of the heart
        public Color trailColor { get; set; } = Color.White;

        // --- State ---
        private readonly Queue<TrailPoint> _trailPoints; // FIFO queue for smooth point management
        private readonly Texture2D _pixel;
        private float _elapsedTime;

        // --- Constructor (With clean parameter handling) ---
        // Using optional parameters and null-coalescing for clean API
        public Trail(float? menuAlpha = null, float? loopDuration = null, float? persistence = null, float? scale = null, Color? color = null)
        {
            _pixel = Texture2D.CreateSinglePixel(Color.White); // Store a global white pixel is better practice!
            _trailPoints = new Queue<TrailPoint>();

            // Clean fallback defaults
            this.MenuAlpha = menuAlpha ?? 1f;
            this.LoopDuration = loopDuration ?? 1f; // One full loop per second
            this.Persistence = persistence ?? 0.5f; // Trail fades after half a second
            this.Scale = scale ?? 15f;
            this.trailColor = color ?? Color.White;
        }

        // ── Game Loop Methods ──────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _elapsedTime += dt;

            // 1. Calculate the current position along the heart curve
            // Convert time to theta (radians), ensuring it keeps looping 0 to 2*PI
            float fullCircle = (float)(Math.PI * 2);
            float radiansPerSecond = fullCircle / LoopDuration;
            float currentT = (_elapsedTime * radiansPerSecond) % fullCircle;

            // 2. Safely grab dimensions from your engine wrapper
            var viewport = Art.Instance.GraphicsDevice.Viewport;
            float centerX = (viewport.Width / 2f) + 50;
            float centerY = viewport.Height / 2f;

            // 3. Generate a new point and enqueue it
            Vector2 newPointPos = GetPerfectHeartPosition(currentT, centerX, centerY, Scale);
            _trailPoints.Enqueue(new TrailPoint(newPointPos, _elapsedTime));

            // 4. Remove points that are older than our persistence threshold
            float oldestAllowedTime = _elapsedTime - Persistence;
            while (_trailPoints.Count > 0 && _trailPoints.Peek().TimeAdded < oldestAllowedTime)
            {
                _trailPoints.Dequeue();
            }
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            if (_trailPoints.Count < 2) return; // Need at least two points to draw a line

            // A standard starting center color
            float oldestAllowedTime = _elapsedTime - Persistence;

            // Iterate through the queue to draw fading segments
            // This is safer than a for loop which might change during modification
            TrailPoint? previousTrailPoint = null;

            foreach (var currentTrailPoint in _trailPoints)
            {
                if (previousTrailPoint != null)
                {
                    // 1. Calculate the age and corresponding alpha fade
                    // Head of trail (age 0) -> Alpha 1.0 (255)
                    // Tail of trail (age Persistence) -> Alpha 0.0 (0)
                    float pointAge = currentTrailPoint.TimeAdded - oldestAllowedTime;
                    float normalizedAlpha = Microsoft.Xna.Framework.MathHelper.Clamp(pointAge / Persistence, 0f, 1f);

                    // 2. Calculate the total combined alpha byte
                    byte combinedAlphaByte = (byte)(255f * normalizedAlpha * MenuAlpha);
                    Color segmentColor = new Color(trailColor.R, trailColor.G, trailColor.B, combinedAlphaByte);

                    // 3. Draw the line between previous and current
                    DrawLine(Art.Instance.spriteBatch,
                        _pixel,
                        previousTrailPoint.Value.Position,
                        currentTrailPoint.Position,
                        segmentColor,
                        22f); // Line thickness
                }

                previousTrailPoint = currentTrailPoint;
            }
        }

        // --- Helper Methods ---
        // (Move these here if they are only used by the Heartbeat, 
        // or call them from GraphicsHelper if they are global)

        public static Vector2 GetPerfectHeartPosition(float t, float centerX, float centerY, float scale)
        {
            // Standard parametric heart equations
            float x = 16f * (float)Math.Pow(Math.Sin(t), 3);

            float y = 13f * (float)Math.Cos(t)
                    - 5f * (float)Math.Cos(2 * t)
                    - 2f * (float)Math.Cos(3 * t)
                    - (float)Math.Cos(4 * t);

            // Note: We subtract Y because math expects Y to go UP, but MonoGame Y goes DOWN
            return new Vector2(centerX + (x * scale), centerY - (y * scale));
        }

        public static void DrawLine(Microsoft.Xna.Framework.Graphics.SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            sb.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
                null,
                color,
                angle,
                new Vector2(0, 0.5f), // Origin at the start-center of the pixel
                Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
                0);
        }
    }
}
