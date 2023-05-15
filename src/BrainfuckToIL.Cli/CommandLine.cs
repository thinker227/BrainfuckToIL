using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace BrainfuckToIL.Cli;

public static class CommandLine
{
    public static Parser GetParser(RootCommand rootCommand)
    {
        var builder = new CommandLineBuilder(rootCommand);
        
        builder.UseDefaults();
        
        return builder.Build();
    }

    public static RootCommand GetRootCommand(Action<FileInfo, FileSystemInfo> handler)
    {
        RootCommand rootCommand = new()
        {
            Name = "BFtoIL",
            Description = "Simplistic Brainfuck to IL compiler."
        };

        var sourceFileArgument = SourceFileArgument();
        rootCommand.AddArgument(sourceFileArgument);

        var outputDestinationArgument = OutputDestinationArgument();
        rootCommand.AddArgument(outputDestinationArgument);

        rootCommand.SetHandler(
            handler,
            sourceFileArgument,
            outputDestinationArgument);

        return rootCommand;
    }

    private static Argument<FileInfo> SourceFileArgument()
    {
        Argument<FileInfo> argument = new()
        {
            Name = "source",
            Description = "The source file to compile."
        };
        argument.LegalFilePathsOnly();
        argument.ExistingOnly();

        return argument;
    }

    private static Argument<FileSystemInfo> OutputDestinationArgument()
    {
        Argument<FileSystemInfo> argument = new()
        {
            Name = "output",
            Description = "The output destination for the compiled DLL file. " +
                          "If the provided value is a directory then " +
                          "the output DLL file will be located in the specified directory " +
                          "and use the file name of the source file."
        };
        argument.LegalFilePathsOnly();
        argument.ExistingOnly();
        
        return argument;
    }
}
