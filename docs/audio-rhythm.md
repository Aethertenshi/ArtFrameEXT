# Audio & Rhythm Synchronization

ArtFrame features a professional, low-latency audio streaming and rhythm synchronization suite built on the **BASS Audio Engine**. This includes independent music/SFX control, Nightcore-style speed/pitch shifting, an interpolating audio clock that absorbs buffer jitter, and metronome beat indexers that synchronize to osu! beatmap timings.

---

## Audio Engine Wrapper (`AudioHelper.cs`)

The static `AudioHelper` handles streams, overlapping sample playbacks, master volumes, speed multipliers, and latency-compensated timings.

### Master Properties
*   `AudioHelper.GlobalSFXVolume` (`float`): Master volume factor (0.0 to 1.0) applied dynamically to all active sound effects.

### Core Audio APIs

#### `LoadMusic`
Loads a music track and establishes a **decoding stream and an FX Tempo channel** to allow real-time speed and pitch shifting.
```csharp
public static int LoadMusic(string musicName, string musicPath)
```

#### `LoadSFX`
Loads a sound effect sample. Configures the asset to allow up to **32 overlapping concurrent playbacks** without voice stealing.
```csharp
public static int LoadSFX(string soundName, string soundPath)
```

#### `PlayMusic` / `PauseMusic` / `StopMusic`
Manages music playback states.
```csharp
public static void PlayMusic(string musicName, bool restart = true)
```
```csharp
public static void PauseMusic(string musicName)
```
```csharp
public static void StopMusic(string musicName)
```

#### `PlaySFX`
Triggers an overlapping sound effect playback. The system automatically combines the sound's local volume with `GlobalSFXVolume`.
```csharp
public static void PlaySFX(string soundName)
```

#### `SetMusicVolume` / `SetSFXVolume`
Sets individual volume parameters (0.0 to 1.0).
```csharp
public static void SetMusicVolume(string musicName, float volume)
```
```csharp
public static void SetSFXVolume(string sfxName, float volume)
```

#### `SeekMusic`
Seeks music playback to the specified absolute position in seconds.
```csharp
public static void SeekMusic(string musicName, float position)
```

---

### Latency Compensation & Time Queries

Standard audio engines report playback positions based on raw buffer blocks, resulting in "jumpy" or chunky updates that lag behind actual audio output. ArtFrame fixes this:

#### `GetMusicLength`
Returns the absolute duration of the music track in seconds.
```csharp
public static float GetMusicLength(string musicName)
```

#### `GetMusicTimePlayed`
Returns the **latency-compensated** playback position in seconds.
```csharp
public static float GetMusicTimePlayed(string musicName)
```
*   **How it works:** This query extracts the current decoder position and subtracts the native audio device latency (`_audioLatency` calculated at initialization):
    $$\text{Compensated Time} = \text{Raw Decode Time} - \text{Device Latency}$$
    This yields a precise, clean timeline that aligns perfectly with audio output.

---

### Nightcore Speed & Pitch Shifting

`AudioHelper` provides real-time speed control with optional pitch adjustments.

#### `SetMusicSpeed`
Adjusts the speed multiplier of the music stream.
```csharp
public static void SetMusicSpeed(string musicName, float speedMultiplier, bool adjustPitch = false)
```
*   **Parameters:**
    *   `speedMultiplier`: The playback speed ratio (e.g. `1.5f` for 1.5x speed, `0.75f` for 75% speed).
    *   `adjustPitch`:
        *   If `false`, **pitch-correction is enabled**. The music plays faster or slower but stays in its original key.
        *   If `true`, **pitch scales with speed (Nightcore style)**. The pitch is shifted in semitones according to the speed multiplier:
            $$\text{Semitone Shift} = 12 \times \log_2(\text{Speed Multiplier})$$
            At `1.5x` speed, this shifts the pitch up by approximately **7.02 semitones** (a perfect fifth), matching classic arcade speed-up effects.

---

## Smooth Audio Clock (`InterpolatingClock.cs`)

The `InterpolatingAudioClock` filters the coarse timing updates of the audio hardware to provide an ultra-smooth time signal suitable for rendering fluid visuals and checking beat boundaries.

### How it Works
1.  On every frame, the clock advances its internal high-resolution timer (`CurrentTime`) by the frame's delta time (`dt`).
2.  When the audio hardware reports a new time step (`rawBassTime != _lastBassTime`), the clock calculates the drift:
    $$\text{Drift} = \text{Raw Hardware Time} - \text{CurrentTime}$$
3.  If the drift exceeds `SnapThreshold` (20 milliseconds, indicating a manual seek or a system lag spike), the clock snaps `CurrentTime` directly to the hardware time.
4.  If the drift is small (normal hardware timing jitter), the clock applies a **50% correction filter**:
    $$\text{CurrentTime} \leftarrow \text{CurrentTime} + (\text{Drift} \times 0.5)$$
    This acts as a visual shock absorber, smoothing out buffer jumps into a continuous, fluid signal.

---

## Metronome Beat Indexer (`RythmTracker.cs`)

The `RhythmTracker` analyzes millisecond timestamps against an array of parsed BPM timing points to determine the active beat state.

### Public Properties
*   `CurrentBeatIndex` (`int`): A strictly increasing integer index representing the absolute beat number since the start of the song.
*   `BeatProgress` (`float`): A normalized value (0.0 to 1.0) indicating the progress through the current beat. Perfect for scaling pulses, flashes, and size bumps on the beat.
*   `IsDownbeat` (`bool`): Returns `true` if the current beat falls on the first beat of the measure (e.g. Beat 0, 4, 8...).

#### `Update`
Processes the timeline and updates the metronome state.
```csharp
public void Update(float musicTimeMs, IEnumerable<OsuTimingPoint> bpmPoints)
```

---

## Rhythm Controller Integration

The `RhythmIndexer` is a helper class that combines the interpolating clock and the rhythm tracker into an active component. It is registered in the engine pool and exposes event hooks.

### Public Events
*   `OnBeat` (`Action<int>`): Fires on the exact frame the beat index advances. Receives the `CurrentBeatIndex`.
*   `OnDownbeat` (`Action<int>`): Fires on the exact frame a new measure begins.

### Public Properties
*   `MusicOffset` (`float`): Global calibration offset in milliseconds (defaults to `-33f` to align visual frames with audio transients).
*   `CurrentProgress` (`float`): The smooth, offset-adjusted millisecond timeline used for hit object positions.

*   **Example metronome setup:**

```csharp
using ArtFrame;
using ArtFrame.RythmModule;

public class MetronomeApp : IArt
{
    private RhythmIndexer _metronome;

    public void Setup()
    {
        AudioHelper.LoadMusic("song", "audio/beat.mp3");

        // 1. Initialize the rhythm controller
        var clock = new InterpolatingAudioClock();
        var tracker = new RhythmTracker();
        _metronome = new RhythmIndexer(clock, tracker, () => AudioHelper.GetMusicTimePlayed("song"));

        // 2. Subscribe to metronome events
        _metronome.OnBeat += OnBeatPressed;
        _metronome.OnDownbeat += OnMeasurePressed;

        // 3. Register to global update pipeline
        RythmHelper.AddHelper(_metronome);

        AudioHelper.PlayMusic("song");
    }

    private void OnBeatPressed(int beatIndex)
    {
        Console.WriteLine($"Beat: {beatIndex}");
    }

    private void OnMeasurePressed(int beatIndex)
    {
        Console.WriteLine($"--- New Measure Started ---");
    }
}
```
