using Librory.Application.Families;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class CurrentFamilyContextAccessorTests
{
    [Fact]
    public void RequireCurrent_returns_the_current_context()
    {
        var expected = new CurrentFamilyContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MemberRole.Admin,
            PreferredLanguage.Chinese);
        var accessor = new CurrentFamilyContextAccessor
        {
            Current = expected,
        };

        var actual = accessor.RequireCurrent();

        Assert.Same(expected, actual);
    }

    [Fact]
    public void RequireCurrent_throws_when_no_context_is_available()
    {
        var accessor = new CurrentFamilyContextAccessor();

        var exception = Assert.Throws<InvalidOperationException>(() => accessor.RequireCurrent());

        Assert.Equal("Current family context is not available.", exception.Message);
    }
}
