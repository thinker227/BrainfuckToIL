using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var parser = CommandLine.GetParser(args);
parser.Invoke(args);
