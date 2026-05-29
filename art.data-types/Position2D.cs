using System;

namespace ArtFrameCore.DataType
{
    /// <summary>
    /// A Roblox UDim2-style 2D position struct.
    /// Combines Scale (0 to 1, relative to parent dimensions) and Offset (absolute pixels).
    /// </summary>
    public struct Position2D
    {
        public float XScale { get; set; }
        public float XOffset { get; set; }
        public float YScale { get; set; }
        public float YOffset { get; set; }

        /// <summary>
        /// Creates a Position2D with specified scale and zero offset.
        /// </summary>
        public Position2D(float xScale, float yScale)
        {
            XScale = xScale;
            XOffset = 0;
            YScale = yScale;
            YOffset = 0;
        }

        /// <summary>
        /// Creates a Position2D with specified scale and absolute pixel offset.
        /// </summary>
        public Position2D(float xScale, float yScale, float xOffset, float yOffset)
        {
            XScale = xScale;
            XOffset = xOffset;
            YScale = yScale;
            YOffset = yOffset;
        }

        /// <summary>
        /// Calculates absolute coordinates based on parent dimensions.
        /// </summary>
        public (float X, float Y) Calculate(float parentWidth, float parentHeight)
        {
            float x = (parentWidth * XScale) + XOffset;
            float y = (parentHeight * YScale) + YOffset;
            return (x, y);
        }

        // Factory methods for high readability
        public static Position2D FromScale(float xScale, float yScale) => new Position2D(xScale, yScale);
        public static Position2D FromOffset(float xOffset, float yOffset) => new Position2D(0, 0, xOffset, yOffset);
    }
}