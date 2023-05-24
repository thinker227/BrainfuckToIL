namespace BrainfuckToIL;

/// <summary>
/// Extensions for <see cref="Instruction"/>.
/// </summary>
public static class InstructionExtensions
{
    /// <summary>
    /// Flattens a sequence of instructions, producing a flat sequence of instructions
    /// without nested instructions. 
    /// </summary>
    /// <param name="instructions">The instructions to flatten.</param>
    public static IEnumerable<Instruction> Flatten(this IEnumerable<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;

            if (instruction is not Instruction.Loop loop) continue;

            foreach (var loopInstruction in loop.Instructions.Flatten()) yield return loopInstruction;
        }
    }
}
