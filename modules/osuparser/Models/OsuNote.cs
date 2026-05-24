namespace OsuLib.Models
{
    /// <summary>
    /// A single hit circle – just tap at <see cref="OsuHitObject.Time"/>.
    /// </summary>
    public class OsuNote : OsuHitObject
    {
        // Hit circles carry no extra fields beyond the base class.
        // Everything you need is in OsuHitObject (X, Y, Time, HitSound, etc.)

        public OsuNote()
        {
            ObjectType = HitObjectType.Note;
        }
    }
}
