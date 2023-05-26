using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace BrainfuckToIL.Emit;

// This entire thing heavily references
// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder

/// <summary>
/// Emits IL.
/// </summary>
public sealed class Emitter
{
    private readonly IReadOnlyList<Instruction> instructions;
    private readonly MetadataBuilder metadata;
    private readonly BlobBuilder ilBuilder;
    private readonly MethodBodyStreamEncoder methodBodyStream;
    private readonly Guid moduleVersionId;
    private readonly EmitPrerequisites prerequisites;
    private readonly EmitOptions options;

    private Emitter(IReadOnlyList<Instruction> instructions,
        BlobBuilder ilBuilder,
        MetadataBuilder metadata,
        EmitPrerequisites prerequisites,
        EmitOptions options)
    {
        this.instructions = instructions;
        this.ilBuilder = ilBuilder;
        methodBodyStream = new(ilBuilder);
        this.metadata = metadata;
        this.prerequisites = prerequisites;
        this.options = options;
        moduleVersionId = options.ModuleVersionId ?? Guid.NewGuid();
    }

    /// <summary>
    /// Emits a list of instructions as IL into a stream.
    /// </summary>
    /// <param name="instructions">The instructions to emit.</param>
    /// <param name="stream">The stream to emit the instructions into.</param>
    /// <param name="options">The options to use for emission.</param>
    public static void Emit(
        IReadOnlyList<Instruction> instructions,
        Stream stream,
        EmitOptions options)
    {
        var ilBuilder = new BlobBuilder();
        var metadata = new MetadataBuilder();

        var prerequisites = EmitPrerequisites.Create(metadata);

        var emitter = new Emitter(instructions, ilBuilder, metadata, prerequisites, options);
        var entryPoint = emitter.EmitEntryPoint();

        emitter.WritePeImage(stream, entryPoint);
    }

    /// <summary>
    /// Emits a list of instructions as a byte array of IL instructions.
    /// </summary>
    /// <param name="instructions">The instructions to emit.</param>
    /// <param name="options">The options to use for emission.</param>
    /// <returns>A byte of IL instructions emitted using <paramref name="instructions"/>.</returns>
    public static byte[] EmitAsBytes(
        IReadOnlyList<Instruction> instructions,
        EmitOptions options)
    {
        var stream = new MemoryStream();
        Emit(instructions, stream, options);
        return stream.ToArray();
    }

    /// <summary>
    /// Emits a list of instructions as an assembly.
    /// </summary>
    /// <param name="instructions">The instructions to emit.</param>
    /// <param name="options">The options to use for emission.</param>
    /// <returns>An assembly constructed from the IL emitted using <paramref name="instructions"/>.</returns>
    public static Assembly EmitAsAssembly(
        IReadOnlyList<Instruction> instructions,
        EmitOptions options)
    {
        var bytes = EmitAsBytes(instructions, options);
        return Assembly.Load(bytes);
    }

    /// <summary>
    /// Emits a list of instructions as a <see cref="BrainfuckMethod"/> delegate.
    /// </summary>
    /// <param name="instructions">The instructions to emit.</param>
    /// <param name="options">The options to use for emission.</param>
    /// <returns>A <see cref="BrainfuckMethod"/> delegate which calls the entry-point of an assembly constructed
    /// from the IL emitted using <paramref name="instructions"/>.</returns>
    public static BrainfuckMethod EmitAsDelegate(
        IReadOnlyList<Instruction> instructions,
        EmitOptions options)
    {
        var assembly = EmitAsAssembly(instructions, options);
        var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException(
            $"Assembly {assembly.FullName} does not have an entry point.");
        return entryPoint.CreateDelegate<BrainfuckMethod>();
    }

    private void WritePeImage(
        Stream stream,
        MethodDefinitionHandle entryPoint)
    {
        var characteristics = options.OutputKind switch
        {
            OutputKind.Executable => Characteristics.ExecutableImage,
            OutputKind.Dll => Characteristics.Dll,
            _ => throw new UnreachableException()
        };
        
        var peHeaderBuilder = new PEHeaderBuilder(
            imageCharacteristics: characteristics);

        // The PE builder allows for a deterministic ID provider, but I doubt that is necessary here.
        var peBuilder = new ManagedPEBuilder(
            header: peHeaderBuilder,
            metadataRootBuilder: new(metadata),
            ilStream: ilBuilder,
            entryPoint: entryPoint,
            flags: CorFlags.ILOnly);

        // Serialize the PE builder into a blob.
        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        
        peBlob.WriteContentTo(stream);
    }

    private MethodDefinitionHandle EmitEntryPoint()
    {
        CreateModuleAndAssembly();

        CreateCtor();
        // This is order-dependent. why
        var read = CreateRead();
        var main = CreateMain(read);
        
        // Read is the first method emitted in the type(s).
        CreateModuleType(read);
        CreateProgramType(read);

        return main;
    }

    private void CreateModuleAndAssembly()
    {
        // Create main module.
        metadata.AddModule(
            generation: 0,
            moduleName: metadata.GetOrAddString(GetModuleName()),
            // Module version ID.
            mvid: metadata.GetOrAddGuid(moduleVersionId),
            encId: default,
            encBaseId: default);

        // Create main assembly.
        metadata.AddAssembly(
            name: metadata.GetOrAddString(options.AssemblyName),
            version: options.AssemblyVersion,
            culture: default,
            // I hope you won't ever use this assembly as a library lmao.
            publicKey: default,
            flags: 0,
            hashAlgorithm: AssemblyHashAlgorithm.None);
    }
    
    private string GetModuleName() => options.AssemblyName + options.OutputKind switch
    {
        OutputKind.Executable => ".exe",
        OutputKind.Dll => ".dll",
        _ => throw new UnreachableException()
    };

    private void CreateCtor()
    {
        var bodyOffset = EmitProgramCtor();
        
        // Create the .ctor method.
        metadata.AddMethodDefinition(
            attributes: MethodAttributes.Public |
                        MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName |
                        MethodAttributes.RTSpecialName,
            implAttributes: MethodImplAttributes.IL,
            name: metadata.GetOrAddString(".ctor"),
            signature: prerequisites.ParameterlessCtor,
            bodyOffset: bodyOffset,
            parameterList: default);
        
        // The ctor method definition handle is never required anywhere according to the docs,
        // so the parameterless ctor likely is only required to be present in the metadata
        // to be used as the base case for all types without other constructors.
    }

    private int EmitProgramCtor()
    {
        var codeBuilder = new BlobBuilder();
        var il = new InstructionEncoder(codeBuilder);
        
        il.LoadArgument(0);
        il.Call(prerequisites.SystemObjectCtor);
        il.OpCode(ILOpCode.Ret);

        var bodyOffset = methodBodyStream.AddMethodBody(il);

        return bodyOffset;
    }

    private MethodDefinitionHandle CreateRead()
    {
        var bodyOffset = EmitRead();

        var signature = metadata.CreateSignature(
            isInstanceMethod: false,
            signature: sig => sig
                .Parameters(0,
                    ret => ret.Type().Byte(),
                    _ => {}));

        var method = metadata.AddMethodDefinition(
            attributes: MethodAttributes.Private |
                        MethodAttributes.Static |
                        MethodAttributes.HideBySig,
            implAttributes: MethodImplAttributes.IL,
            name: metadata.GetOrAddString("Read"),
            signature: signature,
            bodyOffset: bodyOffset,
            parameterList: default);

        return method;
    }

    private int EmitRead()
    {
        var codeBuilder = new BlobBuilder();
        var flowBuilder = new ControlFlowBuilder();
        var il = new InstructionEncoder(codeBuilder, flowBuilder);

        var localsBuilder = new BlobBuilder();
        var locals = new BlobEncoder(localsBuilder).LocalVariableSignature(1);

        switch (options.InputMode)
        {
        case InputMode.Key:
            EmitReadKey(il, locals);
            break;
        
        case InputMode.Stream:
            EmitReadStream(il);
            break;
        
        default:
            throw new UnreachableException();
        }
        
        var bodyOffset = methodBodyStream.AddMethodBody(
            il, localVariablesSignature: metadata.AddStandaloneSignature(metadata.GetOrAddBlob(localsBuilder)));

        return bodyOffset;
    }

    private void EmitReadKey(InstructionEncoder il, LocalVariablesEncoder locals)
    {
        locals.AddVariable()
            .Type().Type(prerequisites.SystemConsoleKeyInfo, true);
        const int localSlot = 0;
        
        /*
        // Reference source:
        var info = Console.ReadKey(v);
        if (info.Key == ConsoleKey.Enter) return 0;
        return (byte)info.KeyChar;
        */
        
        // Yes this was painful to write, why'd you ask?
        
        // Call System.Console.ReadKey(true) and store it into a local variable.
        il.LoadConstantI4(1);
        il.Call(prerequisites.SystemConsoleReadKeyBool);
        il.StoreLocal(localSlot);

        // Define a label and branch to it if the value of info.Key is not ConsoleKey.Enter.
        il.LoadLocalAddress(localSlot);
        il.Call(prerequisites.SystemConsoleKeyInfoKeyGet);
        il.LoadConstantI4((int)ConsoleKey.Enter);
        var label = il.DefineLabel();
        il.Branch(ILOpCode.Bne_un_s, label);
        
        // Return 0.
        il.LoadConstantI4(0);
        il.OpCode(ILOpCode.Ret);
        
        il.MarkLabel(label);
        
        // Get info.KeyChar.
        il.LoadLocalAddress(localSlot);
        il.Call(prerequisites.SystemConsoleKeyInfoKeyCharGet);

        // If the input should not be hidden, write it.
        if (options.InputFormat != InputFormat.Hidden)
        {
            il.OpCode(ILOpCode.Dup);
            il.Call(prerequisites.SystemConsoleWriteChar);
        }

        // If a newline should be appended, do that.
        if (options.InputFormat == InputFormat.Newline)
            il.Call(prerequisites.SystemConsoleWriteLine);

        il.OpCode(ILOpCode.Conv_u1);
        il.OpCode(ILOpCode.Ret);
    }

    private void EmitReadStream(InstructionEncoder il)
    {
        il.Call(prerequisites.SystemConsoleRead);
        il.OpCode(ILOpCode.Conv_u1);
        il.OpCode(ILOpCode.Ret);
    }
    
    private MethodDefinitionHandle CreateMain(MethodDefinitionHandle read)
    {
        // Create the signature for the main method.
        var signature = new BlobBuilder();
        new BlobEncoder(signature)
            .MethodSignature()
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        
        // Get the body for the entry point method.
        var bodyOffset = EmitMain(read);

        // Create the entry point method.
        var method = metadata.AddMethodDefinition(
            attributes:
            MethodAttributes.Public |
            MethodAttributes.Static |
            MethodAttributes.HideBySig,
            implAttributes: MethodImplAttributes.IL,
            name: metadata.GetOrAddString(options.MethodName),
            signature: metadata.GetOrAddBlob(signature),
            bodyOffset: bodyOffset,
            parameterList: default);

        return method;
    }

    private int EmitMain(MethodDefinitionHandle read)
    {
        var codeBuilder = new BlobBuilder();
        var flowBuilder = new ControlFlowBuilder();
        var il = new InstructionEncoder(codeBuilder, flowBuilder);

        var localsBuilder = new BlobBuilder();
        var locals = new BlobEncoder(localsBuilder).LocalVariableSignature(2);

        // Let the magic happen!
        InstructionEmitter.Emit(instructions, options, metadata, il, locals, prerequisites, read);

        var mainBodyOffset = methodBodyStream.AddMethodBody(
            il, localVariablesSignature: metadata.AddStandaloneSignature(metadata.GetOrAddBlob(localsBuilder)));
        codeBuilder.Clear();

        return mainBodyOffset;
    }

    private void CreateModuleType(MethodDefinitionHandle methodList) =>
        metadata.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: metadata.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // This specifies that the method list is empty because this method list
            // should refer to the method list of the next type. 
            methodList: methodList);

    private void CreateProgramType(MethodDefinitionHandle methodList) =>
        metadata.AddTypeDefinition(
            attributes: TypeAttributes.Class |
                        TypeAttributes.Public |
                        TypeAttributes.AutoLayout |
                        TypeAttributes.BeforeFieldInit,
            @namespace: default,
            metadata.GetOrAddString(options.TypeName),
            baseType: prerequisites.SystemObject,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            methodList);
}
