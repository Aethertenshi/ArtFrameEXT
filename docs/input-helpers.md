# Input & File Dropping

This module covers user keyboard inputs, mouse states, and OS-native drag-and-drop actions (such as dragging `.osz` beatmap files directly into the running game window).

---

## Polling Inputs (`Input.cs`)

ArtFrame wraps low-level window inputs in a simplified polling system that tracks transitions between frames. This lets you detect both continuous presses ("is down") and edge-triggered events ("was clicked this frame").

### Keyboard Inputs (`InputHelper.Keyboard`)

Provides keyboard state polling.

#### Public Methods
*   `IsKeyDown(Keys key)`: Returns `true` continuously as long as the specified key is held down.
*   `IsKeyPressed(Keys key)`: Returns `true` **only on the exact frame the key was initially pressed** (edge-triggered). Ideal for UI menus, pauses, or single-action triggers.

*   **Example Usage:**

```csharp
using ArtFrame;
using static ArtFrame.InputHelper;

public void Update(float dt)
{
    // Detect initial tap
    if (Keyboard.IsKeyPressed(Keys.Space))
    {
        TriggerJump();
    }

    // Detect continuous holding
    if (Keyboard.IsKeyDown(Keys.LeftShift))
    {
        Sprint();
    }
}
```

---

### Mouse Inputs (`InputHelper.Mouse`)

Provides cursor positioning, scroll metrics, and mouse button tracking.

#### Public Properties
*   `Position` (`Vector2`): The current window coordinates of the cursor.
*   `CurrentScrollWheelValue` (`int`): The current absolute scroll index.
*   `LastScrollWheelValue` (`int`): The scroll index from the previous frame.

#### Public Methods
*   `LeftDown()`: Returns `true` while the left mouse button is held down.
*   `LeftReleased()`: Returns `true` while the left mouse button is released.
*   `LeftClicked()`: Edge-triggered; returns `true` on the exact frame the left button is clicked.
*   `RightDown()`: Returns `true` while the right mouse button is held down.
*   `RightReleased()`: Returns `true` while the right mouse button is released.
*   `RightClicked()`: Edge-triggered; returns `true` on the exact frame the right button is clicked.

*   **Example Usage:**

```csharp
using ArtFrame;
using static ArtFrame.InputHelper;

public void Update(float dt)
{
    Vector2 mousePos = Mouse.Position;

    if (Mouse.LeftClicked())
    {
        Console.WriteLine($"Click recorded at X: {mousePos.X}, Y: {mousePos.Y}");
    }

    int scrollDiff = Mouse.CurrentScrollWheelValue - Mouse.LastScrollWheelValue;
    if (scrollDiff != 0)
    {
        AdjustZoom(scrollDiff);
    }
}
```

---

## OS File Drag-and-Drop (`OszDropHandler.cs`)

`OszDropHandler` hooks into the native OS drag-and-drop API via SDL3. It listens for file drops, stores paths in a thread-safe queue, and allows the main game loop thread to process them safely.

### Public Methods

#### `Initialize`
Enables drag-and-drop events and registers the native SDL drop-event watcher. Invoke this once during your startup routine.
```csharp
public static void Initialize()
```

#### `DrainQueue`
Drains the queued file paths on the main thread. Passes each file path sequentially to the provided callback action. Call this inside your game's `Update` loop.
```csharp
public static void DrainQueue(Action<string> onFile)
```

*   **Example Setup:**

```csharp
using ArtFrame;
using ArtFrame.FileProcessing;

public class MySketch : IArt
{
    public void Setup()
    {
        // Start listening for drag-and-drop files
        OszDropHandler.Initialize();
    }

    public void Update(float dt)
    {
        // Check for dropped files on every frame
        OszDropHandler.DrainQueue(filePath =>
        {
            Console.WriteLine($"User dropped file: {filePath}");
            ImportDroppedFile(filePath);
        });
    }
}
```

---

## Extracting Beatmaps (`OszImporter.cs`)

Osu! beatmaps are packaged in `.osz` compressed archives (which are standard ZIP archives under a different file extension). `OszImporter` handles validating and extracting these archives into your local songs directory.

### Public Methods

#### `Import`
Validates and extracts an `.osz` archive into a uniquely named sub-folder within the specified target directory.
```csharp
public static string? Import(string oszPath, string songsPath)
```
*   **Parameters:**
    *   `oszPath`: The absolute path to the `.osz` file on disk.
    *   `songsPath`: The target root songs directory.
*   **Returns:** The absolute path to the newly extracted directory containing the beatmap files, or `null` if validation or extraction fails.

#### `IsOszFile`
Utility that returns `true` if the file exists and carries a `.osz` extension (case-insensitive).
```csharp
public static bool IsOszFile(string path)
```
