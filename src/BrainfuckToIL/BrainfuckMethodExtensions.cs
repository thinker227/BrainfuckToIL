using BrainfuckToIL.Emit;

namespace BrainfuckToIL;

public static class BrainfuckMethodExtensions
{
    /// <summary>
    /// Takes an existing <see cref="BrainfuckMethod"/> and returns a <see cref="BrainfuckMethod{TIn}"/>
    /// which redirects user input using an input string.
    /// </summary>
    /// <param name="method">The existing method.</param>
    /// <returns>
    /// A <see cref="BrainfuckMethod{TIn}"/> which takes a sequence of characters as an argument
    /// and uses it as the input for the program.
    /// The input is read lazily as the program requests user input.
    /// If input is requested after the entire string has been read, then 0 will be returned.
    /// </returns>
    /// <remarks>
    /// This method only has an effect if the existing <see cref="BrainfuckMethod"/> was emitted
    /// using <see cref="Emitter.EmitAsDelegate"/> with <see cref="EmitOptions.InputMode"/>
    /// set to <see cref="InputMode.Stream"/>.
    /// </remarks>
    public static BrainfuckMethod<IEnumerable<char>> UseInputRedirection(
        this BrainfuckMethod method) => input =>
    {
        var original = Console.In;
        
        var redirect = new NullTerminatedCharReader(input);
        Console.SetIn(redirect);

        method();
        
        Console.SetIn(original);
    };

    /// <summary>
    /// Takes an existing <see cref="BrainfuckFunction{T}"/> and returns
    /// a <see cref="BrainfuckFunction{TIn, TOut}"/> which redirects user input using an input string.
    /// </summary>
    /// <param name="function">The existing function.</param>
    /// <typeparam name="TOut">The type of the output of the program.</typeparam>
    /// <returns>
    /// A <see cref="BrainfuckFunction{TIn, TOut}"/> which takes a sequence of characters as an argument
    /// and uses it as the input for the program.
    /// The input is read lazily as the program requests user input.
    /// If input is requested after the entire string has been read, then 0 will be returned.
    /// </returns>
    /// <remarks>
    /// This method only has an effect if the existing <see cref="BrainfuckMethod"/> was emitted
    /// using <see cref="Emitter.EmitAsDelegate"/> with <see cref="EmitOptions.InputMode"/>
    /// set to <see cref="InputMode.Stream"/>.
    /// </remarks>
    public static BrainfuckFunction<IEnumerable<char>, TOut> UseInputRedirection<TOut>(
        this BrainfuckFunction<TOut> function) => input =>
    {
        var original = Console.In;
        
        var redirect = new NullTerminatedCharReader(input);
        Console.SetIn(redirect);

        var output = function();
        
        Console.SetIn(original);

        return output;
    };

    /// <summary>
    /// Takes an existing <see cref="BrainfuckMethod"/> and returns a <see cref="BrainfuckFunction{T}"/>
    /// which redirects output and returns it as a string.
    /// </summary>
    /// <param name="method">The existing method.</param>
    /// <returns>A <see cref="BrainfuckFunction{T}"/> which returns the output of the program as a string.</returns>
    public static BrainfuckFunction<string> UseOutputRedirection(
        this BrainfuckMethod method) => () =>
    {
        var original = Console.Out;

        var writer = new StringWriter();
        Console.SetOut(writer);

        method();
        
        Console.SetOut(original);

        return writer.ToString();
    };

    /// <summary>
    /// Takes an existing <see cref="BrainfuckMethod{TIn}"/> and returns a <see cref="BrainfuckFunction{TIn, TOut}"/>
    /// which redirects output and returns it as a string.
    /// </summary>
    /// <param name="method">The existing method.</param>
    /// <typeparam name="TIn">The type of the input to the program.</typeparam>
    /// <returns>A <see cref="BrainfuckFunction{T}"/> which returns the output of the program as a string.</returns>
    public static BrainfuckFunction<TIn, string> UseOutputRedirection<TIn>(
        this BrainfuckMethod<TIn> method) => input =>
    {
        var original = Console.Out;

        var writer = new StringWriter();
        Console.SetOut(writer);

        method(input);
        
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
