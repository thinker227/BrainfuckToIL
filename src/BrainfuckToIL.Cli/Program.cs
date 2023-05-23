using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var parser = CommandLine.GetParser();
parser.Invoke(args);
