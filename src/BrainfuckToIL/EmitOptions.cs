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
    
    public EmitOptions() {}
}

public enum OutputKind
{
    Executable,
    Dll
}
