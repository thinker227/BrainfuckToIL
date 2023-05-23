using System.CommandLine.Parsing;
using Spectre.Console;
using BrainfuckToIL.Cli;

var console = AnsiConsole.Console;

var parser = CommandLine.GetParser(console);
parser.Invoke(args);
