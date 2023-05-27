namespace BrainfuckToIL.Tests.Integration;

public class OutputTests : IntegrationTestBase
{
    [Fact]
    public void ShouldOutputAllAsciiCharacters()
    {
        var output = Run(".+[.+]");

        var expected = new string(
            Enumerable.Range(0, 256)
                .Select(x => (char)x)
                .ToArray());
        
        output.ShouldBe(expected);
    }
}
