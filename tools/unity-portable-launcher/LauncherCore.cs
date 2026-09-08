using System.IO.Compression;
using System.Security.Cryptography;

namespace UnityPortable;

public static class LauncherCore
{
    public static string ComputeSha256Hex(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    public static void ExtractZipSafely(Stream zipStream, string destination)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            var outputPath = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!outputPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Unsafe ZIP entry: {entry.FullName}");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(outputPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var input = entry.Open();
            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
    }

    public static string FindUnityExecutable(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var candidates = Directory.EnumerateFiles(root, "*.exe", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).Contains("CrashHandler", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length != 1)
        {
            throw new InvalidDataException($"Expected exactly one Unity game executable, found {candidates.Length}.");
        }

        var gameExe = candidates[0];
        var dataDirectory = Path.Combine(root, Path.GetFileNameWithoutExtension(gameExe) + "_Data");
        if (!Directory.Exists(dataDirectory) || !File.Exists(Path.Combine(root, "UnityPlayer.dll")))
        {
            throw new InvalidDataException("Unity payload is missing its matching Data directory or UnityPlayer.dll.");
        }

        return gameExe;
    }
}
