namespace AiSetupUi;

internal static class AppPaths
{
    /// <summary>Folder the exe lives in. Replaces $PSScriptRoot from the old script.</summary>
    public static readonly string Root =
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    public static string VenvDir => Path.Combine(Root, ".venv");
    public static string VenvPython => Path.Combine(VenvDir, "Scripts", "python.exe");
    public static string ProjectsDir => Path.Combine(Root, "projects");
    public static string InstalledPackagesFile => Path.Combine(Root, "installed_packages.txt");
    public static string RequirementsFile => Path.Combine(Root, "requirements-full.txt");
    public static string LogFile => Path.Combine(Root, "setup_log.txt");

    public static string KernelDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "jupyter", "kernels", "ai-fullstack");

    /// <summary>The venv's python if it exists, otherwise whatever "python" is on PATH.</summary>
    public static string PythonExe => File.Exists(VenvPython) ? VenvPython : "python";
}
