using Spectre.Console;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Compile
{
    private readonly IAnsiConsole console;

    public Compile(IAnsiConsole console) => this.console = console;

    public int Handle(FileInfo sourceFile, FileSystemInfo? destination, DisplayOutputKind outputKind)
    {
        var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputKind);
        var outputFileName = Path.GetFileNameWithoutExtension(outputFile.Name);
    
        console.MarkupLine($"[green]{sourceFile.FullName}[/] -> [green]{outputFile.FullName}[/]");

        var source = File.ReadAllText(sourceFile.FullName);
        var result = Parser.Parse(source);
        
        // TODO: Error reporting.

        using var outputStream = outputFile.OpenWrite();
        Emitter.Emit(result.Instructions, outputStream, new EmitOptions()
        {
            AssemblyName = outputFileName,
            OutputKind = outputKind.ToCoreOutputKind()
        });

        return 0;
    }
}
