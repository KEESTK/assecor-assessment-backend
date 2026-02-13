using Assecor.Assessment.Domain.Persons;
using Xunit;

namespace Assecor.Assessment.Tests.Domain.Persons;

public sealed class PersonIdTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    public void From_WithPositiveValue_CreatesId(int value)
    {
        var id = PersonId.From(value);

        Assert.Equal(value, id.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void From_WithNonPositiveValue_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PersonId.From(value));
    }
}