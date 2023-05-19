using System.CommandLine.Parsing;
using BrainfuckToIL;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand((sourceFile, destination, outputExe) =>
{
    var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputExe);

    Console.WriteLine($"{sourceFile.FullName} -> {outputFile.FullName}");

    var source = File.ReadAllText(sourceFile.FullName);
    var instructions = BrainfuckToIL.Parser.Parse(source);

    using var outputStream = outputFile.OpenWrite();
    Emitter.Emit(instructions, outputStream);
});

var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);
