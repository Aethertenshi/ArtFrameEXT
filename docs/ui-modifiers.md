# Layout Modifiers (`UIModifiers.cs`)

Layout modifiers are dedicated layout algorithms that implement the `IFrameModifier` interface. When added to a frame's `modifiers` list, they automatically calculate and override the spatial coordinates and dimensions of the nested children on every frame, eliminating the need to position elements manually.

---

## 1. Linear Stacking (`ListLayout`)

`ListLayout` positions child elements in a sequential line, either vertically or horizontally. It includes options for spacing, padding, and cross-axis alignment.

### Public Properties

*   `direction` (`Axis`): The stacking axis.
    *   `Axis.Vertical` — Stacks children in a column.
    *   `Axis.Horizontal` — Stacks children in a row.
*   `controlCrossAxis` (`bool`):
    *   If `true`, the layout manages positioning along the cross-axis (e.g. horizontal alignment in a vertical list).
    *   If `false`, children retain their pre-defined position coordinates along the cross-axis.
*   `spacing` (`float`): Pixel gap between consecutive child elements.
*   `paddingX` / `paddingY` (`float`): Absolute border padding inside the container.
*   `horizontalAlign` (`HAlign`): Horizontal alignment along the cross-axis (`HAlign.Left`, `HAlign.Center`, `HAlign.Right`).
*   `verticalAlign` (`VAlign`): Vertical alignment along the cross-axis (`VAlign.Top`, `VAlign.Center`, `VAlign.Bottom`).

---

### Vertical Layout Positioning Logic

When `direction` is set to `Axis.Vertical`, `ListLayout` executes the following positioning sequence:
1.  It sums the resolved heights of all active children and adds the spacing offsets to calculate the total combined height:
    `Total Height = sum(ChildHeight) + spacing * (Count - 1)`
2.  It positions the starting cursor based on `verticalAlign`:
    *   `VAlign.Center` starts at: `StartY = (ParentHeight - Total Height) / 2`
    *   `VAlign.Bottom` starts at: `StartY = ParentHeight - Total Height - paddingY`
    *   `VAlign.Top` starts at `paddingY`.
3.  It iterates through the children, forcing their anchors to `AnchorX.Left` and `AnchorY.Top` to ensure unambiguous positioning.
4.  If `controlCrossAxis` is active, it aligns the horizontal position of each child based on `horizontalAlign`:
    *   `HAlign.Center` sets: `x = (ParentWidth - ChildWidth) / 2`
    *   `HAlign.Right` sets: `x = ParentWidth - ChildWidth - paddingX`
    *   `HAlign.Left` sets `x = paddingX`.
5.  It sets the child's position to `(x, cursor)` and increments the cursor for the next element:
    `cursor = cursor + ChildHeight + spacing`

*   **Example Usage:**

```csharp
var menuFrame = new Frame { size = UDim2.FromScale(0.3f, 0.6f) };

// Set up a vertical list layout with center-aligned elements
menuFrame.modifiers.Add(new ListLayout
{
    direction = Axis.Vertical,
    spacing = 8f,
    paddingY = 15f,
    horizontalAlign = HAlign.Center,
    verticalAlign = VAlign.Top,
    controlCrossAxis = true
});

// Add buttons (ListLayout will position them automatically!)
menuFrame.children.Add(new Button { size = UDim2.FromOffset(120, 35) });
menuFrame.children.Add(new Button { size = UDim2.FromOffset(120, 35) });
menuFrame.children.Add(new Button { size = UDim2.FromOffset(120, 35) });
```

---

## 2. Table Grid Layout (`GridLayout`)

`GridLayout` arranges child elements in uniform rows and columns.

### Public Properties

*   `columns` (`int`): The number of columns in the grid.
*   `spacingX` / `spacingY` (`float`): The pixel spacing between cells.
*   `paddingX` / `paddingY` (`float`): Absolute border padding inside the container.
*   `cellSize` (`Vector2`):
    *   If set to a positive size, cells use these absolute dimensions.
    *   If set to `Vector2.Zero`, **cells are auto-sized** to divide the horizontal container width evenly among the columns:
        `CellWidth = (ParentWidth - (paddingX * 2) - spacingX * (columns - 1)) / columns`
        `CellHeight = CellWidth` (square aspect ratio)

---

### Grid Layout Positioning Logic

For every child at index `i` in the list, `GridLayout` calculates its grid position:
1.  Determines the column and row index:
    *   `col = i % columns`
    *   `row = i / columns`
2.  Calculates the cell's coordinates:
    *   `x = paddingX + col * (CellWidth + spacingX)`
    *   `y = paddingY + row * (CellHeight + spacingY)`
3.  Overrides the child's position to `UDim2.FromOffset(x, y)` and its size to `UDim2.FromOffset(CellWidth, CellHeight)`.

*   **Example Usage:**

```csharp
var gridFrame = new Frame { size = UDim2.FromOffset(400, 400) };

// Arrange child elements in a 3-column grid
gridFrame.modifiers.Add(new GridLayout
{
    columns = 3,
    spacingX = 6f,
    spacingY = 6f,
    paddingX = 10f,
    paddingY = 10f,
    cellSize = Vector2.Zero // Auto-sizes cells to fit columns
});

// Add elements
for (int i = 0; i < 9; i++)
{
    gridFrame.children.Add(new Frame());
}
```
