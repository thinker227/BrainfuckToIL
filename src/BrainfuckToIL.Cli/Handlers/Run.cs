using System.CommandLine;
using System.CommandLine.IO;
using System.Reflection;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Run
{
    private readonly IConsole console;
    private readonly TextReader reader;

    public Run(IConsole console, TextReader reader)
    {
        this.console = console;
        this.reader = reader;
    }

    public int Handle(FileInfo sourceFile)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Name);
    
        var source = File.ReadAllText(sourceFile.FullName);
        var result = Parser.Parse(source);

        // TODO: Error reporting.
        
        var stream = new MemoryStream();
        Emitter.Emit(result.Instructions, stream, new EmitOptions()
        {
            AssemblyName = sourceFileName,
            OutputKind = OutputKind.Dll
        });
        var bytes = stream.ToArray();

        var assembly = Assembly.Load(bytes);
        var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException(
            $"Assembly {assembly.FullName} does not have an entry point.");
        var main = entryPoint.CreateDelegate<Action>();
    
        main();
        
        return 0;
    }
}
