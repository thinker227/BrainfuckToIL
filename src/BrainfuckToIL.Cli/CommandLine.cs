using System.CommandLine;
using System.CommandLine.Builder;

namespace BrainfuckToIL.Cli;

internal static class CommandLine
{
    public static CommandLineParser GetParser(RootCommand rootCommand)
    {
        var builder = new CommandLineBuilder(rootCommand);
        
        builder.UseDefaults();
        
        return builder.Build();
    }

    public static RootCommand GetRootCommand(
        Action<FileInfo, FileSystemInfo?, DisplayOutputKind> compileCommandHandler,
        Action<FileInfo> runCommandHandler)
    {
        var rootCommand = new RootCommand()
        {
            Name = "BFtoIL",
            Description = "Simplistic Brainfuck to IL compiler."
        };

        var compileCommand = CompileCommand(compileCommandHandler);
        rootCommand.AddCommand(compileCommand);

        var runCommand = RunCommand(runCommandHandler);
        rootCommand.AddCommand(runCommand);

        return rootCommand;
    }

    private static Command CompileCommand(Action<FileInfo, FileSystemInfo?, DisplayOutputKind> handler)
    {
        var command = new Command("compile")
        {
            Description = "Compiles a file to IL."
        };
        
        var sourceArgument = new Argument<FileInfo>("source")
        {
            Description = "The source file to compile."
        };
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);
        
        var outputArgument = new Argument<FileSystemInfo?>("output")
        {
            Description = "The output destination for the compiled binary file. " +
                          "If the provided value is a directory then " +
                          "the output file will be located in the specified directory " +
                          "and use the file name of the source file. " +
                          "If not specified, the output file will be located in the same " +
                          "directory as the source file and use the file name of the source file."
        };
        outputArgument.LegalFilePathsOnly();
        outputArgument.SetDefaultValue(null);
        command.AddArgument(outputArgument);

        var outputKindOption = new Option<DisplayOutputKind>("--output-kind")
        {
            Description = "Whether to output an exe or DLL file."
        };
        outputKindOption.AddAlias("-o");
        outputKindOption.SetDefaultValue(DisplayOutputKind.Exe);
        command.AddOption(outputKindOption);
        
        command.SetHandler(
            handler,
            sourceArgument,
            outputArgument,
            outputKindOption);

        return command;
    }

    private static Command RunCommand(Action<FileInfo> handler)
    {
        var command = new Command("run")
        {
            Description = "Compiles and runs a file."
        };

        var sourceArgument = new Argument<FileInfo>("source")
        {
            Description = "The source file to compile and run."
        };
        sourceArgument.LegalFilePathsOnly();
        sourceArgument.ExistingOnly();
        command.AddArgument(sourceArgument);
        
        command.SetHandler(
            handler,
            sourceArgument);

        return command;
    }
}
