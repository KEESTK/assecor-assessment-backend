using Assecor.Assessment.Domain.Colours;
using Xunit;

namespace Assecor.Assessment.Tests.Domain.Colours;

public sealed class ColourCodeMapperTests
{
    [Theory]
    [InlineData(1, "blau")]
    [InlineData(2, "grün")]
    [InlineData(3, "violett")]
    [InlineData(4, "rot")]
    [InlineData(5, "gelb")]
    [InlineData(6, "türkis")]
    [InlineData(7, "weiß")]
    public void FromCode_MapsKnownCodes(int code, string expectedCanonical)
    {
        var c = ColourCodeMapper.FromCode(code);

        Assert.Equal(expectedCanonical, c.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(8)]
    [InlineData(999)]
    public void FromCode_UnsupportedCode_Throws(int code)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColourCodeMapper.FromCode(code));
    }
}