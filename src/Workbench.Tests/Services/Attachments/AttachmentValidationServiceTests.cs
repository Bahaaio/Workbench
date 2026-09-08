using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Services.Implementations;

namespace Workbench.Tests.Services.Attachments;

public class AttachmentValidationServiceTests
{
    private readonly AttachmentValidationService _service;

    public AttachmentValidationServiceTests()
    {
        _service = new AttachmentValidationService(Mock.Of<ILogger<AttachmentValidationService>>());
    }

    private static IFormFile MakeFile(long length = 100, string fileName = "test.pdf")
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.FileName).Returns(fileName);
        return mock.Object;
    }

    private static AttachmentOptions MakeOptions(
        int maxCount = 10,
        List<string>? extensions = null) =>
        new TestAttachmentOptions
        {
            MaxSizeBytesLead = 1024 * 1024,
            MaxSizeBytesMember = 512,
            MaxCount = maxCount,
            AllowedExtensions = extensions ?? [".pdf", ".jpg", ".png"]
        };

    [Fact]
    public void Validate_Passes_WhenValidFile()
    {
        var file = MakeFile(length: 100, fileName: "doc.pdf");
        var options = MakeOptions();

        _service.Validate(file, options, 1024);
    }

    [Fact]
    public void Validate_Throws_WhenFileEmpty()
    {
        var file = MakeFile(length: 0);
        var options = MakeOptions();

        var ex = Assert.Throws<BadRequestException>(() => _service.Validate(file, options, 1024));
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenFileSizeExceedsMax()
    {
        var file = MakeFile(length: 2048);
        var options = MakeOptions();

        var ex = Assert.Throws<BadRequestException>(() => _service.Validate(file, options, 1024));
        Assert.Contains("1024", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenExtensionNotAllowed()
    {
        var file = MakeFile(fileName: "script.exe");
        var options = MakeOptions();

        var ex = Assert.Throws<BadRequestException>(() => _service.Validate(file, options, 1024));
        Assert.Contains("extension", ex.Message);
    }

    [Fact]
    public void Validate_Passes_WhenExtensionAllowed()
    {
        var file = MakeFile(fileName: "image.jpg");
        var options = MakeOptions(extensions: [".jpg", ".png"]);

        _service.Validate(file, options, 1024);
    }

    [Fact]
    public void Validate_CaseSensitive_Extension()
    {
        var file = MakeFile(fileName: "doc.PDF");
        var options = MakeOptions(extensions: [".pdf"]);

        var ex = Assert.Throws<BadRequestException>(() => _service.Validate(file, options, 1024));
        Assert.Contains("extension", ex.Message);
    }

    [Fact]
    public void Validate_Passes_WhenExtensionMatchesExactly()
    {
        var file = MakeFile(fileName: "doc.pdf");
        var options = MakeOptions(extensions: [".pdf"]);

        _service.Validate(file, options, 1024);
    }

    [Fact]
    public void ValidateCount_Passes_WhenUnderLimit()
    {
        _service.ValidateCount(5, 10);
    }

    [Fact]
    public void ValidateCount_Passes_WhenAtLimit()
    {
        _service.ValidateCount(10, 10);
    }

    [Fact]
    public void ValidateCount_Throws_WhenOverLimit()
    {
        var ex = Assert.Throws<BadRequestException>(() => _service.ValidateCount(11, 10));
        Assert.Contains("10", ex.Message);
    }

    [Fact]
    public void ValidateCount_Passes_WhenZeroAttachments()
    {
        _service.ValidateCount(0, 5);
    }

    [Fact]
    public void ValidateCount_Throws_WhenOneOverLimit()
    {
        var ex = Assert.Throws<BadRequestException>(() => _service.ValidateCount(6, 5));
        Assert.Contains("5", ex.Message);
    }

    private class TestAttachmentOptions : AttachmentOptions;
}
