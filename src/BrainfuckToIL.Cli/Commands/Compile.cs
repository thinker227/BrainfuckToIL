using System.CommandLine;
using Spectre.Console;

namespace BrainfuckToIL.Cli.Commands;

internal sealed class Compile : Command, ICommand<Compile>
{
    public static Compile Command { get; } = GetCommand();
    public static new string Name => "compile";
    public static new string Description => "Compiles a file to IL.";

    private static string ArgsAndOptions => $"""
    <source> [<output>] [--output-kind <exe|dll>] [{MemorySizeOption.Syntax}] [{NoWrapOption.Syntax}]
    """;
    
    public static string TopLevelSyntax => $"""
    bftoil {ArgsAndOptions} 
    """;
    
    // Compile is used as one of the top-level commands.
    public static string Syntax => $"""
    bftoil compile {ArgsAndOptions} 
    """;
    
    public static string ShortSyntax => """
    compile <source> [<output>]
    """;

    public static new IEnumerable<(string syntax, string description)> Arguments => new[]
    {
        (
            "<source>",
            "The source file to compile."),
        
        (
            "[<output>]",
            "The output destination for the compiled binary file. " +
            "If the provided value is a directory then " +
            "the output file will be located in the specified directory " +
            "and use the file name of the source file. " +
            "If not specified, the output file will be located in the same " +
            "directory as the source file and use the file name of the source file.")
    };

    public static new IEnumerable<(string syntax, string description)> Options => new[]
    {
        (
            "--output-kind <exe|dll>",
            "Whether to output an exe or DLL file. [default: exe]"),
        
        (
            MemorySizeOption.Syntax,
            MemorySizeOption.Description),
        
        (
            NoWrapOption.Syntax,
            NoWrapOption.Description)
    };

    public static new IEnumerable<(string syntax, string description)> Subcommands =>
        Array.Empty<(string, string)>();

    public Compile() : base(Name, Description) {}

    private static Compile GetCommand()
    {
        var command = new Compile();
        
        var sourceArgument = new Argument<FileInfo>("source");
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);
        
        var outputArgument = new Argument<FileSystemInfo?>("output");
        outputArgument.LegalFilePathsOnly();
        outputArgument.SetDefaultValue(null);
        command.AddArgument(outputArgument);

        var outputKindOption = new Option<DisplayOutputKind>("--output-kind");
        outputKindOption.AddAlias("-o");
        outputKindOption.SetDefaultValue(DisplayOutputKind.Exe);
        command.AddOption(outputKindOption);
        
        command.AddOption(MemorySizeOption.Option);
        
        command.AddOption(NoWrapOption.Option);
        
        command.SetHandler(ctx =>
        {
            var handler = new Handlers.Compile(
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForArgument(outputArgument),
                ctx.ParseResult.GetValueForOption(outputKindOption),
                ctx.ParseResult.GetValueForOption(MemorySizeOption.Option),
                ctx.ParseResult.GetValueForOption(NoWrapOption.Option));
        });

        return command;
    }
}
