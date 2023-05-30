using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace BrainfuckToIL.Cli;

internal static class CommandLine
{
    /// <summary>
    /// Gets the <see cref="CommandLineParser"/> which parses the command line.
    /// </summary>
    /// <param name="rawArgs">The raw arguments passed to the program.</param>
    public static CommandLineParser GetParser(string[] rawArgs) =>
        GetDefaultBuilder(Commands.Root.Command)
            .AddMiddleware(DefaultCommandMiddleware(rawArgs))
            .Build();

    /// <summary>
    /// Gets the default <see cref="CommandLineBuilder"/>.
    /// </summary>
    private static CommandLineBuilder GetDefaultBuilder(Command rootCommand) =>
        new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelpBuilder(_ => new Help())
            .AddMiddleware(PlainOutputMiddleware, MiddlewareOrder.Configuration);

    /// <summary>
    /// Middleware which switches off color output for the <see cref="IAnsiConsole"/>
    /// used in the program if the --plain option is specified.
    /// </summary>
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

    /// <summary>
    /// Middleware which invokes compile if no other command is specified.
    /// </summary>
    private static Action<InvocationContext> DefaultCommandMiddleware(string[] rawArgs) => ctx =>
    {
        // If this is not the root command that is invoked then don't do anything.
        if (ctx.ParseResult.CommandResult.Command != ctx.ParseResult.RootCommandResult.Command) return;

        var command = Commands.Compile.Command;
        var builder = GetDefaultBuilder(command);
        var parser = builder.Build();
        
        var result = parser.Parse(rawArgs);

        // Override the parse result with the new one.
        ctx.ParseResult = result;
    };
}
