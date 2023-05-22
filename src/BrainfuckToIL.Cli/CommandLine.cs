using System.CommandLine;
using System.CommandLine.Builder;
using Spectre.Console;
using BrainfuckToIL.Cli.Handlers;

namespace BrainfuckToIL.Cli;

internal static class CommandLine
{
    public static CommandLineParser GetParser(RootCommand rootCommand, IAnsiConsole console, TextReader reader)
    {
        var builder = new CommandLineBuilder(rootCommand);
        
        builder.UseDefaults();
        
        builder.AddMiddleware(ctx =>
        {
            ctx.Console = new SpectreConsoleConsole(console);
            ctx.BindingContext.AddService(_ => reader);
        });
        
        return builder.Build();
    }

    public static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand()
        {
            Name = "BFtoIL",
            Description = "Simplistic Brainfuck to IL compiler."
        };

        var compileCommand = CompileCommand();
        rootCommand.AddCommand(compileCommand);

        var runCommand = RunCommand();
        rootCommand.AddCommand(runCommand);

        return rootCommand;
    }

    private static Command CompileCommand()
    {
        var command = new Command("compile")
        {
            Description = "Compiles a file to IL."
        };
        
        var sourceArgument = new Argument<FileInfo>("source")
        {
            Description = "The source file to compile."
        };
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);
        
        var outputArgument = new Argument<FileSystemInfo?>("output")
        {
            Description = "The output destination for the compiled binary file. " +
                          "If the provided value is a directory then " +
                          "the output file will be located in the specified directory " +
                          "and use the file name of the source file. " +
                          "If not specified, the output file will be located in the same " +
                          "directory as the source file and use the file name of the source file."
        };
        outputArgument.LegalFilePathsOnly();
        outputArgument.SetDefaultValue(null);
        command.AddArgument(outputArgument);

        var outputKindOption = new Option<DisplayOutputKind>("--output-kind")
        {
            Description = "Whether to output an exe or DLL file."
        };
        outputKindOption.AddAlias("-o");
        outputKindOption.SetDefaultValue(DisplayOutputKind.Exe);
        command.AddOption(outputKindOption);
        
        command.SetHandler(ctx =>
        {
            var handler = new Compile(
                ctx.Console,
                ctx.BindingContext.GetRequiredService<TextReader>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForArgument(outputArgument),
                ctx.ParseResult.GetValueForOption(outputKindOption));
        });

        return command;
    }

    private static Command RunCommand()
    {
        var command = new Command("run")
        {
            Description = "Compiles and runs a file."
        };

        var sourceArgument = new Argument<FileInfo>("source")
        {
            Description = "The source file to compile and run."
        };
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);
        
        command.SetHandler(ctx =>
        {
            var handler = new Run(
                ctx.Console,
                ctx.BindingContext.GetRequiredService<TextReader>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument));
        });

        return command;
    }
}
