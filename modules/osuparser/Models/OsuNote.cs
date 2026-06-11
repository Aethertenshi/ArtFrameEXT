namespace OsuLib.Models
{
    /// <summary>
    /// A single hit circle – just tap at <see cref="OsuHitObject.Time"/>.
    /// Also used for spinners (with <see cref="OsuHitObject.ObjectType"/> set to <see cref="HitObjectType.Spinner"/>).
    /// </summary>
    public class OsuNote : OsuHitObject
    {
        // Hit circles carry no extra fields beyond the base class.
        // Everything you need is in OsuHitObject (X, Y, Time, HitSound, etc.)

        /// <summary>
        /// Duration of the note in milliseconds.
        /// Only relevant for spinners (where this is the spin duration).
        /// Zero for regular hit circles.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Millisecond timestamp when the note/spinner ends.
        /// For circles this equals <see cref="OsuHitObject.Time"/>.
        /// For spinners this is <c>Time + DurationMs</c>.
        /// </summary>
        public double EndTime => Time + DurationMs;

        public OsuNote()
        {
            ObjectType = HitObjectType.Note;
        }
    }
}
