using System.Collections;
using System.Collections.Immutable;

namespace BrainfuckToIL.Cli;

/// <summary>
/// A map between a string and lines within the string.
/// </summary>
internal sealed class LineMap : IReadOnlyCollection<TextSpan>
{
    private readonly ImmutableArray<TextSpan> lineSpans;
    private readonly int characterCount;

    /// <summary>
    /// The amount of lines in the map.
    /// </summary>
    public int LineCount => lineSpans.Length;
    
    int IReadOnlyCollection<TextSpan>.Count => LineCount;
    
    /// <summary>
    /// Gets the line at a given position in the string the map was constructed from.
    /// </summary>
    /// <param name="position">The position to get the line at.</param>
    public LineInfo? this[int position] =>
        FindLine(position) is int lineNumber
            ? new(lineNumber, lineSpans[lineNumber])
            : null;
    
    private LineMap(ImmutableArray<TextSpan> lineSpans, int characterCount)
    {
        this.lineSpans = lineSpans;
        this.characterCount = characterCount;
    }

    /// <summary>
    /// Creates a new <see cref="LineMap"/>.
    /// </summary>
    /// <param name="span">The span of characters to create the map from.</param>
    public static LineMap Create(ReadOnlySpan<char> span)
    {
        var builder = ImmutableArray.CreateBuilder<TextSpan>();
        
        var position = 0;

        while (true)
        {
            var newlineIndex = span.IndexOf("\n");
            var end = newlineIndex >= 0
                ? newlineIndex + 1
                : null as int?;

            var textSpan = end is not null
                ? new TextSpan(position, position + end.Value)
                : new TextSpan(position, span.Length);
            builder.Add(textSpan);

            if (end is null || end.Value >= span.Length) break;
            
            position += end.Value;
            span = span[end.Value..];
        }

        return new(builder.ToImmutable(), span.Length);
    }
    
    private int? FindLine(int position)
    {
        if (position < 0 || position >= characterCount) return null;

        var lower = 0;
        var upper = lineSpans.Length;
        
        while (true)
        {
            var current = lower + (upper - lower) / 2;
            var lineSpan = lineSpans[current];
            
            var value = Compare(position, lineSpan);

            switch (value)
            {
            case 0:
                return current;
            
            case < 0:
                upper = current;
                break;
            
            case > 0:
                lower = current;
                break;
            }
        }
    }

    private static int Compare(int a, TextSpan b)
    {
        if (a >= b.Start && a < b.End) return 0;
        if (a >= b.End) return 1;
        return -1;
    }

    /// <summary>
    /// Gets the lines that a <see cref="TextSpan"/> spans.
    /// </summary>
    /// <param name="span">The span to get the lines of.</param>
    public IEnumerable<LineInfo> GetLinesOfSpan(TextSpan span)
    {
        if (span.IsEmpty) yield break;

        var c = FindLine(span.Start);
        if (c is null) yield break;
        var current = c.Value;

        while (current < lineSpans.Length)
        {
            var lineSpan = lineSpans[current];

            if (lineSpan.Start >= span.End) yield break;
            
            yield return new(current, lineSpan);

            current++;
        }
    }
    
    /// <summary>
    /// Get an enumerator of lines in the map.
    /// </summary>
    public ImmutableArray<TextSpan>.Enumerator GetEnumerator() =>
        lineSpans.GetEnumerator();

    IEnumerator<TextSpan> IEnumerable<TextSpan>.GetEnumerator() =>
        ((IEnumerable<TextSpan>)lineSpans).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)lineSpans).GetEnumerator();

    /// <summary>
    /// Info about a line in a map.
    /// </summary>
    /// <param name="LineNumber">The 0-indexed line number of the line.</param>
    /// <param name="Span">The span of the line in the string the map was constructed from.</param>
    public readonly record struct LineInfo(int LineNumber, TextSpan Span);
}
