namespace BrainfuckToIL;

// These are equivalent to Action, Action<T>, Func<T>, and Func<T1, TResult>
// but they serve to distinguish a Brainfuck program delegate from other more general delegates.

/// <summary>
/// Represents a method which executes a piece of Brainfuck code.
/// </summary>
public delegate void BrainfuckMethod();

/// <summary>
/// Represents a method which executes a piece of Brainfuck code
/// using a specified input.
/// </summary>
/// <param name="input">The input to the program.</param>
/// <typeparam name="TIn">The type of the input to the program.</typeparam>
public delegate void BrainfuckMethod<in TIn>(TIn input);

/// <summary>
/// Represents a function which executes a piece of Brainfuck code
/// and returns the output of the program.
/// </summary>
/// <returns>The output of the program.</returns>
/// <typeparam name="TOut">The type of the output of the program.</typeparam>
public delegate TOut BrainfuckFunction<out TOut>();

/// <summary>
/// Represents a function which executes a piece of Brainfuck code
/// using a specified input and returns the output of the program.
/// </summary>
/// <param name="input">The input to the program.</param>
/// <typeparam name="TIn">The type of the input to the program.</typeparam>
/// <typeparam name="TOut">The type of the output of the program.</typeparam>
/// <returns>The output of the program.</returns>
public delegate TOut BrainfuckFunction<in TIn, out TOut>(TIn input);
