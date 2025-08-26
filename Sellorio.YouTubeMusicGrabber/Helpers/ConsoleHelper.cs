using System;

namespace Sellorio.YouTubeMusicGrabber.Helpers;

internal static class ConsoleHelper
{
    public static void WriteLine(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;

        try
        {
            Console.WriteLine(text);
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;

        try
        {
            Console.Write(text);
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public static Uri PromptForUri(string prompt, bool required = false, UriKind kind = UriKind.Absolute)
    {
        Console.Write("\r\n" + prompt);
        var uriString = Console.ReadLine();

        if (string.IsNullOrEmpty(uriString))
        {
            if (required)
            {
                WriteLine("You must enter a value.", ConsoleColor.Red);
                return PromptForUri(prompt, required, kind);
            }

            return null;
        }

        if (!Uri.TryCreate(uriString, kind, out var result))
        {
            WriteLine("Not a valid url.", ConsoleColor.Red);
            return PromptForUri(prompt, required, kind);
        }

        return result;
    }
}
