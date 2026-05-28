using System;
using System.Collections.Generic;

namespace ArtFrame.RythmModule
{
    public class ControlPointInfo
    {
        public List<TimingControlPoint> TimingPoints { get; } = new();
        public List<DifficultyControlPoint> DifficultyPoints { get; } = new();
        public List<SoundControlPoint> SoundPoints { get; } = new();
        public List<EffectControlPoint> EffectPoints { get; } = new();

        /// <summary>
        /// Finds the active TimingControlPoint (BPM) at a specific time in milliseconds.
        /// </summary>
        public TimingControlPoint TimingPointAt(double time)
        {
            return BinarySearchActivePoint(TimingPoints, time) ?? DefaultTimingPoint;
        }

        /// <summary>
        /// Finds the active DifficultyControlPoint (Speed Multiplier) at a specific time in milliseconds.
        /// </summary>
        public DifficultyControlPoint DifficultyPointAt(double time)
        {
            return BinarySearchActivePoint(DifficultyPoints, time) ?? DefaultDifficultyPoint;
        }

        /// <summary>
        /// Finds the active SoundControlPoint (Volume/Samples) at a specific time in milliseconds.
        /// </summary>
        public SoundControlPoint SoundPointAt(double time)
        {
            return BinarySearchActivePoint(SoundPoints, time) ?? DefaultSoundPoint;
        }

        /// <summary>
        /// Finds the active EffectControlPoint (Kiai/Visual effects) at a specific time in milliseconds.
        /// </summary>
        public EffectControlPoint EffectPointAt(double time)
        {
            return BinarySearchActivePoint(EffectPoints, time) ?? DefaultEffectPoint;
        }

        private static T? BinarySearchActivePoint<T>(List<T> points, double time) where T : ControlPoint
        {
            if (points.Count == 0) return null;
            if (time < points[0].Time) return null; // Before first point

            int low = 0;
            int high = points.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                double midTime = points[mid].Time;

                if (midTime == time)
                {
                    return points[mid];
                }
                else if (midTime < time)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            // high is the index of the last element smaller than time
            return points[high];
        }

        // --- Defaults ---
        private static readonly TimingControlPoint DefaultTimingPoint = new() { Time = 0, BeatLength = 500, Meter = 4 };
        private static readonly DifficultyControlPoint DefaultDifficultyPoint = new() { Time = 0, SpeedMultiplier = 1.0 };
        private static readonly SoundControlPoint DefaultSoundPoint = new() { Time = 0, Volume = 100, SampleSet = 0, SampleIndex = 0 };
        private static readonly EffectControlPoint DefaultEffectPoint = new() { Time = 0, IsKiai = false, OmitFirstBarLine = false };
    }
}
