# Advanced UI Components

This section covers interactive, dynamic, and render-target-based components in the ArtFrame User Interface suite. These elements handle sophisticated rendering pipelines, scissor clipping, shader post-processing, and rich keyboard/mouse interactions.

---

## 1. Dynamic Slider (`SliderFrame`)

`SliderFrame` provides an interactive slider containing a text label, value readout, track filled segment, White/Accent accent handle, and an automated reset button.

```
  Volume 
  80%     [▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░] (#)
          ▲ Track Fill                     ▲ Handle   ▲ Reset
```

### Features
*   **Constant Layout Widths:** To prevent mouse positioning spikes mid-drag, the track and reset button columns maintain a fixed layout, regardless of whether the reset button is currently visible.
*   **Interpolated Reset:** Clicking the reset button (`#` glyph) triggers a smooth `_resetTween` (using `Easing.Cubic`, `Direction.Out`) to animate the slider back to `defaultValue` without visual snapping.

### Public Properties
*   `minValue` / `maxValue` (`float`): The boundaries of the slider range.
*   `defaultValue` (`float`): The default value (clicks to return here).
*   `currentValue` (`float`): The active value, clamped between min and max.
*   `valueFormat` (`string`): Formatting string for the readout label (e.g. `"0"` for integers, `"0.##"` for decimals).
*   `title` (`string`): Text label displayed on the left column.
*   `textWidth` (`float`): Fixed layout width reserved for the text column.
*   `trackColor` / `fillColor` / `handleColor` (`Color`): Styling colors.
*   `handleWidth` (`float`): Pixel thickness of the handle cap.

### Events
*   `onSlide` (`Action<SliderFrame>`): Invoked continuously on every frame the slider value changes during dragging (ideal for spawning audio tick sounds).
*   `onValueChanges` (`Action<SliderFrame>`): Invoked once when dragging is released, or when the automated reset tween completes.

---

## 2. Scrollable Viewport (`ScrollingFrame`)

A scrollable container supporting mouse-wheel input, smooth momentum scroll interpolation, and scissor clipping to hide overflow.

### Public Properties
*   `scrollDirection` (`Axis`): Scrolling axis (`Axis.Vertical` or `Axis.Horizontal`).
*   `clipMode` (`ClipMode`):
    *   `ClipMode.None`: Children render beyond the bounding box.
    *   `ClipMode.Clip`: Activates custom scissor clipping. Children are clipped to the viewport boundaries.
*   `scrollSensitivity` (`float`): Pixels scrolled per mouse-wheel tick.
*   `smoothing` (`float`): Scroll momentum smoothing. A value of `0` makes scroll inputs instant. Higher values (e.g. `8f`) smoothly interpolate scrolling.
*   `showScrollbar` (`bool`): Toggle scrollbar visibility.
*   `scrollbarWidth` / `scrollbarColor` / `scrollbarTrackColor` (`Color`): Scrollbar visuals.

### Technical Concept: Scissor Clipping
When `clipMode` is set to `ClipMode.Clip`, the framework manages graphics device states:
1.  It calculates the absolute viewport rectangle (`topLeft` to `resolvedSize`).
2.  It sets `ScissorTestEnable = true` using a customized rasterizer state (`_scissorState`).
3.  To ensure nested scrollboxes clip correctly, the current scissor bounding box is intersected with the parent's:
    $$\text{Active Scissor} = \text{Viewport Scissor} \cap \text{Active Device Scissor}$$
4.  Children are drawn inside this active boundary, after which standard rasterizer states are restored.

---

## 3. Keyboard Input Box (`TextBoxFrame`)

`TextBoxFrame` is an interactive text input element. It handles character capturing, caret blinking, arrow navigation, backspaces, and key-repeat delays.

### Features
*   **Native IME & Layout Capturing:** Hooks directly into SDL3 text events via `RegisterTextInput`. This automatically handles international keyboard layouts, CapsLock, and Shift combinations.
*   **Key Repeat Engine:** Polling key states can cause actions like Backspace to repeat too fast. `TextBoxFrame` incorporates a repeat controller:
    *   `RepeatDelay`: `0.40s` delay before repeating starts.
    *   `RepeatRate`: `0.05s` interval between repeats while a key is held.

### Public Properties
*   `currentText` (`string`): The active text content inside the box.
*   `placeholder` (`string`): Placeholder text displayed when the box is empty and unfocused.
*   `maxLength` (`int`): Maximum character limit (-1 represents unlimited).
*   `padding` (`float`): Inner horizontal and vertical pixel padding.
*   `isFocused` (`bool`): Read-only focus state.
*   `cursorBlinkRate` (`float`): Caret blink interval in seconds (defaults to `0.53s`).

### Events
*   `onEnter` (`Action<TextBoxFrame>`): Invoked when the user presses the Enter key. Typically defocuses the box.
*   `onFocusLost` (`Action<TextBoxFrame>`): Invoked when the element loses focus.
*   `onTextChanged` (`Action<TextBoxFrame>`): Invoked on every key entry or character deletion.

---

## 4. Shader Canvas (`EffectFrame`)

An advanced container that redirects the rendering of all its nested children into an offscreen buffer target (`RenderTarget2D`), applies a custom pixel shader, and renders the final composite on the screen.

### Public Properties
*   `Effect` (`IArtEffect`): The custom shader effect instance to apply (e.g. `GaussianBlurEffect`).
*   `BypassEffect` (`bool`):
    *   If `false`, children are drawn to the offscreen buffer to apply the shader.
    *   If `true`, **shading is completely bypassed**. Children render directly to the screen (perfect for toggling off heavy blurs or motion blurs to preserve readability).

---

## 5. Ring Primitive (`CircleFrame`)

Draws solid circles, hollow rings, or arc pie segments by generating mathematical vertex coordinates.

### Public Properties
*   `innerRadius` (`float`): Set to `0` for a solid circle. Set to a positive pixel value for a hollow ring.
*   `startAngle` (`float`): Starting angle in degrees (clockwise).
*   `endAngle` (`float`): Ending angle in degrees. Animating this property creates clean pie-charts or progress spinners.
*   `segments` (`int`): The number of triangle segments used to draw the circular edge (defaults to `64`).

---

## 6. Heartbeat Trail Emitter (`Trail`)

A specialized mathematical component that plots a parametric heart curve over time, enqueues coordinates into a FIFO queue, and draws a fading trail.

### Mathematical Definition
The trail coordinates are calculated from parametric heart equations:
$$x(\theta) = 16 \sin^3(\theta)$$
$$y(\theta) = 13 \cos(\theta) - 5 \cos(2\theta) - 2 \cos(3\theta) - \cos(4\theta)$$
The coordinate $y$ is inverted to align with the screen coordinate space (where $Y$ increases downwards).

### Public Properties
*   `LoopDuration` (`float`): The time in seconds taken to complete one full heart loop.
*   `Persistence` (`float`): The lifetime in seconds of trail segments before they fade out completely.
*   `Scale` (`float`): The physical size multiplier of the heart path.
*   `trailColor` (`Color`): The color of the trail. The alpha of individual segments fades out relative to their age.
