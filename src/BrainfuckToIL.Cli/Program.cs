using System.CommandLine.Parsing;
using Spectre.Console;
using BrainfuckToIL.Cli;

var console = AnsiConsole.Console;
var reader = Console.In;

var rootCommand = CommandLine.GetRootCommand();
var parser = CommandLine.GetParser(rootCommand, console, reader);
parser.Invoke(args);
