using System.Collections.Immutable;

namespace BrainfuckToIL;

/// <summary>
/// The result of a parse operation.
/// </summary>
/// <param name="Instructions">The parsed instructions.</param>
public readonly record struct ParseResult(ImmutableArray<Instruction> Instructions);
