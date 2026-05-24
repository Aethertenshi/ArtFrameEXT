# UI Basics: Frames & Buttons

ArtFrame features a declarative, Roblox- and CSS-inspired User Interface (UI) system built on the base `ArtObject` class. All components support hierarchy nesting, relative scaling + absolute pixel positioning (`UDim2`), and anchor alignments.

---

## 1. Bounding Container (`Frame`)

The `Frame` is a solid rectangular component. It can render a background color or serve as an invisible layout folder containing child nodes.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `color` | `Color` | The background color tint. |
| `alpha` | `float` | The opacity factor (0.0 to 1.0). |
| `rotation` | `float` | Rotation in degrees around its anchor pivot. |
| `children` | `List<ArtObject>` | The nested list of child elements. |
| `modifiers` | `List<IFrameModifier>` | Layout engines (like `ListLayout`) applied automatically to position children. |
| `onUpdate` | `Action<Frame, float>` | Delegate invoked on every logic tick. |

*   **Example Usage:**

```csharp
var mainPanel = new Frame
{
    position = UDim2.FromScale(0.1f, 0.1f),
    size = UDim2.FromScale(0.8f, 0.8f),
    color = new Color("#1A1A24"),
    anchorX = AnchorX.Left,
    anchorY = AnchorY.Top
};
SpriteHelper.Add(mainPanel);
```

---

## 2. Image Container (`ImageFrame`)

Renders a texture asset with advanced aspect-ratio fitting controls.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `texture` | `Image` | The image texture, loaded via `SpriteHelper.LoadImage`. |
| `fit` | `ObjectFit` | The aspect-ratio fit mode: `Fill` (stretch to fit), `Contain` (scale uniform to fit inside), `Cover` (scale uniform to crop cover), or `None` (native size centered). |
| `color` | `Color` | Color tint. |
| `alpha` | `float` | Opacity (0.0 to 1.0). |
| `rotation` | `float` | Rotation in degrees. |
| `children` | `List<ArtObject>` | Nested child elements. |
| `onUpdate` | `Action<ImageFrame, float>` | Logic tick callback. |

*   **Example Usage:**

```csharp
SpriteHelper.LoadImage("logo", "images/logo.png");

var logoImage = new ImageFrame
{
    texture = SpriteHelper.LoadImage("logo"),
    fit = ObjectFit.Contain,
    size = UDim2.FromOffset(200, 200),
    position = new UDim2(0.5f, 0, 0f, 20),
    anchorX = AnchorX.Center,
    anchorY = AnchorY.Top
};
```

---

## 3. Formatted Text (`TextFrame`)

Renders multi-channel signed distance field (MTSDF) text. It includes options for background panels, padding, and borders.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `fontName` | `string` | The registered name of the MTSDF font to use. |
| `text` | `string` | The text string to display. Supports multi-line text split by `\n` escape sequences. |
| `scale` | `float` | Font size multiplier. |
| `color` | `Color` | Text color. |
| `alpha` | `float` | Master opacity. |
| `rotation` | `float` | Rotation in degrees. |
| `textAnchorX` | `AnchorX` | Horizontal text alignment inside the bounding box. |
| `textAnchorY` | `AnchorY` | Vertical text alignment inside the bounding box. |
| `strokeWidth` | `float` | Outline stroke thickness (defaults to `0f` for no stroke). |
| `strokeColor` | `Color?` | Color of the outline stroke. |

#### Background Panel Options

| Option | Type | Description |
| :--- | :--- | :--- |
| `backgroundColor` | `Color?` | Draws a solid background behind the text. |
| `backgroundAlpha` | `float` | Opacity of the background panel. |
| `backgroundPadding` | `float` | Pixel padding between the text boundaries and the background edges. |
| `backgroundStroke` | `float?` | Draws an outline border around the background panel. |
| `backgroundStrokeColor` | `Color` | The color of the background panel's border. |

*   **Example Usage:**

```csharp
var titleText = new TextFrame
{
    fontName = "main",
    text = "Leaderboard\nStage Complete",
    scale = 1.5f,
    color = Color.White,
    strokeWidth = 1.5f,
    strokeColor = Color.Black,
    textAnchorX = AnchorX.Center,
    textAnchorY = AnchorY.Center,
    position = UDim2.FromScale(0.5f, 0.3f),
    anchorX = AnchorX.Center,
    anchorY = AnchorY.Center,

    // Background box parameters
    backgroundColor = new Color("#0E0E14"),
    backgroundPadding = 12f,
    backgroundStroke = 2f,
    backgroundStrokeColor = new Color("#8A2BE2")
};
```

---

## 4. Solid Button (`Button`)

An interactive rectangular button. It automatically updates its visual state between hovered, pressed, and default based on mouse coordinates.

### Properties & State

| Property | Type | Description |
| :--- | :--- | :--- |
| `color` | `Color` | The default background color. |
| `hoverColor` | `Color` | The background color when the mouse cursor is inside the button's hitbox. |
| `pressedColor` | `Color` | The background color when clicked and held. |
| `alpha` | `float` | Button opacity. |
| `rotation` | `float` | Button rotation. |
| `IsHovered` | `bool` | Read-only state indicating if the cursor is hovering over the button. |
| `IsPressed` | `bool` | Read-only state indicating if the button is currently clicked. |

### Events

| Event | Type | Description |
| :--- | :--- | :--- |
| `onClick` | `Action<Button>` | Invoked when the mouse button is pressed and released inside the button hitbox. |
| `onHoverEnter` | `Action<Button>` | Invoked on the exact frame the cursor enters the button hitbox. |
| `onHoverExit` | `Action<Button>` | Invoked on the exact frame the cursor leaves the button hitbox. |

*   **Example Usage:**

```csharp
var submitBtn = new Button
{
    size = UDim2.FromOffset(150, 45),
    position = UDim2.FromScale(0.5f, 0.8f),
    anchorX = AnchorX.Center,
    anchorY = AnchorY.Center,
    color = new Color("#2A2C3E"),
    hoverColor = new Color("#3A3D5E"),
    pressedColor = new Color("#8A2BE2")
};

// Attach click callback
submitBtn.onClick += btn =>
{
    Console.WriteLine("Button Clicked!");
    PlayMenuTickSound();
};

// Nest text inside the button container
submitBtn.children.Add(new TextFrame
{
    fontName = "main",
    text = "SUBMIT",
    scale = 1f,
    color = Color.White,
    position = UDim2.FromScale(0.5f, 0.5f),
    anchorX = AnchorX.Center,
    anchorY = AnchorY.Center
});
```

---

## 5. Textured Button (`ImageButton`)

An interactive button rendered with texture assets.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `texture` | `Image` | The default image texture. |
| `hoverImage` | `Image?` | An optional alternative texture to display when hovered. Falls back to `texture` if not specified. |
| `pressedImage` | `Image?` | An optional alternative texture to display when pressed. |
| `fit` | `ObjectFit` | The aspect-ratio fit mode. |
| `color` | `Color` | Texture color tint. |
| `alpha` | `float` | Opacity. |
| `hoverAlpha` | `float` | Opacity applied when hovered (defaults to `1f`). |
| `pressedAlpha` | `float` | Opacity applied when pressed (defaults to `0.7f`). |

### Events

| Event | Type | Description |
| :--- | :--- | :--- |
| `onClick` | `Action<ImageButton>` | Invoked when the button is clicked. |
| `onHoverEnter` | `Action<ImageButton>` | Invoked when hover starts. |
| `onHoverExit` | `Action<ImageButton>` | Invoked when hover ends. |
