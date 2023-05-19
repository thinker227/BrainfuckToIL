using System.CommandLine.Parsing;
using System.Reflection;
using BrainfuckToIL;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand(Compile, Run);

var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);



static void Compile(FileInfo sourceFile, FileSystemInfo? destination, bool outputExe)
{
    var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputExe);
    var outputFileName = Path.GetFileNameWithoutExtension(outputFile.Name);
    
    Console.WriteLine($"{sourceFile.FullName} -> {outputFile.FullName}");

    var source = File.ReadAllText(sourceFile.FullName);
    var instructions = Parser.Parse(source);

    using var outputStream = outputFile.OpenWrite();
    Emitter.Emit(instructions, outputStream, new EmitOptions()
    {
        AssemblyName = outputFileName,
        OutputKind = outputExe
            ? OutputKind.Executable
            : OutputKind.Dll
    });
}

static void Run(FileInfo sourceFile)
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
}
