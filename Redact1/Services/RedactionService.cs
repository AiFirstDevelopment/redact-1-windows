using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Redact1.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace Redact1.Services
{
    public class RedactionService : IRedactionService
    {
        public async Task<byte[]> RedactImageAsync(byte[] imageData, List<Detection> detections, List<ManualRedaction> manualRedactions)
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream(imageData);
                using var original = new Bitmap(ms);
                using var graphics = Graphics.FromImage(original);

                var width = original.Width;
                var height = original.Height;

                // Draw approved detections as black boxes
                foreach (var detection in detections.Where(d => d.Status == "approved" && d.HasBoundingBox))
                {
                    var rect = new Rectangle(
                        (int)(detection.BboxX!.Value * width),
                        (int)(detection.BboxY!.Value * height),
                        (int)(detection.BboxWidth!.Value * width),
                        (int)(detection.BboxHeight!.Value * height)
                    );
                    graphics.FillRectangle(Brushes.Black, rect);
                }

                // Draw manual redactions as black boxes
                foreach (var redaction in manualRedactions.Where(r => r.BboxX.HasValue))
                {
                    var rect = new Rectangle(
                        (int)(redaction.BboxX!.Value * width),
                        (int)(redaction.BboxY!.Value * height),
                        (int)(redaction.BboxWidth!.Value * width),
                        (int)(redaction.BboxHeight!.Value * height)
                    );
                    graphics.FillRectangle(Brushes.Black, rect);
                }

                // Save as JPEG with 90% quality
                using var output = new MemoryStream();
                var encoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                original.Save(output, encoder, encoderParams);
                return output.ToArray();
            });
        }

        public async Task<byte[]> RedactPdfAsync(byte[] pdfData, List<Detection> detections, List<ManualRedaction> manualRedactions)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(pdfData);
                var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                var outputDoc = new PdfDocument();

                for (int pageIndex = 0; pageIndex < inputDoc.PageCount; pageIndex++)
                {
                    var pageNumber = pageIndex + 1;
                    var inputPage = inputDoc.Pages[pageIndex];
                    var outputPage = outputDoc.AddPage(inputPage);

                    using var gfx = XGraphics.FromPdfPage(outputPage);

                    var pageWidth = outputPage.Width.Point;
                    var pageHeight = outputPage.Height.Point;

                    // Draw approved detections for this page
                    var pageDetections = detections.Where(d =>
                        d.Status == "approved" &&
                        d.HasBoundingBox &&
                        (d.PageNumber == null || d.PageNumber == pageNumber));

                    foreach (var detection in pageDetections)
                    {
                        var rect = new XRect(
                            detection.BboxX!.Value * pageWidth,
                            detection.BboxY!.Value * pageHeight,
                            detection.BboxWidth!.Value * pageWidth,
                            detection.BboxHeight!.Value * pageHeight
                        );
                        gfx.DrawRectangle(XBrushes.Black, rect);
                    }

                    // Draw manual redactions for this page
                    var pageRedactions = manualRedactions.Where(r =>
                        r.BboxX.HasValue &&
                        (r.PageNumber == null || r.PageNumber == pageNumber));

                    foreach (var redaction in pageRedactions)
                    {
                        var rect = new XRect(
                            redaction.BboxX!.Value * pageWidth,
                            redaction.BboxY!.Value * pageHeight,
                            redaction.BboxWidth!.Value * pageWidth,
                            redaction.BboxHeight!.Value * pageHeight
                        );
                        gfx.DrawRectangle(XBrushes.Black, rect);
                    }
                }

                using var outputStream = new MemoryStream();
                outputDoc.Save(outputStream);
                return outputStream.ToArray();
            });
        }

        public async Task<byte[]> RenderPdfPageToImageAsync(byte[] pdfData, int pageNumber, double scale = 2.0)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(pdfData);
                var doc = PdfReader.Open(inputStream, PdfDocumentOpenMode.ReadOnly);

                if (pageNumber < 1 || pageNumber > doc.PageCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(pageNumber));
                }

                var page = doc.Pages[pageNumber - 1];
                var width = (int)(page.Width.Point * scale);
                var height = (int)(page.Height.Point * scale);

                // Create a bitmap and render the page
                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.White);
                graphics.ScaleTransform((float)scale, (float)scale);

                // Note: PdfSharp doesn't have built-in rendering to bitmap
                // In production, you'd use a library like PdfiumViewer or PDFtoImage
                // For now, return a placeholder image
                graphics.DrawString($"Page {pageNumber}", new Font("Arial", 24), Brushes.Black, 10, 10);

                using var output = new MemoryStream();
                bitmap.Save(output, ImageFormat.Png);
                return output.ToArray();
            });
        }

        public int GetPdfPageCount(byte[] pdfData)
        {
            using var stream = new MemoryStream(pdfData);
            var doc = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            return doc.PageCount;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return codecs[0];
        }
    }
}
