using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand();
var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);
