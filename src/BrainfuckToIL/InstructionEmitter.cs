using System.Collections.Immutable;
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
    private readonly LocalVariablesEncoder locals;
    private readonly Types types;

    private const int MemorySize = 30_000;
    private const int MemorySlot = 0;
    private const int DataPointerSlot = 1;

    private InstructionEmitter(
        IReadOnlyList<Instruction> instructions,
        MetadataBuilder metadata,
        InstructionEncoder il,
        LocalVariablesEncoder locals,
        Types types)
    {
        this.instructions = instructions;
        this.metadata = metadata;
        this.il = il;
        this.locals = locals;
        this.types = types;
    }

    public static void Emit(
        IReadOnlyList<Instruction> instructions,
        MetadataBuilder metadata,
        InstructionEncoder il,
        LocalVariablesEncoder locals,
        Types types)
    {
        var emitter = new InstructionEmitter(instructions, metadata, il, locals, types);
        
        emitter.EmitHeader();
        emitter.Emit();
    }

    /// <summary>
    /// Emits the header of the main method which doesn't vary depending on instructions.
    /// This includes allocating the memory array and setting up locals.
    /// </summary>
    private void EmitHeader()
    {
        // Add a byte[] variable.
        locals.AddVariable().Type().Array(
            elementType => elementType.Byte(),
            shape => shape.Shape(
                1,
                // The array will always contain 30000 elements.
                ImmutableArray.Create(1),
                ImmutableArray.Create(MemorySize)));
        
        // Add an int variable.
        locals.AddVariable().Type().Int32();
        
        // Create an array of 30,000 bytes and store it into a local variable. 
        il.LoadConstantI4(MemorySize);
        il.OpCode(ILOpCode.Newarr);
        il.Token(types.Byte);
        il.StoreLocal(MemorySlot);

        // Initialize the data pointer local variable to 0.
        il.LoadConstantI4(0);
        il.StoreLocal(DataPointerSlot);
    }

    /// <summary>
    /// Emits the main part of the body based on <see cref="instructions"/>.
    /// </summary>
    private void Emit()
    {
        il.OpCode(ILOpCode.Nop);
        il.OpCode(ILOpCode.Ret);
    }
}
