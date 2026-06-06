using System;
using Art2Core.Modules;
using Art2Core.SdlBindings;

namespace Art2Core.UserInterface
{
    /// <summary>
    /// UI element that displays text using the native SDL3_ttf font rendering system.
    /// Inherits layout, color, and hierarchy behavior from Element.
    /// </summary>
    public class TextFrame : Element
    {
        /// <summary>
        /// The text content to display.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The registered font name to use. Defaults to "gsans".
        /// </summary>
        public string FontName { get; set; } = "gsans";

        /// <summary>
        /// The font size/scale at which to render the text. Defaults to 24.
        /// </summary>
        public float TextScale { get; set; } = 24f;

        /// <summary>
        /// The color of the rendered text. Defaults to white.
        /// </summary>
        public SDL_FColor TextColor { get; set; } = new SDL_FColor(1f, 1f, 1f, 1f);

        /// <summary>
        /// Initializes a new empty TextFrame.
        /// </summary>
        public TextFrame() : base()
        {
            // By default, text frames do not draw a background unless Color is explicitly changed
            Color = new SDL_FColor(0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// Initializes a new TextFrame with specified text.
        /// </summary>
        /// <param name="text">The initial text content.</param>
        public TextFrame(string text) : this()
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// Draws the text frame, its background quad, and its children recursively.
        /// </summary>
        public override void Draw(float parentAbsoluteX = 0, float parentAbsoluteY = 0, float parentWidth = 800, float parentHeight = 600)
        {
            // 1. Calculate absolute position on screen
            var (localX, localY) = Position.Calculate(parentWidth, parentHeight);
            float absoluteX = parentAbsoluteX + localX;
            float absoluteY = parentAbsoluteY + localY;

            // Calculate absolute size of this element
            var (absoluteWidth, absoluteHeight) = Size.Calculate(parentWidth, parentHeight);

            // 2. Draw background quad if it has size and is visible (alpha > 0)
            if (absoluteWidth > 0 && absoluteHeight > 0 && Color.a > 0f)
            {
                Renderer.DrawQuad(absoluteX, absoluteY, absoluteWidth, absoluteHeight, Color);
            }

            // 3. Draw text content
            if (!string.IsNullOrEmpty(Text))
            {
                float drawX = absoluteX;
                float drawY = absoluteY;

                if (absoluteWidth > 0 && absoluteHeight > 0)
                {
                    // Center the text inside the element boundaries
                    var (textW, textH) = Fonts.MeasureText(FontName, Text, TextScale);
                    drawX = absoluteX + (absoluteWidth - textW) / 2f;
                    drawY = absoluteY + (absoluteHeight - textH) / 2f;
                }

                Fonts.DrawText(FontName, Text, drawX, drawY, TextScale, TextColor);
            }

            // 4. Propagate layout size and draw children recursively
            float currentWidth = absoluteWidth > 0 ? absoluteWidth : parentWidth;
            float currentHeight = absoluteHeight > 0 ? absoluteHeight : parentHeight;

            foreach (var child in _children)
            {
                child.Draw(absoluteX, absoluteY, currentWidth, currentHeight);
            }
        }
    }
}
