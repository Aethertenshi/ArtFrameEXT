using System;

namespace ArtFrame.RythmModule
{
    public class InterpolatingAudioClock
    {
        /// <summary>
        /// The ultra-smooth time (in seconds) you should use for ALL visual rendering and beat calculations.
        /// </summary>
        public float CurrentTime { get; private set; }

        private float _lastBassTime;

        // If the audio and visual get separated by more than 20ms, snap them back together instantly.
        // This handles lag spikes or if you manually seek to a new part of the song.
        private const float SnapThreshold = 0.02f;
        private float _ignoreBassTimer = 0f;

        public void Reset(float startTime = 0f)
        {
            CurrentTime = startTime;
            _lastBassTime = startTime;
            _ignoreBassTimer = 0.15f;
        }

        public void Update(float rawBassTime, float dt, bool isAudioPlaying)
        {
            // If the game is paused or music stopped, just lock exactly to whatever BASS says.
            if (!isAudioPlaying)
            {
                CurrentTime = rawBassTime;
                _lastBassTime = rawBassTime;
                return;
            }

            if (_ignoreBassTimer > 0)
            {
                _ignoreBassTimer -= dt;
                CurrentTime += dt;
                _lastBassTime = rawBassTime; // Keep tracking so we don't snap when the timer ends
                return;
            }

            // 1. Advance our internal high-res clock by the frame's delta time
            CurrentTime += dt;

            // 2. Did the BASS buffer just spit out a new time chunk?
            if (rawBassTime != _lastBassTime)
            {
                float drift = rawBassTime - CurrentTime;

                // 3. If we are completely desynchronized (e.g., massive lag spike or a track seek)
                if (MathF.Abs(drift) > SnapThreshold)
                {
                    CurrentTime = rawBassTime; // Violent snap to resync
                }
                else
                {
                    // 4. We are only slightly off (normal BASS buffer jitter). 
                    // We apply a 50% correction of the error. This acts as a shock absorber, 
                    // smoothing out the chunky BASS updates into a fluid time signal.
                    CurrentTime += drift * 0.5f;
                }

                _lastBassTime = rawBassTime;
            }
        }
    }
}