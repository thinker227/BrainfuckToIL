using System.Collections.Immutable;

namespace BrainfuckToIL;

public abstract record Instruction
{
    public sealed record MoveRight : Instruction;

    public sealed record MoveLeft : Instruction;

    public sealed record Increment : Instruction;

    public sealed record Decrement : Instruction;

    public sealed record Output : Instruction;

    public sealed record Input : Instruction;

    public sealed record Loop(ImmutableArray<Instruction> Instructions) : Instruction;
}
