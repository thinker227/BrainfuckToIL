namespace BrainfuckToIL.Tests.Integration;

public class MemoryTests : IntegrationTestBase
{
    [Fact]
    public void ShouldWrapMemoryForwards()
    {
        var output = Run(
            "+>>>.",
            configureEmitOptions: options => options with
            {
                MemorySize = 3,
                WrapMemory = true
            });
        
        output.ShouldBe("\u0001");
    }
    
    [Fact]
    public void ShouldWrapMemoryBackwards()
    {
        var output = Run(
            "+<<<.",
            configureEmitOptions: options => options with
            {
                MemorySize = 3,
                WrapMemory = true
            });
        
        output.ShouldBe("\u0001");
    }

    [Fact]
    public void ShouldWrapMemoryDoubleForwards()
    {
        var output = Run(
            "+>>>>>>>>>.",
            configureEmitOptions: options => options with
            {
                MemorySize = 3,
                WrapMemory = true
            });
        
        output.ShouldBe("\u0001");
    }

    [Fact]
    public void ShouldWrapMemoryDoubleBackwards()
    {
        var output = Run(
            "+<<<<<<<<<.",
            configureEmitOptions: options => options with
            {
                MemorySize = 3,
                WrapMemory = true
            });
        
        output.ShouldBe("\u0001");
    }

    [Fact]
    public void ShouldThrowOutOfRange() =>
        Should.Throw<IndexOutOfRangeException>(() =>
        {
            Run(">>>.",
                configureEmitOptions: options => options with
                {
                    MemorySize = 3,
                    WrapMemory = false
                });
        });
}
