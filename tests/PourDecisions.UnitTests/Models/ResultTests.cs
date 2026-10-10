using PourDecisions.Application.Models;

namespace PourDecisions.UnitTests.Models;

public class ResultTests
{
    [Fact]
    public void Result_Success_SetsIsSuccessTrueAndErrorMessageNull()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Result_Error_SetsIsSuccessFalseAndErrorMessage()
    {
        var errorMessage = "Something went wrong";
        var result = Result.Error(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void GenericResult_Success_SetsIsSuccessTrueValueAndNullErrorMessage()
    {
        var value = "test value";
        var result = Result<string>.Success(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void GenericResult_Success_WithComplexType()
    {
        var value = new { Name = "Gin", Id = 1 };
        var result = Result<object>.Success(value);

        Assert.True(result.IsSuccess);
        Assert.Same(value, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void GenericResult_Error_SetsIsSuccessFalseErrorMessageAndNullValue()
    {
        var errorMessage = "Validation failed";
        var result = Result<string>.Error(errorMessage);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }
}
