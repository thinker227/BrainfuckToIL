namespace BrainfuckToIL.Cli;

/// <summary>
/// Manages files for the CLI.
/// </summary>
internal static class Files
{
    /// <summary>
    /// Gets or creates an output file.
    /// </summary>
    /// <param name="inputFile">The input file.</param>
    /// <param name="outputDestination">The output destination, or <see langword="null"/>.</param>
    /// <param name="outputKind">The kind of file to output.</param>
    /// <returns>The created output file, or <paramref name="inputFile"/> if it already exists.</returns>
    public static FileInfo GetOrCreateOutputFile(
        FileInfo inputFile,
        FileSystemInfo? outputDestination,
        DisplayOutputKind outputKind) =>
        outputDestination switch
        {
            FileInfo { Exists: true } file => file,
            
            FileInfo file => CreateFile(file),
            
            DirectoryInfo dir => ToDirectory(inputFile, dir, outputKind),
            
            null => ToDirectory(
                inputFile,
                inputFile.Directory ?? throw new InvalidOperationException(
                    $"File {inputFile.FullName} not have a parent directory."),
                outputKind),
            
            _ => throw new UnreachableException()
        };

    private static FileInfo CreateFile(FileInfo file)
    {
        CreateEmptyFile(file.FullName);
        return new(file.FullName);
    }

    private static FileInfo ToDirectory(FileInfo inputFile, DirectoryInfo outputDirectory, DisplayOutputKind outputKind)
    {
        var inputName = Path.GetFileNameWithoutExtension(inputFile.Name);
        var outputExtension = outputKind.GetFileExtension();
        var outputName = inputName + outputExtension;
        var outputPath = Path.Combine(outputDirectory.FullName, outputName);
        
        CreateEmptyFile(outputPath);
        return new(outputPath);
    }

    /// <summary>
    /// Creates an empty file with a given path.
    /// Overrides the file if it already exists.
    /// </summary>
    /// <param name="path">The path of the file to create.</param>
    public static void CreateEmptyFile(string path) =>
        File.WriteAllBytes(path, Array.Empty<byte>());
}
