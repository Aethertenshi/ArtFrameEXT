using System;
using System.Collections.Generic;

namespace ArtFrame.RythmModule
{
    public class RhythmTracker
    {
        /// <summary>A strictly increasing integer representing the current beat. Safe to use for trigger checks (Current > Last).</summary>
        public int CurrentBeatIndex { get; private set; }

        /// <summary>A value between 0.0 and 1.0 representing the scale/animation progress of the current beat.</summary>
        public float BeatProgress { get; private set; }

        /// <summary>True if the current beat is the first beat of the measure (the downbeat).</summary>
        public bool IsDownbeat { get; private set; }

        public void Update(float musicTimeMs, ControlPointInfo controlPoints)
        {
            TimingControlPoint? activeRedLine = null;
            int priorWholeBeats = 0;

            // 1. Find the active red line and sum up the WHOLE beats of previous sections
            foreach (var tp in controlPoints.TimingPoints)
            {
                if (tp.Time > musicTimeMs)
                    break;

                if (activeRedLine != null)
                {
                    // Discard the fractional drift. A red line forcefully snaps the metronome to 0.
                    float sectionDurationMs = (float)(tp.Time - activeRedLine.Time);
                    priorWholeBeats += (int)Math.Floor(sectionDurationMs / activeRedLine.BeatLength);
                }

                activeRedLine = tp;
            }

            // 2. Handle the "Pre-First Timing Point" Void
            if (activeRedLine == null)
            {
                CurrentBeatIndex = 0;
                BeatProgress = 0f;
                IsDownbeat = false;
                return;
            }

            // 3. Calculate phase based strictly on the current active red line
            float msSinceRedLine = musicTimeMs - (float)activeRedLine.Time;
            float beatsSinceRedLine = msSinceRedLine / (float)activeRedLine.BeatLength;

            // Prevent negative beats if we fall exactly on the boundary
            if (beatsSinceRedLine < 0) beatsSinceRedLine = 0;

            // 4. Assemble the final state
            int localBeatIndex = (int)Math.Floor(beatsSinceRedLine);

            CurrentBeatIndex = priorWholeBeats + localBeatIndex;
            BeatProgress = beatsSinceRedLine - localBeatIndex;

            // 5. Downbeat calculation uses the LOCAL beat phase, guaranteeing it hits on Beat 0 of the red line
            int meter = activeRedLine.Meter > 0 ? activeRedLine.Meter : 4;
            IsDownbeat = (localBeatIndex % meter) == 0;
        }
    }
}