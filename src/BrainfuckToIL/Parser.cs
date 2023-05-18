using System.Collections.Immutable;

namespace BrainfuckToIL;

/// <summary>
/// A parser from a sequence of characters to a series of instructions.
/// </summary>
public sealed class Parser
{
    private readonly record struct CharParseResult(
        Instruction? Instruction,
        bool MoveNext)
    {
        public static CharParseResult Discard { get; } = new(null, true);
    }
    
    private readonly record struct InstructionArray(
        ImmutableArray<Instruction>.Builder Instructions,
        int? StartPosition);
    
    private enum SequentialKind
    {
        Move,
        Arithmetic
    }

    private record struct SequentialInstruction(SequentialKind Kind, int Value)
    {
        public SequentialKind Kind { get; } = Kind;
    }
    
    private readonly IEnumerator<char> input;
    private readonly Stack<InstructionArray> instructionsStack;
    private int position;
    private SequentialInstruction? sequentialInstruction;

    private Parser(IEnumerator<char> input)
    {
        this.input = input;
        instructionsStack = new();
        position = 0;
        sequentialInstruction = null;
    }

    /// <summary>
    /// Parses a sequence of instructions.
    /// </summary>
    /// <param name="input">The input to parse. Should be finite.</param>
    /// <returns>An immutable array of instructions.</returns>
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

        var moveNext = true;
        
        while (!moveNext || input.MoveNext())
        {
            var result = ParseChar(input.Current);
            
            moveNext = result.MoveNext;
            if (moveNext) position++;

            if (result.Instruction is null) continue;
            
            // Assuming stack is never empty, in which case parsing has gone awry.
            instructionsStack.Peek().Instructions.Add(result.Instruction);
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

    private CharParseResult ParseChar(char c)
    {
        if (sequentialInstruction is not null && GetSequentialKind(c) != sequentialInstruction.Value.Kind)
            return new(FinishSequentialInstruction(), false);
        
        if (ParseSequential(c)) return CharParseResult.Discard;

        if (ParseSimple(c) is Instruction simple) return new(simple, true);

        if (ParseLoopStart(c)) return CharParseResult.Discard;

        if (ParseLoopEnd(c) is Instruction loopEnd) return new(loopEnd, true);
        
        return CharParseResult.Discard;
    }

    private static Instruction? ParseSimple(char c) => c switch
    {
        '.' => new Instruction.Output(),
        ',' => new Instruction.Input(),
        _ => null
    };

    private Instruction? FinishSequentialInstruction()
    {
        if (sequentialInstruction is null) throw new InvalidOperationException(
            "Attempted to finish a sequential instruction which was null.");
        
        if (sequentialInstruction is { Value: 0 }) return null;
        
        var seq = sequentialInstruction.Value;
        sequentialInstruction = null;
        
        return seq.Kind switch
        {
            SequentialKind.Move => new Instruction.Move(seq.Value),
            SequentialKind.Arithmetic => new Instruction.Arithmetic(seq.Value),
            _ => throw new UnreachableException()
        };
    }

    private bool ParseSequential(char c)
    {
        if (GetSequentialKind(c) is not SequentialKind kind) return false;
        var value = GetSequentialValue(c);
        
        if (sequentialInstruction is null)
        {
            sequentialInstruction = new(kind, value);
            return true;
        }
        
        var seq = sequentialInstruction.Value;
        
        if (seq.Kind != kind) throw new InvalidOperationException(
            "Mismatched sequential instruction kinds.");
        
        sequentialInstruction = seq with
        {
            Value = seq.Value + value
        };

        return true;
    }

    private static SequentialKind? GetSequentialKind(char c) => c switch
    {
        '>' or '<' => SequentialKind.Move,
        '+' or '-' => SequentialKind.Arithmetic,
        _ => null
    };
    
    private static int GetSequentialValue(char c) => c switch
    {
        '>' or '+' => 1,
        '<' or '-' => -1,
        _ => throw new InvalidOperationException(
            "Invalid character for sequential instruction.")
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
