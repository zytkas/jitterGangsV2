using System.IO;

public static class Logger
{
    private static string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "JitterGang",
        "app.log"
    );

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.AppendAllText(LogPath, $"[{DateTime.Now}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}