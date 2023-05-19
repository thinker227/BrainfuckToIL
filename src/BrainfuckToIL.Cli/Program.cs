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

    Console.WriteLine($"{sourceFile.FullName} -> {outputFile.FullName}");

    var source = File.ReadAllText(sourceFile.FullName);
    var instructions = BrainfuckToIL.Parser.Parse(source);

    using var outputStream = outputFile.OpenWrite();
    Emitter.Emit(instructions, outputStream);
}

static void Run(FileInfo sourceFile)
{
    var source = File.ReadAllText(sourceFile.FullName);
    var instructions = BrainfuckToIL.Parser.Parse(source);

    var stream = new MemoryStream();
    Emitter.Emit(instructions, stream);
    var bytes = stream.ToArray();

    var assembly = Assembly.Load(bytes);
    var programType = assembly.GetType("$<Program>") ?? throw new InvalidOperationException(
        "Could not find or load type $<Program>.");
    var mainMethod = programType.GetMethod("$<Main>") ?? throw new InvalidOperationException(
        "Could not find or load method $<Main>.");
    var main = mainMethod.CreateDelegate<Action>();
    
    main();
}
