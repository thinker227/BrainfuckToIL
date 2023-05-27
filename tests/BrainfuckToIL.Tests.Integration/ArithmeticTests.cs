namespace BrainfuckToIL.Tests.Integration;

public class ArithmeticTests : IntegrationTestBase
{
    [Fact]
    public void WrapsForwards()
    {
        var output = Run(">+++++++++++++++++++++++++[<++++++++++>-]<++++++.");
        
        output.ShouldBe("\u0000");
    }

    [Fact]
    public void WrapsBackwards()
    {
        var output = Run("-.");
        
        output.ShouldBe("\u00ff");
    }
}
