using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace BrainfuckToIL;

// This entire thing heavily references
// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder

/// <summary>
/// Emits IL.
/// </summary>
public sealed class Emitter
{
    private readonly IReadOnlyList<Instruction> instructions;
    private readonly BlobBuilder ilBuilder;
    private readonly MetadataBuilder metadata;
    private readonly AssemblyReferenceHandle corelib;
    private readonly EmitOptions options;
    private readonly Guid guid;

    private Emitter(IReadOnlyList<Instruction> instructions,
        BlobBuilder ilBuilder,
        MetadataBuilder metadata,
        AssemblyReferenceHandle corelib,
        EmitOptions options)
    {
        this.instructions = instructions;
        this.ilBuilder = ilBuilder;
        this.metadata = metadata;
        this.corelib = corelib;
        this.options = options;
        guid = Guid.NewGuid();
    }

    /// <summary>
    /// Emits a list of instructions into a stream as IL.
    /// </summary>
    /// <param name="instructions">The instructions to emit.</param>
    /// <param name="stream">The stream to emit the instruction into.</param>
    /// <param name="options">The options to use for emission.</param>
    public static void Emit(
        IReadOnlyList<Instruction> instructions,
        Stream stream,
        EmitOptions options)
    {
        var ilBuilder = new BlobBuilder();
        var metadata = new MetadataBuilder();
        
        var corelib = metadata.AddAssemblyReference(
            name: metadata.GetOrAddString("mscorlib"),
            version: new(4, 0, 0, 0),
            culture: default,
            publicKeyOrToken: metadata.GetOrAddBlob(
                // Magic key identifying mscorlib.
                new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }),
            flags: default,
            hashValue: default);

        var emitter = new Emitter(instructions, ilBuilder, metadata, corelib, options);
        var entryPoint = emitter.EmitEntryPoint();

        emitter.WritePeImage(stream, entryPoint);
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

        var peBuilder = new ManagedPEBuilder(
            peHeaderBuilder,
            new(metadata),
            ilBuilder,
            entryPoint: entryPoint,
            flags: CorFlags.ILOnly);

        // Serializes the PE builder into a blob.
        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        
        peBlob.WriteContentTo(stream);
    }

    private MethodDefinitionHandle EmitEntryPoint()
    {
        // Create main module.
        metadata.AddModule(
            generation: 0,
            moduleName: metadata.GetOrAddString(GetModuleName()),
            // Module version ID.
            mvid: metadata.GetOrAddGuid(guid),
            encId: default,
            encBaseId: default);

        // Create main assembly.
        metadata.AddAssembly(
            name: metadata.GetOrAddString(options.AssemblyName),
            version: new(1, 0, 0, 0),
            culture: default,
            // I hope you won't ever use this assembly as a library lmao.
            publicKey: default,
            flags: 0,
            hashAlgorithm: AssemblyHashAlgorithm.None);

        // Get a type reference to System.Object.
        var systemObject = metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Object"));

        // Create the signature for the parameterless constructor.
        var parameterlessCtorSignature = new BlobBuilder();
        new BlobEncoder(parameterlessCtorSignature)
            .MethodSignature(isInstanceMethod: true)
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        var parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

        // Get a member reference to System.Object..ctor.
        var objectCtorMember = metadata.AddMemberReference(
            systemObject,
            metadata.GetOrAddString(".ctor"),
            parameterlessCtorBlobIndex);

        // Create the signature for the main method.
        var mainSignature = new BlobBuilder();
        new BlobEncoder(mainSignature)
            .MethodSignature()
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        
        var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);
        var codeBuilder = new BlobBuilder();

        // Get the body for .ctor.
        var ctorBodyOffset = EmitProgramCtor(codeBuilder, methodBodyStream, objectCtorMember);
        // Get the body for the entry point method.
        var mainBodyOffset = EmitMain(codeBuilder, methodBodyStream);

        // Create the entry point method.
        var mainMethod = metadata.AddMethodDefinition(
            attributes:
                MethodAttributes.Public |
                MethodAttributes.Static |
                MethodAttributes.HideBySig,
            implAttributes: MethodImplAttributes.IL,
            name: metadata.GetOrAddString(options.MethodName),
            signature: metadata.GetOrAddBlob(mainSignature),
            bodyOffset: mainBodyOffset,
            parameterList: default);

        // Create the .ctor method.
        metadata.AddMethodDefinition(
            attributes: MethodAttributes.Public |
                        MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName |
                        MethodAttributes.RTSpecialName,
            implAttributes: MethodImplAttributes.IL,
            name: metadata.GetOrAddString(".ctor"),
            signature: parameterlessCtorBlobIndex,
            bodyOffset: ctorBodyOffset,
            parameterList: default);

        // Create the <Module> type.
        metadata.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: metadata.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // No idea why the main method has to be specified in both <Module> and the program type.
            methodList: mainMethod);

        // Create the type containing the entry point method.
        metadata.AddTypeDefinition(
            attributes: TypeAttributes.Class |
                        TypeAttributes.Public |
                        TypeAttributes.AutoLayout |
                        TypeAttributes.BeforeFieldInit,
            @namespace: default,
            metadata.GetOrAddString(options.TypeName),
            baseType: systemObject,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            mainMethod);

        return mainMethod;
    }

    private string GetModuleName() => options.AssemblyName + options.OutputKind switch
    {
        OutputKind.Executable => ".exe",
        OutputKind.Dll => ".dll",
        _ => throw new UnreachableException()
    };

    private static int EmitProgramCtor(
        BlobBuilder codeBuilder,
        MethodBodyStreamEncoder methodBodyStream,
        MemberReferenceHandle objectCtorMember)
    {
        var il = new InstructionEncoder(codeBuilder);
        
        il.LoadArgument(0);
        il.Call(objectCtorMember);
        il.OpCode(ILOpCode.Ret);

        var ctorBodyOffset = methodBodyStream.AddMethodBody(il);
        codeBuilder.Clear();

        return ctorBodyOffset;
    }

    private int EmitMain(
        BlobBuilder codeBuilder,
        MethodBodyStreamEncoder methodBodyStream)
    {
        var flowBuilder = new ControlFlowBuilder();
        var il = new InstructionEncoder(codeBuilder, flowBuilder);

        var localsBuilder = new BlobBuilder();
        var locals = new BlobEncoder(localsBuilder).LocalVariableSignature(2);

        // Let the magic happen!
        InstructionEmitter.Emit(instructions, metadata, il, locals, GetTypes());

        var mainBodyOffset = methodBodyStream.AddMethodBody(
            il, localVariablesSignature: metadata.AddStandaloneSignature(metadata.GetOrAddBlob(localsBuilder)));
        codeBuilder.Clear();

        return mainBodyOffset;
    }

    private InstructionEmitter.Types GetTypes() => new(
        metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Byte")));
}
