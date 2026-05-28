namespace ArtFrame.RythmModule
{
    public abstract class ControlPoint
    {
        /// <summary>The start time of this control point in milliseconds.</summary>
        public double Time { get; set; }
    }

    public class TimingControlPoint : ControlPoint
    {
        /// <summary>Milliseconds per beat.</summary>
        public double BeatLength { get; set; } = 500.0; // Default 120 BPM

        /// <summary>Time signature numerator (beats per measure).</summary>
        public int Meter { get; set; } = 4;

        public double BPM => 60000.0 / BeatLength;
    }

    public class DifficultyControlPoint : ControlPoint
    {
        /// <summary>Slider velocity multiplier (e.g. 1.0 = normal, 1.5 = fast).</summary>
        public double SpeedMultiplier { get; set; } = 1.0;
    }

    public class SoundControlPoint : ControlPoint
    {
        /// <summary>Sample set for hit sounds (0=default, 1=normal, 2=soft, 3=drum).</summary>
        public int SampleSet { get; set; }

        /// <summary>Custom sample index.</summary>
        public int SampleIndex { get; set; }

        /// <summary>Volume percentage (0–100).</summary>
        public int Volume { get; set; } = 100;
    }

    public class EffectControlPoint : ControlPoint
    {
        /// <summary>Whether Kiai mode is active at this point.</summary>
        public bool IsKiai { get; set; }

        /// <summary>Whether to omit the first bar line.</summary>
        public bool OmitFirstBarLine { get; set; }
    }
}
