using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BrainfuckToIL;

internal sealed class InstructionEmitter
{
    public readonly record struct Types(
        TypeReferenceHandle Byte);

    private readonly IReadOnlyList<Instruction> instructions;
    private readonly MetadataBuilder metadata;
    private readonly InstructionEncoder il;
    private readonly Types types;

    private InstructionEmitter(
        IReadOnlyList<Instruction> instructions,
        MetadataBuilder metadata,
        InstructionEncoder il,
        Types types)
    {
        this.instructions = instructions;
        this.metadata = metadata;
        this.il = il;
        this.types = types;
    }

    public static void Emit(
        IReadOnlyList<Instruction> instructions,
        MetadataBuilder metadata,
        InstructionEncoder il,
        Types types)
    {
        var emitter = new InstructionEmitter(instructions, metadata, il, types);
        emitter.Emit();
    }

    private void Emit()
    {
        il.OpCode(ILOpCode.Nop);
        il.OpCode(ILOpCode.Ret);
    }
}
