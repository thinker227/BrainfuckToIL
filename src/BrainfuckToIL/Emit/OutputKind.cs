namespace BrainfuckToIL.Emit;

/// <summary>
/// Represents kinds of emit output.
/// </summary>
public enum OutputKind
{
    /// <summary>
    /// Emit an executable.
    /// </summary>
    Executable,
    /// <summary>
    /// Emit a DLL.
    /// </summary>
    Dll
}
