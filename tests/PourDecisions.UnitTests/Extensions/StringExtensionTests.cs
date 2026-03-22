using PourDecisions.Shared.Extensions;

namespace PourDecisions.UnitTests.Extensions;

public class StringExtensionTests
{
    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hEllO wOrlD", "Hello World")]
    [InlineData("apple", "Apple")]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("a b c", "A B C")]
    [InlineData("123 apple", "123 Apple")]
    [InlineData("!!! symbols", "!!! Symbols")]
    [InlineData("1st place", "1St Place")]
    [InlineData("the quick brown fox", "The Quick Brown Fox")]
    [InlineData("QUICK BROWN FOX", "Quick Brown Fox")]
    [InlineData("  hello world  ", "  Hello World  ")]
    [InlineData("hello-world", "Hello-World")]
    public void ToTitleCase_ReturnsExpectedResult(string input, string expected)
    {
        var result = input.ToTitleCase();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToTitleCase_NullString_ThrowsNullReferenceException()
    {
        string? input = null;
        Assert.Throws<NullReferenceException>(() => input!.ToTitleCase());
    }
}
