using System.CommandLine.Parsing;
using Spectre.Console;
using BrainfuckToIL.Cli;

var console = AnsiConsole.Console;

var rootCommand = CommandLine.GetRootCommand();
var parser = CommandLine.GetParser(rootCommand, console);
parser.Invoke(args);
