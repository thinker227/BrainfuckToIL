using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Spectre.Console;
using BrainfuckToIL.Cli.Handlers;

namespace BrainfuckToIL.Cli;

internal static class CommandLine
{
    public static CommandLineParser GetParser(string[] rawArgs) =>
        GetDefaultBuilder(GetRootCommand())
            .AddMiddleware(DefaultCommandMiddleware(rawArgs))
            .Build();

    private static CommandLineBuilder GetDefaultBuilder(Command rootCommand) =>
        new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .AddMiddleware(PlainOutputMiddleware, MiddlewareOrder.Configuration);

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

        var memorySizeOption = new Option<int>("--memory-size")
        {
            Description = "The size of the memory in the amount of cells long it is."
        };
        memorySizeOption.AddAlias("-m");
        memorySizeOption.SetDefaultValue(30_000);
        memorySizeOption.AddValidator(MemorySizeValidator);
        command.AddOption(memorySizeOption);

        var noWrapOption = new Option<bool>("--no-wrap")
        {
            Description = "Whether to disable memory wrapping, " +
                          "i.e. that memory below cell 0 wraps around to the maximum cell " +
                          "and that memory above the maximum cell wraps around to cell 0."
        };
        command.AddOption(noWrapOption);
        
        command.SetHandler(ctx =>
        {
            var handler = new Compile(
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForArgument(outputArgument),
                ctx.ParseResult.GetValueForOption(outputKindOption),
                ctx.ParseResult.GetValueForOption(memorySizeOption),
                ctx.ParseResult.GetValueForOption(noWrapOption));
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

        var inputOption = new Option<string?>("--input")
        {
            Description = "If specified, the program will read from this value " +
                          "when encountering a , instruction rather than using input from the console. " +
                          "If a , instruction is encountered after the entire input has already been read, " +
                          "0 will always be returned."
        };
        inputOption.SetDefaultValue(null);
        command.AddOption(inputOption);

        var memorySizeOption = new Option<int>("--memory-size")
        {
            Description = "The size of the memory in the amount of cells long it is."
        };
        memorySizeOption.AddAlias("-m");
        memorySizeOption.SetDefaultValue(30_000);
        memorySizeOption.AddValidator(MemorySizeValidator);
        command.AddOption(memorySizeOption);

        var noWrapOption = new Option<bool>("--no-wrap")
        {
            Description = "Whether to disable memory wrapping, " +
                          "i.e. that memory below cell 0 wraps around to the maximum cell " +
                          "and that memory above the maximum cell wraps around to cell 0."
        };
        command.AddOption(noWrapOption);
        
        command.SetHandler(ctx =>
        {
            var handler = new Run(
                ctx.BindingContext.GetRequiredService<IAnsiConsole>());
            
            ctx.ExitCode = handler.Handle(
                ctx.ParseResult.GetValueForArgument(sourceArgument),
                ctx.ParseResult.GetValueForOption(memorySizeOption),
                ctx.ParseResult.GetValueForOption(noWrapOption),
                ctx.ParseResult.GetValueForOption(inputOption));
        });

        return command;
    }

    private static void MemorySizeValidator(OptionResult result)
    {
        var value = result.GetValueOrDefault<int>();

        if (value <= 0)
        {
            result.ErrorMessage = "Memory size cannot be less than or equal to 0.";
        }
    }

    private static void PlainOutputMiddleware(InvocationContext ctx)
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
    }

    private static Action<InvocationContext> DefaultCommandMiddleware(string[] rawArgs) => ctx =>
    {
        // If this is not the root command that is invoked then don't do anything.
        if (ctx.ParseResult.CommandResult.Command != ctx.ParseResult.RootCommandResult.Command) return;

        var command = CompileCommand();
        var builder = GetDefaultBuilder(command);
        var parser = builder.Build();
        
        var result = parser.Parse(rawArgs);

        // Override the parse result with the new one.
        ctx.ParseResult = result;
    };
}
