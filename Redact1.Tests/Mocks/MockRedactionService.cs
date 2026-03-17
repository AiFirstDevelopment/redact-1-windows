using Moq;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Mocks;

public static class MockRedactionService
{
    public static Mock<IRedactionService> Create()
    {
        var mock = new Mock<IRedactionService>();

        // Return a simple 1x1 black pixel PNG for image redaction
        var blackPixelPng = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
            0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x60, 0x60, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x01, 0x5C, 0xCD, 0xFF, 0x69, 0x00, 0x00, 0x00,
            0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };

        mock.Setup(x => x.RedactImageAsync(
                It.IsAny<byte[]>(),
                It.IsAny<List<Detection>>(),
                It.IsAny<List<ManualRedaction>>()))
            .ReturnsAsync(blackPixelPng);

        mock.Setup(x => x.RedactPdfAsync(
                It.IsAny<byte[]>(),
                It.IsAny<List<Detection>>(),
                It.IsAny<List<ManualRedaction>>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

        mock.Setup(x => x.GetPdfPageCount(It.IsAny<byte[]>()))
            .Returns(1);

        mock.Setup(x => x.RenderPdfPageToImageAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(blackPixelPng);

        return mock;
    }
}
