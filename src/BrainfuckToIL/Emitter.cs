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
        var main = CreateMain();
        
        CreateModuleType(main);
        CreateProgramType(main);

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

    private MethodDefinitionHandle CreateMain()
    {
        // Create the signature for the main method.
        var signature = new BlobBuilder();
        new BlobEncoder(signature)
            .MethodSignature()
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        
        // Get the body for the entry point method.
        var bodyOffset = EmitMain();

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

    private void CreateModuleType(MethodDefinitionHandle mainMethod) =>
        metadata.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            name: metadata.GetOrAddString("<Module>"),
            baseType: default,
            fieldList: MetadataTokens.FieldDefinitionHandle(1),
            // No idea why the main method has to be specified in both <Module> and the program type.
            methodList: mainMethod);

    private void CreateProgramType(MethodDefinitionHandle mainMethod) =>
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
}
