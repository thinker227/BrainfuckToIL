using System.CommandLine;
using Spectre.Console;

namespace BrainfuckToIL.Cli.Commands;

internal sealed class Run : Command, ICommand<Run>
{
    public static Run Command { get; } = GetCommand();

    public static new string Name => "run";

    public static new string Description => "Compiles and runs a file.";

    public static string Syntax => $"""
    bftoil run <source> [--input <input>] [{MemorySizeOption.Syntax}] [{NoWrapOption.Syntax}]
    """;

    public static string ShortSyntax => """
    run <source>
    """;

    public static new IEnumerable<(string syntax, string description)> Arguments => new[]
    {
        (
            "<source>",
            "The source file to compile and run.")
    };

    public static new IEnumerable<(string syntax, string description)> Options => new[]
    {
        (
            "--input",
            "If specified, the program will read from this value " +
            "when encountering a , instruction rather than using input from the console. " +
            "If a , instruction is encountered after the entire input has already been read, " +
            "0 will always be returned."),
        
        (
            MemorySizeOption.Syntax,
            MemorySizeOption.Description),
        
        (
            NoWrapOption.Syntax,
            NoWrapOption.Description)
    };
    
    public static new IEnumerable<(string syntax, string description)> Subcommands =>
        Array.Empty<(string, string)>();

    public Run() : base(Name, Description) {}

    private static Run GetCommand()
    {
        var command = new Run();

        var sourceArgument = new Argument<FileInfo>("source");
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);

        var inputOption = new Option<string?>("--input");
        inputOption.SetDefaultValue(null);
        command.AddOption(inputOption);

        command.AddOption(MemorySizeOption.Option);
        
        command.AddOption(NoWrapOption.Option);
        
        command.SetHandler(ctx =>
        {
            var handler = new Handlers.Run(
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForOption(MemorySizeOption.Option),
                ctx.ParseResult.GetValueForOption(NoWrapOption.Option),
                ctx.ParseResult.GetValueForOption(inputOption));
        });

        return command;
    }
}
