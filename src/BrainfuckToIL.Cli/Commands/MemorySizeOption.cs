using System.CommandLine;
using System.CommandLine.Parsing;

namespace BrainfuckToIL.Cli.Commands;

internal static class MemorySizeOption
{
    public static string Syntax => "-m|--memory-size <size>";
    
    public static string Description => "The size of the memory in the amount of cells long it is. [default: 30000]";
    
    public static Option<int> Option { get; } = GetOption();

    private static Option<int> GetOption()
    {
        var option = new Option<int>("--memory-size");
        
        option.AddAlias("-m");
        option.SetDefaultValue(30_000);
        option.AddValidator(Validator);
        
        return option;
    }
    
    private static void Validator(OptionResult result)
    {
        var value = result.GetValueOrDefault<int>();

        if (value <= 0)
        {
            result.ErrorMessage = "Memory size cannot be less than or equal to 0.";
        }
    }
}
