using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnityPortable;

internal static class Program
{
    private const string ResourceName = "UnityPortable.payload.zip";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var payload = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("Embedded Unity payload is missing.");
            var payloadHash = LauncherCore.ComputeSha256Hex(payload);
            payload.Position = 0;

            var product = Sanitize(assembly.GetName().Name ?? "UnityGame");
            var cacheRoot = Path.Combine(Path.GetTempPath(), "CodexUnityPortable", $"{product}-{payloadHash[..16]}");
            var marker = Path.Combine(cacheRoot, ".payload.sha256");
            var mutexName = $"Local\\CodexUnityPortable-{product}-{payloadHash[..16]}";

            using (var mutex = new Mutex(false, mutexName))
            {
                if (!mutex.WaitOne(TimeSpan.FromMinutes(2)))
                {
                    throw new TimeoutException("Timed out while preparing the portable game.");
                }

                try
                {
                    var validCache = File.Exists(marker)
                        && string.Equals(File.ReadAllText(marker), payloadHash, StringComparison.Ordinal);
                    if (!validCache)
                    {
                        if (Directory.Exists(cacheRoot)) Directory.Delete(cacheRoot, recursive: true);
                        Directory.CreateDirectory(cacheRoot);
                        LauncherCore.ExtractZipSafely(payload, cacheRoot);
                        _ = LauncherCore.FindUnityExecutable(cacheRoot);
                        File.WriteAllText(marker, payloadHash);
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            var gameExe = LauncherCore.FindUnityExecutable(cacheRoot);
            var startInfo = new ProcessStartInfo
            {
                FileName = gameExe,
                WorkingDirectory = cacheRoot,
                UseShellExecute = false
            };
            foreach (var argument in args) startInfo.ArgumentList.Add(argument);

            using var child = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unity process did not start.");
            child.WaitForExit();
            return child.ExitCode;
        }
        catch (Exception error)
        {
            var logDirectory = Path.Combine(Path.GetTempPath(), "CodexUnityPortable");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, "launcher-error.log");
            File.WriteAllText(logPath, error.ToString());
            MessageBoxW(IntPtr.Zero, $"게임을 열지 못했습니다.\n\n{error.Message}\n\n로그: {logPath}", "Portable 실행 오류", 0x10);
            return 1;
        }
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
