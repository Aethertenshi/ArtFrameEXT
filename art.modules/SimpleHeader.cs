using System;
using ArtFrameCore.DataType;
using ArtFrameCore.SdlBindings;

namespace ArtFrameCore.Modules
{
    public class SimpleHeader : UIGroup
    {
        public SimpleHeader()
        {
            Size = new Size2D(1.0f, 0.1f);   // Full width, 10% height
            Position = new Position2D(0, 0); // Top-left corner
            
            // Standard C# 12+ collection expression syntax inside a constructor:
            Children = [
                new Frame
                {
                    Size = new Size2D(0.1f, 1.0f), // 10% width, full height
                    Color = new SDL_FColor(0.3f, 0.3f, 0.35f, 1.0f)
                },
                new Frame
                {
                    Size = new Size2D(1.0f, 1.5f), // Relative to parent
                    Color = new SDL_FColor(0.2f, 0.2f, 0.25f, 1.0f),

                    // Nested children declared natively inside collection initializers!
                    Children =
                    {
                        new Frame
                        {
                            // Center-positioned title box inside the header background
                            Position = new Position2D(0.5f, 0.5f, -100, -15),
                            Size = new Size2D(0, 0, 200, 30),
                            Color = new SDL_FColor(0.8f, 0.3f, 0.3f, 1.0f),
                
                            // Element-specific real-time updates assigned inline via delegate!
                            UpdateAction = (self) =>
                            {
                                // Generate a smooth color pulsing wave recursively frame-by-frame
                                double pulse = Math.Sin(DateTime.UtcNow.Ticks / 2000000.0) * 0.5 + 0.5;
                                self.Color = new SDL_FColor(
                                    (float)(0.4 + pulse * 0.5),
                                    0.3f,
                                    0.3f,
                                    1.0f
                                );
                            }
                        }
                    }
                }
            ];
        }
    }
}
