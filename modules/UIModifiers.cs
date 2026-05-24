using ArtFrame.ArtTypes;

namespace ArtFrame.UIModifier
{
    public enum HAlign { Left, Center, Right }
    public enum VAlign { Top, Center, Bottom }
    public enum Axis { Horizontal, Vertical }
    public enum ClipMode { None, Clip }

    public class ListLayout : IFrameModifier
    {
        public Axis direction { get; set; } = Axis.Vertical;
        public bool controlCrossAxis { get; set; } = true;
        public float spacing { get; set; } = 4f;
        public float paddingX { get; set; } = 0f;
        public float paddingY { get; set; } = 0f;
        public HAlign horizontalAlign { get; set; } = HAlign.Left;   // used on cross-axis
        public VAlign verticalAlign { get; set; } = VAlign.Top;

        public void Apply(List<ArtObject> children, Vector2 frameSize)
        {
            if (direction == Axis.Vertical)
            {
                // Pre-compute total height so we can center the whole block
                float totalHeight = children.Sum(c => c.GetResolvedSize(frameSize).Y)
                                  + spacing * (children.Count - 1);

                float cursor = verticalAlign switch
                {
                    VAlign.Center => (frameSize.Y - totalHeight) / 2f,
                    VAlign.Bottom => frameSize.Y - totalHeight - paddingY,
                    _ => paddingY
                };

                foreach (var child in children)
                {
                    Vector2 childSize = child.GetResolvedSize(frameSize);

                    float x = controlCrossAxis
                        ? horizontalAlign switch
                        {
                            HAlign.Center => (frameSize.X - childSize.X) / 2f,
                            HAlign.Right => frameSize.X - childSize.X - paddingX,
                            _ => paddingX
                        }
                        : child.position.Resolve(frameSize).X; // preserve existing X

                    // Force anchors to Left/Top so positions are unambiguous
                    child.anchorX = AnchorX.Left;
                    child.anchorY = AnchorY.Top;
                    child.position = UDim2.FromOffset(x, cursor);
                    cursor += childSize.Y + spacing;
                }
            }
            else
            {
                float totalWidth = children.Sum(c => c.GetResolvedSize(frameSize).X)
                                 + spacing * (children.Count - 1);

                float cursor = horizontalAlign switch
                {
                    HAlign.Center => (frameSize.X - totalWidth) / 2f,
                    HAlign.Right => frameSize.X - totalWidth - paddingX,
                    _ => paddingX
                };

                foreach (var child in children)
                {
                    Vector2 childSize = child.GetResolvedSize(frameSize);

                    float y = controlCrossAxis
                    ? verticalAlign switch
                    {
                        VAlign.Center => (frameSize.Y - childSize.Y) / 2f,
                        VAlign.Bottom => frameSize.Y - childSize.Y - paddingY,
                        _ => paddingY
                    }
                    : child.position.Resolve(frameSize).Y; // preserve existing Y

                    child.anchorX = AnchorX.Left;
                    child.anchorY = AnchorY.Top;
                    child.position = UDim2.FromOffset(cursor, y);
                    cursor += childSize.X + spacing;
                }
            }
        }
    }   

    public class GridLayout : IFrameModifier
    {
        public int columns { get; set; } = 2;
        public float spacingX { get; set; } = 4f;
        public float spacingY { get; set; } = 4f;
        public float paddingX { get; set; } = 0f;
        public float paddingY { get; set; } = 0f;

        // If Zero, cells are auto-sized to fill columns evenly
        public Vector2 cellSize { get; set; } = Vector2.Zero;

        public void Apply(List<ArtObject> children, Vector2 frameSize)
        {
            Vector2 resolvedCell = cellSize == Vector2.Zero
                ? new Vector2((frameSize.X - paddingX * 2 - spacingX * (columns - 1)) / columns,
                              (frameSize.X - paddingX * 2 - spacingX * (columns - 1)) / columns)
                : cellSize;

            for (int i = 0; i < children.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                float x = paddingX + col * (resolvedCell.X + spacingX);
                float y = paddingY + row * (resolvedCell.Y + spacingY);

                children[i].position = UDim2.FromOffset(x, y);
                children[i].size = UDim2.FromOffset(resolvedCell.X, resolvedCell.Y);
            }
        }
    }
}