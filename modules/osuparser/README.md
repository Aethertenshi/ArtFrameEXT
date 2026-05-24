# OsuLib — C# .osu File Parsing Library

A clean, zero-dependency C# library for scanning beatmap folders, parsing `.osu` files,
and working with hit objects, timing points, and metadata.

---

## Table of Contents

1. [Project Setup](#1-project-setup)
2. [Core Classes at a Glance](#2-core-classes-at-a-glance)
3. [Parsing a Single File](#3-parsing-a-single-file)
4. [Reading Metadata](#4-reading-metadata)
5. [Reading General / Difficulty Sections](#5-reading-general--difficulty-sections)
6. [Timing Points](#6-timing-points)
7. [Hit Objects — Notes vs Sliders](#7-hit-objects--notes-vs-sliders)
8. [Slider Velocity & Duration](#8-slider-velocity--duration)
9. [Scanning a Whole Folder](#9-scanning-a-whole-folder)
10. [Full Working Example](#10-full-working-example)
11. [API Reference](#11-api-reference)

---

## 1. Project Setup

Add **OsuLib** as a project reference (or drop the source files into your project):

```xml
<!-- YourProject.csproj -->
<ItemGroup>
  <ProjectReference Include="..\OsuLib\OsuLib.csproj" />
</ItemGroup>
```

Target framework: **.NET 8** (or any modern .NET — change `<TargetFramework>` as needed).

---

## 2. Core Classes at a Glance

| Class | What it does |
|---|---|
| `OsuParser` | Parses a single `.osu` file or raw text into an `OsuBeatmap` |
| `OsuBeatmap` | Container for all sections of one difficulty |
| `OsuTimingPoint` | One row from `[TimingPoints]` |
| `OsuHitObject` | Abstract base — either `OsuNote` or `OsuSlider` |
| `OsuNote` | A single tap/circle |
| `OsuSlider` | A slider with curve data, velocity, and duration |
| `OsuScanner` | Walks a directory tree, finds all `.osu` files, bulk-parses |

---

## 3. Parsing a Single File

```csharp
using OsuLib;

var parser  = new OsuParser();
OsuBeatmap bm = parser.Parse(@"C:\osu!\Songs\519521 Hotaru Murasaki - ReTrymenT\map.osu");

Console.WriteLine(bm);
// → [Hotaru Murasaki – Re:TrymenT] Cookiezi's Insane  (ID:1415439)  512 objects
```

You can also parse from a string in memory:

```csharp
string content = File.ReadAllText("map.osu");
OsuBeatmap bm  = parser.ParseText(content);
```

---

## 4. Reading Metadata

```csharp
// Strongly-typed convenience properties (most common fields)
Console.WriteLine(bm.Title);           // Re:TrymenT
Console.WriteLine(bm.TitleUnicode);    // Re:TrymenT
Console.WriteLine(bm.Artist);          // Hotaru Murasaki
Console.WriteLine(bm.ArtistUnicode);   // 紫咲ほたる
Console.WriteLine(bm.Creator);         // eLy
Console.WriteLine(bm.Version);         // Cookiezi's Insane
Console.WriteLine(bm.BeatmapId);       // 1415439  (int)
Console.WriteLine(bm.BeatmapSetId);    // 519521   (int)

// Generic accessor — works for ANY key in [Metadata]
string tags   = bm.GetMeta("Tags");
string source = bm.GetMeta("Source");

// With a fallback default if the key is missing
string extra  = bm.GetMeta("SomeUnknownKey", "N/A");
```

---

## 5. Reading General / Difficulty Sections

```csharp
// --- [General] ---
string audioFile   = bm.GetGeneral("AudioFilename");   // "audio.mp3"
string previewTime = bm.GetGeneral("PreviewTime");     // "57811"
string mode        = bm.GetGeneral("Mode");            // "0"

// Typed shorthand properties
Console.WriteLine(bm.AudioFilename);  // audio.mp3
Console.WriteLine(bm.PreviewTime);    // 57811  (int)
Console.WriteLine(bm.Mode);           // 0      (int)

// --- [Difficulty] ---
string ar  = bm.GetDifficulty("ApproachRate");    // e.g. "9"
string cs  = bm.GetDifficulty("CircleSize");      // e.g. "4"
string od  = bm.GetDifficulty("OverallDifficulty");
string hp  = bm.GetDifficulty("HPDrainRate");
string sm  = bm.GetDifficulty("SliderMultiplier");
string stv = bm.GetDifficulty("SliderTickRate");

// All values come back as strings; parse as needed:
double approachRate = double.Parse(bm.GetDifficulty("ApproachRate"),
                                   CultureInfo.InvariantCulture);
```

---

## 6. Timing Points

Every row in `[TimingPoints]` becomes an `OsuTimingPoint`.

```csharp
foreach (OsuTimingPoint tp in bm.TimingPoints)
{
    if (tp.IsUninherited)
    {
        // Red line — sets the BPM
        Console.WriteLine($"BPM change at {tp.Time}ms → {tp.BPM:F2} BPM");
    }
    else
    {
        // Green line — multiplies slider velocity
        Console.WriteLine($"Velocity ×{tp.VelocityMultiplier:F2} at {tp.Time}ms");
    }
}
```

**Key properties:**

| Property | Type | Description |
|---|---|---|
| `Time` | `double` | Start time in milliseconds |
| `BeatLength` | `double` | ms/beat (positive) or velocity raw value (negative) |
| `IsUninherited` | `bool` | `true` = red line, `false` = green line |
| `BPM` | `double` | Beats per minute; `NaN` for inherited points |
| `VelocityMultiplier` | `double` | Slider speed multiplier; always `1.0` for red lines |
| `Meter` | `int` | Beats per measure |
| `Volume` | `int` | Hit-sound volume 0–100 |
| `IsKiai` | `bool` | Whether Kiai time is active |

**Get the BPM at a specific timestamp:**

```csharp
double bpmAt10s = bm.GetBpmAt(10_000);   // ms
double bpmAt30s = bm.GetBpmAt(30_000);
```

**Get the active timing point at a time:**

```csharp
// Nearest timing point (any type)
OsuTimingPoint? active = bm.GetTimingPointAt(12_500);

// Nearest red line (BPM) only
OsuTimingPoint? redLine = bm.GetTimingPointAt(12_500, uninheritedOnly: true);
```

**Iterate only BPM points:**

```csharp
foreach (var pt in bm.BpmPoints)
    Console.WriteLine($"{pt.Time}ms → {pt.BPM:F2} BPM");
```

---

## 7. Hit Objects — Notes vs Sliders

All hit objects live in `bm.HitObjects` as `OsuHitObject`.
Use pattern matching (or LINQ) to split them:

```csharp
using OsuLib.Models;

foreach (OsuHitObject obj in bm.HitObjects)
{
    if (obj is OsuNote note)
    {
        Console.WriteLine($"Note at {note.Time}ms  pos=({note.X},{note.Y})");
    }
    else if (obj is OsuSlider slider)
    {
        Console.WriteLine($"Slider at {slider.Time}ms  pos=({slider.X},{slider.Y})"
                        + $"  slides={slider.Slides}  len={slider.Length:F0}px"
                        + $"  dur={slider.DurationMs:F0}ms  end={slider.EndTime:F0}ms");
    }
}
```

**Filtered views (no casting needed):**

```csharp
// All hit circles
foreach (OsuNote note in bm.Notes)    { ... }

// All sliders
foreach (OsuSlider s in bm.Sliders)   { ... }
```

### OsuNote properties

| Property | Type | Description |
|---|---|---|
| `X`, `Y` | `int` | Position on the 512×384 playfield |
| `Time` | `int` | When to tap, in milliseconds |
| `HitSound` | `int` | Bitmask: Normal=1, Whistle=2, Finish=4, Clap=8 |
| `IsNewCombo` | `bool` | Starts a new combo |
| `ComboSkip` | `int` | How many combo colours to skip (0–7) |

### OsuSlider properties

All `OsuNote` properties, plus:

| Property | Type | Description |
|---|---|---|
| `CurveType` | `SliderCurveType` | `Bezier`, `Linear`, `PerfectCircle`, `CatmullRom` |
| `CurvePoints` | `List<SliderPoint>` | Control points (excluding the start X,Y) |
| `Slides` | `int` | 1 = one direction, 2 = back-and-forth, etc. |
| `Length` | `double` | Visual path length in osu! pixels (one pass) |
| `DurationMs` | `double` | Total slider duration in ms (all slides) |
| `EndTime` | `double` | `Time + DurationMs` |
| `EffectiveVelocityPxPerMs` | `double` | Pixels per millisecond the ball travels |
| `EdgeSounds` | `List<int>` | Hit-sound at head, each repeat, and tail |

---

## 8. Slider Velocity & Duration

After parsing, `OsuParser` automatically calls `bm.ResolveSliderVelocities()`,
which populates `DurationMs`, `EndTime`, and `EffectiveVelocityPxPerMs` on
every slider using the official osu! formula:

```
pixelsPerBeat = 100 × SliderMultiplier × velocityMultiplier
durationMs    = (Length / pixelsPerBeat) × beatLengthMs × Slides
velocityPxMs  = Length / (durationMs / Slides)
```

where `velocityMultiplier` comes from the nearest green-line timing point
(or `1.0` if none exists), and `beatLengthMs` comes from the nearest red line.

```csharp
foreach (OsuSlider s in bm.Sliders)
{
    Console.WriteLine(
        $"  Slider @{s.Time}ms  "
      + $"velocity={s.EffectiveVelocityPxPerMs:F3} px/ms  "
      + $"duration={s.DurationMs:F1}ms  "
      + $"ends@{s.EndTime:F1}ms");
}
```

**Query the effective slider velocity at any arbitrary timestamp** (without a
real slider being there — useful for editors or hit-object generators):

```csharp
double velAt5s  = bm.GetSliderVelocityAt(5_000);   // px/ms
double velAt60s = bm.GetSliderVelocityAt(60_000);
Console.WriteLine($"Slider ball moves {velAt5s:F3} px/ms at the 5-second mark");
```

**Re-run the resolver** if you add/modify timing points at runtime:

```csharp
bm.TimingPoints.Add(new OsuTimingPoint { Time = 10_000, BeatLength = -75, IsUninherited = false });
bm.TimingPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
bm.ResolveSliderVelocities();   // recalculate everything
```

---

## 9. Scanning a Whole Folder

```csharp
var scanner = new OsuScanner();

// ── Option A: parse everything, collect failures ──────────────────────────
var beatmaps = scanner.ScanAll(
    @"C:\osu!\Songs",
    onError: (path, ex) => Console.Error.WriteLine($"Failed: {path} — {ex.Message}")
);

Console.WriteLine($"Parsed {beatmaps.Count} beatmaps");

// ── Option B: lazy / streaming (low memory) ───────────────────────────────
foreach (OsuBeatmap bm in scanner.ScanLazy(@"C:\osu!\Songs"))
{
    ProcessBeatmap(bm);   // process one at a time, not all in RAM
}

// ── Option C: filtered scan ───────────────────────────────────────────────
// Only osu!standard maps with more than 300 objects
var stdMaps = scanner.ScanFiltered(
    @"C:\osu!\Songs",
    filter: bm => bm.Mode == 0 && bm.HitObjects.Count > 300
);

// ── Option D: single beatmapset folder ───────────────────────────────────
// All difficulties inside one set folder
var diffs = scanner.ParseSet(@"C:\osu!\Songs\519521 Hotaru Murasaki - ReTrymenT");
foreach (var diff in diffs)
    Console.WriteLine($"  {diff.Version}  ({diff.HitObjects.Count} objects)");

// ── Just get file paths without parsing ──────────────────────────────────
IReadOnlyList<string> paths = scanner.FindOsuFiles(@"C:\osu!\Songs");
Console.WriteLine($"Found {paths.Count} .osu files");
```

---

## 10. Full Working Example

```csharp
using System;
using System.Globalization;
using System.Linq;
using OsuLib;
using OsuLib.Models;

// --- Parse ---
var parser  = new OsuParser();
OsuBeatmap  bm = parser.Parse(@"C:\osu!\Songs\519521\map.osu");

// --- Identity ---
Console.WriteLine($"Song   : {bm.Artist} – {bm.Title}");
Console.WriteLine($"Mapper : {bm.Creator}  |  Diff: {bm.Version}");
Console.WriteLine($"Audio  : {bm.AudioFilename}  preview @ {bm.PreviewTime}ms");

// --- Difficulty numbers ---
Console.WriteLine($"AR={bm.GetDifficulty("ApproachRate")}  " +
                  $"CS={bm.GetDifficulty("CircleSize")}  " +
                  $"OD={bm.GetDifficulty("OverallDifficulty")}");

// --- Timing ---
Console.WriteLine($"\nBPM points:");
foreach (var tp in bm.BpmPoints)
    Console.WriteLine($"  {tp.Time,8:F0}ms  {tp.BPM:F2} BPM");

// --- Objects ---
int noteCount   = bm.Notes.Count();
int sliderCount = bm.Sliders.Count();
Console.WriteLine($"\nHit objects: {noteCount} notes, {sliderCount} sliders");

// --- First 5 notes ---
Console.WriteLine("\nFirst 5 notes:");
foreach (var n in bm.Notes.Take(5))
    Console.WriteLine($"  @{n.Time}ms  ({n.X},{n.Y})");

// --- First 5 sliders with velocity ---
Console.WriteLine("\nFirst 5 sliders:");
foreach (var s in bm.Sliders.Take(5))
{
    Console.WriteLine($"  @{s.Time}ms  ({s.X},{s.Y})"
        + $"  {s.CurveType}  ×{s.Slides}"
        + $"  {s.Length:F0}px"
        + $"  {s.DurationMs:F0}ms"
        + $"  → {s.EffectiveVelocityPxPerMs:F3}px/ms"
        + $"  ends@{s.EndTime:F0}ms");
}

// --- Velocity at a specific time ---
double vel = bm.GetSliderVelocityAt(30_000);
Console.WriteLine($"\nSlider velocity at 30s: {vel:F4} px/ms");

// --- Active timing point at a time ---
var at20 = bm.GetTimingPointAt(20_000);
Console.WriteLine($"Active timing @ 20s: {at20}");
```

---

## 11. API Reference

### `OsuParser`

| Method | Returns | Description |
|---|---|---|
| `Parse(path)` | `OsuBeatmap` | Parse a `.osu` file by path |
| `ParseText(content, sourcePath?)` | `OsuBeatmap` | Parse from a string |

### `OsuBeatmap`

**Section accessors**

| Method | Description |
|---|---|
| `GetGeneral(key, default?)` | Value from `[General]` |
| `GetMeta(key, default?)` | Value from `[Metadata]` |
| `GetDifficulty(key, default?)` | Value from `[Difficulty]` |
| `GetEditor(key, default?)` | Value from `[Editor]` |

**Convenience properties**

`Title`, `TitleUnicode`, `Artist`, `ArtistUnicode`, `Creator`, `Version`,
`BeatmapId`, `BeatmapSetId`, `AudioFilename`, `PreviewTime`, `Mode`

**Collections**

| Property | Type | Description |
|---|---|---|
| `HitObjects` | `List<OsuHitObject>` | All objects, sorted by time |
| `Notes` | `IEnumerable<OsuNote>` | Only tap notes |
| `Sliders` | `IEnumerable<OsuSlider>` | Only sliders |
| `TimingPoints` | `List<OsuTimingPoint>` | All timing points |
| `BpmPoints` | `IEnumerable<OsuTimingPoint>` | Red lines only |
| `Events` | `List<string>` | Raw `[Events]` lines |

**Timing helpers**

| Method | Description |
|---|---|
| `GetTimingPointAt(ms, uninheritedOnly?)` | Active timing point at given time |
| `GetBpmAt(ms)` | BPM active at given time |
| `GetSliderVelocityAt(ms)` | Slider velocity (px/ms) at any time |
| `ResolveSliderVelocities()` | Recalculate all slider durations/velocities |

### `OsuScanner`

| Method | Description |
|---|---|
| `FindOsuFiles(dir)` | Returns all `.osu` paths recursively |
| `ScanAll(dir, onError?)` | Parse all, return list |
| `ScanLazy(dir, onError?)` | Parse lazily via `IEnumerable` |
| `ScanFiltered(dir, filter, onError?)` | Parse and filter by predicate |
| `ParseSet(dir)` | Parse all diffs in one beatmapset folder |

### `OsuTimingPoint`

| Property | Description |
|---|---|
| `Time` | Start offset in ms |
| `BPM` | Beats per minute (`NaN` for inherited) |
| `VelocityMultiplier` | Slider speed multiplier (`1.0` for uninherited) |
| `IsUninherited` | `true` = red line (BPM), `false` = green line (velocity) |
| `IsKiai` | Kiai mode active |
| `Volume` | Hit-sound volume (0–100) |
| `Meter` | Beats per measure |

### `OsuSlider`

| Property | Description |
|---|---|
| `CurveType` | `Bezier`, `Linear`, `PerfectCircle`, `CatmullRom` |
| `CurvePoints` | Control points (not including start X,Y) |
| `Slides` | Number of passes (1 = one-way) |
| `Length` | Path length in osu! pixels (one pass) |
| `DurationMs` | Total duration in ms (all slides) |
| `EndTime` | `Time + DurationMs` |
| `EffectiveVelocityPxPerMs` | Slider ball speed |
| `EdgeSounds` | Hit-sounds at head/repeats/tail |

---

*Built against the [osu! file format v14 specification](https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29).*
