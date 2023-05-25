using System.Collections.Immutable;
using Spectre.Console;
using BrainfuckToIL.Emit;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Compile
{
    private readonly IAnsiConsole console;

    public Compile(IAnsiConsole console) => this.console = console;

    public int Handle(FileInfo sourceFile, FileSystemInfo? destination, DisplayOutputKind outputKind, int memorySize)
    {
        var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputKind);
        var outputFileName = Path.GetFileNameWithoutExtension(outputFile.Name);

        var source = File.ReadAllText(sourceFile.FullName);
        var result = Parser.Parse(source);
        
        var errors = result.Errors.ToImmutableArray();
        if (!errors.IsEmpty)
        {
            console.WriteErrors(errors, source);
            return 1;
        }
        
        console.MarkupLine($"[green]{sourceFile.FullName}[/] -> [green]{outputFile.FullName}[/]");

        using var outputStream = outputFile.OpenWrite();
        Emitter.Emit(result.Instructions, outputStream, new EmitOptions()
        {
            AssemblyName = outputFileName,
            OutputKind = outputKind.ToCoreOutputKind(),
            MemorySize = memorySize
        });

        return 0;
    }
}
