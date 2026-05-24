using ManagedBass;
using ManagedBass.Fx;
using ArtFrame.RythmModule;
using OsuLib;

namespace ArtFrame
{
    public static class AudioHelper
    {
        // Variables
        private static Dictionary<string, int> _musics = new();
        private static Dictionary<string, int> _sfxs = new();
        private static Dictionary<string, float> _sfxLocalVolumes = new Dictionary<string, float>();
        private static float _audioLatency = 0f;

        public static float GlobalSFXVolume { get; set; } = 1.0f;

        // Methods
        public static void UseAudioEngine()
        {
            Bass.Configure(Configuration.PlaybackBufferLength, 20);
            Bass.Configure(Configuration.UpdatePeriod, 5);

            if (!Bass.Init(-1, 44100, DeviceInitFlags.Latency, IntPtr.Zero))
                Console.WriteLine($"BASS Init Error: {Bass.LastError}");

            Bass.GetInfo(out BassInfo info);
            _audioLatency = info.Latency / 1000f;
        }

        public static int LoadMusic(string musicName, string musicPath)
        {
            if (!_musics.ContainsKey(musicName))
            {
                // 1. Create a DECODING stream (required for BASS_FX)
                int decoder = Bass.CreateStream(musicPath, 0, 0, BassFlags.Decode);

                if (decoder == 0)
                {
                    Console.WriteLine($"Failed to load decoder for {musicName}: {Bass.LastError}");
                    return 0;
                }

                // 2. Create the Tempo stream from the decoder
                // BassFlags.FxFreeSource ensures that when we free the tempo stream, the decoder is freed too
                int tempoHandle = BassFx.TempoCreate(decoder, BassFlags.FxFreeSource);

                if (tempoHandle == 0)
                {
                    Console.WriteLine($"Failed to create tempo stream for {musicName}: {Bass.LastError}");
                    Bass.StreamFree(decoder);
                    return 0;
                }

                _musics.Add(musicName, tempoHandle);
            }
            return _musics[musicName];
        }

        public static int LoadSFX(string soundName, string soundPath)
        {
            if (!_sfxs.ContainsKey(soundName))
            {
                // 1. Switch to SampleLoad. 
                // Set 'max' (the 4th param) to 16 or 32 to allow that many overlapping sounds.
                int handle = Bass.SampleLoad(soundPath, 0, 0, 32, BassFlags.Default);

                if (handle == 0)
                    Console.WriteLine($"Failed to load sample {soundName}: {Bass.LastError}");

                _sfxs.Add(soundName, handle);
            }

            // 2. Return the Sample Handle
            return _sfxs[soundName];
        }

        public static void PlayMusic(string musicName, bool restart = true)
        {
            if (_musics.TryGetValue(musicName, out int handle))
                Bass.ChannelPlay(handle, restart);
        }

        public static void PlaySFX(string soundName)
        {
            if (_sfxs.TryGetValue(soundName, out int sampleHandle))
            {
                // 1. Request a live playback channel instance from the sample data
                int channel = Bass.SampleGetChannel(sampleHandle);

                // 0 means BASS failed to allocate a channel (e.g., ran out of max playback channels)
                if (channel != 0)
                {
                    // 2. Fetch local scale modifier if it exists, otherwise default to full (1.0f)
                    if (!_sfxLocalVolumes.TryGetValue(soundName, out float localVolume))
                    {
                        localVolume = 1.0f;
                    }

                    // 3. Combine local balance adjustments with your master settings slider
                    float finalVolume = localVolume * GlobalSFXVolume;

                    Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, finalVolume);
                    Bass.ChannelPlay(channel);
                }
            }
        }

        public static void PauseMusic(string musicName)
        {
            if (_musics.TryGetValue(musicName, out int handle))
                Bass.ChannelPause(handle);
        }

        public static void StopMusic(string musicName)
        {
            if (_musics.TryGetValue(musicName, out int handle))
                Bass.ChannelStop(handle);
        }

        public static void SetMusicVolume(string musicName, float volume)
        {
            if (_musics.TryGetValue(musicName, out int handle))
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, volume);
        }

        public static void SetMusicSpeed(string musicName, float speedMultiplier, bool adjustPitch = false)
        {
            if (_musics.TryGetValue(musicName, out int handle))
            {
                // 1. Double Time / Half Time: Calculate Tempo percentage difference
                // A multiplier of 1.5x means a +50% tempo increase.
                float tempoPercent = (speedMultiplier - 1.0f) * 100f;
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Tempo, tempoPercent);

                // 2. Adjust Pitch (Nightcore style)
                if (adjustPitch)
                {
                    // Calculate how many semitones to shift to match the speed multiplier
                    // Formula: 12 * log2(multiplier)
                    float semitones = 12f * MathF.Log2(speedMultiplier);
                    Bass.ChannelSetAttribute(handle, ChannelAttribute.Pitch, semitones);
                }
                else
                {
                    // Lock pitch back to its original key
                    Bass.ChannelSetAttribute(handle, ChannelAttribute.Pitch, 0f);
                }
            }
        }

        public static void SetSFXVolume(string sfxName, float volume)
        {
            // Store the native asset base volume modifier locally (0.0f to 1.0f)
            _sfxLocalVolumes[sfxName] = Math.Clamp(volume, 0f, 1f);
        }

        public static double GetMusicVolume(string musicName)
        {
            if (_musics.TryGetValue(musicName, out int handle))
                return Bass.ChannelGetAttribute(handle, ChannelAttribute.Volume);
            return 0;
        }

        public static float GetMusicLength(string musicName)
        {
            if (_musics.TryGetValue(musicName, out int handle))
            {
                long byteLength = Bass.ChannelGetLength(handle);
                return (float)Bass.ChannelBytes2Seconds(handle, byteLength);
            }
            return 0f;
        }

        public static float GetMusicTimePlayed(string musicName)
        {
            if (_musics.TryGetValue(musicName, out int handle))
            {
                long bytePosition = Bass.ChannelGetPosition(handle);
                float decodeTime = (float)Bass.ChannelBytes2Seconds(handle, bytePosition);

                return Math.Max(0f, decodeTime - _audioLatency);
            }
            return 0f;
        }

        public static void SeekMusic(string musicName, float position)
        {
            if (_musics.TryGetValue(musicName, out int handle))
            {
                long bytePosition = Bass.ChannelSeconds2Bytes(handle, position);
                Bass.ChannelSetPosition(handle, bytePosition);
            }
        }

        public static void AudioCleanup()
        {
            foreach (var handle in _musics.Values)
            {
                // Only free if the handle is still valid
                if (handle != 0)
                    Bass.StreamFree(handle);
            }
            _musics.Clear();

            // Finally, free the device
            Bass.Free();
        }
    }

    // === Rythm Helper ===
    public static class RythmHelper
    {
        internal static List<IArtHelper> helperPool = new();

        public static void AddHelper(IArtHelper helper)
        {
            helperPool.Add(helper);
        }

        public class RhythmIndexer : IArtHelper
        {
            // Dependencies
            private readonly InterpolatingAudioClock _audioClock;
            private RhythmTracker _rhythmTracker;
            private readonly Func<float> _getRawMusicTime;

            // Configuration
            public float MusicOffset { get; set; } = -33f;

            // Events you can subscribe to
            public event Action<int>? OnBeat;
            public event Action<int>? OnDownbeat;

            // Public state for other classes to read if needed
            public int CurrentBeatIndex { get; private set; }
            public bool IsDownbeat { get; private set; }
            public float BeatProgress { get; private set; }
            public float CurrentProgress { get; private set; }
            public OsuBeatmap? Beatmap { get; set; }

            private int _lastBeatIndex = -1;

            public RhythmIndexer(
                InterpolatingAudioClock audioClock,
                RhythmTracker rhythmTracker,
                Func<float> timeProvider)
            {
                _audioClock = audioClock;
                _rhythmTracker = rhythmTracker;
                _getRawMusicTime = timeProvider;
            }

            public void Reset(float expectedTimeSeconds)
            {
                // 1. Utilize your built-in clock reset and latency absorber[cite: 15]
                _audioClock.Reset(expectedTimeSeconds);

                // 2. Flush the tracker[cite: 16]
                _rhythmTracker = new RhythmTracker();
                _lastBeatIndex = -1;

                // 3. CRITICAL FIX: Pre-calculate the current progress instantly!
                // This destroys the 1-frame ghost value so the Playfield doesn't mass-miss notes.
                CurrentProgress = (expectedTimeSeconds * 1000f) - MusicOffset;
            }

            public void Update(float dt)
            {
                float rawBassTimeSeconds = _getRawMusicTime();

                // Let the InterpolatingAudioClock handle the BASS latency natively[cite: 15]
                _audioClock.Update(rawBassTimeSeconds, dt, isAudioPlaying: true);

                float smoothMusicTimeMs = (_audioClock.CurrentTime * 1000f) - MusicOffset;
                _rhythmTracker.Update(smoothMusicTimeMs, Beatmap.BpmPoints);

                // Cache current state
                CurrentBeatIndex = _rhythmTracker.CurrentBeatIndex;
                IsDownbeat = _rhythmTracker.IsDownbeat;
                BeatProgress = _rhythmTracker.BeatProgress;
                CurrentProgress = smoothMusicTimeMs;

                // Fire events only when the beat actually changes
                if (CurrentBeatIndex != _lastBeatIndex)
                {
                    OnBeat?.Invoke(CurrentBeatIndex);
                    if (IsDownbeat) OnDownbeat?.Invoke(CurrentBeatIndex);
                    _lastBeatIndex = CurrentBeatIndex;
                }
            }
        }
    }
}