using System.CommandLine;
using System.CommandLine.Help;
using System.Reflection;
using BrainfuckToIL.Cli.Commands;

namespace BrainfuckToIL.Cli;

internal sealed class Help : HelpBuilder
{
    public Help() : base(LocalizationResources.Instance, Console.WindowWidth) {}

    // This looks ugly but I love it.
    public override void Write(HelpContext context) => ((Action<HelpContext>)(
        context.Command switch
        {
            Root => Write<Root>,
            Compile => Write<Compile>,
            Run => Write<Run>,
            _ => throw new UnreachableException()
        }))(context);

    private void Write<TCommand>(HelpContext ctx)
        where TCommand : Command, ICommand<TCommand>
    {
        WriteHeading("Usage:", TCommand.Syntax, ctx);
        
        ctx.Output.WriteLine();
        WriteHeading("Description:", TCommand.Description, ctx);
        
        WriteMany("Arguments:", TCommand.Arguments, ctx);
        
        WriteMany("Options:", TCommand.Options, ctx);
        
        WriteMany("Commands:", TCommand.Subcommands, ctx);

        // Write some additional space at the bottom.
        ctx.Output.WriteLine();
        ctx.Output.WriteLine();
        ctx.Output.WriteLine();
    }

    private void WriteMany(string? title, IEnumerable<(string left, string right)> sections, HelpContext ctx)
    {
        var columns = sections
            .Select(s => new TwoColumnHelpRow(s.left, s.right))
            .ToArray();
        
        if (columns.Length == 0) return;
        
        ctx.Output.WriteLine();
        ctx.Output.WriteLine(title);
        WriteColumns(columns, ctx);
    }

    private static Action<HelpBuilder, string?, string?, TextWriter>? writeHeading;

    // Idk why WriteHeading is private but it's too useful not to pull some reflection for.
    private void WriteHeading(string title, string description, HelpContext ctx)
    {
        writeHeading ??= typeof(HelpBuilder).GetMethod(
                "WriteHeading",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action<HelpBuilder, string?, string?, TextWriter>>();

        writeHeading(this, title, description, ctx.Output);
    }
}
