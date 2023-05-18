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
        emitter.EmitInstructions();
        emitter.EmitFooter();
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
    /// Emits the footer of the method.
    /// </summary>
    private void EmitFooter() =>
        il.OpCode(ILOpCode.Ret);

    /// <summary>
    /// Emits the main part of the body based on <see cref="instructions"/>.
    /// </summary>
    private void EmitInstructions()
    {
        foreach (var instruction in instructions)
            EmitInstruction(instruction);
    }

    /// <summary>
    /// Emits a single instruction.
    /// </summary>
    /// <param name="instruction">The instruction to emit.</param>
    private void EmitInstruction(Instruction instruction)
    {
        switch (instruction)
        {
        case Instruction.Increment:
            EmitIncrement(IncrementKind.Increment);
            break;
            
        case Instruction.Decrement:
            EmitIncrement(IncrementKind.Decrement);
            break;
            
        case Instruction.MoveRight:
            EmitMove(MoveKind.Right);
            break;
            
        case Instruction.MoveLeft:
            EmitMove(MoveKind.Left);
            break;
            
        case Instruction.Input:
            EmitInput();
            break;
            
        case Instruction.Output:
            EmitOutput();
            break;
            
        case Instruction.Loop loop:
            EmitLoop(loop);
            break;
        }
    }
    
    private void EmitIncrement(IncrementKind kind)
    {
        // The majority of this code comes from looking at the IL of the bellow code on Sharplab
        // and checking the decompilation of the produced IL using ILSpy.
        /*
        var bytes = new byte[20];
        bytes[2] += 1;
        */
        
        // Load the memory array onto the stack.
        il.LoadLocal(MemorySlot);
        
        // Load the address of the element in the memory array at the index of the data pointer.
        il.LoadLocal(DataPointerSlot);
        il.OpCode(ILOpCode.Ldelema);
        il.Token(types.Byte);
        
        // Duplicate the the address, one for the read and one for the write.
        il.OpCode(ILOpCode.Dup);
        
        // Load the value at the address (?) as a byte onto the stack.
        // u1 is an unsigned 1-byte integer, i.e. a byte.
        il.OpCode(ILOpCode.Ldind_u1);
        
        // Add or subtract 1.
        il.LoadConstantI4(1);
        il.OpCode(kind switch
        {
            IncrementKind.Increment => ILOpCode.Add,
            IncrementKind.Decrement => ILOpCode.Sub,
            _ => throw new UnreachableException()
        });
        
        // Convert the result of the addition/subtraction into a byte.
        il.OpCode(ILOpCode.Conv_u1);
        
        // Store the result into the index of the memory array.
        // Idk why this is i1 and not u1 but there exists no Stind_u1 instruction.
        il.OpCode(ILOpCode.Stind_i1);
    }

    private void EmitMove(MoveKind kind)
    {
        // Load the data pointer onto the stack.
        il.LoadLocal(DataPointerSlot);
        
        // TODO: Bounds checking.
        
        // Add or subtract 1.
        il.LoadConstantI4(1);
        il.OpCode(kind switch
        {
            MoveKind.Right => ILOpCode.Add,
            MoveKind.Left => ILOpCode.Sub,
            _ => throw new UnreachableException()
        });
        
        // Store the result back into the data pointer.
        il.StoreLocal(DataPointerSlot);
    }

    private void EmitInput() => throw new NotImplementedException();

    private void EmitOutput() => throw new NotImplementedException();

    private void EmitLoop(Instruction.Loop loop) => throw new NotImplementedException();
}

internal enum IncrementKind
{
    Increment,
    Decrement
}

internal enum MoveKind
{
    Right,
    Left
}
