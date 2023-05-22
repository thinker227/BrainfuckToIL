using System.CommandLine;
using System.CommandLine.IO;
using Spectre.Console;

namespace BrainfuckToIL.Cli;

internal sealed class SpectreConsoleConsole : IConsole
{
    private readonly IAnsiConsole console;
    private readonly Style standardStyle = Style.Plain;
    private readonly Style errorStyle = new(foreground: Color.Red);
    
    public IStandardStreamWriter Out { get; }
    
    public IStandardStreamWriter Error { get; }

    public bool IsOutputRedirected => true;
    
    public bool IsErrorRedirected => true;
    
    public bool IsInputRedirected => false;

    public SpectreConsoleConsole(IAnsiConsole console)
    {
        this.console = console;

        Out = new SpectreConsoleStreamWriter(console, standardStyle);
        Error = new SpectreConsoleStreamWriter(console, errorStyle);
    }
}

internal sealed class SpectreConsoleStreamWriter : IStandardStreamWriter
{
    private readonly IAnsiConsole console;
    private readonly Style style; 

    public SpectreConsoleStreamWriter(IAnsiConsole console, Style style)
    {
        this.console = console;
        this.style = style;
    }

    public void Write(string? value)
    {
        if (value is null) return;
            
        console.Write(value, style);
    }
}
