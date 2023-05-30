using System.CommandLine;

namespace BrainfuckToIL.Cli.Commands;

internal interface ICommand<TSelf> where TSelf : Command, ICommand<TSelf>
{
    static abstract TSelf Command { get; }
    
    static abstract string Name { get; }
    
    static abstract string Description { get; }
    
    static abstract string Syntax { get; }
    
    static abstract string ShortSyntax { get; }
    
    static abstract IEnumerable<(string syntax, string description)> Arguments { get; }
    
    static abstract IEnumerable<(string syntax, string description)> Options { get; }
    
    static abstract IEnumerable<(string syntax, string description)> Subcommands { get; } 
}
