# Getting Started with ArtFrame

To build applications with the ArtFrame framework, you should follow this guide to prepare your environment and create your first creative sketch. We skip standard language guides and jump directly into the technical setup.

---

## Prerequisites

To compile and run an ArtFrame application, your system requires the following components:

### 1. .NET Runtime
*   **You need .NET SDK version 8.0 or newer to start.** Older versions (.NET 6.0, 7.0 or .NET Framework) are not supported.
*   You can verify your version by running:

```bash
dotnet --version
```

### 2. Native Dynamic Link Libraries (DLLs)
ArtFrame relies on low-level native bindings. The following compiled libraries must reside in your application's output execution directory (where the `.exe` is generated, or loaded in the system path):
*   `SDL3.dll` — Windowing, input handling, and drag-and-drop.
*   `FNA3D.dll` — Core graphics wrapper and rendering drivers.
*   `bass.dll` — Core audio decoding and playback engine.
*   `bass_fx.dll` — Pitch-shifting, tempo-shifting, and advanced audio effect filters.

### 3. Font Shader & Atlases
Because ArtFrame utilizes multi-channel distance field rendering for pixel-perfect fonts, you must compile the HLSL shader code:
*   A compiled HLSL font shader must be located at `shaders/atlas.fxb`.
*   Font assets must be generated in JSON + PNG atlas format using tools like `msdf-atlas-gen`.

---

## Step-by-Step Setup Guide

Follow these steps to create and run your first ArtFrame application:

### Step 1: Create a Console Application
Create a new C# console project using the .NET CLI:
```bash
dotnet new console -n MyArtFrameProject
cd MyArtFrameProject
```

### Step 2: Reference Art2Framework
Reference the `Art2Framework` library. Add the project reference or compile the DLL and add it to your project references in `MyArtFrameProject.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\Art2Framework\Art2Framework.csproj" />
</ItemGroup>
```

### Step 3: Copy Native Binaries
Ensure that `SDL3.dll`, `FNA3D.dll`, `bass.dll`, and `bass_fx.dll` are copied to the build output directory (`bin/Debug/net8.0/`). You can configure this in your project file to automate the copy:
```xml
<ItemGroup>
  <None Update="SDL3.dll;FNA3D.dll;bass.dll;bass_fx.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Step 4: Write Your First Application
Open `Program.cs` and replace its contents with the code below. This sets up a window, loads a font, constructs a text element, and registers it to the rendering pipeline:

```csharp
using ArtFrame;
using ArtFrame.ArtTypes;
using ArtFrame.UserInterface;

namespace MyProject
{
    public class SimpleSketch : IArt
    {
        private TextFrame _titleLabel;

        public void Setup()
        {
            // 1. Configure the game window (Width, Height, Title)
            GraphicsHelper.ConfigureWindow(1280, 720, "ArtFrame - Hello World");

            // 2. Load the Multi-channel Signed Distance Field Font
            FontHelper.LoadAtlasFont("main", "fonts/main.json", "fonts/main.png");

            // 3. Initialize and position a declarative Text Element
            _titleLabel = new TextFrame
            {
                fontName = "main",
                text = "Hello, ArtFrame!",
                scale = 3.5f,
                color = Color.White,
                position = UDim2.FromScale(0.5f, 0.5f), // Center of the screen
                anchorX = AnchorX.Center,               // Pivot alignment horizontally
                anchorY = AnchorY.Center                // Pivot alignment vertically
            };

            // 4. Register the component to the engine's active draw pool
            SpriteHelper.Add(_titleLabel);
        }

        public void Update(float dt)
        {
            // Custom update logic goes here...
        }

        public void ManualDraw(float dt)
        {
            // Optional direct low-level drawing...
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Kick off the framework lifecycle
            Engine.Run<SimpleSketch>();
        }
    }
}
```

### Step 5: Run the Project
Build and run the project:
```bash
dotnet run
```
You will see a window appear with the text "Hello, ArtFrame!" perfectly centered, rendering at high-performance vector quality.
