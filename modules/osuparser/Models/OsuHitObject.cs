namespace OsuLib.Models
{
    /// <summary>
    /// The kind of hit object.
    /// </summary>
    public enum HitObjectType
    {
        /// <summary>A single tap / click (circle).</summary>
        Note,

        /// <summary>A slider (hold and follow the curve).</summary>
        Slider,

        /// <summary>A spinner (rotate for duration).</summary>
        Spinner,

        /// <summary>An osu!mania hold note (LN).</summary>
        Hold,

        /// <summary>Unrecognised type bit combination.</summary>
        Unknown
    }

    /// <summary>
    /// Base class for every object that lives inside [HitObjects].
    /// Cast to <see cref="OsuNote"/> or <see cref="OsuSlider"/> for type-specific data.
    /// </summary>
    public abstract class OsuHitObject
    {
        // ── Common fields (all hit objects) ─────────────────────────────────────

        /// <summary>X position on the 512×384 osu! playfield.</summary>
        public int X { get; set; }

        /// <summary>Y position on the 512×384 osu! playfield.</summary>
        public int Y { get; set; }

        /// <summary>When the object should be hit, in milliseconds from song start.</summary>
        public int Time { get; set; }

        /// <summary>Raw integer type bitmask from the .osu file.</summary>
        public int TypeRaw { get; set; }

        /// <summary>Resolved type (Note / Slider / Spinner / Hold).</summary>
        public HitObjectType ObjectType { get; set; }

        /// <summary>True if this object starts a new combo.</summary>
        public bool IsNewCombo { get; set; }

        /// <summary>How many combo colours to skip (0–7) when IsNewCombo is true.</summary>
        public int ComboSkip { get; set; }

        /// <summary>Raw hit-sound bitmask (Normal=1, Whistle=2, Finish=4, Clap=8).</summary>
        public int HitSound { get; set; }

        /// <summary>Raw hit-sample string appended to the line (may be empty).</summary>
        public string HitSample { get; set; } = string.Empty;

        public override string ToString() =>
            $"{ObjectType} @ {Time}ms  ({X},{Y})";
    }
}
