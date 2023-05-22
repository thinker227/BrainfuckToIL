using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BrainfuckToIL;

/// <summary>
/// Prerequisite assembly/type/member references for <see cref="Emitter"/> to function.
/// </summary>
internal readonly struct EmitPrerequisites
{
    /// <summary>
    /// Assembly reference to mscorlib. 
    /// </summary>
    public required AssemblyReferenceHandle Corelib { get; init; }
    
    /// <summary>
    /// Type reference to <see cref="object"/>.
    /// </summary>
    public required TypeReferenceHandle SystemObject { get; init; }
    
    /// <summary>
    /// Handle to the method signature for a parameterless constructor.
    /// </summary>
    public required BlobHandle ParameterlessCtor { get; init; }
    
    /// <summary>
    /// Member reference to <see cref="object()"/>.
    /// </summary>
    public required MemberReferenceHandle SystemObjectCtor { get; init; }

    public static EmitPrerequisites Create(MetadataBuilder metadata)
    {
        var corelib = metadata.AddAssemblyReference(
            name: metadata.GetOrAddString("mscorlib"),
            version: new(4, 0, 0, 0),
            culture: default,
            publicKeyOrToken: metadata.GetOrAddBlob(
                // Magic key identifying mscorlib.
                new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }),
            flags: default,
            hashValue: default);
        
        var systemObject = metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Object"));
        
        // Create the signature for a parameterless constructor.
        var parameterlessCtorSignature = new BlobBuilder();
        new BlobEncoder(parameterlessCtorSignature)
            .MethodSignature(isInstanceMethod: true)
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});
        var parameterlessCtor = metadata.GetOrAddBlob(parameterlessCtorSignature);

        var systemObjectCtor = metadata.AddMemberReference(
            systemObject,
            metadata.GetOrAddString(".ctor"),
            parameterlessCtor);

        return new()
        {
            Corelib = corelib,
            SystemObject = systemObject,
            ParameterlessCtor = parameterlessCtor,
            SystemObjectCtor = systemObjectCtor
        };
    }
}
