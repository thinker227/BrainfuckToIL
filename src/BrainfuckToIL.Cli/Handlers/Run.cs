using System.CommandLine;
using System.Reflection;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Run
{
    private readonly IConsole console;

    public Run(IConsole console) => this.console = console;

    public int Handle(FileInfo sourceFile)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Name);
    
        var source = File.ReadAllText(sourceFile.FullName);
        var instructions = Parser.Parse(source);

        var stream = new MemoryStream();
        Emitter.Emit(instructions, stream, new EmitOptions()
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
