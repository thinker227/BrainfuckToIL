using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand((sourceFile, destination, outputExe) =>
{
    var outputFile = Files.GetOrCreateOutputFile(sourceFile, destination, outputExe);

    Console.WriteLine($"""
    Source file: {sourceFile.FullName}
    Output file: {outputFile.FullName}
    """);
});

var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);
