namespace OsuLib.Models
{
    /// <summary>
    /// Represents a single entry in the [TimingPoints] section.
    /// Can be either an uninherited (BPM) point or an inherited (velocity) point.
    /// </summary>
    public class OsuTimingPoint
    {
        /// <summary>Start time of this timing point in milliseconds.</summary>
        public double Time { get; set; }

        /// <summary>
        /// For uninherited points: milliseconds per beat (positive).
        /// For inherited points: negative slider velocity multiplier * -100.
        /// </summary>
        public double BeatLength { get; set; }

        /// <summary>Time signature numerator (beats per measure).</summary>
        public int Meter { get; set; }

        /// <summary>Sample set for hit sounds (0=default,1=normal,2=soft,3=drum).</summary>
        public int SampleSet { get; set; }

        /// <summary>Custom sample index.</summary>
        public int SampleIndex { get; set; }

        /// <summary>Volume percentage (0–100).</summary>
        public int Volume { get; set; }

        /// <summary>True = this is a BPM timing point. False = inherited (green line).</summary>
        public bool IsUninherited { get; set; }

        /// <summary>
        /// Kiai time or other effect flags.
        /// Bit 0 = Kiai, Bit 3 = OmitFirstBarLine.
        /// </summary>
        public int Effects { get; set; }

        // ── Derived helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// BPM of this timing point.
        /// Only valid when <see cref="IsUninherited"/> is true;
        /// returns NaN for inherited (green-line) points.
        /// </summary>
        public double BPM => IsUninherited ? 60_000.0 / BeatLength : double.NaN;

        /// <summary>
        /// Slider velocity multiplier from this timing point.
        /// For inherited points this is -100 / BeatLength (e.g. BeatLength=-50 → 2.0×).
        /// For uninherited points this is always 1.0.
        /// </summary>
        public double VelocityMultiplier => IsUninherited ? 1.0 : -100.0 / BeatLength;

        /// <summary>Whether Kiai mode is active at this point.</summary>
        public bool IsKiai => (Effects & 1) != 0;

        public override string ToString() =>
            IsUninherited
                ? $"[Timing] t={Time}ms  BPM={BPM:F2}  Meter={Meter}"
                : $"[Inherited] t={Time}ms  Velocity×{VelocityMultiplier:F2}";
    }
}
