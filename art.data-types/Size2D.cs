using System;

namespace ArtFrameCore.DataType
{
    /// <summary>
    /// A Roblox UDim2-style 2D size struct.
    /// Combines Scale (0 to 1, relative to parent dimensions) and Offset (absolute pixels).
    /// </summary>
    public struct Size2D
    {
        public float XScale { get; set; }
        public float XOffset { get; set; }
        public float YScale { get; set; }
        public float YOffset { get; set; }

        /// <summary>
        /// Creates a Size2D with specified scale and zero offset.
        /// </summary>
        public Size2D(float xScale, float yScale)
        {
            XScale = xScale;
            XOffset = 0;
            YScale = yScale;
            YOffset = 0;
        }

        /// <summary>
        /// Creates a Size2D with specified scale and absolute pixel offset.
        /// </summary>
        public Size2D(float xScale, float yScale, float xOffset, float yOffset)
        {
            XScale = xScale;
            XOffset = xOffset;
            YScale = yScale;
            YOffset = yOffset;
        }

        /// <summary>
        /// Calculates absolute dimensions based on parent dimensions.
        /// </summary>
        public (float Width, float Height) Calculate(float parentWidth, float parentHeight)
        {
            float w = (parentWidth * XScale) + XOffset;
            float h = (parentHeight * YScale) + YOffset;
            return (w, h);
        }

        // Factory methods for high readability
        public static Size2D FromScale(float xScale, float yScale) => new Size2D(xScale, yScale);
        public static Size2D FromOffset(float xOffset, float yOffset) => new Size2D(0, 0, xOffset, yOffset);
    }
}
