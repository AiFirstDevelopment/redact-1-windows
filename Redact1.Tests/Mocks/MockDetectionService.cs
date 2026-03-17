using Moq;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Mocks;

public static class MockDetectionService
{
    public static Mock<IDetectionService> Create()
    {
        var mock = new Mock<IDetectionService>();

        mock.Setup(x => x.DetectInImageAsync(It.IsAny<byte[]>()))
            .ReturnsAsync(new List<CreateDetectionRequest>
            {
                new CreateDetectionRequest
                {
                    DetectionType = "ssn",
                    BboxX = 10,
                    BboxY = 20,
                    BboxWidth = 100,
                    BboxHeight = 20,
                    Confidence = 0.95
                }
            });

        mock.Setup(x => x.DetectInPdfPageAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CreateDetectionRequest>
            {
                new CreateDetectionRequest
                {
                    DetectionType = "phone",
                    BboxX = 50,
                    BboxY = 100,
                    BboxWidth = 80,
                    BboxHeight = 15,
                    Confidence = 0.90,
                    PageNumber = 1
                }
            });

        return mock;
    }
}
