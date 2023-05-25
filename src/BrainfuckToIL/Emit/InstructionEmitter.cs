using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BrainfuckToIL.Emit;

internal sealed class InstructionEmitter
{
    private readonly IReadOnlyList<Instruction> instructions;
    private readonly EmitOptions options;
    private readonly MetadataBuilder metadata;
    private readonly InstructionEncoder il;
    private readonly LocalVariablesEncoder locals;
    private readonly EmitPrerequisites prerequisites;
    private readonly MethodDefinitionHandle readMethod;

    private const int MemorySlot = 0;
    private const int DataPointerSlot = 1;

    private InstructionEmitter(
        IReadOnlyList<Instruction> instructions,
        EmitOptions options,
        MetadataBuilder metadata,
        InstructionEncoder il,
        LocalVariablesEncoder locals,
        EmitPrerequisites prerequisites,
        MethodDefinitionHandle readMethod)
    {
        this.instructions = instructions;
        this.options = options;
        this.metadata = metadata;
        this.il = il;
        this.locals = locals;
        this.prerequisites = prerequisites;
        this.readMethod = readMethod;
    }

    public static void Emit(
        IReadOnlyList<Instruction> instructions,
        EmitOptions options,
        MetadataBuilder metadata,
        InstructionEncoder il,
        LocalVariablesEncoder locals,
        EmitPrerequisites prerequisites,
        MethodDefinitionHandle readMethod)
    {
        var emitter = new InstructionEmitter(
            instructions,
            options,
            metadata,
            il,
            locals,
            prerequisites,
            readMethod);
        
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
                // The array will always contain the same amount of elements.
                ImmutableArray.Create(1),
                ImmutableArray.Create(options.MemorySize)));
        
        // Add an int variable.
        locals.AddVariable().Type().Int32();
        
        // Create an array of bytes and store it into a local variable. 
        il.LoadConstantI4(options.MemorySize);
        il.OpCode(ILOpCode.Newarr);
        il.Token(prerequisites.SystemByte);
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
        case Instruction.Arithmetic arithmetic:
            EmitArithmetic(arithmetic.Value);
            break;
            
        case Instruction.Move move:
            EmitMove(move.Distance);
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
    
    private void EmitArithmetic(int value)
    {
        // The majority of this code comes from looking at the IL of the bellow code on Sharplab
        // and checking the decompilation of the produced IL using ILSpy.
        /*
        var bytes = new byte[20];
        bytes[2] += 1;
        */

        // No need to do anything if we're not supposed to add/subtract anything.
        if (value == 0) return;
        
        // Load the memory array onto the stack.
        il.LoadLocal(MemorySlot);
        
        // Load the address of the element in the memory array at the index of the data pointer.
        il.LoadLocal(DataPointerSlot);
        il.OpCode(ILOpCode.Ldelema);
        il.Token(prerequisites.SystemByte);
        
        // Duplicate the the address, one for the read and one for the write.
        il.OpCode(ILOpCode.Dup);
        
        // Load the value at the address (?) as a byte onto the stack.
        // u1 is an unsigned 1-byte integer, i.e. a byte.
        il.OpCode(ILOpCode.Ldind_u1);
        
        // Add or subtract.
        EmitAddOrSubtract(value);

        // Call rem if wrapping is enabled.
        if (options.WrapMemory)
        {
            il.LoadConstantI4(options.MemorySize);
            il.OpCode(ILOpCode.Rem);
        }
        
        // Convert the result of the addition/subtraction into a byte.
        il.OpCode(ILOpCode.Conv_u1);
        
        // Store the result into the index of the memory array.
        // Idk why this is i1 and not u1 but there exists no Stind_u1 instruction.
        il.OpCode(ILOpCode.Stind_i1);
    }

    private void EmitMove(int distance)
    {
        // No need to do anything if we're not supposed to move anywhere.
        if (distance == 0) return;
        
        // Load the data pointer onto the stack.
        il.LoadLocal(DataPointerSlot);
        
        // TODO: Bounds checking.
        
        // Add or subtract.
        EmitAddOrSubtract(distance);
        
        // Store the result back into the data pointer.
        il.StoreLocal(DataPointerSlot);
    }

    private void EmitInput()
    {
        il.LoadLocal(MemorySlot);
        il.LoadLocal(DataPointerSlot);

        il.Call(readMethod);
        
        // Store value into memory.
        il.OpCode(ILOpCode.Stelem_i1);
    }

    private void EmitOutput()
    {
        EmitReadCurrentMemory();
        
        // Converts the current value to a char.
        // Apparently a ushort is equivalent to a char.
        il.OpCode(ILOpCode.Conv_u2);
        
        il.Call(prerequisites.SystemConsoleWriteChar);
    }

    private void EmitLoop(Instruction.Loop loop)
    {
        // This is just a while(mem[ind] != 0) loop.
        
        // Note: Use Br and Brtrue here instead of Br_s and Brtrue_s
        // because loop offsets can easily exceed the bounds of a single byte. 
        
        // Unconditionally branch to the condition.
        var condition = il.DefineLabel();
        il.Branch(ILOpCode.Br, condition);
        
        // Define and mark the body label at the current location.
        var body = il.DefineLabel();
        il.MarkLabel(body);
        
        // Emit the body.
        foreach (var instruction in loop.Instructions)
            EmitInstruction(instruction);
        
        // Branch to the start of the body if the value at the current memory index is not 0.
        il.MarkLabel(condition);
        EmitReadCurrentMemory();
        il.Branch(ILOpCode.Brtrue, body);
    }

    /// <summary>
    /// Emits instructions to read the data at the index in memory specified by the data pointer.
    /// </summary>
    private void EmitReadCurrentMemory()
    {
        // Load the array and data pointer onto the stack.
        il.LoadLocal(MemorySlot);
        il.LoadLocal(DataPointerSlot);
        
        // Read the element at the index specified by the data pointer.
        il.OpCode(ILOpCode.Ldelem_u1);
    }

    /// <summary>
    /// Emits instructions for either adding or subtracting an absolute value.
    /// </summary>
    /// <param name="value">The value to add or subtract.</param>
    private void EmitAddOrSubtract(int value)
    {
        // My only justification for this method existing is that
        // using |value| and switching out the operation just looks neater
        // than always adding by a positive or negative amount lmao
        
        il.LoadConstantI4(Math.Abs(value));
        
        il.OpCode(value switch
        {
            > 0 => ILOpCode.Add,
            < 0 => ILOpCode.Sub,
            _ => throw new UnreachableException()
        });
    }
}
