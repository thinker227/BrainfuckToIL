namespace BrainfuckToIL.Emit;

/// <summary>
/// Represents modes of reading user input when input when encountering a <c>,</c> instruction.
/// </summary>
public enum InputMode
{
    /// <summary>
    /// The emitted program will wait for a key to be pressed using <see cref="Console.ReadKey(bool)"/>.
    /// </summary>
    Key,
    /// <summary>
    /// The emitted program will read directly from <see cref="Console.In"/> using <see cref="Console.Read()"/>.
    /// </summary>
    Stream
}
