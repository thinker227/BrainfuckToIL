using System.Collections.Immutable;

namespace BrainfuckToIL;

/// <summary>
/// A parser from a sequence of characters to a series of instructions.
/// </summary>
public sealed class Parser
{
    /// <summary>
    /// The result of a parse operation.
    /// </summary>
    /// <param name="Instruction">The instruction which was produced.</param>
    /// <param name="MoveNext">Whether to continue to the next character of input or remain on the current one.</param>
    private readonly record struct CharParseResult(
        Instruction? Instruction,
        bool MoveNext)
    {
        /// <summary>
        /// A <see cref="CharParseResult"/> which discards the current character.
        /// </summary>
        public static CharParseResult Discard { get; } = new(null, true);
    }
    
    /// <summary>
    /// An array of instructions.
    /// </summary>
    /// <param name="Instructions">A mutable builder for the instructions.</param>
    /// <param name="StartPosition">The start position in the input of the array.</param>
    private readonly record struct InstructionArray(
        ImmutableArray<Instruction>.Builder Instructions,
        int? StartPosition);
    
    /// <summary>
    /// The kind of a sequential instruction.
    /// </summary>
    private enum SequentialKind
    {
        Move,
        Arithmetic
    }

    /// <summary>
    /// An accumulative sequential instruction. 
    /// </summary>
    /// <param name="Kind">The kind of the instruction.</param>
    /// <param name="Value">The accumulative value of the instruction.</param>
    private record struct SequentialInstruction(SequentialKind Kind, int Value)
    {
        public SequentialKind Kind { get; } = Kind;
    }
    
    private readonly IEnumerator<char> input;
    
    /// <summary>
    /// The current position in the input.
    /// </summary>
    private int position;
    
    /// <summary>
    /// A stack of instruction arrays to which new elements are pushed
    /// when entering a nested context such as a loop.
    /// </summary>
    private readonly Stack<InstructionArray> instructionsStack;

    /// <summary>
    /// Keeps track of the current sequential instruction, i.e. if there has previously been a
    /// <c>&gt;</c>, <c>&lt;</c>, <c>+</c>, or <c>-</c> instruction and the parser is currently
    /// trying to accumulate the remaining similar instructions.
    /// </summary>
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

    /// <summary>
    /// Parses all characters in the input.
    /// </summary>
    /// <returns>The parsed instructions.</returns>
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

    /// <summary>
    /// Parses a single character, possibly updating the state of the parser.
    /// </summary>
    /// <param name="c">The character to parse.</param>
    /// <returns>The result of the parsing operation.</returns>
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

    /// <summary>
    /// Tries to parse a simple instruction,
    /// i.e. an instruction which does not require any special logic.
    /// </summary>
    /// <param name="c">The character to parse.</param>
    /// <returns>An instruction created from the character,
    /// or <see langword="null"/> if the character could not be parsed as a simple instruction.</returns>
    private static Instruction? ParseSimple(char c) => c switch
    {
        '.' => new Instruction.Output(),
        ',' => new Instruction.Input(),
        _ => null
    };

    /// <summary>
    /// Tries to finish the current sequential instruction.
    /// </summary>
    /// <returns>The created sequential instruction,
    /// or <see langword="null"/> if the total accumulated value of the instruction is 0.</returns>
    private Instruction? FinishSequentialInstruction()
    {
        if (sequentialInstruction is null) throw new InvalidOperationException(
            "Attempted to finish a sequential instruction which was null.");

        var seq = sequentialInstruction.Value;
        sequentialInstruction = null;
        
        if (seq is { Value: 0 }) return null;
        
        return seq.Kind switch
        {
            SequentialKind.Move => new Instruction.Move(seq.Value),
            SequentialKind.Arithmetic => new Instruction.Arithmetic(seq.Value),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Tries to parse a sequential instruction by either continuing
    /// the current sequential instruction or by creating a new one.
    /// </summary>
    /// <param name="c">The character to parse.</param>
    /// <returns>Whether a sequential instruction could be parsed from the character.</returns>
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

    /// <summary>
    /// Gets the kind of a sequential instruction from a character.
    /// </summary>
    /// <param name="c">The character to get the kind from.</param>
    private static SequentialKind? GetSequentialKind(char c) => c switch
    {
        '>' or '<' => SequentialKind.Move,
        '+' or '-' => SequentialKind.Arithmetic,
        _ => null
    };
    
    /// <summary>
    /// Gets the sequential value of a character,
    /// i.e. whether it's an increment/move right or decrement/move left.
    /// </summary>
    /// <param name="c">The character to get the value of.</param>
    private static int GetSequentialValue(char c) => c switch
    {
        '>' or '+' => 1,
        '<' or '-' => -1,
        _ => throw new InvalidOperationException(
            "Invalid character for sequential instruction.")
    };

    /// <summary>
    /// Tries to parse a loop start by pushing a new context to the instructions stack.
    /// </summary>
    /// <param name="c">The character to parse.</param>
    /// <returns>Whether the character is a loop start.</returns>
    private bool ParseLoopStart(char c)
    {
        if (c is not '[') return false;

        instructionsStack.Push(new(
            ImmutableArray.CreateBuilder<Instruction>(),
            position));

        return true;
    }

    /// <summary>
    /// Tries to parse a loop ending by popping the instructions stack
    /// and returning the accumulated instructions.
    /// </summary>
    /// <param name="c">The character to parse.</param>
    /// <returns>The parsed loop instruction,
    /// or <see langword="null"/> if the character is not a loop ending.</returns>
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
