namespace SkyGlance.Services;

static class Log
{
    public static readonly string Path = System.IO.Path.Combine(AppPaths.Data, "error.log");

    public static void Error(Exception? ex) => Write(ex?.ToString() ?? "(null)");

    public static void Info(string message) => Write(message);

    static void Write(string text)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Data);
            File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}\r\n\r\n");
        }
        catch { }
    }
}
