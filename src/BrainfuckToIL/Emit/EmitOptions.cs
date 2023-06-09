﻿namespace BrainfuckToIL.Emit;

/// <summary>
/// Options for an <see cref="Emitter"/>.
/// </summary>
public readonly struct EmitOptions
{
    /// <summary>
    /// The kind of output to emit. Default is <see cref="BrainfuckToIL.Emit.OutputKind.Dll"/>.
    /// </summary>
    public OutputKind OutputKind { get; init; } = OutputKind.Dll;

    /// <summary>
    /// The method of displaying user input when encountering a <c>,</c> instruction.
    /// Default is <see cref="BrainfuckToIL.Emit.InputFormat.Newline"/>.
    /// </summary>
    public InputFormat InputFormat { get; init; } = InputFormat.Newline;

    /// <summary>
    /// Whether to wrap around the memory if the pointer goes outside the bounds of the memory.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool WrapMemory { get; init; } = true;

    /// <summary>
    /// The size of the memory.
    /// Default is 30,000.
    /// </summary>
    public int MemorySize { get; init; } = 30_000;
    
    /// <summary>
    /// The mode to use for reading user input.
    /// </summary>
    public InputMode InputMode { get; init; } = InputMode.Key;
    
    /// <summary>
    /// The name of the emitted assembly.
    /// </summary>
    public required string AssemblyName { get; init; }

    /// <summary>
    /// The name of the type to emit containing the entry point method.
    /// Default is <c>$&lt;Program&gt;</c>.
    /// </summary>
    public string TypeName { get; init; } = "$<Program>";

    /// <summary>
    /// The name of the entry point method.
    /// Default is <c>$&lt;Main&gt;</c>.
    /// </summary>
    public string MethodName { get; init; } = "$<Main>";
    
    /// <summary>
    /// The version ID of the module. Good luck figuring out what that is.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/> then a random guid will be generated.
    /// </remarks>
    public Guid? ModuleVersionId { get; init; }

    /// <summary>
    /// The version of the assembly. Default is 1.0.0.0.
    /// </summary>
    public Version AssemblyVersion { get; init; } = new Version(1, 0, 0, 0);
    
    public EmitOptions() {}
}
