# 🎨 ArtFrame

[![.NET Version](https://img.shields.io/badge/.NET-8.0%2B-blueviolet?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Engine](https://img.shields.io/badge/Graphics-Microsoft%20FNA-orange?style=for-the-badge&logo=monogame)](https://fna-xna.github.io/)
[![Audio](https://img.shields.io/badge/Audio-BASS%20Engine-brightgreen?style=for-the-badge)](https://www.un4seen.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)

A premium, lightweight 2D game and interactive art framework for C#, built on top of the robust **Microsoft FNA** console rendering platform. ArtFrame is engineered to get creative installations, games, and audio-reactive projects running at maximum performance — with a declarative UI system, a tween engine, rhythm-aware audio tools, and multi-channel signed distance field font rendering included out of the box.

---

## 🚀 Live Interactive Documentation

We have launched a full-fledged, detailed documentation site covering guides, layout rules, advanced components, and complete API references.

👉 **[Explore the Full ArtFrame Documentation Site](https://Aethertenshi.github.io/ArtFrameEXT/)**

---

## 💎 The Five Pillars of ArtFrame

ArtFrame eliminates low-level XNA boilerplate, replacing it with five designer-friendly modules:

### 1. Declarative Vector UI System
Roblox- and CSS-inspired responsive scaling + offset positioning (`UDim2`) with layout elements (`Frame`, `ImageFrame`, `TextFrame`, `Button`, `TextBoxFrame`, `SliderFrame`). Out of the box support for aspect-ratio matching (`ObjectFit.Cover` / `Contain` / `None`), scissor clipping viewports, and automated layout modifiers (`ListLayout`, `GridLayout`).

### 2. High-Resolution Tweening
A robust tween engine (`Tweener`) featuring 11 smooth mathematical easing curves (Linear, Sine, Cubic, Back, Elastic, Fluid) supporting `In`, `Out`, and `InOut` directions. Redirection-aware (`Restart`) to prevent visual snapping when targets change mid-transition.

### 3. Latency-Compensated Audio
High-fidelity streaming, per-channel attributes, and overlapping SFX sample channels powered by BASS. Includes Nightcore-style speed/pitch shifting, adjusting tempo percents and semitone frequencies dynamically.

### 4. Metronomes & Rhythm Sync
An interpolating audio clock that filters hardware buffer jitter, combined with a `RhythmTracker` to sync visual animation ticks and tap events directly with parsed **osu! beatmaps (`.osu` files)**.

### 5. MTSDF Font Rendering
Multi-channel signed distance field text rendering. Produces razor-sharp, vector-quality character outlines at any scale with custom thickness strokes and precise physical boundary bounding-box metrics.

---

## 🛠️ Quick Preview

Here is how simple it is to build a high-performance interactive window with a centered vector label:

```csharp
using ArtFrame;
using ArtFrame.ArtTypes;
using ArtFrame.UserInterface;

public class MySketch : IArt
{
    private TextFrame _label;

    public void Setup()
    {
        // 1. Configure the graphics window
        GraphicsHelper.ConfigureWindow(1280, 720, "My ArtFrame Sketch");
        
        // 2. Load the multi-channel distance field font atlas
        FontHelper.LoadAtlasFont("main", "fonts/main.json", "fonts/main.png");

        // 3. Create a centered, responsive text element
        _label = new TextFrame
        {
            fontName = "main",
            text = "Hello, ArtFrame!",
            scale = 3.5f,
            color = Color.White,
            position = UDim2.FromScale(0.5f, 0.5f), // Relative screen center
            anchorX = AnchorX.Center,               // Pivot center alignment
            anchorY = AnchorY.Center
        };

        // 4. Register to the engine's active draw pool
        SpriteHelper.Add(_label);
    }
}

class Program
{
    static void Main() => Engine.Run<MySketch>();
}
```

---

## ⚠️ Third-Party Licensing Warnings

ArtFrame incorporates specialized third-party libraries. Please review their obligations:

*   **BASS Audio Engine (Commercial Restriction)**: Audio streaming is powered by BASS (by Un4seen Developments). **BASS is free for non-commercial use only.** If you distribute a commercial product containing ArtFrame, you must purchase a BASS commercial license from [Un4seen Developments](https://www.un4seen.com/).
*   **Microsoft FNA**: Licensed under the **Microsoft Public License (Ms-PL)**.
*   **OsuLib & ManagedBass**: C# bindings are licensed under the **MIT License**.

---

## 📄 License

ArtFrame source code is licensed under the **MIT License**. 

```
Copyright (c) 2025 Aethertenshi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
...
```
*(See [LICENSE](LICENSE) for the full license text.)*