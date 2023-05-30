using System.CommandLine;

namespace BrainfuckToIL.Cli.Commands;

internal static class NoWrapOption
{
    public static string Syntax => "--no-wrap";

    public static string Description => "Whether to disable memory wrapping, " +
                                        "i.e. that memory below cell 0 wraps around to the maximum cell " +
                                        "and that memory above the maximum cell wraps around to cell 0.";
    
    public static Option<bool> Option { get; } = GetOption();
    
    private static Option<bool> GetOption() => new("--no-wrap");
}
