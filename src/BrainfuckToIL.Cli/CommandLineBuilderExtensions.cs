using System.CommandLine;
using System.CommandLine.Builder;

namespace BrainfuckToIL.Cli;

public static class CommandLineBuilderExtensions
{
    /// <summary>
    /// Adds middleware which invokes a default command if the root command is invoked.
    /// </summary>
    /// <param name="builder">The source <see cref="CommandLineBuilder"/>.</param>
    /// <param name="rawArgs">The raw command-line arguments.</param>
    /// <param name="defaultCommand">The command to use as the default command.</param>
    public static CommandLineBuilder UseDefaultCommand(
        this CommandLineBuilder builder,
        string[] rawArgs,
        Command defaultCommand) =>
        builder.AddMiddleware(ctx =>
        {
            // If this is not the root command that is invoked then don't do anything.
            if (ctx.ParseResult.CommandResult.Command != ctx.ParseResult.RootCommandResult.Command) return;

            // This should probably have the same configuration used by the rest of your CLI.
            var builder = CommandLine.GetDefaultBuilder(defaultCommand);
            
            var parser = builder.Build();
            var result = parser.Parse(rawArgs);

            // Override the parse result with the new one.
            ctx.ParseResult = result;
        });
}
