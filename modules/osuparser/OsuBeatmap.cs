using System;
using System.Collections.Generic;
using System.Linq;
using OsuLib.Models;

namespace OsuLib
{
    /// <summary>
    /// Holds every parsed section of a single .osu file and provides
    /// high-level helpers to query metadata, timing, and hit objects.
    /// </summary>
    public class OsuBeatmap
    {
        // ── Raw section dictionaries ─────────────────────────────────────────────

        /// <summary>Key→Value pairs from the [General] section.</summary>
        public Dictionary<string, string> General { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Key→Value pairs from the [Editor] section.</summary>
        public Dictionary<string, string> Editor { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Key→Value pairs from the [Metadata] section.</summary>
        public Dictionary<string, string> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Key→Value pairs from the [Difficulty] section.</summary>
        public Dictionary<string, string> Difficulty { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raw lines from the [Events] section (unparsed).</summary>
        public List<string> Events { get; } = new();

        /// <summary>Parsed timing points, sorted by time.</summary>
        public List<OsuTimingPoint> TimingPoints { get; } = new();

        /// <summary>All parsed hit objects (Notes and Sliders), sorted by time.</summary>
        public List<OsuHitObject> HitObjects { get; } = new();

        /// <summary>Format version read from the first line of the file (e.g. 14).</summary>
        public int FormatVersion { get; set; }

        /// <summary>Full path of the source .osu file.</summary>
        public string FilePath { get; set; } = string.Empty;

        // ── Typed convenience views ──────────────────────────────────────────────

        /// <summary>Only the hit circles (tap notes).</summary>
        public IEnumerable<OsuNote> Notes =>
            HitObjects.OfType<OsuNote>();

        /// <summary>Only the sliders.</summary>
        public IEnumerable<OsuSlider> Sliders =>
            HitObjects.OfType<OsuSlider>();

        /// <summary>Only the uninherited (red-line) timing points that carry BPM.</summary>
        public IEnumerable<OsuTimingPoint> BpmPoints =>
            TimingPoints.Where(t => t.IsUninherited);

        // ── Section accessors ────────────────────────────────────────────────────

        /// <summary>
        /// Returns a value from the [General] section.
        /// Returns <paramref name="defaultValue"/> if the key is not found.
        /// </summary>
        public string GetGeneral(string key, string defaultValue = "")
            => General.TryGetValue(key, out var v) ? v : defaultValue;

        /// <summary>
        /// Returns a value from the [Metadata] section.
        /// Returns <paramref name="defaultValue"/> if the key is not found.
        /// </summary>
        public string GetMeta(string key, string defaultValue = "")
            => Metadata.TryGetValue(key, out var v) ? v : defaultValue;

        /// <summary>
        /// Returns a value from the [Difficulty] section.
        /// Returns <paramref name="defaultValue"/> if the key is not found.
        /// </summary>
        public string GetDifficulty(string key, string defaultValue = "")
            => Difficulty.TryGetValue(key, out var v) ? v : defaultValue;

        /// <summary>
        /// Returns a value from the [Editor] section.
        /// Returns <paramref name="defaultValue"/> if the key is not found.
        /// </summary>
        public string GetEditor(string key, string defaultValue = "")
            => Editor.TryGetValue(key, out var v) ? v : defaultValue;

        // ── Timing helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Finds the timing point that is active at the given time.
        /// Returns the last timing point whose <c>Time ≤ t</c>,
        /// or the very first timing point if <paramref name="t"/> is before everything.
        /// </summary>
        /// <param name="t">Timestamp in milliseconds.</param>
        /// <param name="uninheritedOnly">
        ///   If true, only considers red-line (BPM) timing points.
        /// </param>
        public OsuTimingPoint? GetTimingPointAt(double t, bool uninheritedOnly = false)
        {
            var pts = uninheritedOnly
                ? TimingPoints.Where(p => p.IsUninherited).ToList()
                : TimingPoints;

            OsuTimingPoint? result = null;
            foreach (var pt in pts)
            {
                if (pt.Time <= t) result = pt;
                else break;
            }
            return result ?? pts.FirstOrDefault();
        }

        /// <summary>
        /// Returns the BPM active at the specified time.
        /// Always walks back to the nearest uninherited timing point.
        /// </summary>
        public double GetBpmAt(double timeMs)
        {
            var pt = GetTimingPointAt(timeMs, uninheritedOnly: true);
            return pt?.BPM ?? double.NaN;
        }

        // ── Slider velocity / duration resolver ──────────────────────────────────

        /// <summary>
        /// Computes and caches <see cref="OsuSlider.EffectiveVelocityPxPerMs"/>
        /// and <see cref="OsuSlider.DurationMs"/> for every slider in the beatmap.
        ///
        /// <para>
        /// Formula (official osu! spec):
        /// <code>
        ///   pixelsPerBeat   = 100 × SliderMultiplier × velocityMultiplier
        ///   durationOneBeat = BeatLength (ms per beat from the nearest red line)
        ///   durationMs      = (Length / pixelsPerBeat) × durationOneBeat × Slides
        ///   velocityPxPerMs = Length / (durationMs / Slides)
        /// </code>
        /// where <c>velocityMultiplier</c> is 1.0 for red lines and
        /// <c>-100 / beatLength</c> for green lines.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Called automatically by <see cref="OsuParser"/> after parsing.
        /// You can call it again if you modify timing points at runtime.
        /// </remarks>
        public void ResolveSliderVelocities()
        {
            // SliderMultiplier lives in [Difficulty]
            double sliderMultiplier = 1.4;
            if (Difficulty.TryGetValue("SliderMultiplier", out var smStr)
                && double.TryParse(smStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double sm))
            {
                sliderMultiplier = sm;
            }

            foreach (var obj in HitObjects.OfType<OsuSlider>())
            {
                // Holds already carry a real DurationMs; the slider-velocity math doesn't apply.
                if (obj.ObjectType == HitObjectType.Hold) continue;

                // Active uninherited point → gives us beatLength (ms per beat)
                var redLine = GetTimingPointAt(obj.Time, uninheritedOnly: true);
                if (redLine == null) continue;
                double beatLengthMs = redLine.BeatLength;

                // Active any-type point → gives us velocity multiplier
                var activePt = GetTimingPointAt(obj.Time, uninheritedOnly: false);
                double velMult = activePt?.VelocityMultiplier ?? 1.0;

                // osu! pixels per beat at this slider
                double pixelsPerBeat = 100.0 * sliderMultiplier * velMult;

                // Duration of ONE pass through the slider path (ms)
                double singlePassMs = (obj.Length / pixelsPerBeat) * beatLengthMs;

                obj.DurationMs = singlePassMs * obj.Slides;
                obj.EffectiveVelocityPxPerMs = obj.Length / singlePassMs;
            }
        }

        /// <summary>
        /// Returns the effective velocity in osu! pixels per millisecond
        /// that a slider placed at <paramref name="timeMs"/> would have,
        /// given the beatmap's current timing points.
        /// Useful for computing expected slider speeds at arbitrary times.
        /// </summary>
        public double GetSliderVelocityAt(double timeMs)
        {
            double sliderMultiplier = 1.4;
            if (Difficulty.TryGetValue("SliderMultiplier", out var smStr)
                && double.TryParse(smStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double sm))
            {
                sliderMultiplier = sm;
            }

            var redLine  = GetTimingPointAt(timeMs, uninheritedOnly: true);
            if (redLine == null) return 0;

            var activePt = GetTimingPointAt(timeMs, uninheritedOnly: false);
            double velMult = activePt?.VelocityMultiplier ?? 1.0;

            // pixels per ms = (100 * SliderMultiplier * velocityMultiplier) / beatLength
            return (100.0 * sliderMultiplier * velMult) / redLine.BeatLength;
        }

        // ── Convenience properties ───────────────────────────────────────────────

        /// <summary>Title of the song (ASCII).</summary>
        public string Title      => GetMeta("Title");

        /// <summary>Title in Unicode.</summary>
        public string TitleUnicode  => GetMeta("TitleUnicode");

        /// <summary>Artist (ASCII).</summary>
        public string Artist     => GetMeta("Artist");

        /// <summary>Artist in Unicode.</summary>
        public string ArtistUnicode => GetMeta("ArtistUnicode");

        /// <summary>Mapper (creator) username.</summary>
        public string Creator    => GetMeta("Creator");

        /// <summary>Difficulty name.</summary>
        public string Version    => GetMeta("Version");

        /// <summary>Numeric beatmap ID on osu! website.</summary>
        public int BeatmapId =>
            int.TryParse(GetMeta("BeatmapID"), out int id) ? id : 0;

        /// <summary>Numeric beatmap set ID on osu! website.</summary>
        public int BeatmapSetId =>
            int.TryParse(GetMeta("BeatmapSetID"), out int id) ? id : 0;

        /// <summary>Audio filename from [General].</summary>
        public string AudioFilename => GetGeneral("AudioFilename");

        /// <summary>Preview time in ms from [General].</summary>
        public int PreviewTime =>
            int.TryParse(GetGeneral("PreviewTime"), out int t) ? t : 0;

        /// <summary>Game mode (0=osu!,1=Taiko,2=CtB,3=Mania).</summary>
        public int Mode =>
            int.TryParse(GetGeneral("Mode"), out int m) ? m : 0;

        // ── Background ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the background image filename as written in the [Events] section,
        /// or an empty string if no background is defined.
        ///
        /// <para>
        /// To get the full path on disk, combine with the beatmap folder yourself,
        /// or use <see cref="GetBackgroundFullPath"/> for a one-liner:
        /// <code>
        ///   string folder = Path.GetDirectoryName(bm.FilePath)!;
        ///   string full   = Path.Combine(folder, bm.GetBackground());
        /// </code>
        /// </para>
        /// </summary>
        public string GetBackground()
        {
            // Background event format:  0,0,"filename.jpg",0,0
            // Type field "0" = background image.
            foreach (var line in Events)
            {
                if (line.StartsWith("//") || line.StartsWith(" ")) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                if (parts[0].Trim() != "0") continue;

                // Third token is the filename, possibly wrapped in quotes
                string filename = parts[2].Trim().Trim('"');
                if (filename.Length > 0)
                    return filename;
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the full absolute path to the background image,
        /// or an empty string if no background is defined or <see cref="FilePath"/> is unknown.
        /// </summary>
        public string GetBackgroundFullPath()
        {
            string bg = GetBackground();
            if (bg.Length == 0 || FilePath.Length == 0) return string.Empty;

            string? folder = System.IO.Path.GetDirectoryName(FilePath);
            return folder is null ? string.Empty : System.IO.Path.Combine(folder, bg);
        }

        public override string ToString() =>
            $"[{Artist} – {Title}] {Version}  (ID:{BeatmapId})  {HitObjects.Count} objects";
    }
}
