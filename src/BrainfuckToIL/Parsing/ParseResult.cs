using System.Collections.Immutable;

namespace BrainfuckToIL.Parsing;

/// <summary>
/// The result of a parse operation.
/// </summary>
/// <param name="Instructions">The parsed instructions.</param>
public readonly record struct ParseResult(ImmutableArray<Instruction> Instructions)
{
    /// <summary>
    /// The errors produced by the parser.
    /// </summary>
    public IEnumerable<Error> Errors =>
        Instructions.Flatten().SelectMany(i => i.Errors);
}
