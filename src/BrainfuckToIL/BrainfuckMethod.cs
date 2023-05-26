namespace BrainfuckToIL;

/// <summary>
/// Represents a method which executes a piece of Brainfuck code.
/// </summary>
public delegate void BrainfuckMethod();

/// <summary>
/// Represents a method which executes a piece of Brainfuck code and returns a value.
/// </summary>
/// <typeparam name="T">The type of the value the method returns.</typeparam>
public delegate T BrainfuckMethod<out T>();
