# Core Types (`ArtTypes.cs`)

The core types module defines the basic spatial, visual, and mathematical primitives used throughout the ArtFrame framework. These custom types replace standard XNA structs to keep the API clean, platform-independent, and developer-friendly.

---

## Alignments & Scaling Enums

### `AnchorX`
Specifies the horizontal pivot/alignment point of a UI component relative to its position.
```csharp
public enum AnchorX
{
    Left,   // Alignment pivot at the left edge (0.0)
    Center, // Alignment pivot at the horizontal center (0.5)
    Right   // Alignment pivot at the right edge (1.0)
}
```

### `AnchorY`
Specifies the vertical pivot/alignment point of a UI component relative to its position.
```csharp
public enum AnchorY
{
    Top,    // Alignment pivot at the top edge (0.0)
    Center, // Alignment pivot at the vertical center (0.5)
    Bottom  // Alignment pivot at the bottom edge (1.0)
}
```

### `ObjectFit`
Defines how an image fits inside a bounding box of different aspect ratios.
```csharp
public enum ObjectFit
{
    Fill,     // Stretches/squashes image to fill the exact box dimensions
    Contain,  // Scales image uniformly to fit completely within the box (letterbox)
    Cover,    // Scales image uniformly to cover the box, cropping overflow (no empty bars)
    None      // Draws image at its native resolution, centered inside the box
}
```

---

## Mathematical Structs

### `Color`
A custom 4-byte RGBA color representation supporting hexadecimal string parsing, linear interpolation, and fading.

#### Constructors
*   **Byte-based Color:** Creates a color with R, G, B, and optional Alpha (defaults to 255/fully opaque).

```csharp
public Color(byte r, byte g, byte b, byte a = 255)
```

*   **Hex String Color:** Parses hex colors, supporting standard formats (e.g. `"#FF0000"` or `"#FF0000FF"`).

```csharp
public Color(string hex)
```

#### Static Methods
*   `LerpColor`: Linearly interpolates color channel values between `a` and `b` by fraction `t` (0.0 to 1.0).

```csharp
public static Color LerpColor(Color a, Color b, float t)
```

*   `Fade`: Returns a copy of the color with its alpha scaled by a float multiplier `alpha` (0.0 to 1.0).

```csharp
public static Color Fade(Color color, float alpha)
```

#### Predefined Colors
*   `Color.White` — `new Color(255, 255, 255)`
*   `Color.Black` — `new Color(0, 0, 0)`
*   `Color.Blue`  — `new Color(0, 0, 255)`

---

### `UDim2`
A two-dimensional coordinate system using relative scales (0.0 to 1.0) and absolute pixel offsets. This allows fluid responsive layouts inspired by web and engine systems.

$$\text{Resolved Position} = (\text{ParentWidth} \times \text{ScaleX}) + \text{OffsetX}$$

#### Fields
| Field | Type | Description |
| :--- | :--- | :--- |
| `ScaleX` | `float` | Fractional scale along the horizontal axis (relative to parent size). |
| `ScaleY` | `float` | Fractional scale along the vertical axis (relative to parent size). |
| `OffsetX` | `float` | Absolute pixel offset along the horizontal axis. |
| `OffsetY` | `float` | Absolute pixel offset along the vertical axis. |

#### Constructors
*   **Scale-only UDim2:** Offset defaults to zero.

```csharp
public UDim2(float scaleX, float scaleY)
```

*   **Scale and Offset UDim2:** Full parameter initialization.

```csharp
public UDim2(float scaleX, float scaleY, float offsetX, float offsetY)
```

#### Public Methods
*   `Resolve`: Calculates the absolute pixel vector (`Vector2`) based on parent container dimensions.

```csharp
public Vector2 Resolve(Vector2 parentSize)
```

#### Static Helpers & Shorthands
*   `UDim2.FromOffset(x, y)`: Returns a `UDim2` with 0% scale and absolute pixel offsets.
*   `UDim2.FromScale(x, y)`: Returns a `UDim2` with percentage scale and 0 pixel offsets.
*   `UDim2.Lerp(a, b, t)`: Linearly interpolates scales and offsets between `a` and `b` by fraction `t`.

---

### `Vector2`
A custom 2D float vector representation.

#### Fields
*   `X` — Horizontal float coordinate.
*   `Y` — Vertical float coordinate.

#### Constructors
```csharp
public Vector2(float x, float y)
```

#### Public Methods
*   `Length()`: Calculates the magnitude (hypotenuse) of the vector.
*   `Normalize()`: Returns a unit vector with a length of 1.0 pointing in the same direction.
*   `Distance(v1, v2)`: Calculates the Euclidean distance between two vectors.
*   `Lerp(v1, v2, amount)`: Performs linear interpolation.

#### Predefined Vectors
*   `Vector2.Zero` — `new Vector2(0f, 0f)`
*   `Vector2.One`  — `new Vector2(1f, 1f)`
*   `Vector2.UnitX` — `new Vector2(1f, 0f)`
*   `Vector2.UnitY` — `new Vector2(0f, 1f)`

---

### `Rectangle`
A float-based 2D rectangle representation.

#### Fields
*   `X`, `Y` — The coordinates of the top-left corner.
*   `Width`, `Height` — Dimensions of the box.

#### Properties
*   `Top` (Y), `Left` (X), `Bottom` (Y + Height), `Right` (X + Width).
*   `Center` — Returns the geometric center of the rectangle as a `Vector2`.

#### Public Methods
*   `Contains(x, y)`: Returns true if the coordinates reside within the rectangle bounds.
*   `Intersect(r1, r2)`: Returns the overlapping rectangle segment. Returns `Rectangle.Empty` if no overlap exists.
