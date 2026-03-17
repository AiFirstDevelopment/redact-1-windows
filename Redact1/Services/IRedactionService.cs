using Redact1.Models;

namespace Redact1.Services
{
    public interface IRedactionService
    {
        Task<byte[]> RedactImageAsync(byte[] imageData, List<Detection> detections, List<ManualRedaction> manualRedactions);
        Task<byte[]> RedactPdfAsync(byte[] pdfData, List<Detection> detections, List<ManualRedaction> manualRedactions);
        Task<byte[]> RenderPdfPageToImageAsync(byte[] pdfData, int pageNumber, double scale = 2.0);
        int GetPdfPageCount(byte[] pdfData);
    }
}
