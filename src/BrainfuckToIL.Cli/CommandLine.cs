using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Spectre.Console;
using BrainfuckToIL.Cli.Handlers;

namespace BrainfuckToIL.Cli;

internal static class CommandLine
{
    public static CommandLineParser GetParser()
    {
        var rootCommand = GetRootCommand();
        var builder = new CommandLineBuilder(rootCommand);
        
        builder.UseDefaults();
        
        builder.AddMiddleware(ctx =>
        {
            var plain = ctx.ParseResult.GetValueForOptionWithName<bool>("plain");

            var console = AnsiConsole.Create(new AnsiConsoleSettings()
            {
                ColorSystem = plain
                    ? ColorSystemSupport.NoColors
                    : ColorSystemSupport.Detect
            });
            
            ctx.Console = new SpectreConsoleConsole(console);
            ctx.BindingContext.AddService(_ => console);
        }, MiddlewareOrder.Configuration);
        
        return builder.Build();
    }

    private static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand()
        {
            Name = "BFtoIL",
            Description = "Simplistic Brainfuck to IL compiler."
        };

        var plainOption = new Option<bool>("--plain")
        {
            Description = "Disables color output from the compiler CLI."
        };
        rootCommand.AddGlobalOption(plainOption);

        var memorySizeOption = new Option<int>("--memory-size")
        {
            Description = "The size of the memory in the amount of cells long it is."
        };
        memorySizeOption.AddAlias("-m");
        memorySizeOption.SetDefaultValue(30_000);
        memorySizeOption.AddValidator(MemorySizeValidator);
        rootCommand.AddGlobalOption(memorySizeOption);

        var compileCommand = CompileCommand();
        rootCommand.AddCommand(compileCommand);

        var runCommand = RunCommand();
        rootCommand.AddCommand(runCommand);

        return rootCommand;
    }

    private static void MemorySizeValidator(OptionResult result)
    {
        var value = result.GetValueOrDefault<int>();

        if (value <= 0)
        {
            result.ErrorMessage = "Memory size cannot be less than or equal to 0.";
        }
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
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForArgument(outputArgument),
                ctx.ParseResult.GetValueForOption(outputKindOption),
                ctx.ParseResult.GetValueForOptionWithName<int>("memory-size"));
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
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForOptionWithName<int>("memory-size"));
        });

        return command;
    }
}
