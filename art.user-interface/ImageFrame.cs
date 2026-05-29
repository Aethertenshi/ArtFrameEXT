using System;
using ArtFrameCore.SdlBindings;

namespace ArtFrameCore.UserInterface
{
    /// <summary>
    /// A visual UI component that renders a hardware-accelerated image (PNG or JPG) from a file.
    /// </summary>
    public class ImageFrame : Element
    {
        private string _imagePath = string.Empty;
        private IntPtr _texturePtr = IntPtr.Zero;

        /// <summary>
        /// Gets or sets the path to the image file to load and display.
        /// </summary>
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value ?? string.Empty;
                    FreeTexture(); // Free old texture so new path is loaded lazily on the next frame
                }
            }
        }

        /// <summary>
        /// Constructor for the ImageFrame.
        /// </summary>
        public ImageFrame()
        {
        }

        /// <summary>
        /// Lazily loads the image texture from file if the renderer is initialized and ready.
        /// </summary>
        private void EnsureTextureLoaded()
        {
            if (_texturePtr == IntPtr.Zero && !string.IsNullOrEmpty(_imagePath))
            {
                IntPtr renderer = Renderer.Pointer;
                if (renderer != IntPtr.Zero)
                {
                    _texturePtr = SdlImage.LoadTexture(renderer, _imagePath);
                    if (_texturePtr == IntPtr.Zero)
                    {
                        Console.WriteLine($"[ArtFrameCore] Error: Failed to load image texture from '{_imagePath}'");
                    }
                }
            }
        }

        /// <summary>
        /// Frees the allocated native texture resource.
        /// </summary>
        private void FreeTexture()
        {
            if (_texturePtr != IntPtr.Zero)
            {
                Renderer.DestroyTexture(_texturePtr);
                _texturePtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Draws the image frame by calculating parent-relative dimensions and invoking the hardware renderer.
        /// </summary>
        /// <param name="parentAbsoluteX">The parent element's absolute X offset.</param>
        /// <param name="parentAbsoluteY">The parent element's absolute Y offset.</param>
        /// <param name="parentWidth">The parent element's rendered layout width.</param>
        /// <param name="parentHeight">The parent element's rendered layout height.</param>
        public override void Draw(float parentAbsoluteX = 0, float parentAbsoluteY = 0, float parentWidth = 800, float parentHeight = 600)
        {
            // Calculate absolute position on screen
            var (localX, localY) = Position.Calculate(parentWidth, parentHeight);
            float absoluteX = parentAbsoluteX + localX;
            float absoluteY = parentAbsoluteY + localY;

            // Calculate absolute size of this element
            var (absoluteWidth, absoluteHeight) = Size.Calculate(parentWidth, parentHeight);

            // Lazy-load the image resource on demand
            EnsureTextureLoaded();

            // Render the image if we have a valid texture and dimensions
            if (_texturePtr != IntPtr.Zero && absoluteWidth > 0 && absoluteHeight > 0)
            {
                Renderer.DrawTexture(_texturePtr, absoluteX, absoluteY, absoluteWidth, absoluteHeight);
            }
            else if (absoluteWidth > 0 && absoluteHeight > 0)
            {
                // Fallback debug translucent magenta background if the image fails to load
                Renderer.DrawQuad(absoluteX, absoluteY, absoluteWidth, absoluteHeight, new SDL_FColor(1.0f, 0.0f, 1.0f, 0.3f));
            }

            // Propagate layout size: if this element has no size (like a UIGroup), pass down parent size
            float currentWidth = absoluteWidth > 0 ? absoluteWidth : parentWidth;
            float currentHeight = absoluteHeight > 0 ? absoluteHeight : parentHeight;

            // Draw all children recursively
            foreach (var child in _children)
            {
                child.Draw(absoluteX, absoluteY, currentWidth, currentHeight);
            }
        }

        /// <summary>
        /// Finalizer to ensure the native texture is always cleanly destroyed on garbage collection.
        /// </summary>
        ~ImageFrame()
        {
            FreeTexture();
        }
    }
}
