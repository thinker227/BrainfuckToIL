using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BrainfuckToIL.Emit;

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
    /// Type reference to <see cref="Byte"/>.
    /// </summary>
    public required TypeReferenceHandle SystemByte { get; init; }
    
    /// <summary>
    /// Handle to the method signature for a parameterless constructor.
    /// </summary>
    public required BlobHandle ParameterlessCtor { get; init; }
    
    /// <summary>
    /// Member reference to <see cref="object()"/>.
    /// </summary>
    public required MemberReferenceHandle SystemObjectCtor { get; init; }

    /// <summary>
    /// Member reference to <see cref="Console.WriteLine(Char)"/>.
    /// </summary>
    public required MemberReferenceHandle SystemConsoleWriteChar { get; init; }
    
    /// <summary>
    /// Member reference to <see cref="Console.ReadKey(bool)"/>.
    /// </summary>
    public required MemberReferenceHandle SystemConsoleReadKeyBool { get; init; }
    
    /// <summary>
    /// Member reference to <see cref="Console.WriteLine()"/>.
    /// </summary>
    public required MemberReferenceHandle SystemConsoleWriteLine { get; init; }
    
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
        
        var systemByte = metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Byte"));

        var systemConsole = metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Console"));

        var systemConsoleKeyInfo = metadata.AddTypeReference(
            corelib,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("ConsoleKeyInfo"));
        
        // Create the signature for a parameterless constructor.
        var voidNoArgsSignature = new BlobBuilder();
        new BlobEncoder(voidNoArgsSignature)
            .MethodSignature(isInstanceMethod: true)
            .Parameters(0, 
                ret => ret.Void(),
                _ => {});

        var systemObjectCtor = metadata.AddMemberReference(
            systemObject,
            metadata.GetOrAddString(".ctor"),
            metadata.GetOrAddBlob(voidNoArgsSignature));

        var systemConsoleWriteCharSignature = new BlobBuilder();
        new BlobEncoder(systemConsoleWriteCharSignature)
            .MethodSignature(isInstanceMethod: false)
            .Parameters(1,
                ret => ret.Void(),
                parameters => parameters.AddParameter().Type().Char());

        var systemConsoleWriteChar = metadata.AddMemberReference(
            systemConsole,
            metadata.GetOrAddString("Write"),
            metadata.GetOrAddBlob(systemConsoleWriteCharSignature));

        var systemConsoleWriteLineSignature = new BlobBuilder();
        new BlobEncoder(systemConsoleWriteLineSignature)
            .MethodSignature(isInstanceMethod: false)
            .Parameters(0,
                ret => ret.Void(),
                _ => {});

        var systemConsoleWriteLine = metadata.AddMemberReference(
            systemConsole,
            metadata.GetOrAddString("WriteLine"),
            metadata.GetOrAddBlob(systemConsoleWriteCharSignature));

        var systemConsoleReadKeyBoolSignature = new BlobBuilder();
        new BlobEncoder(systemConsoleReadKeyBoolSignature)
            .MethodSignature(isInstanceMethod: false)
            .Parameters(0,
                ret => ret.Type().Type(
                    systemConsoleKeyInfo,
                    isValueType: true),
                parameters => parameters.AddParameter().Type().Boolean());

        var systemConsoleReadKeyBool = metadata.AddMemberReference(
            systemConsoleKeyInfo,
            metadata.GetOrAddString("ReadKey"),
            metadata.GetOrAddBlob(systemConsoleReadKeyBoolSignature));

        return new()
        {
            Corelib = corelib,
            SystemObject = systemObject,
            SystemByte = systemByte,
            ParameterlessCtor = metadata.GetOrAddBlob(voidNoArgsSignature),
            SystemObjectCtor = systemObjectCtor,
            SystemConsoleWriteChar = systemConsoleWriteChar,
            SystemConsoleWriteLine = systemConsoleWriteLine,
            SystemConsoleReadKeyBool = systemConsoleReadKeyBool,
        };
    }
}
