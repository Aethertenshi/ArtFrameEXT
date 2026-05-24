# ArtFrame

A lightweight 2D game and interactive art framework for C#, built on top of [Microsoft FNA](https://fna-xna.github.io/). ArtFrame is designed to get creative projects running quickly — with a declarative UI system, a tween engine, rhythm-aware audio tools, and MTSDF font rendering all included out of the box.

---

## Features

- **Declarative UI system** — Roblox/CSS-inspired `UDim2` layout with `Frame`, `ImageFrame`, `TextFrame`, `Button`, `ImageButton`, `SliderFrame`, `RingFrame`, and more. Supports anchoring, rotation, alpha, `ObjectFit` modes (`Fill`, `Contain`, `Cover`, `None`), and nestable children.
- **Tween engine** — `Tweener` with 11 easing curves (`Linear`, `Quadratic`, `Cubic`, `Sine`, `Elastic`, `Back`, `Fluid`, and more) and `In / Out / InOut` directions. Start, restart from current value, or snap instantly.
- **MTSDF font rendering** — Sharp, resolution-independent text at any scale via multi-channel signed distance field atlases, with stroke support, per-glyph metrics, and precise bounding-box measurement.
- **Audio system** — Music streaming with tempo and pitch shifting, overlapping SFX sample playback, per-channel volume control, and latency-compensated playback time. Powered by [BASS / ManagedBass](https://www.un4seen.com/).
- **Rhythm indexing** — Beat-tracking helpers (`RhythmIndexer`) with event callbacks for `OnBeat` and `OnDownbeat`, an interpolating audio clock for smooth sync, and osu! beatmap support via OsuLib.
- **Primitive drawing** — Rectangles, rotated rectangles, and ring segments drawn via `BasicEffect` vertex primitives.
- **Average color sampling** — `TextureHelper.GetAverageColor` for dynamic tinting from texture data.
- **Input helpers** — Keyboard and mouse wrappers for pressed/held/clicked state and text input registration.
- **Draw suppression** — Independent input and render framerates for high-precision timing without overdrawing.

---

## Getting Started

### Prerequisites

- [.NET 6+](https://dotnet.microsoft.com/)
- [FNA](https://fna-xna.github.io/) (included as a submodule or NuGet reference)
- FNA native libraries for your target platform
- MTSDF font atlas (JSON + PNG) generated with [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen)
- Compiled font shader at `shaders/atlas.fxb`

### Basic Usage

Implement `IArt` and call `Engine.Run<T>()`:

```csharp
using ArtFrame;
using ArtFrame.ArtTypes;
using ArtFrame.UserInterface;

public class MyArt : IArt
{
    private TextFrame _label;

    public void Setup()
    {
        GraphicsHelper.ConfigureWindow(1280, 720, "My Art");
        FontHelper.LoadAtlasFont("main", "fonts/main.json", "fonts/main.png");

        _label = new TextFrame
        {
            fontName = "main",
            text = "Hello, ArtFrame!",
            scale = 3f,
            color = Color.White,
            position = new UDim2(0.5f, 0, 0.5f, 0),
            anchorX = AnchorX.Center,
            anchorY = AnchorY.Center
        };

        SpriteHelper.Add(_label);
    }

    public void Update(float dt) { }
    public void ManualDraw(float dt) { }
}

class Program
{
    static void Main() => Engine.Run<MyArt>();
}
```

### Loading Audio

```csharp
AudioHelper.LoadMusic("bgm", "audio/track.mp3");
AudioHelper.LoadSFX("hit", "audio/hit.wav");

AudioHelper.PlayMusic("bgm");
AudioHelper.PlaySFX("hit");
```

### Tweening

```csharp
var tween = TweenHelper.AddTween(new Tweener());
tween.Start(0.4f, 0f, 1f, Easing.Cubic, Direction.Out);

// In Update:
myFrame.alpha = tween.CurrentValue;
```

---

## Project Structure

```
ArtFrame/
├── Engine.cs            # IArt interface, ArtObject base class, game loop
├── GraphicsHelper.cs    # SpriteBatch management, primitives, tween pool, image loading
├── UserInterfaces.cs    # UI components (Frame, ImageFrame, TextFrame, Button, Slider…)
├── FontsHelper.cs       # MTSDF font loading and rendering
├── AudioHelper.cs       # BASS audio wrapper and rhythm indexer
├── SimpleEasings.cs     # Tweener and easing math
└── shaders/
    └── atlas.fxb        # Compiled MTSDF font shader (HLSL)
```

---

## Third-Party Dependencies

ArtFrame is built on top of and makes use of the following third-party libraries. Please review their respective licenses before redistributing.

### Microsoft FNA
ArtFrame uses [FNA](https://fna-xna.github.io/) as its underlying game framework for windowing, rendering, input, and the game loop.
FNA is developed by Ethan Lee and is licensed under the **Microsoft Public License (Ms-PL)**.
See: https://github.com/FNA-XNA/FNA/blob/master/licenses/LICENSE

### BASS Audio Engine (via ManagedBass)
**Audio playback is powered by [BASS](https://www.un4seen.com/) by Un4seen Developments.**

> ⚠️ **Important licensing notice:** BASS is **free for non-commercial use only**. If you use ArtFrame in a commercial product, you must purchase a BASS license from Un4seen Developments. See https://www.un4seen.com/ for details and pricing.

The C# bindings are provided by [ManagedBass](https://github.com/ManagedBass/ManagedBass) and [ManagedBass.Fx](https://github.com/ManagedBass/ManagedBass), both licensed under the **MIT License**.

### OsuLib
Beatmap parsing and rhythm utilities use **OsuLib** for reading osu! `.osu` beatmap files.

---

## License

```
MIT License

Copyright (c) 2025 Aethertenshi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

> Note: The MIT license above applies to ArtFrame source code only. Third-party dependencies (BASS, FNA, etc.) are governed by their own licenses as described above.