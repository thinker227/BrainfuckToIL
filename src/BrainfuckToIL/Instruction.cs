using System.Collections.Immutable;

namespace BrainfuckToIL;

/// <summary>
/// A Brainfuck instruction.
/// </summary>
public abstract record Instruction
{
    /// <summary>
    /// A move right <c>&gt;</c> instruction
    /// </summary>
    public sealed record MoveRight : Instruction;

    /// <summary>
    /// A move left <c>&lt;</c> instruction
    /// </summary>
    public sealed record MoveLeft : Instruction;

    /// <summary>
    /// An increment <c>+</c> instruction
    /// </summary>
    public sealed record Increment : Instruction;

    /// <summary>
    /// A decrement <c>-</c> instruction
    /// </summary>
    public sealed record Decrement : Instruction;

    /// <summary>
    /// An output <c>.</c> instruction
    /// </summary>
    public sealed record Output : Instruction;

    /// <summary>
    /// An input <c>,</c> instruction
    /// </summary>
    public sealed record Input : Instruction;

    /// <summary>
    /// A loop <c>[]</c> instruction.
    /// </summary>
    /// <param name="Instructions">The instruction within the loop.</param>
    public sealed record Loop(ImmutableArray<Instruction> Instructions) : Instruction;
}
