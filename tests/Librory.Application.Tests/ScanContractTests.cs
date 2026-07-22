using Librory.Application.Scanning;
using Xunit;

namespace Librory.Application.Tests;

public class ScanContractTests
{
    [Fact]
    public void ScanShelfRequest_has_family_and_language_fields()
    {
        var request = new ScanShelfRequest(Guid.NewGuid(), "zh", "shelf-photo.jpg");

        Assert.Equal("zh", request.PreferredLanguage);
        Assert.Equal("shelf-photo.jpg", request.ShelfPhotoPath);
    }
}
