# Welcome to ArtFrame

**ArtFrame** is a lightweight, high-performance 2D game and interactive art framework for C#, built on top of the Microsoft FNA graphics library. It is designed to get creative projects, games, and interactive rhythm-based installations running quickly — with a declarative UI system, a powerful tween engine, rhythm-aware audio tools, and Multi-channel Signed Distance Field (MTSDF) font rendering all included out of the box.

---

## What is ArtFrame?

ArtFrame bridges the gap between low-level graphics frameworks and high-level game engines. By leveraging **Microsoft FNA** (an accurate open-source re-implementation of the XNA 4.0 Refresh), ArtFrame runs with exceptional stability and speed on Windows, macOS, and Linux.

It introduces developer-friendly layers that eliminate the boilerplate of standard game loops, drawing routines, and asset management, replacing them with modern declarative layouts, smooth easing functions, and latency-compensated rhythm indexing.

### Key Capabilities

*   **Declarative UI System** — Roblox- and CSS-inspired vector-driven interface trees including nested containers, aspect-ratio controls, text inputs, sliders, and automatic alignment modifiers.
*   **Tweener Easing Engine** — 11 smooth easing curves supporting In, Out, and In-Out directions to animate variables fluidly without snapping.
*   **Multi-Channel Signed Distance Field (MTSDF) Fonts** — Razor-sharp, resolution-independent text at any scale with outline/stroke overlays and precise bounding measurements.
*   **High-Precision Rhythm Indexing** — Precise beat-tracking metronomes and an interpolating audio clock that absorbs buffer latency, synchronized with full **osu! beatmap (.osu)** files.
*   **Audio Engine** — High-quality music streaming with tempo-scaling and pitch-shifting (Nightcore style), overlapping SFX sample management, and individual channel attributes powered by BASS.
*   **Native Drag-and-Drop** — Real-time drag-and-drop file hooks (e.g. dragging `.osz` archives directly onto the game window) built directly on SDL3 events.
*   **Framerate Suppressed Game Loop** — Separated logic updates and render ticks allowing raw input to process at high frequencies while avoiding unnecessary overdraw.

---

## Legal & Licensing Notice

ArtFrame is open-source, but it is built on top of third-party libraries that carry specific licensing obligations. Please read this notice carefully before building or distributing products with ArtFrame.

### 1. BASS Audio Engine (Commercial Restriction)
*   **Audio streaming and effect features are powered by the BASS Audio Library (by Un4seen Developments).**
*   BASS is **free for non-commercial use only**.
*   If you distribute ArtFrame in a commercial product, you **must purchase a BASS commercial license** directly from Un4seen Developments. See [Un4seen Developments](https://www.un4seen.com/) for pricing and terms.
*   The C# wrapper bindings are provided by **ManagedBass** and **ManagedBass.Fx**, which are licensed under the MIT License.

### 2. Microsoft FNA
*   FNA is licensed under the **Microsoft Public License (Ms-PL)**. You must include the FNA license in distributions containing its binaries.

### 3. ArtFrame Framework
*   The core ArtFrame source code is licensed under the **MIT License**, permitting unrestricted non-commercial use, modification, and integration.

---

## Overview of Documentation Sections

*   **[Getting Started](getting-started.md)**: System prerequisites and setting up your first project.
*   **[Core Engine](engine.md)**: Structure of the game loop, object hierarchies, and suppression.
*   **[Core Types](core-types.md)**: Color HEX parsing, `UDim2` vectors, and alignments.
*   **[Input & File Drop](input-helpers.md)**: Input managers and SDL3 drag-and-drop systems.
*   **[Graphics & Tweens](graphics-primitives.md)**: Easing curves, tweens, window parameters, and shaders.
*   **[MTSDF Fonts](fonts-rendering.md)**: Signed distance text rendering, sizing, and outline strokes.
*   **[Audio & Rhythm Sync](audio-rhythm.md)**: Audio streams, latency absorption, and beat tracker indexing.
*   **[User Interface System](ui-components.md)**: Basic and advanced components (Frames, Sliders, TextBoxes, Effect targets).
*   **[osu! Beatmaps](osu-beatmaps.md)**: Complete parsing and loading of `.osu` files and hit object models.
