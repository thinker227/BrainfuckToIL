using BrainfuckToIL.Emit;
using BrainfuckToIL.Parsing;

namespace BrainfuckToIL.Tests.Integration;

/// <summary>
/// Base for integration tests.
/// </summary>
public abstract class IntegrationTestBase
{
    /// <summary>
    /// Runs a string with the given input and returns the output.
    /// </summary>
    /// <param name="source">The source string to run.</param>
    /// <param name="input">The input to the program.</param>
    /// <param name="configureParseOptions">A function called to configure
    /// the <see cref="ParseOptions"/> used for parsing.</param>
    /// <param name="configureEmitOptions">A function called to configure
    /// the <see cref="EmitOptions"/> used for emission.
    /// The <see cref="EmitOptions.InputFormat"/> will always be set to <see cref="InputMode.Stream"/>
    /// after configuration.</param>
    /// <returns>The output of the program.</returns>
    protected string Run(
        string source,
        string input = "",
        Func<ParseOptions, ParseOptions>? configureParseOptions = null,
        Func<EmitOptions, EmitOptions>? configureEmitOptions = null)
    {
        var baseParseOptions = new ParseOptions();
        var parseOptions = configureParseOptions?.Invoke(baseParseOptions) ?? baseParseOptions;
        
        var result = Parser.Parse(source, parseOptions);

        result.Errors.ShouldBeEmpty();

        var baseEmitOptions = new EmitOptions()
        {
            AssemblyName = GetType().Name
        };
        var emitOptions = configureEmitOptions?.Invoke(baseEmitOptions) ?? baseEmitOptions;
        
        var main = Emitter.EmitAsDelegate(result.Instructions, emitOptions)
            .UseInputRedirection(input)
            .UseOutputRedirection();
        
        var output = main();

        return output;
    }
}
