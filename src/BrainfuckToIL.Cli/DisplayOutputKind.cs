namespace BrainfuckToIL.Cli;

// This is just to provide better names for the output kind CLI option.
internal enum DisplayOutputKind
{
    Exe,
    Dll
}

internal static class OutputKindExtensions
{
    public static string GetFileExtension(this DisplayOutputKind kind) => kind switch
    {
        DisplayOutputKind.Exe => ".exe",
        DisplayOutputKind.Dll => ".dll",
        _ => throw new UnreachableException()
    };

    public static OutputKind ToCoreOutputKind(this DisplayOutputKind kind) => kind switch
    {
        DisplayOutputKind.Exe => OutputKind.Executable,
        DisplayOutputKind.Dll => OutputKind.Dll,
        _ => throw new UnreachableException()
    };
}
