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

    public static RootCommand GetRootCommand(
        Action<FileInfo, FileSystemInfo?, bool> handler)
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

        var outputExeOption = OutputExeOption();
        rootCommand.AddOption(outputExeOption);
        
        rootCommand.SetHandler(
            handler,
            sourceFileArgument,
            outputDestinationArgument,
            outputExeOption);

        return rootCommand;
    }

    private static Argument<FileInfo> SourceFileArgument()
    {
        var argument = new Argument<FileInfo>("source")
        {
            Description = "The source file to compile."
        };
        argument.LegalFilePathsOnly();
        argument.ExistingOnly();

        return argument;
    }

    private static Argument<FileSystemInfo?> OutputDestinationArgument()
    {
        var argument = new Argument<FileSystemInfo?>("output")
        {
            Description = "The output destination for the compiled binary file. " +
                          "If the provided value is a directory then " +
                          "the output file will be located in the specified directory " +
                          "and use the file name of the source file. " +
                          "If not specified, the output file will be located in the same " +
                          "directory as the source file and use the file name of the source file."
        };
        argument.LegalFilePathsOnly();
        argument.SetDefaultValue(null);
        
        return argument;
    }

    private static Option<bool> OutputExeOption()
    {
        var option = new Option<bool>("--exe")
        {
            Description = "Whether to output an exe file instead of a DLL."
        };

        return option;
    }
}
