using Redact1.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Redact1.Services
{
    public class DetectionService : IDetectionService
    {
        // PII Detection Patterns (matching iOS implementation)
        private static readonly Regex SsnPattern = new(@"\d{3}-\d{2}-\d{4}", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new(@"(\(\d{3}\)\s?|\d{3}[-.])\d{3}[-.]?\d{4}", RegexOptions.Compiled);
        private static readonly Regex EmailPattern = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex DobPattern = new(@"(0[1-9]|1[0-2])[/\-](0[1-9]|[12]\d|3[01])[/\-](19|20)\d{2}", RegexOptions.Compiled);
        private static readonly Regex LicensePlatePattern = new(@"\b[A-Z0-9]{5,8}\b", RegexOptions.Compiled);

        public async Task<List<CreateDetectionRequest>> DetectInImageAsync(byte[] imageData)
        {
            var detections = new List<CreateDetectionRequest>();

            await Task.Run(() =>
            {
                using var ms = new MemoryStream(imageData);
                using var bitmap = new Bitmap(ms);

                // Face Detection using OpenCV/Emgu.CV
                var faceDetections = DetectFaces(bitmap);
                detections.AddRange(faceDetections);

                // OCR and PII Detection
                var text = PerformOcr(bitmap);
                var piiDetections = DetectPiiInText(text, bitmap.Width, bitmap.Height);
                detections.AddRange(piiDetections);

                // License Plate Detection (simplified - uses text-based detection)
                var plateDetections = DetectLicensePlates(text, bitmap.Width, bitmap.Height);
                detections.AddRange(plateDetections);
            });

            return detections;
        }

        public async Task<List<CreateDetectionRequest>> DetectInPdfPageAsync(byte[] pageImageData, int pageNumber)
        {
            var detections = await DetectInImageAsync(pageImageData);

            // Add page number to all detections
            foreach (var detection in detections)
            {
                detection.PageNumber = pageNumber;
            }

            return detections;
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream(imageData);
                using var bitmap = new Bitmap(ms);
                return PerformOcr(bitmap);
            });
        }

        private List<CreateDetectionRequest> DetectFaces(Bitmap bitmap)
        {
            var detections = new List<CreateDetectionRequest>();

            try
            {
                // Using Emgu.CV for face detection
                using var image = BitmapToMat(bitmap);

                // Load Haar cascade for face detection
                var cascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");

                if (File.Exists(cascadePath))
                {
                    using var faceCascade = new Emgu.CV.CascadeClassifier(cascadePath);
                    using var grayImage = new Emgu.CV.Mat();
                    Emgu.CV.CvInvoke.CvtColor(image, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                    var faces = faceCascade.DetectMultiScale(
                        grayImage,
                        scaleFactor: 1.1,
                        minNeighbors: 5,
                        minSize: new Size(30, 30)
                    );

                    foreach (var face in faces)
                    {
                        detections.Add(new CreateDetectionRequest
                        {
                            DetectionType = "face",
                            BboxX = (double)face.X / bitmap.Width,
                            BboxY = (double)face.Y / bitmap.Height,
                            BboxWidth = (double)face.Width / bitmap.Width,
                            BboxHeight = (double)face.Height / bitmap.Height,
                            Confidence = 0.9
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Face detection error: {ex.Message}");
            }

            return detections;
        }

        private Emgu.CV.Mat BitmapToMat(Bitmap bitmap)
        {
            var mat = new Emgu.CV.Mat();
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;
            var bytes = ms.ToArray();
            Emgu.CV.CvInvoke.Imdecode(bytes, Emgu.CV.CvEnum.ImreadModes.Color, mat);
            return mat;
        }

        private string PerformOcr(Bitmap bitmap)
        {
            try
            {
                // Using Tesseract for OCR
                var tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                if (!Directory.Exists(tessDataPath))
                {
                    return string.Empty;
                }

                using var engine = new Tesseract.TesseractEngine(tessDataPath, "eng", Tesseract.EngineMode.Default);
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                using var pix = Tesseract.Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);

                return page.GetText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR error: {ex.Message}");
                return string.Empty;
            }
        }

        private List<CreateDetectionRequest> DetectPiiInText(string text, int imageWidth, int imageHeight)
        {
            var detections = new List<CreateDetectionRequest>();

            // SSN Detection
            foreach (Match match in SsnPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("ssn", match, text));
            }

            // Phone Detection
            foreach (Match match in PhonePattern.Matches(text))
            {
                detections.Add(CreateTextDetection("phone", match, text));
            }

            // Email Detection
            foreach (Match match in EmailPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("email", match, text));
            }

            // DOB Detection
            foreach (Match match in DobPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("dob", match, text));
            }

            return detections;
        }

        private List<CreateDetectionRequest> DetectLicensePlates(string text, int imageWidth, int imageHeight)
        {
            var detections = new List<CreateDetectionRequest>();

            foreach (Match match in LicensePlatePattern.Matches(text))
            {
                // Simple heuristic: must contain both letters and numbers
                var value = match.Value;
                bool hasLetter = value.Any(char.IsLetter);
                bool hasDigit = value.Any(char.IsDigit);

                if (hasLetter && hasDigit && value.Length >= 5 && value.Length <= 8)
                {
                    detections.Add(CreateTextDetection("plate", match, text));
                }
            }

            return detections;
        }

        private CreateDetectionRequest CreateTextDetection(string type, Match match, string fullText)
        {
            return new CreateDetectionRequest
            {
                DetectionType = type,
                TextContent = match.Value,
                TextStart = match.Index,
                TextEnd = match.Index + match.Length,
                Confidence = 0.95
            };
        }
    }
}
