# Graphics, Tweens & Effects

This module outlines screen management APIs, the tween animation engine, and custom post-processing shaders. In accordance with keeping developer integration clean and simple, low-level drawing primitives and internal batching pipelines are handled automatically by the engine and omitted here.

---

## Screen & Window Management (`GraphicsHelper.cs`)

Use these static helpers to configure the window viewport, adjust render settings, and compute layout positions.

### Public Methods

#### `ConfigureWindow`
Configures the window viewport size, title, and fullscreen state. Call this inside your `IArt.Setup()` method.
```csharp
public static void ConfigureWindow(int width, int height, string title = "ArtFramework", bool fullscreen = false)
```
*   **Example Usage:**

```csharp
public void Setup()
{
    GraphicsHelper.ConfigureWindow(1920, 1080, "My Interactive Art Project", fullscreen: true);
}
```

#### `SetFrameRate`
Sets the target rendering frame rate (draw ticks per second) for draw suppression. The application will suppress drawing if the delta time is below this target, saving GPU resources.
```csharp
public static void SetFrameRate(int fps)
```

#### `GetAnchorOffset`
Utility that calculates a spatial pixel offset vector based on a box's size and specified anchor alignments.
```csharp
public static Vector2 GetAnchorOffset(AnchorX anchorX, AnchorY anchorY, Vector2 size)
```

---

ArtFrame features an integrated, high-precision tween engine (`Tweener`) that lets you animate values smoothly using 11 different easing curves and three directions.

### Easing Curves (`Easing` Enum)
*   `Linear` — Consistent, uniform speed.
*   `Quadratic`, `Cubic`, `Quartic`, `Quintic` — Power-based acceleration.
*   `Sine` — Smooth acceleration based on sinusoidal waves.
*   `Exponential` — Exponential ramp.
*   `Circular` — Acceleration based on circular geometry.
*   `Back` — Overshoots the destination slightly before pulling back.
*   `Elastic` — Bounces back and forth like a rubber band around the target.
*   `Fluid` — Custom high-order smooth deceleration curve.

### Easing Directions (`Direction` Enum)
*   `In` — Easing is applied at the beginning of the transition.
*   `Out` — Easing is applied at the end of the transition.
*   `InOut` — Easing is applied at both the beginning and the end.

---

### The `Tweener` Class

A `Tweener` handles the mathematical state of an active animation. Once added to the tween pool, it updates automatically on every frame.

#### Properties
*   `IsPlaying` (`bool`): Returns `true` if the animation is currently running.
*   `CurrentValue` (`float`): The current animated value, ready to be applied.

#### Public Methods

##### `Start`
Starts a new tween animation.
```csharp
public void Start(float duration, float startValue, float endValue, Easing easing = Easing.Linear, Direction direction = Direction.In)
```
*   **Parameters:**
    *   `duration`: Duration of the animation in seconds.
    *   `startValue`: The value to animate from.
    *   `endValue`: The value to animate to.
    *   `easing`: The mathematical curve to apply.
    *   `direction`: The easing direction behavior.

##### `Restart`
Starts a new animation **using the current value of the tweener as the starting point**. This prevents visual snapping when changing targets mid-transition.
```csharp
public void Restart(float duration, float targetValue, Easing easing = Easing.Linear, Direction direction = Direction.In)
```

##### `SetValue`
Instantly snaps the tweener to the specified value and halts any active animation.
```csharp
public void SetValue(float value)
```

*   **Example Usage:**

```csharp
using ArtFrame;
using ArtFrame.Easings;

public class TweenDemo : IArt
{
    private Tweener _fadeTween;
    private Frame _myPanel;

    public void Setup()
    {
        _myPanel = new Frame { size = UDim2.FromScale(0.3f, 0.3f) };
        SpriteHelper.Add(_myPanel);

        // 1. Create and register a new Tweener to the global update pool
        _fadeTween = TweenHelper.AddTween(new Tweener());

        // 2. Start animating from 0% opacity to 100% opacity over 0.5s
        _fadeTween.Start(0.5f, 0f, 1f, Easing.Cubic, Direction.Out);
    }

    public void Update(float dt)
    {
        // Apply the active animated value directly to the UI component
        _myPanel.alpha = _fadeTween.CurrentValue;

        // Trigger a clean redirection on mouse click without snapping
        if (InputHelper.Mouse.LeftClicked())
        {
            _fadeTween.Restart(0.4f, 0f, Easing.Sine, Direction.In);
        }
    }
}
```

---

## Custom Shader Effects (`Effect.cs`)

ArtFrame provides an interface and wrapper classes to load custom pixel shaders and apply two-pass post-processing filter sweeps to UI containers.

### The `IArtEffect` Interface
Represents a custom shader effect wrapper.
```csharp
public interface IArtEffect : IDisposable
{
    void DrawEffect(Texture2D content, Rectangle destination, Color tint);
}
```

### The `GaussianBlurEffect` Class
A production-ready two-pass Gaussian blur filter. It performs horizontal and vertical blur sweeps sequentially inside custom render targets.

#### Public Properties
*   `BlurAmount` (`float`): The intensity of the blur (defaults to `2f`). Higher values produce stronger blurs.

*   **Example Usage (Applied via an `EffectFrame` container):**

```csharp
using ArtFrame;
using ArtFrame.Effects;
using ArtFrame.UserInterface;

public class ShaderDemo : IArt
{
    private EffectFrame _blurContainer;
    private GaussianBlurEffect _blurShader;

    public void Setup()
    {
        // 1. Instantiate the Blur wrapper
        _blurShader = new GaussianBlurEffect { BlurAmount = 4.5f };

        // 2. Create the container frame and assign the shader
        _blurContainer = new EffectFrame
        {
            size = UDim2.FromScale(0.5f, 0.5f),
            Effect = _blurShader // Inject the custom shader
        };

        // 3. Add child elements (everything inside this container will be blurred!)
        _blurContainer.children.Add(new TextFrame { text = "This text is blurred!" });

        SpriteHelper.Add(_blurContainer);
    }
}
```
