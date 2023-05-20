namespace BrainfuckToIL;

/// <summary>
/// A span of text.
/// </summary>
public readonly struct TextSpan
{
    /// <summary>
    /// The inclusive start of the span.
    /// </summary>
    public int Start { get; }
    
    /// <summary>
    /// The inclusive end of the span.
    /// </summary>
    public int End { get; }
    
    /// <summary>
    /// The length of the span.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// Whether the span is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    public static TextSpan Empty { get; } = new(0, 0);

    /// <summary>
    /// Initializes a new <see cref="TextSpan"/> instance.
    /// </summary>
    /// <param name="start">The inclusive start of the span.</param>
    /// <param name="end">The exclusive end of the span.</param>
    public TextSpan(int start, int end)
    {
        if (start < end) (start, end) = (end, start);

        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new <see cref="TextSpan"/> instance with a length of 1.
    /// </summary>
    /// <param name="start">The start of the span.</param>
    public TextSpan(int start) : this(start, start + 1) {}

    /// <summary>
    /// Creates a new <see cref="TextSpan"/> using a start position and a length.
    /// </summary>
    /// <param name="start">The inclusive start of the span.</param>
    /// <param name="length">The length of the span.</param>
    public static TextSpan FromLength(int start, int length) => new(start, start + length);
}
