using System.Collections.Immutable;

namespace BrainfuckToIL;

public sealed class Parser
{
    private readonly record struct InstructionArray(
        ImmutableArray<Instruction>.Builder Instructions,
        int? StartPosition);
    
    private readonly IEnumerator<char> input;
    private readonly Stack<InstructionArray> instructionsStack;
    private int position;

    private Parser(IEnumerator<char> input)
    {
        this.input = input;
        instructionsStack = new();
        position = 0;
    }

    public static ImmutableArray<Instruction> Parse(IEnumerable<char> input)
    {
        using var enumerator = input.GetEnumerator();
        var parser = new Parser(enumerator);
        return parser.Parse();
    }

    private ImmutableArray<Instruction> Parse()
    {
        instructionsStack.Push(new(
            ImmutableArray.CreateBuilder<Instruction>(),
            null));

        while (input.MoveNext())
        {
            var ast = ParseChar(input.Current);
            position++;
            
            if (ast is null) continue;
            
            // Assuming stack is never empty, in which case parsing has gone awry.
            instructionsStack.Peek().Instructions.Add(ast);
        }

        switch (instructionsStack.Count)
        {
        case > 1:
            {
                // If the instructions stack has more than a single element
                // then there's an unterminated loop somewhere.

                var loopStartPosition = instructionsStack.Peek().StartPosition;
            
                // TODO: Better error handling.
                throw new InvalidOperationException(
                    $"Unterminated loop at position {loopStartPosition}.");
            }
        
        case <= 0:
            throw new InvalidOperationException(
                "Instructions stack was empty.");
        }
        
        return instructionsStack.Pop().Instructions.ToImmutable();
    }

    private Instruction? ParseChar(char c)
    {
        if (ParseSimple(c) is Instruction simple) return simple;

        if (ParseLoopStart(c)) return null;

        if (ParseLoopEnd(c) is Instruction loopEnd) return loopEnd;
        
        return null;
    }

    private static Instruction? ParseSimple(char c) => c switch
    {
        '>' => new Instruction.MoveRight(),
        '<' => new Instruction.MoveLeft(),
        '+' => new Instruction.Increment(),
        '-' => new Instruction.Decrement(),
        '.' => new Instruction.Output(),
        ',' => new Instruction.Input(),
        _ => null
    };

    private bool ParseLoopStart(char c)
    {
        if (c is not '[') return false;

        instructionsStack.Push(new(
            ImmutableArray.CreateBuilder<Instruction>(),
            position));

        return true;
    }

    private Instruction? ParseLoopEnd(char c)
    {
        if (c is not ']') return null;

        switch (instructionsStack.Count)
        {
        case 1:
            // If the instructions stack only has a single element and a loop end is encountered
            // then it's a loop ending with no corresponding loop start.
            
            // TODO: Better error handling.
            throw new InvalidOperationException(
                $"Encountered loop ending at position {position} with no loop start.");
        
        case <= 0:
            throw new InvalidOperationException(
                "Instructions stack was empty.");
        }
        
        // Gather up all the accumulated instructions and turn them into a Loop instruction.
        var instructions = instructionsStack.Pop().Instructions.ToImmutable();
        return new Instruction.Loop(instructions);
    }
}
