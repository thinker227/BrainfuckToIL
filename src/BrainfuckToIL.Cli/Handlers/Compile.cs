using System.CommandLine;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Compile
{
    private readonly IConsole console;

    public Compile(IConsole console) => this.console = console;

    public int Handle(FileInfo sourceFile, FileSystemInfo? destination, DisplayOutputKind outputKind)
    {
        var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputKind);
        var outputFileName = Path.GetFileNameWithoutExtension(outputFile.Name);
    
        console.WriteLine($"{sourceFile.FullName} -> {outputFile.FullName}");

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
