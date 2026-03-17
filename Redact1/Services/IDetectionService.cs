using Redact1.Models;
using System.Drawing;

namespace Redact1.Services
{
    public interface IDetectionService
    {
        Task<List<CreateDetectionRequest>> DetectInImageAsync(byte[] imageData);
        Task<List<CreateDetectionRequest>> DetectInPdfPageAsync(byte[] pageImageData, int pageNumber);
        Task<string> ExtractTextAsync(byte[] imageData);
    }

    public class DetectedRegion
    {
        public string Type { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Confidence { get; set; }
        public string? TextContent { get; set; }
        public int? TextStart { get; set; }
        public int? TextEnd { get; set; }
    }
}
