using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OsuLib
{
    /// <summary>
    /// Scans a directory (recursively) for .osu files and parses them all.
    /// </summary>
    public class OsuScanner
    {
        private readonly OsuParser _parser = new();

        // ── Discovery ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns every .osu file path inside <paramref name="rootDirectory"/>,
        /// searching all sub-folders.
        /// </summary>
        public IReadOnlyList<string> FindOsuFiles(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Directory not found: {rootDirectory}");

            var osuFiles = new List<string>();
            ScanDirectoryRecursive(rootDirectory, osuFiles);
            return osuFiles;
        }

        private void ScanDirectoryRecursive(string path, List<string> results)
        {
            try
            {
                // Get .osu files in the current directory
                var files = Directory.GetFiles(path, "*.osu");
                results.AddRange(files);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OsuScanner] Error getting files in {path}: {ex.Message}");
            }

            try
            {
                // Get subdirectories and recurse
                var subDirs = Directory.GetDirectories(path);
                foreach (var dir in subDirs)
                {
                    ScanDirectoryRecursive(dir, results);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OsuScanner] Error enumerating subdirectories in {path}: {ex.Message}");
            }
        }

        // ── Batch parsing ────────────────────────────────────────────────────────

        /// <summary>
        /// Finds and parses every .osu file in <paramref name="rootDirectory"/>.
        /// Files that fail to parse are skipped; errors are reported via
        /// <paramref name="onError"/> (if provided).
        /// </summary>
        /// <param name="rootDirectory">Root folder to search.</param>
        /// <param name="onError">
        ///   Optional callback invoked when a file fails to parse.
        ///   Receives (filePath, exception).
        /// </param>
        /// <returns>List of successfully parsed beatmaps.</returns>
        public IReadOnlyList<OsuBeatmap> ScanAll(
            string rootDirectory,
            Action<string, Exception>? onError = null)
        {
            var paths = FindOsuFiles(rootDirectory);
            var results = new List<OsuBeatmap>(paths.Count);

            foreach (var path in paths)
            {
                try
                {
                    results.Add(_parser.Parse(path));
                }
                catch (Exception ex)
                {
                    onError?.Invoke(path, ex);
                }
            }

            return results;
        }

        /// <summary>
        /// Lazily parses .osu files one at a time using an iterator.
        /// Useful when memory is a concern or you want to process files
        /// as they are parsed without waiting for the full scan.
        /// Files that fail are skipped (errors reported via <paramref name="onError"/>).
        /// </summary>
        public IEnumerable<OsuBeatmap> ScanLazy(
            string rootDirectory,
            Action<string, Exception>? onError = null)
        {
            var paths = FindOsuFiles(rootDirectory);

            foreach (var path in paths)
            {
                OsuBeatmap? bm = null;
                try
                {
                    bm = _parser.Parse(path);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(path, ex);
                }

                if (bm != null) yield return bm;
            }
        }

        /// <summary>
        /// Scans the folder but returns only beatmaps that match a predicate.
        /// </summary>
        /// <param name="rootDirectory">Root folder to search.</param>
        /// <param name="filter">
        ///   Returns true for beatmaps you want to keep.
        ///   Example: <c>bm => bm.Mode == 0</c> keeps only osu!standard maps.
        /// </param>
        /// <param name="onError">Optional error callback.</param>
        public IReadOnlyList<OsuBeatmap> ScanFiltered(
            string rootDirectory,
            Func<OsuBeatmap, bool> filter,
            Action<string, Exception>? onError = null)
        {
            return ScanLazy(rootDirectory, onError)
                .Where(filter)
                .ToList();
        }

        // ── Single-set helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns all .osu files directly inside a single beatmap folder
        /// (i.e. a single beatmapset, multiple difficulties).
        /// </summary>
        public IReadOnlyList<OsuBeatmap> ParseSet(string beatmapSetDirectory)
        {
            if (!Directory.Exists(beatmapSetDirectory))
                throw new DirectoryNotFoundException(beatmapSetDirectory);

            var results = new List<OsuBeatmap>();
            foreach (var f in Directory.GetFiles(beatmapSetDirectory, "*.osu"))
            {
                try { results.Add(_parser.Parse(f)); }
                catch { /* skip corrupt files */ }
            }
            return results;
        }
    }
}
