using Spectre.Console;

namespace BrainfuckToIL.Cli;

internal static class AnsiConsoleExtensions
{
    /// <summary>
    /// Writes a sequence of errors to a console.
    /// </summary>
    /// <param name="errors">The errors to write.</param>
    /// <param name="source">The string of source code.</param>
    /// <param name="console">The console to write to.</param>
    public static void WriteErrors(this IAnsiConsole console, IReadOnlyCollection<Error> errors, string source)
    {
        if (errors.Count == 0) throw new InvalidOperationException("No errors to write.");

        var map = LineMap.Create(source);

        foreach (var error in errors)
        {
            console.Markup($"[red]{error.Message}[/]");

            if (error.Location is not TextSpan location) continue;

            if (location.Length == 1)
            {
                if (map[location.Start] is not LineMap.LineInfo line) continue;
                
                console.MarkupLine($" at [aqua]{line.LineNumber + 1}:{location.Start - line.Span.Start + 1}[/]");
                
                continue;
            }
            
            var lines = map.GetLinesOfSpan(location).ToArray();
            if (lines.Length == 0) continue;

            var start = lines[0];
            var end = lines[^1];
            
            console.MarkupLine(" at " +
                               $"[aqua]{start.LineNumber + 1}:{location.Start - start.Span.Start + 1}[/]" +
                               " to " +
                               $"[aqua]{end.LineNumber + 1}:{location.End - end.Span.End}[/]");
        }
    }
}
