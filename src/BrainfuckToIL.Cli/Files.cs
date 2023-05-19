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
    /// <param name="outputExe">Whether to output an exe file or not.</param>
    /// <returns>The created output file, or <paramref name="inputFile"/> if it already exists.</returns>
    public static FileInfo GetOrCreateOutputFile(
        FileInfo inputFile,
        FileSystemInfo? outputDestination,
        bool outputExe) =>
        outputDestination switch
        {
            FileInfo { Exists: true } file => file,
            
            FileInfo file => CreateFile(file),
            
            DirectoryInfo dir => ToDirectory(inputFile, dir, outputExe),
            
            null => ToDirectory(inputFile, inputFile.Directory, outputExe),
            
            _ => throw new UnreachableException()
        };

    private static FileInfo CreateFile(FileInfo file)
    {
        using var _ = File.Create(file.FullName);
        return new(file.FullName);
    }

    private static FileInfo ToDirectory(FileInfo inputFile, DirectoryInfo outputDirectory, bool outputExe)
    {
        var inputName = Path.GetFileNameWithoutExtension(inputFile.Name);
        var outputExtension = outputExe ? ".exe" : ".dll";
        var outputName = inputName + outputExtension;
        var outputPath = Path.Combine(outputDirectory.FullName, outputName);
        
        using var _ = File.Create(outputPath);
        return new(outputPath);
    }
}
