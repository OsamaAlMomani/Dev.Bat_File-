namespace AiSetupUi;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var settings = AppSettings.Load();
            ThemeManager.Initialize(settings);
            Application.Run(new MainForm(settings));
        }
        catch (Exception ex)
        {
            // Last-resort net so a crash leaves something readable behind.
            try
            {
                File.WriteAllText(Path.Combine(AppPaths.Root, "setup_crash.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{ex}");
            }
            catch { /* nothing more we can do */ }

            MessageBox.Show(
                "The setup window hit an unexpected error and has to close.\n\n" + ex.Message +
                "\n\nDetails were saved to setup_crash.txt next to the app.",
                "Setup crashed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
