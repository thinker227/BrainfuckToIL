using System.CommandLine.Parsing;
using BrainfuckToIL.Cli;

var rootCommand = CommandLine.GetRootCommand((sourceFile, destination, outputExe) =>
{
    FileInfo outputFile;
    switch (destination)
    {
    case FileInfo { Exists: true } file:
        outputFile = file;
        break;
    
    case FileInfo:
        File.Create(destination.FullName);
        outputFile = new(destination.FullName);
        break;
    
    case DirectoryInfo:
        {
            var inputName = Path.GetFileNameWithoutExtension(sourceFile.Name);
            var outputExtension = outputExe ? ".exe" : ".dll";
            var outputName = inputName + outputExtension;
            var outputPath = Path.Combine(sourceFile.DirectoryName, outputName);
            
            File.Create(outputPath);
            outputFile = new(outputPath);
            break;
        }
    
    default:
        throw new UnreachableException();
    }

    Console.WriteLine($"""
    Source file: {sourceFile.FullName}
    Output file: {outputFile.FullName}
    """);
});

var parser = CommandLine.GetParser(rootCommand);
parser.Invoke(args);
