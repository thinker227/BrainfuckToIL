using System.Collections.Immutable;

namespace BrainfuckToIL;

/// <summary>
/// A Brainfuck instruction.
/// </summary>
public abstract record Instruction
{
    /// <summary>
    /// The parse errors caused by the instruction.
    /// </summary>
    public ImmutableArray<BrainfuckToIL.Error> Errors { get; init; } = ImmutableArray<BrainfuckToIL.Error>.Empty;

    /// <summary>
    /// One or more move right <c>&gt;</c> or move left <c>&lt;</c> instructions.
    /// </summary>
    /// <param name="Distance">The accumulative distance of the instructions.</param>
    public sealed record Move(int Distance) : Instruction;

    /// <summary>
    /// One or more increment <c>+</c> or decrement <c>-</c> instructions.
    /// </summary>
    /// <param name="Value">The accumulative value of the instructions.</param>
    public sealed record Arithmetic(int Value) : Instruction;

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

    /// <summary>
    /// An instruction which is the result of a parse error.
    /// </summary>
    public sealed record Error : Instruction;
}
