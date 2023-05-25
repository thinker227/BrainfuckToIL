namespace BrainfuckToIL.Emit;

/// <summary>
/// Represents methods of displaying and formatting user when input encountering a <c>,</c> instruction.
/// </summary>
public enum InputFormat
{
    /// <summary>
    /// Do not display the inputted character.
    /// </summary>
    Hidden,
    /// <summary>
    /// Display the inputted character but don't write anything afterwards.
    /// </summary>
    Shown,
    /// <summary>
    /// Display the inputted character and write a newline character afterwards.
    /// </summary>
    Newline
}
