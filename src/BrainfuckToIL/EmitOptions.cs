namespace BrainfuckToIL;

/// <summary>
/// Options for an <see cref="Emitter"/>.
/// </summary>
public readonly struct EmitOptions
{
    /// <summary>
    /// The kind of output to emit. Default is <see cref="BrainfuckToIL.OutputKind.Dll"/>.
    /// </summary>
    public OutputKind OutputKind { get; init; } = OutputKind.Dll;
    
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
    
    public EmitOptions() {}
}

public enum OutputKind
{
    Executable,
    Dll
}
