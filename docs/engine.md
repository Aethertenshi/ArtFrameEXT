# Core Engine (`Engine.cs`)

The core engine module establishes the application lifecycle, frame loops, and base hierarchies of ArtFrame. It manages the underlying XNA game loop and handles draw suppression to run rendering and updates efficiently.

---

## Technical Architecture

ArtFrame separates application lifecycle methods from internal framework pipelines. By implementing the `IArt` interface, you define setup, logic ticks, and manual draw procedures, while the engine handles execution, input state tracking, tween steps, and garbage-collected layouts under the hood.

```
  ┌─────────────────┐
  │  Engine.Run<T>  │ ──> Bootstraps application
  └────────┬────────┘
           │
           ▼
  ┌─────────────────┐
  │   IArt.Setup    │ ──> Configures window, assets, UI elements
  └────────┬────────┘
           │
     [ Game Loop ]
           │
           ├────────────────────────────┐
           ▼                            ▼
  ┌─────────────────┐          ┌─────────────────┐
  │  IArt.Update    │          │ IArt.ManualDraw │
  │                 │          │                 │
  │  - Inputs       │          │  - Solid colors │
  │  - Tweens       │          │  - SpriteBatch  │
  │  - UI States    │          │  - Primitives   │
  └─────────────────┘          └─────────────────┘
```

---

## Public Interfaces

### `IArt`
This is the primary interface that developers implement to hook their custom game or art logic into the framework.

```csharp
public interface IArt
{
    /// <summary>
    /// Invoked once at startup. Use this to configure the window, load texture
    /// and font assets, and build initial UI trees.
    /// </summary>
    void Setup();

    /// <summary>
    /// Invoked on every logic tick. Receives delta time in seconds.
    /// </summary>
    /// <param name="dt">Elapsed seconds since the last frame.</param>
    void Update(float dt);

    /// <summary>
    /// Invoked on every render tick. Use this for low-level direct drawing.
    /// Render operations in this method run after window clearing.
    /// </summary>
    /// <param name="dt">Elapsed seconds since the last frame.</param>
    void ManualDraw(float dt);
}
```

### `IArtHelper`
Used by framework helper services that hook into the central update and draw pipelines.

```csharp
public interface IArtHelper
{
    void Update(float dt);
    void Draw(float dt);
}
```

### `IFrameModifier`
Defines layout layout behaviors (like list structures or grid structures) applied dynamically to children of frames.

```csharp
public interface IFrameModifier
{
    /// <summary>
    /// Re-calculates and overrides positions/sizes of child components inside a parent container.
    /// </summary>
    /// <param name="children">The list of child elements within the frame.</param>
    /// <param name="frameSize">The resolved dimensions of the parent frame.</param>
    void Apply(List<ArtObject> children, ArtTypes.Vector2 frameSize);
}
```

---

## Base Class: `ArtObject`

The `ArtObject` is the fundamental abstract base class for all visual components, declarative UI elements, and layout containers in ArtFrame. It includes spatial properties and virtual methods to hook custom components into the layout pipeline.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `position` | `UDim2` | The relative scale + absolute pixel offset positioning vector. |
| `size` | `UDim2` | The relative scale + absolute pixel offset dimensions vector. |
| `anchorX` | `AnchorX` | The horizontal pivot alignment anchor (`Left`, `Center`, `Right`). |
| `anchorY` | `AnchorY` | The vertical pivot alignment anchor (`Top`, `Center`, `Bottom`). |

### Public Methods

#### `GetResolvedSize`
Resolves the component's relative `size` (`UDim2`) into absolute pixel dimensions (`Vector2`) based on the dimensions of the parent container.
```csharp
public ArtTypes.Vector2 GetResolvedSize(ArtTypes.Vector2 parentSize)
```

---

## Static Bootstrapper: `Engine`

The `Engine` class is the static entry point that manages the initialization and execution of the application.

### Public Methods

#### `Run<T>`
Bootstraps the framework and runs the application. The generic argument `T` must implement `IArt` and expose a default constructor.
```csharp
public static void Run<T>() where T : IArt, new()
```

*   **Example Usage:**

```csharp
class Program
{
    static void Main() => Engine.Run<MyArtApp>();
}
```
