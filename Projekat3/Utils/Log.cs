namespace Projekat3.Utils;

public static class Log
{
    public static void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[INFO] {msg}");
        Console.ResetColor();
    }

    public static void Err(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {msg}");
        Console.ResetColor();
    }

    public static void Rx(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"[RX] {msg}");
        Console.ResetColor();
    }

    public static void Ml(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[ML] {msg}");
        Console.ResetColor();
    }

    public static void Api(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[API] {msg}");
        Console.ResetColor();
    }
}
