using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand((sourceFile, destination) =>
{
    Console.WriteLine($"""
    Input file: {sourceFile.FullName}
    Output destination: {destination}
    """);
});

var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);
