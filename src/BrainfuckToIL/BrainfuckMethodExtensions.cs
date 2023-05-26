using BrainfuckToIL.Emit;

namespace BrainfuckToIL;

public static class BrainfuckMethodExtensions
{
    /// <summary>
    /// Takes an existing <see cref="BrainfuckMethod"/> and returns a new <see cref="BrainfuckMethod"/>
    /// which redirects user input using an input string.
    /// </summary>
    /// <param name="method">The existing method.</param>
    /// <param name="input">The input to use for the program.
    /// The input is read lazily as the program requests user input.
    /// If input is requested when there is no more input, <c>0</c> will be returned.</param>
    /// <remarks>
    /// This method only has an effect if the existing <see cref="BrainfuckMethod"/> was emitted
    /// using <see cref="Emitter.EmitAsDelegate"/> with <see cref="EmitOptions.InputMode"/>
    /// set to <see cref="InputMode.Stream"/>.
    /// </remarks>
    public static BrainfuckMethod UseInputRedirection(
        this BrainfuckMethod method,
        string input) => () =>
    {
        var original = Console.In;
        
        var redirect = new NullTerminatedCharReader(input);
        Console.SetIn(redirect);

        method();
        
        Console.SetIn(original);
    };

    /// <summary>
    /// Takes an existing <see cref="BrainfuckMethod"/> and returns a <see cref="BrainfuckMethod{T}"/>
    /// which redirects output and returns it as a string.
    /// </summary>
    /// <param name="method">The existing method.</param>
    /// <returns>A <see cref="BrainfuckMethod{T}"/> which returns the output of the program as a string.</returns>
    public static BrainfuckMethod<string> UseOutputRedirection(
        this BrainfuckMethod method) => () =>
    {
        var original = Console.Out;

        var writer = new StringWriter();
        Console.SetOut(writer);

        method();
        
        Console.SetOut(original);

        return writer.ToString();
    };

    private sealed class NullTerminatedCharReader : TextReader
    {
        private readonly IEnumerator<char> input;

        public NullTerminatedCharReader(IEnumerable<char> input) =>
            this.input = input.GetEnumerator();

        public override int Read() =>
            input.MoveNext()
                ? (byte)input.Current
                : 0;
    }
}
