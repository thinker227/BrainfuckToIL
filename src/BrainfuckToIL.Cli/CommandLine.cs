using System.CommandLine;
using System.CommandLine.Builder;

namespace BrainfuckToIL.Cli;

public static class CommandLine
{
    public static CommandLineParser GetParser(RootCommand rootCommand)
    {
        var builder = new CommandLineBuilder(rootCommand);
        
        builder.UseDefaults();
        
        return builder.Build();
    }

    public static RootCommand GetRootCommand(Action<FileInfo, FileSystemInfo> handler)
    {
        var rootCommand = new RootCommand()
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
        var argument = new Argument<FileInfo>()
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
        var argument = new Argument<FileSystemInfo>()
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
