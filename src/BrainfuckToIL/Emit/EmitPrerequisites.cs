﻿using System.Reflection.Metadata;
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
        
        var systemObject = metadata.GetType(
            corelib,
            "System",
            "Object");
        
        var systemByte = metadata.GetType(
            corelib,
            "System",
            "Byte");

        var systemConsole = metadata.GetType(
            corelib,
            "System",
            "Console");

        var systemConsoleKeyInfo = metadata.GetType(
            corelib,
            "System",
            "ConsoleKeyInfo");
        
        // Create the signature for a parameterless constructor.
        var (parameterlessCtor, systemObjectCtor) = metadata.GetMethod(
            name: ".ctor",
            containingType: systemObject,
            isInstanceMethod: true,
            signature: sig => sig
                .Parameters(0,
                    ret => ret.Void(),
                    _ => { }));

        var (_, systemConsoleWriteChar) = metadata.GetMethod(
            name: "Write",
            containingType: systemConsole,
            isInstanceMethod: false,
            signature: sig => sig
                .Parameters(1,
                    ret => ret.Void(),
                    parameters => parameters.AddParameter().Type().Char()));

        var (_, systemConsoleWriteLine) = metadata.GetMethod(
            name: "WriteLine",
            containingType: systemConsole,
            isInstanceMethod: false,
            signature: sig => sig
                .Parameters(0,
                    ret => ret.Void(),
                    _ => {}));

        var (_, systemConsoleReadKeyBool) = metadata.GetMethod(
            name: "ReadKey",
            containingType: systemConsole,
            isInstanceMethod: false,
            signature: sig => sig
                .Parameters(0,
                    ret => ret.Type().Type(
                        systemConsoleKeyInfo,
                        isValueType: true),
                    parameters => parameters.AddParameter().Type().Boolean()));

        return new()
        {
            Corelib = corelib,
            SystemObject = systemObject,
            SystemByte = systemByte,
            ParameterlessCtor = parameterlessCtor,
            SystemObjectCtor = systemObjectCtor,
            SystemConsoleWriteChar = systemConsoleWriteChar,
            SystemConsoleWriteLine = systemConsoleWriteLine,
            SystemConsoleReadKeyBool = systemConsoleReadKeyBool,
        };
    }
}
