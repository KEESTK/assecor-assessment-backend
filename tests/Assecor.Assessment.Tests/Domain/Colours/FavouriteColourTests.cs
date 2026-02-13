using Assecor.Assessment.Domain.Colours;
using Xunit;

namespace Assecor.Assessment.Tests.Domain.Colours;

public sealed class FavouriteColourTests
{
    [Theory]
    [InlineData("blau", "blau")]
    [InlineData(" Blau ", "blau")]
    [InlineData("BLAU", "blau")]
    [InlineData("grün", "grün")]
    [InlineData(" GRÜN ", "grün")]
    [InlineData("türkis", "türkis")]
    [InlineData("weiß", "weiß")]
    public void From_NormalizesTrimAndCase(string input, string expectedCanonical)
    {
        var c = FavouriteColour.From(input);

        Assert.Equal(expectedCanonical, c.Value);
    }

    [Theory]
    [InlineData("gruen", "grün")]
    [InlineData(" tuerkis ", "türkis")]
    [InlineData("WEISS", "weiß")]
    public void From_AcceptsAsciiVariants(string input, string expectedCanonical)
    {
        var c = FavouriteColour.From(input);

        Assert.Equal(expectedCanonical, c.Value);
    }

    [Theory]
    [InlineData("")]

// empty
    [InlineData(" ")]
    [InlineData("pink")]
    [InlineData("blue")]
    [InlineData("1")]
    public void From_UnknownOrInvalid_Throws(string input)
    {
        Assert.ThrowsAny<Exception>(() => FavouriteColour.From(input));
    }
}