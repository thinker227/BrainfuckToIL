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
            .UseDefaultCommand(rawArgs, Commands.Compile.Command)
            .Build();

    /// <summary>
    /// Gets the default <see cref="CommandLineBuilder"/>.
    /// </summary>
    public static CommandLineBuilder GetDefaultBuilder(Command rootCommand) =>
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
}
