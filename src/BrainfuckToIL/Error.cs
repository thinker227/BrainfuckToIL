namespace BrainfuckToIL;

/// <summary>
/// An error produced by a compilation operation.
/// Can either be an error produced by a <see cref="Parser"/> or by an <see cref="Emitter"/>.
/// </summary>
/// <param name="Message">The message describing the error.</param>
/// <param name="Location">The location of the error,
/// or <see langword="null"/> if the error does not have a specific location.</param>
public readonly record struct Error(
    string Message,
    TextSpan? Location);
