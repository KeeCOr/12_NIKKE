using System.IO.Compression;
using System.Text;
using UnityPortable;

var passed = 0;

Run("SHA-256 is deterministic", () =>
{
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
    Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", LauncherCore.ComputeSha256Hex(stream));
});

Run("safe ZIP preserves nested files", () =>
{
    using var zip = BuildZip(("Game.exe", "exe"), ("Game_Data/state.bin", "data"), ("UnityPlayer.dll", "dll"));
    var root = NewTempDirectory();
    try
    {
        LauncherCore.ExtractZipSafely(zip, root);
        Equal("data", File.ReadAllText(Path.Combine(root, "Game_Data", "state.bin")));
        Equal(Path.Combine(root, "Game.exe"), LauncherCore.FindUnityExecutable(root));
    }
    finally { Directory.Delete(root, recursive: true); }
});

Run("ZIP traversal is rejected", () =>
{
    using var zip = BuildZip(("../escape.txt", "bad"));
    var root = NewTempDirectory();
    try { Throws<InvalidDataException>(() => LauncherCore.ExtractZipSafely(zip, root)); }
    finally { Directory.Delete(root, recursive: true); }
});

Run("crash handler is not selected as the game", () =>
{
    var root = NewTempDirectory();
    try
    {
        File.WriteAllText(Path.Combine(root, "Game.exe"), "exe");
        File.WriteAllText(Path.Combine(root, "UnityCrashHandler64.exe"), "crash");
        File.WriteAllText(Path.Combine(root, "UnityPlayer.dll"), "dll");
        Directory.CreateDirectory(Path.Combine(root, "Game_Data"));
        Equal(Path.Combine(root, "Game.exe"), LauncherCore.FindUnityExecutable(root));
    }
    finally { Directory.Delete(root, recursive: true); }
});

Console.WriteLine($"PASS {passed}/4");
return;

void Run(string name, Action test)
{
    test();
    passed++;
    Console.WriteLine($"PASS: {name}");
}

static MemoryStream BuildZip(params (string Name, string Content)[] entries)
{
    var stream = new MemoryStream();
    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var item in entries)
        {
            var entry = archive.CreateEntry(item.Name);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
            writer.Write(item.Content);
        }
    }
    stream.Position = 0;
    return stream;
}

static string NewTempDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), "unity-launcher-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

static void Equal(string expected, string actual)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new Exception($"Expected '{expected}', got '{actual}'.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
