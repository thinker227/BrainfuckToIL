namespace BrainfuckToIL.Parsing;

/// <summary>
/// Options for a <see cref="Parser"/>.
/// </summary>
public readonly struct ParseOptions
{
    /// <summary>
    /// Whether to group together sequential instructions (<c>+</c> and <c>-</c>, <c>&gt;</c> and <c>&lt;</c>).
    /// Default is <see langword="true"/>.
    /// </summary>
    /// <example>
    /// If enabled, <c>&gt;&gt;&gt;</c> will be parsed as <code>
    /// dataPointer += 3;
    /// </code>
    /// <br/>
    /// If disabled, <c>&gt;&gt;&gt;</c> will be parsed as <code>
    /// dataPointer++;
    /// dataPointer++;
    /// dataPointer++;
    /// </code>
    /// </example>
    public bool GroupSequentialInstructions { get; init; } = true;
    
    public ParseOptions() {}
}
