using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace AiSetupUi;

/// <summary>Plain-text report about this PC and its Python setup. No UI code.</summary>
internal static class SystemInfoService
{
    public static async Task<string> BuildAsync(CancellationToken ct)
    {
        var py = AppPaths.PythonExe;
        var sb = new StringBuilder();
        sb.AppendLine("=== SYSTEM INFO ===");

        try
        {
            sb.AppendLine($"OS         : {GetOsCaption()}");
            sb.AppendLine($"Build      : {Environment.OSVersion.Version}");
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref mem))
            {
                sb.AppendLine($"RAM total  : {mem.ullTotalPhys / 1073741824.0:0.0} GB");
                sb.AppendLine($"RAM free   : {mem.ullAvailPhys / 1073741824.0:0.0} GB");
            }
        }
        catch (Exception ex) { sb.AppendLine($"OS info    : unavailable ({ex.Message})"); }

        try
        {
            var root = Path.GetPathRoot(AppPaths.Root) ?? "C:\\";
            var drive = new DriveInfo(root);
            sb.AppendLine($"{drive.Name.TrimEnd('\\')} free    : {drive.AvailableFreeSpace / 1073741824.0:0.0} GB");
        }
        catch { }

        // Useful before installing torch / vllm / bitsandbytes: is there a CUDA-capable GPU?
        var gpu = await ProcessRunner.RunAsync("nvidia-smi",
            new[] { "--query-gpu=name,memory.total,driver_version", "--format=csv,noheader" }, ct);
        sb.AppendLine(gpu.Started && gpu.ExitCode == 0
            ? $"GPU        : {gpu.Stdout.Trim().ReplaceLineEndings(" | ")}"
            : "GPU        : no NVIDIA GPU / driver detected (CPU-only installs will be used)");

        var pyv = await ProcessRunner.RunAsync(py, new[] { "--version" }, ct);
        sb.AppendLine(pyv.Started
            ? $"Python     : {pyv.Combined}  ({py})"
            : "Python     : unavailable");

        var pipv = await ProcessRunner.RunAsync(py, new[] { "-m", "pip", "--version" }, ct);
        if (pipv.Started && pipv.ExitCode == 0) sb.AppendLine($"pip        : {pipv.Combined}");

        var pkgs = await PipService.ListAsync(ct);
        if (pkgs != null) sb.AppendLine($"pip pkgs   : {pkgs.Count} installed");

        return sb.ToString();
    }

    private static string GetOsCaption()
    {
        const string key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        var product = Registry.GetValue(key, "ProductName", null) as string ?? "Windows";
        // The registry still says "Windows 10" on Windows 11; build 22000+ is 11.
        if (Environment.OSVersion.Version.Build >= 22000)
            product = product.Replace("Windows 10", "Windows 11");
        return product;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
