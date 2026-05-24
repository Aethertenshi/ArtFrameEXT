using System;
using System.IO;
using System.IO.Compression;

namespace ArtFrame.FileProcessing;

/// <summary>
/// Extracts an .osz file (which is just a ZIP) into a randomly-named
/// subfolder inside the songs directory.
///
/// Usage:
///   string folderPath = OszImporter.Import("path/to/file.osz", songsPath);
/// </summary>
public static class OszImporter
{
    private static readonly Random _rng = new Random();

    /// <summary>
    /// Imports an .osz file into <paramref name="songsPath"/>.
    /// </summary>
    /// <param name="oszPath">Full path to the .osz file.</param>
    /// <param name="songsPath">Root songs directory, e.g. C:/Users/.../123123/</param>
    /// <returns>The full path of the extracted folder, or null on failure.</returns>
    public static string? Import(string oszPath, string songsPath)
    {
        if (!File.Exists(oszPath))
        {
            Console.WriteLine($"[OszImporter] File not found: {oszPath}");
            return null;
        }

        if (!Path.GetExtension(oszPath).Equals(".osz", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[OszImporter] Not an .osz file: {oszPath}");
            return null;
        }

        // Generate a unique folder name — random int, retry if it somehow collides
        string destFolder;
        do
        {
            int id = _rng.Next(100_000, 999_999_999);
            destFolder = Path.Combine(songsPath, id.ToString());
        }
        while (Directory.Exists(destFolder));

        try
        {
            Directory.CreateDirectory(destFolder);
            ZipFile.ExtractToDirectory(oszPath, destFolder, overwriteFiles: true);
            Console.WriteLine($"[OszImporter] Extracted → {destFolder}");
            return destFolder;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OszImporter] Extraction failed: {ex.Message}");

            // Clean up partial extraction
            if (Directory.Exists(destFolder))
                Directory.Delete(destFolder, recursive: true);

            return null;
        }
    }

    /// <summary>
    /// Returns true if the path looks like a valid .osz file that can be imported.
    /// </summary>
    public static bool IsOszFile(string path) =>
        !string.IsNullOrEmpty(path) &&
        File.Exists(path) &&
        Path.GetExtension(path).Equals(".osz", StringComparison.OrdinalIgnoreCase);
}