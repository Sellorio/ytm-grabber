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
}
