using System.CommandLine;

namespace BrainfuckToIL.Cli.Commands;

internal sealed class Root : Command, ICommand<Root>
{
    public static Root Command { get; } = GetCommand();
    
    public static new string Name => "bftoil";

    public static new string Description => "Simplistic Brainfuck to IL compiler.";
    
    public static string Syntax => $"""
    {Compile.TopLevelSyntax}
    bftoil {Run.ShortSyntax}
    """;

    public static string ShortSyntax => Syntax;

    public static new IEnumerable<(string syntax, string description)> Arguments => Compile.Arguments;

    public static new IEnumerable<(string syntax, string description)> Options => Compile.Options.Concat(new[]
    {
        (
            "--plain",
            "Disables color output from the compiler CLI."),
        (
            "-?|-h|--help",
            "Shows help and usage information."),
        (
            "--version",
            "Shows version information.")
    });

    public static new IEnumerable<(string syntax, string description)> Subcommands => new[]
    {
        (
            Run.ShortSyntax,
            Run.Description)
    };

    private Root() : base(Name, Description) {}

    private static Root GetCommand()
    {
        var rootCommand = new Root();

        var plainOption = new Option<bool>("--plain");
        rootCommand.AddGlobalOption(plainOption);

        rootCommand.AddCommand(Compile.Command);

        rootCommand.AddCommand(Run.Command);

        return rootCommand;
    }
}
