namespace GothicSaveBackupper;
public static class Helpers
{
    public static ConsoleColor _basicForegroundColor = ConsoleColor.White;

    public static void ConsoleWriteLine(string message, ConsoleColor? color = null)
    {
        if (color.HasValue == false)
            color = _basicForegroundColor;

        Console.ForegroundColor = color.Value;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]: {message}");

        Console.ForegroundColor = _basicForegroundColor;
    }
}
