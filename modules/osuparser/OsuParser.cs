using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OsuLib.Models;

namespace OsuLib
{
    /// <summary>
    /// Parses a single .osu file into an <see cref="OsuBeatmap"/>.
    /// </summary>
    public class OsuParser
    {
        // ── Type bit masks (osu! spec) ───────────────────────────────────────────
        private const int TYPE_CIRCLE   = 1 << 0;   // 1
        private const int TYPE_SLIDER   = 1 << 1;   // 2
        private const int TYPE_NEWCOMBO = 1 << 2;   // 4
        private const int TYPE_SPINNER  = 1 << 3;   // 8
        private const int COMBO_SKIP    = 0b0111_0000; // bits 4-6 → how many colours to skip
        private const int TYPE_HOLD     = 1 << 7;   // 128  (osu!mania only)

        /// <summary>
        /// An offset which needs to be applied to old beatmaps (v4 and lower)
        /// to correct timing changes that were applied at a game client level.
        /// Matches <c>LegacyBeatmapDecoder.EARLY_VERSION_TIMING_OFFSET</c>.
        /// </summary>
        private const int EARLY_VERSION_TIMING_OFFSET = 24;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Parses the .osu file at <paramref name="path"/> and returns
        /// a fully populated <see cref="OsuBeatmap"/>.
        /// Slider velocities are resolved automatically.
        /// </summary>
        /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        /// <exception cref="InvalidDataException">If the file is not a valid .osu file.</exception>
        public OsuBeatmap Parse(string path, bool metadataOnly = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found", path);

            var lines = File.ReadAllLines(path);
            return ParseLines(lines, path, metadataOnly);
        }

        /// <summary>
        /// Parses .osu content from a string instead of a file path.
        /// </summary>
        public OsuBeatmap ParseText(string content, string sourcePath = "", bool metadataOnly = false)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return ParseLines(lines, sourcePath, metadataOnly);
        }

        // ── Internal parsing ─────────────────────────────────────────────────────

        private OsuBeatmap ParseLines(string[] lines, string filePath, bool metadataOnly = false)
        {
            var beatmap = new OsuBeatmap { FilePath = filePath };

            // First line: "osu file format v14"
            if (lines.Length > 0 && lines[0].StartsWith("osu file format v"))
            {
                if (int.TryParse(lines[0].Replace("osu file format v", "").Trim(), out int ver))
                    beatmap.FormatVersion = ver;
            }

            // osu!stable applies a +24ms offset for format versions ≤ 4.
            // See: LegacyBeatmapDecoder.EARLY_VERSION_TIMING_OFFSET
            double offset = beatmap.FormatVersion < 5 ? EARLY_VERSION_TIMING_OFFSET : 0;

            string currentSection = "";

            for (int i = 1; i < lines.Length; i++)
            {
                string raw  = lines[i];
                string line = raw.Trim();

                // Skip blank lines and comments
                if (line.Length == 0 || line.StartsWith("//")) continue;

                // Section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1]; // trim [ ]

                    // Stop parsing immediately if we hit Colours or HitObjects in metadata-only mode
                    if (metadataOnly && (currentSection == "Colours" || currentSection == "HitObjects"))
                    {
                        break;
                    }
                    continue;
                }

                switch (currentSection)
                {
                    case "General":
                    case "Editor":
                    case "Metadata":
                    case "Difficulty":
                        ParseKeyValue(line, currentSection, beatmap);
                        break;

                    case "Events":
                        beatmap.Events.Add(line);
                        break;

                    case "TimingPoints":
                        var tp = ParseTimingPoint(line, offset);
                        if (tp != null)
                        {
                            beatmap.TimingPoints.Add(tp);

                            // Decoupled Control Points alignment with osu!lazer
                            if (tp.IsUninherited)
                            {
                                beatmap.ControlPoints.TimingPoints.Add(new ArtFrame.RythmModule.TimingControlPoint
                                {
                                    Time = tp.Time,
                                    BeatLength = tp.BeatLength,
                                    Meter = tp.Meter
                                });

                                // Red lines reset speed multiplier back to 1.0 (default velocity multiplier)
                                beatmap.ControlPoints.DifficultyPoints.Add(new ArtFrame.RythmModule.DifficultyControlPoint
                                {
                                    Time = tp.Time,
                                    SpeedMultiplier = 1.0
                                });
                            }
                            else
                            {
                                // Green lines modify velocity multiplier
                                beatmap.ControlPoints.DifficultyPoints.Add(new ArtFrame.RythmModule.DifficultyControlPoint
                                {
                                    Time = tp.Time,
                                    SpeedMultiplier = tp.VelocityMultiplier
                                });
                            }

                            // Sound point
                            beatmap.ControlPoints.SoundPoints.Add(new ArtFrame.RythmModule.SoundControlPoint
                            {
                                Time = tp.Time,
                                Volume = tp.Volume,
                                SampleSet = tp.SampleSet,
                                SampleIndex = tp.SampleIndex
                            });

                            // Effect point
                            beatmap.ControlPoints.EffectPoints.Add(new ArtFrame.RythmModule.EffectControlPoint
                            {
                                Time = tp.Time,
                                IsKiai = tp.IsKiai,
                                OmitFirstBarLine = (tp.Effects & 8) != 0
                            });
                        }
                        break;

                    case "HitObjects":
                        var obj = ParseHitObject(line, offset);
                        if (obj != null) beatmap.HitObjects.Add(obj);
                        break;
                }
            }

            // Sort by time (the file should already be sorted, but just in case)
            beatmap.TimingPoints.Sort((a, b) => a.Time.CompareTo(b.Time));

            // Decoupled lists sorting
            beatmap.ControlPoints.TimingPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
            beatmap.ControlPoints.DifficultyPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
            beatmap.ControlPoints.SoundPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
            beatmap.ControlPoints.EffectPoints.Sort((a, b) => a.Time.CompareTo(b.Time));

            if (!metadataOnly)
            {
                beatmap.HitObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
                // Fill slider velocity / duration
                beatmap.ResolveSliderVelocities();
            }

            return beatmap;
        }

        // ── Key : Value sections ─────────────────────────────────────────────────

        private static void ParseKeyValue(string line, string section, OsuBeatmap beatmap)
        {
            int colonIdx = line.IndexOf(':');
            if (colonIdx < 0) return;

            string key   = line[..colonIdx].Trim();
            string value = line[(colonIdx + 1)..].Trim();

            switch (section)
            {
                case "General":    beatmap.General[key]    = value; break;
                case "Editor":     beatmap.Editor[key]     = value; break;
                case "Metadata":   beatmap.Metadata[key]   = value; break;
                case "Difficulty": beatmap.Difficulty[key] = value; break;
            }
        }

        // ── TimingPoints ─────────────────────────────────────────────────────────

        private static OsuTimingPoint? ParseTimingPoint(string line, double offset)
        {
            // time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
            var parts = line.Split(',');
            if (parts.Length < 2) return null;

            var tp = new OsuTimingPoint();

            // osu!stable truncates timing point time to int.
            // We apply the format-version offset here.
            if (TryParseDouble(parts, 0, out double t))
                tp.Time = Math.Floor(t) + offset;

            if (TryParseDouble(parts, 1, out double bl))  tp.BeatLength = bl;
            if (TryParseInt(parts, 2, out int meter))     tp.Meter      = meter;
            if (TryParseInt(parts, 3, out int ss))        tp.SampleSet  = ss;
            if (TryParseInt(parts, 4, out int si))        tp.SampleIndex = si;
            if (TryParseInt(parts, 5, out int vol))       tp.Volume     = vol;
            if (TryParseInt(parts, 6, out int uninh))     tp.IsUninherited = uninh == 1;
            if (TryParseInt(parts, 7, out int fx))        tp.Effects    = fx;

            // ── osu!stable clamping for inherited (green) lines ──────────────────
            // In osu!stable, the beatLength field for inherited points is clamped
            // to [-1000, -10], producing a velocity multiplier range of [0.1, 10.0].
            // Uninherited (red) points clamp beatLength to [6, 60000] (≈1–10000 BPM).
            if (tp.IsUninherited)
            {
                tp.BeatLength = Math.Clamp(tp.BeatLength, 6.0, 60000.0);
            }
            else
            {
                // For legacy files (v4 and below) that don't have the uninherited flag,
                // a positive beatLength still means it's a timing point.
                // But if explicitly inherited, clamp to the expected negative range.
                if (tp.BeatLength >= 0)
                {
                    // Some old maps have positive beatLength on green lines; treat as 1× velocity.
                    tp.BeatLength = -100.0;
                }
                else
                {
                    tp.BeatLength = Math.Clamp(tp.BeatLength, -1000.0, -10.0);
                }
            }

            return tp;
        }

        // ── HitObjects ───────────────────────────────────────────────────────────

        private static OsuHitObject? ParseHitObject(string line, double offset)
        {
            // Minimum: x,y,time,type,hitSound
            var parts = line.Split(',');
            if (parts.Length < 5) return null;

            if (!TryParseInt(parts, 0, out int x))    return null;
            if (!TryParseInt(parts, 1, out int y))    return null;
            if (!TryParseInt(parts, 2, out int time)) return null;
            if (!TryParseInt(parts, 3, out int type)) return null;
            if (!TryParseInt(parts, 4, out int hs))   return null;

            // Apply the format-version offset to the hit object time.
            double startTime = time + offset;
            int adjustedTime = (int)Math.Round(startTime);

            bool isNewCombo  = (type & TYPE_NEWCOMBO) != 0;
            int  comboSkip   = (type & COMBO_SKIP) >> 4;

            OsuHitObject obj;

            if ((type & TYPE_SLIDER) != 0)
            {
                obj = ParseSlider(parts, 5);
            }
            else if ((type & TYPE_SPINNER) != 0)
            {
                // Spinners: endTime is at parts[5], hitSample at parts[6]
                obj = ParseSpinner(parts, 5, startTime, offset);
            }
            else if ((type & TYPE_HOLD) != 0)
            {
                obj = ParseHold(parts, 5, startTime, offset);
            }
            else if ((type & TYPE_CIRCLE) != 0)
            {
                var note = new OsuNote();
                // Circle hitSample is at parts[5]
                if (parts.Length > 5)
                    note.HitSample = parts[5].Trim();
                obj = note;
            }
            else
            {
                obj = new OsuNote { ObjectType = HitObjectType.Unknown };
            }

            obj.X          = x;
            obj.Y          = y;
            obj.Time       = adjustedTime;
            obj.TypeRaw    = type;
            obj.HitSound   = hs;
            obj.IsNewCombo = isNewCombo;
            obj.ComboSkip  = comboSkip;

            return obj;
        }

        // ── Spinner parsing ──────────────────────────────────────────────────────
        // Format: x,y,time,type,hitSound,endTime,hitSample
        // Official: duration = Math.Max(0, endTime + offset - startTime)
        private static OsuNote ParseSpinner(string[] parts, int paramsIdx, double startTime, double offset)
        {
            var spinner = new OsuNote { ObjectType = HitObjectType.Spinner };

            if (paramsIdx < parts.Length && TryParseDouble(parts, paramsIdx, out double endTime))
            {
                // osu!stable: duration = max(0, endTime + offset - startTime)
                // startTime already includes offset, so: duration = max(0, (endTime + offset) - startTime)
                spinner.DurationMs = Math.Max(0, (endTime + offset) - startTime);
            }

            if (paramsIdx + 1 < parts.Length)
                spinner.HitSample = parts[paramsIdx + 1].Trim();

            return spinner;
        }

        // objectParams for slider start at parts[5]
        // Format: curveType|cx:cy|cx:cy,...  then slides,length,edgeSounds,edgeSets,hitSample
        private static OsuSlider ParseSlider(string[] parts, int paramsIdx)
        {
            var s = new OsuSlider();
            if (paramsIdx >= parts.Length) return s;

            // --- curve type + control points ---
            string curveStr = parts[paramsIdx];
            var curveParts  = curveStr.Split('|');

            s.CurveType = curveParts[0] switch
            {
                "B" => SliderCurveType.Bezier,
                "C" => SliderCurveType.CatmullRom,
                "L" => SliderCurveType.Linear,
                "P" => SliderCurveType.PerfectCircle,
                _   => SliderCurveType.Unknown
            };

            for (int i = 1; i < curveParts.Length; i++)
            {
                var xy = curveParts[i].Split(':');
                if (xy.Length == 2
                    && int.TryParse(xy[0], out int cx)
                    && int.TryParse(xy[1], out int cy))
                {
                    s.CurvePoints.Add(new SliderPoint { X = cx, Y = cy });
                }
            }

            // --- slides (span count) ---
            // osu!stable: this value is called "repeat" but it's actually the total span count.
            // The official parser does repeatCount = Math.Max(0, value - 1) to get the
            // number of repeats, then uses repeatCount + 1 for total spans.
            // We store it as Slides which is the total span count from the file.
            if (TryParseInt(parts, paramsIdx + 1, out int slides))
                s.Slides = Math.Max(1, slides);

            // --- length (pixel length of one span) ---
            if (TryParseDouble(parts, paramsIdx + 2, out double len))
                s.Length = Math.Max(0, len);

            // --- edge sounds ---
            if (paramsIdx + 3 < parts.Length && parts[paramsIdx + 3].Trim().Length > 0)
            {
                foreach (var es in parts[paramsIdx + 3].Split('|'))
                    if (int.TryParse(es, out int esv)) s.EdgeSounds.Add(esv);
            }

            // --- edge sets ---
            if (paramsIdx + 4 < parts.Length && parts[paramsIdx + 4].Trim().Length > 0)
            {
                foreach (var eSet in parts[paramsIdx + 4].Split('|'))
                    s.EdgeSets.Add(eSet);
            }

            // --- hit sample ---
            if (paramsIdx + 5 < parts.Length)
                s.HitSample = parts[paramsIdx + 5].Trim();

            return s;
        }

        // osu!mania hold: endTime:hitSample at params position
        // Official: endTime = Math.Max(startTime, ParseDouble(ss[0])); duration = endTime + offset - startTime
        private static OsuSlider ParseHold(string[] parts, int paramsIdx, double startTime, double offset)
        {
            var s = new OsuSlider { ObjectType = HitObjectType.Hold };
            if (paramsIdx >= parts.Length) return s;

            string paramStr = parts[paramsIdx];

            if (!string.IsNullOrEmpty(paramStr))
            {
                var holdParams = paramStr.Split(':');

                if (TryParseDouble(holdParams, 0, out double endTime))
                {
                    // osu!stable: endTime = max(startTime, endTime)
                    // duration = endTime + offset - startTime
                    // startTime already includes offset, so: duration = (endTime + offset) - startTime
                    endTime = Math.Max(startTime, endTime + offset);
                    s.DurationMs = endTime - startTime;
                }

                // Remaining parts after endTime are the sample bank info (colon-separated)
                // We don't parse sample banks deeply but store the raw string if present
                if (holdParams.Length > 1)
                {
                    s.HitSample = string.Join(':', holdParams, 1, holdParams.Length - 1);
                }
            }

            s.Slides = 1;
            return s;
        }

        // ── Parse helpers ────────────────────────────────────────────────────────

        private static bool TryParseInt(string[] parts, int idx, out int value)
        {
            value = 0;
            return idx < parts.Length
                && int.TryParse(parts[idx].Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value);
        }

        private static bool TryParseDouble(string[] parts, int idx, out double value)
        {
            value = 0;
            return idx < parts.Length
                && double.TryParse(parts[idx].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
        }
    }
}
