using System.CommandLine;
using System.CommandLine.Parsing;

namespace BrainfuckToIL.Cli;

internal static class CommandExtensions
{
    /// <summary>
    /// Gets all parents of a command, including itself.
    /// </summary>
    /// <param name="command">The command to get the parents of.</param>
    public static IEnumerable<Command> GetParentCommandsAndSelf(this Command command) => command.Parents
        .OfType<Command>()
        .SelectMany(cmd => cmd.GetParentCommandsAndSelf())
        .Prepend(command);
    
    /// <summary>
    /// Gets an argument with a specific name from a command.
    /// </summary>
    /// <param name="command">The command to get the argument from.</param>
    /// <param name="name">The name of the argument to get.</param>
    /// <typeparam name="TArgument">The type of the argument to get.</typeparam>
    public static TArgument GetArgumentWithName<TArgument>(
        this Command command,
        string name)
        where TArgument : Argument
    {
        var arguments = command
            .GetParentCommandsAndSelf()
            .SelectMany(cmd => cmd.Arguments);
        
        var arg = arguments
            .OfType<TArgument>()
            .FirstOrDefault(arg => arg.Name == name);

        return arg ?? throw new InvalidOperationException($"Command has no argument '{name}'.");
    }
    
    /// <summary>
    /// Gets an option with a specific name from a command.
    /// </summary>
    /// <param name="command">The command to get the option from.</param>
    /// <param name="name">The name of the option to get, without the leading <c>--</c>.</param>
    /// <typeparam name="TOption">The type of the option to get.</typeparam>
    public static TOption GetOptionWithName<TOption>(
        this Command command,
        string name)
        where TOption : Option
    {
        var options = command
            .GetParentCommandsAndSelf()
            .SelectMany(cmd => cmd.Options);
        
        var option = options
            .OfType<TOption>()
            .FirstOrDefault(arg => arg.Name == name);

        return option ?? throw new InvalidOperationException($"Command has no option '{name}'.");
    }

    /// <summary>
    /// Gets the value for an argument with a specific name from a <see cref="ParseResult"/>.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <param name="name">The name of the argument to get the value of.</param>
    /// <typeparam name="T">The contained type of the argument to get.</typeparam>
    public static T GetValueForArgumentWithName<T>(
        this ParseResult parseResult,
        string name) =>
        parseResult.GetValueForArgument(
            parseResult.CommandResult.Command.GetArgumentWithName<Argument<T>>(name));

    /// <summary>
    /// Gets the value for an option with a specific name from a <see cref="ParseResult"/>.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <param name="name">The name of the option to get the value of.</param>
    /// <typeparam name="T">The contained type of the option to get.</typeparam>
    public static T? GetValueForOptionWithName<T>(
        this ParseResult parseResult,
        string name) =>
        parseResult.GetValueForOption(
            parseResult.CommandResult.Command.GetOptionWithName<Option<T>>(name));
}
