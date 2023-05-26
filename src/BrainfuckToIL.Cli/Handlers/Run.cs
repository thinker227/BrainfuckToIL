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

        var main = Emitter.EmitAsDelegate(result.Instructions, new EmitOptions()
        {
            AssemblyName = sourceFileName,
            OutputKind = OutputKind.Dll,
            MemorySize = memorySize,
            WrapMemory = !noWrap
        });
    
        main();
        
        return 0;
    }
}
