using System.Collections.Immutable;
using System.Reflection;
using Spectre.Console;
using BrainfuckToIL.Emit;

namespace BrainfuckToIL.Cli.Handlers;

internal sealed class Run
{
    private readonly IAnsiConsole console;

    public Run(IAnsiConsole console) => this.console = console;

    public int Handle(
        FileInfo sourceFile,
        int memorySize,
        bool noWrap)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Name);
    
        var source = File.ReadAllText(sourceFile.FullName);
        var result = Parser.Parse(source);

        var errors = result.Errors.ToImmutableArray();
        if (!errors.IsEmpty)
        {
            console.WriteErrors(errors, source);
            return 1;
        }
        
        var stream = new MemoryStream();
        Emitter.Emit(result.Instructions, stream, new EmitOptions()
        {
            AssemblyName = sourceFileName,
            OutputKind = OutputKind.Dll,
            MemorySize = memorySize,
            WrapMemory = !noWrap
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
