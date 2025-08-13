using System;

namespace Sellorio.YouTubeMusicGrabber.Helpers;
internal static class ConsoleHelper
{
    public static void ResetBackToPositionAndClearConsole((int Left, int Top) position)
    {
        var endPosition = Console.GetCursorPosition();
        var linesToClear = endPosition.Left == 0 ? (endPosition.Top - position.Top) : (endPosition.Top - position.Top + 1);

        Console.SetCursorPosition(position.Left, position.Top);

        for (var i = 0; i < linesToClear; i++)
        {
            Console.WriteLine(new string(' ', Console.WindowWidth));
        }

        Console.SetCursorPosition(position.Left, position.Top);
    }
}
