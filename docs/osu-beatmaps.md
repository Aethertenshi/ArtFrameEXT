# osu! Beatmap Integration

ArtFrame features an integrated parsing and discovery suite for **osu! beatmaps (`.osu` files)**. This enables developers to scan song directories, parse metadata, read individual hit objects (taps, sliders, holds), extract background images, and synchronize metronome animations using beatmap timing points.

---

## 1. Beatmap Model (`OsuBeatmap.cs`)

The `OsuBeatmap` class holds the parsed data of a `.osu` file and provides helpers to query metadata, timing, and hit objects.

### Raw Data Dictionaries
*   `General` (`Dictionary<string, string>`): Settings like `AudioFilename` or `PreviewTime`.
*   `Metadata` (`Dictionary<string, string>`): Track identifiers (`Title`, `Artist`, `Creator`, `Version`/Difficulty).
*   `Difficulty` (`Dictionary<string, string>`): Game parameters (`HPDifficulty`, `CS` (Circle Size), `OverallDifficulty`, `ApproachRate`, `SliderMultiplier`).
*   `TimingPoints` (`List<OsuTimingPoint>`): List of all timing points (red and green lines).
*   `HitObjects` (`List<OsuHitObject>`): List of all parsed interactive objects, sorted chronologically.

### Filtered Enumerations
*   `Notes` (`IEnumerable<OsuNote>`): Filters and returns only circle tap notes.
*   `Sliders` (`IEnumerable<OsuSlider>`): Filters and returns only sliders.
*   `BpmPoints` (`IEnumerable<OsuTimingPoint>`): Filters and returns only uninherited (red-line) timing points carrying BPM parameters.

### Timing & Speed Helpers

#### `GetTimingPointAt`
Returns the active timing point at the specified timestamp.
```csharp
public OsuTimingPoint? GetTimingPointAt(double t, bool uninheritedOnly = false)
```

#### `GetBpmAt`
Returns the BPM active at the specified timestamp.
```csharp
public double GetBpmAt(double timeMs)
```

#### `GetBackgroundFullPath`
Returns the absolute path to the beatmap's background image filename listed in the events section.
```csharp
public string GetBackgroundFullPath()
```

---

### Technical Detail: Resolving Slider Velocities
Osu! sliders store their physical length in pixels and the number of slides (repeats), but do not explicitly specify their duration in milliseconds. The duration depends on the active BPM timing point (red line) and the active velocity multiplier (green line).

ArtFrame's `ResolveSliderVelocities()` automatically calculates this duration at load time:
1.  Locates the active BPM timing point (`BeatLength` in ms per beat) and the active velocity multiplier (`velMult`):
    $$\text{velMult} = \begin{cases} 1.0 & \text{if uninherited (red line)} \\ -100 / \text{BeatLength} & \text{if inherited (green line)} \end{cases}$$
2.  Computes the slider speed in pixels per beat based on the map's `SliderMultiplier`:
    $$\text{PixelsPerBeat} = 100 \times \text{SliderMultiplier} \times \text{velMult}$$
3.  Calculates the duration of a single pass of the slider (in milliseconds):
    $$\text{SinglePassMs} = \frac{\text{Length}}{\text{PixelsPerBeat}} \times \text{BeatLength}$$
4.  Calculates the total slider duration and effective velocity:
    $$\text{DurationMs} = \text{SinglePassMs} \times \text{Slides}$$
    $$\text{EffectiveVelocityPxPerMs} = \frac{\text{Length}}{\text{SinglePassMs}}$$

---

## 2. Beatmap Parser (`OsuParser.cs`)

Parses raw `.osu` text data into an `OsuBeatmap` instance.

### Public Methods

#### `Parse`
Reads and parses a `.osu` file from disk. Automatically resolves slider velocities before returning.
```csharp
public OsuBeatmap Parse(string path)
```

#### `ParseText`
Parses raw `.osu` text content directly from a string.
```csharp
public OsuBeatmap ParseText(string content, string sourcePath = "")
```

---

## 3. Beatmap Discovery (`OsuScanner.cs`)

A utility for scanning folders and parsing multiple beatmaps in batch.

### Public Methods

#### `FindOsuFiles`
Recursively searches a root folder and returns the absolute paths of all discovered `.osu` files.
```csharp
public IReadOnlyList<string> FindOsuFiles(string rootDirectory)
```

#### `ScanAll`
Finds and parses every `.osu` file in the specified root folder, skipping files that fail to parse.
```csharp
public IReadOnlyList<OsuBeatmap> ScanAll(string rootDirectory, Action<string, Exception>? onError = null)
```

#### `ScanLazy`
Lazily parses files one at a time using an iterator. This is memory-efficient for scanning very large directories.
```csharp
public IEnumerable<OsuBeatmap> ScanLazy(string rootDirectory, Action<string, Exception>? onError = null)
```

#### `ParseSet`
Parses all `.osu` files directly inside a single beatmap directory (representing a single song containing multiple difficulties).
```csharp
public IReadOnlyList<OsuBeatmap> ParseSet(string beatmapSetDirectory)
```

---

## 4. Domain Models (`Models/`)

### `OsuHitObject` (Base Class)
The abstract base class for all objects in the beatmap's hit list.

#### Properties
*   `X` / `Y` (`int`): Coordinates (on the standard $512 \times 384$ osu! playfield coordinate space).
*   `Time` (`int`): The starting timestamp of the hit object in milliseconds.
*   `ObjectType` (`HitObjectType`): Enumeration type (`Note`, `Slider`, `Spinner`, `Hold`, `Unknown`).
*   `IsNewCombo` (`bool`): True if this object starts a new combo chain.
*   `HitSound` (`int`): Bitmask flags representing active hitsound effects (Normal, Whistle, Finish, Clap).

---

### Circle Notes (`OsuNote`)
Inherits all properties from the base `OsuHitObject`. Represents a single tap/click circle.

---

### Sliders (`OsuSlider`)
Represents a slider path.

#### Properties
*   `CurveType` (`SliderCurveType`): The interpolation path type (`Bezier`, `CatmullRom`, `Linear`, `PerfectCircle`, `Unknown`).
*   `CurvePoints` (`List<SliderPoint>`): Control points defining the slider path (excluding the starting position coordinate).
*   `Slides` (`int`): The number of repeat passes (1 = one way, 2 = back-and-forth once).
*   `Length` (`double`): Visual length of the slider path in osu! pixels.
*   `DurationMs` (`double`): The resolved total duration in milliseconds (calculated by the engine).
*   `EndTime` (`double`): The millisecond timestamp when the slider ends.

---

### Timing Points (`OsuTimingPoint`)
Represents an entry in the beatmap's timing list.

#### Properties
*   `Time` (`double`): The starting timestamp of the timing point in milliseconds.
*   `BeatLength` (`double`):
    *   For uninherited points (red lines): Milliseconds per beat (positive value).
    *   For inherited points (green lines): Velocity multiplier factor expressed as a negative percentage ($-\text{multiplier} \times 100$).
*   `Meter` (`int`): Time signature numerator (defaults to `4` for 4/4 time).
*   `IsUninherited` (`bool`):
    *   `true` = Red line (defines new BPM and resets metronome synchronization).
    *   `false` = Green line (inherits BPM, adjusts local volume or slider velocity).

#### Derived Properties
*   `BPM` (`double`): Returns the calculated BPM if the point is uninherited, otherwise returns `NaN`.
    $$\text{BPM} = \frac{60,000}{\text{BeatLength}}$$
*   `VelocityMultiplier` (`double`): Calculates the slider velocity multiplier.
    $$\text{Multiplier} = \begin{cases} 1.0 & \text{if IsUninherited} \\ -100 / \text{BeatLength} & \text{if Inherited} \end{cases}$$
*   `IsKiai` (`bool`): Returns `true` if Kiai flash mode is active at this timing point.
