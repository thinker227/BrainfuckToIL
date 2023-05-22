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
    private readonly MethodBodyStreamEncoder methodBodyStream;
    private readonly MetadataBuilder metadata;
    private readonly EmitPrerequisites prerequisites;
    private readonly EmitOptions options;
    private readonly Guid guid;

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

        var prerequisites = EmitPrerequisites.Create(metadata);

        var emitter = new Emitter(instructions, ilBuilder, metadata, prerequisites, options);
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
        CreateModuleAndAssembly();

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
            prerequisites.SystemObject,
            metadata.GetOrAddString(".ctor"),
            parameterlessCtorBlobIndex);

        // Create the signature for the main method.
        var mainSignature = new BlobBuilder();
        new BlobEncoder(mainSignature)
            .MethodSignature()
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        
        // Get the body for .ctor.
        var ctorBodyOffset = EmitProgramCtor(objectCtorMember);
        // Get the body for the entry point method.
        var mainBodyOffset = EmitMain();

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
            baseType: prerequisites.SystemObject,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            mainMethod);

        return mainMethod;
    }

    private void CreateModuleAndAssembly()
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
    }
    
    private string GetModuleName() => options.AssemblyName + options.OutputKind switch
    {
        OutputKind.Executable => ".exe",
        OutputKind.Dll => ".dll",
        _ => throw new UnreachableException()
    };

    private void CreateCtor()
    {
        // Create signature.
        var signature = new BlobBuilder();
        new BlobEncoder(signature)
            .MethodSignature(isInstanceMethod: true)
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        var signatureIndex = metadata.GetOrAddBlob(signature);
        
        // Get a member reference to System.Object..ctor.
        var objectCtorMember = metadata.AddMemberReference(
            prerequisites.SystemObject,
            metadata.GetOrAddString(".ctor"),
            signatureIndex);
        
        var ctorBodyOffset = EmitProgramCtor(objectCtorMember);
    }

    private int EmitProgramCtor(MemberReferenceHandle objectCtorMember)
    {
        var codeBuilder = new BlobBuilder();
        var il = new InstructionEncoder(codeBuilder);
        
        il.LoadArgument(0);
        il.Call(objectCtorMember);
        il.OpCode(ILOpCode.Ret);

        var ctorBodyOffset = methodBodyStream.AddMethodBody(il);

        return ctorBodyOffset;
    }

    private int EmitMain()
    {
        var codeBuilder = new BlobBuilder();
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
            prerequisites.Corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Byte")));
}
