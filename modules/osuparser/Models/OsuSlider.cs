using System.Collections.Generic;

namespace OsuLib.Models
{
    /// <summary>
    /// Curve type encoded in the slider's curve-type character.
    /// </summary>
    public enum SliderCurveType
    {
        /// <summary>Bézier curve (B).</summary>
        Bezier,
        /// <summary>Catmull-Rom (C) – legacy, rarely used.</summary>
        CatmullRom,
        /// <summary>Linear (L).</summary>
        Linear,
        /// <summary>Perfect circle arc (P).</summary>
        PerfectCircle,
        /// <summary>Unknown / unset.</summary>
        Unknown
    }

    /// <summary>
    /// A 2-D control point on the slider path.
    /// </summary>
    public struct SliderPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>
    /// A slider (or osu!mania hold note treated as slider).
    /// Inherits position and timing from <see cref="OsuHitObject"/>.
    /// </summary>
    public class OsuSlider : OsuHitObject
    {
        // ── Curve / path ────────────────────────────────────────────────────────

        /// <summary>How the control points should be interpolated.</summary>
        public SliderCurveType CurveType { get; set; }

        /// <summary>
        /// Control points that define the slider path,
        /// NOT including the slider's start point (that is X,Y from the base class).
        /// </summary>
        public List<SliderPoint> CurvePoints { get; set; } = new();

        // ── Repeat / length ─────────────────────────────────────────────────────

        /// <summary>
        /// Number of times the slider travels its path.
        /// 1 = one way, 2 = back-and-forth once, etc.
        /// </summary>
        public int Slides { get; set; }

        /// <summary>
        /// Visual length of the slider path in osu! pixels (one pass).
        /// </summary>
        public double Length { get; set; }

        // ── Edge sounds ─────────────────────────────────────────────────────────

        /// <summary>Hit-sound at each slider edge (head + repeats + tail).</summary>
        public List<int> EdgeSounds { get; set; } = new();

        /// <summary>Sample-set strings for each slider edge.</summary>
        public List<string> EdgeSets { get; set; } = new();

        // ── Velocity / duration (filled by OsuBeatmap after parsing) ────────────

        /// <summary>
        /// Effective slider velocity in osu! pixels per millisecond.
        /// Set by <see cref="OsuBeatmap.ResolveSliderVelocities"/>.
        /// </summary>
        public double EffectiveVelocityPxPerMs { get; set; }

        /// <summary>
        /// Total duration of the slider in milliseconds (all slides).
        /// Set by <see cref="OsuBeatmap.ResolveSliderVelocities"/>.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Millisecond timestamp when the slider ends (tail / last edge).
        /// Set by <see cref="OsuBeatmap.ResolveSliderVelocities"/>.
        /// </summary>
        public double EndTime => Time + DurationMs;

        public OsuSlider()
        {
            ObjectType = HitObjectType.Slider;
        }

        public override string ToString() =>
            $"Slider @ {Time}ms  ({X},{Y})  len={Length}px  slides={Slides}  dur≈{DurationMs:F0}ms";
    }
}
