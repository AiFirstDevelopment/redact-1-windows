using Redact1.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Redact1.Services
{
    public class DetectionService : IDetectionService
    {
        // PII Detection Patterns
        private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new(@"(\(\d{3}\)\s?|\b\d{3}[-.])\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
        private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex DobPattern = new(@"\b(0[1-9]|1[0-2])[/\-](0[1-9]|[12]\d|3[01])[/\-](19|20)\d{2}\b", RegexOptions.Compiled);
        private static readonly Regex AddressPattern = new(@"\b\d+\s+[\w\s]+(?:Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Drive|Dr|Lane|Ln|Court|Ct|Way|Place|Pl)\.?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // License plate: 5-8 characters total (common US format)
        private static readonly Regex LicensePlatePattern = new(@"\b[A-Z]{2,4}\d{2,5}\b|\b\d{1,3}[A-Z]{2,4}\d{1,4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string? _tesseractPath;

        public DetectionService()
        {
            // Look for tesseract executable
            var possiblePaths = new[]
            {
                // macOS (Homebrew)
                "/opt/homebrew/bin/tesseract",
                "/usr/local/bin/tesseract",
                // Linux
                "/usr/bin/tesseract",
                // Windows
                @"C:\Program Files\Tesseract-OCR\tesseract.exe",
                @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
            };

            _tesseractPath = possiblePaths.FirstOrDefault(File.Exists);

            // If not found, check if in PATH
            if (_tesseractPath == null)
            {
                try
                {
                    var psi = new ProcessStartInfo("tesseract", "--version")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(2000);
                    if (proc?.ExitCode == 0)
                    {
                        _tesseractPath = "tesseract";
                    }
                }
                catch { /* Not in PATH */ }
            }

            Console.WriteLine($"[Detection] Tesseract path: {_tesseractPath ?? "NOT FOUND"}");
        }

        public async Task<List<CreateDetectionRequest>> DetectInImageAsync(byte[] imageData)
        {
            // Write marker file to confirm detection is called
            await File.WriteAllTextAsync("/tmp/detection_started.txt", $"Detection started at {DateTime.Now}");

            var detections = new List<CreateDetectionRequest>();

            if (_tesseractPath == null)
            {
                Console.WriteLine("[Detection] Tesseract not found, skipping OCR");
                return detections;
            }

            try
            {
                // Get image dimensions
                using var ms = new MemoryStream(imageData);
                using var image = Image.Load<Rgba32>(ms);
                var imageWidth = image.Width;
                var imageHeight = image.Height;

                // Save to temp file for tesseract
                var tempInputPath = Path.Combine(Path.GetTempPath(), $"tesseract_input_{Guid.NewGuid()}.png");
                var tempOutputPath = Path.Combine(Path.GetTempPath(), $"tesseract_output_{Guid.NewGuid()}");

                try
                {
                    // Save image as PNG
                    await image.SaveAsPngAsync(tempInputPath);

                    // Run tesseract with TSV output for bounding boxes
                    var psi = new ProcessStartInfo(_tesseractPath, $"\"{tempInputPath}\" \"{tempOutputPath}\" -l eng tsv")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                    {
                        Console.WriteLine("[Detection] Failed to start tesseract process");
                        return detections;
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine($"[Detection] Tesseract error: {error}");
                        return detections;
                    }

                    // Read TSV output
                    var tsvPath = tempOutputPath + ".tsv";
                    if (!File.Exists(tsvPath))
                    {
                        Console.WriteLine("[Detection] Tesseract TSV output not found");
                        return detections;
                    }

                    var lines = await File.ReadAllLinesAsync(tsvPath);
                    Console.WriteLine($"[Detection] TSV has {lines.Length} lines");

                    // Save a copy for debugging
                    Console.WriteLine($"[Detection] Saving TSV to /tmp/tesseract_debug.tsv");
                    await File.WriteAllLinesAsync("/tmp/tesseract_debug.tsv", lines);
                    Console.WriteLine($"[Detection] TSV saved successfully");

                    // Build a list of words with bounding boxes
                    var words = new List<(string Text, int Left, int Top, int Width, int Height)>();

                    // Parse TSV (header: level page_num block_num par_num line_num word_num left top width height conf text)
                    foreach (var line in lines.Skip(1)) // Skip header
                    {
                        var parts = line.Split('\t');
                        if (parts.Length < 12)
                        {
                            Console.WriteLine($"[Detection] Skipping line with {parts.Length} parts: {line}");
                            continue;
                        }

                        var confidence = double.TryParse(parts[10], out var conf) ? conf : 0;
                        var text = parts[11].Trim();

                        if (string.IsNullOrWhiteSpace(text) || confidence < 30) continue;

                        var left = int.TryParse(parts[6], out var l) ? l : 0;
                        var top = int.TryParse(parts[7], out var t) ? t : 0;
                        var width = int.TryParse(parts[8], out var w) ? w : 0;
                        var height = int.TryParse(parts[9], out var h) ? h : 0;

                        words.Add((text, left, top, width, height));

                        // Check individual word for PII patterns
                        var detection = TryMatchPii(text, left, top, width, height, imageWidth, imageHeight);
                        if (detection != null)
                        {
                            detections.Add(detection);
                        }
                    }

                    // Also check combined text for multi-word patterns
                    var fullText = string.Join(" ", words.Select(w => w.Text));
                    Console.WriteLine($"[Detection] Full text ({fullText.Length} chars): {fullText.Substring(0, Math.Min(200, fullText.Length))}...");

                    // Find patterns in full text and try to locate them
                    FindPatternsWithLocation(SsnPattern, "ssn", fullText, words, imageWidth, imageHeight, detections);
                    FindPatternsWithLocation(EmailPattern, "email", fullText, words, imageWidth, imageHeight, detections);
                    FindPatternsWithLocation(DobPattern, "dob", fullText, words, imageWidth, imageHeight, detections);
                    FindPatternsWithLocation(PhonePattern, "phone", fullText, words, imageWidth, imageHeight, detections);
                    FindPatternsWithLocation(AddressPattern, "address", fullText, words, imageWidth, imageHeight, detections);

                    Console.WriteLine($"[Detection] Found {detections.Count} detections");
                }
                finally
                {
                    // Cleanup temp files
                    try { File.Delete(tempInputPath); } catch { }
                    try { File.Delete(tempOutputPath + ".tsv"); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Detection] OCR error: {ex.Message}");
                Console.WriteLine($"[Detection] Stack: {ex.StackTrace}");
            }

            return detections;
        }

        private void FindPatternsWithLocation(
            Regex pattern,
            string type,
            string fullText,
            List<(string Text, int Left, int Top, int Width, int Height)> words,
            int imageWidth,
            int imageHeight,
            List<CreateDetectionRequest> detections)
        {
            foreach (Match match in pattern.Matches(fullText))
            {
                // Check if already found
                if (detections.Any(d => d.TextContent == match.Value)) continue;

                // Try to find bounding box by matching words
                var matchWords = match.Value.Split(new[] { ' ', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
                var firstWord = matchWords.FirstOrDefault();
                var lastWord = matchWords.LastOrDefault();

                (string Text, int Left, int Top, int Width, int Height)? firstWordBox = null;
                (string Text, int Left, int Top, int Width, int Height)? lastWordBox = null;

                foreach (var word in words)
                {
                    if (firstWord != null && word.Text.Contains(firstWord, StringComparison.OrdinalIgnoreCase))
                    {
                        firstWordBox = word;
                    }
                    if (lastWord != null && word.Text.Contains(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        lastWordBox = word;
                    }
                }

                if (firstWordBox != null)
                {
                    var left = firstWordBox.Value.Left;
                    var top = firstWordBox.Value.Top;
                    var right = lastWordBox?.Left + lastWordBox?.Width ?? firstWordBox.Value.Left + firstWordBox.Value.Width;
                    var bottom = Math.Max(firstWordBox.Value.Top + firstWordBox.Value.Height,
                                         lastWordBox?.Top + lastWordBox?.Height ?? 0);

                    var width = right - left;
                    var height = bottom - top;

                    if (width > 0 && height > 0)
                    {
                        detections.Add(new CreateDetectionRequest
                        {
                            DetectionType = type,
                            TextContent = match.Value,
                            BboxX = (double)left / imageWidth,
                            BboxY = 1.0 - ((double)(top + height) / imageHeight), // Flip Y for PDF coordinates
                            BboxWidth = (double)width / imageWidth,
                            BboxHeight = (double)height / imageHeight,
                            Confidence = 0.85
                        });
                    }
                }
            }
        }

        public async Task<List<CreateDetectionRequest>> DetectInPdfPageAsync(byte[] pageImageData, int pageNumber)
        {
            var detections = await DetectInImageAsync(pageImageData);

            foreach (var detection in detections)
            {
                detection.PageNumber = pageNumber;
            }

            return detections;
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            if (_tesseractPath == null) return string.Empty;

            try
            {
                var tempInputPath = Path.Combine(Path.GetTempPath(), $"tesseract_input_{Guid.NewGuid()}.png");
                var tempOutputPath = Path.Combine(Path.GetTempPath(), $"tesseract_output_{Guid.NewGuid()}");

                try
                {
                    await File.WriteAllBytesAsync(tempInputPath, imageData);

                    var psi = new ProcessStartInfo(_tesseractPath, $"\"{tempInputPath}\" \"{tempOutputPath}\" -l eng")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return string.Empty;

                    await process.WaitForExitAsync();
                    return await File.ReadAllTextAsync(tempOutputPath + ".txt");
                }
                finally
                {
                    try { File.Delete(tempInputPath); } catch { }
                    try { File.Delete(tempOutputPath + ".txt"); } catch { }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private CreateDetectionRequest? TryMatchPii(string text, int left, int top, int width, int height, int imageWidth, int imageHeight)
        {
            string? type = null;

            if (SsnPattern.IsMatch(text))
                type = "ssn";
            else if (EmailPattern.IsMatch(text))
                type = "email";
            else if (PhonePattern.IsMatch(text))
                type = "phone";
            else if (DobPattern.IsMatch(text))
                type = "dob";

            if (type == null) return null;

            // Convert to normalized coordinates with Y flip for PDF
            return new CreateDetectionRequest
            {
                DetectionType = type,
                TextContent = text,
                BboxX = (double)left / imageWidth,
                BboxY = 1.0 - ((double)(top + height) / imageHeight), // Flip Y
                BboxWidth = (double)width / imageWidth,
                BboxHeight = (double)height / imageHeight,
                Confidence = 0.95
            };
        }

        /// <summary>
        /// Detect PII patterns in plain text (for unit testing regex patterns)
        /// </summary>
        public List<CreateDetectionRequest> DetectPiiInText(string text)
        {
            var detections = new List<CreateDetectionRequest>();

            if (string.IsNullOrEmpty(text)) return detections;

            // SSN
            foreach (Match match in SsnPattern.Matches(text))
            {
                detections.Add(new CreateDetectionRequest
                {
                    DetectionType = "ssn",
                    TextContent = match.Value,
                    TextStart = match.Index,
                    TextEnd = match.Index + match.Length,
                    Confidence = 0.95
                });
            }

            // Phone
            foreach (Match match in PhonePattern.Matches(text))
            {
                detections.Add(new CreateDetectionRequest
                {
                    DetectionType = "phone",
                    TextContent = match.Value,
                    TextStart = match.Index,
                    TextEnd = match.Index + match.Length,
                    Confidence = 0.95
                });
            }

            // Email
            foreach (Match match in EmailPattern.Matches(text))
            {
                detections.Add(new CreateDetectionRequest
                {
                    DetectionType = "email",
                    TextContent = match.Value,
                    TextStart = match.Index,
                    TextEnd = match.Index + match.Length,
                    Confidence = 0.95
                });
            }

            // DOB
            foreach (Match match in DobPattern.Matches(text))
            {
                detections.Add(new CreateDetectionRequest
                {
                    DetectionType = "dob",
                    TextContent = match.Value,
                    TextStart = match.Index,
                    TextEnd = match.Index + match.Length,
                    Confidence = 0.95
                });
            }

            // License Plate (minimum 5 characters)
            foreach (Match match in LicensePlatePattern.Matches(text))
            {
                if (match.Value.Length >= 5)
                {
                    detections.Add(new CreateDetectionRequest
                    {
                        DetectionType = "plate",
                        TextContent = match.Value,
                        TextStart = match.Index,
                        TextEnd = match.Index + match.Length,
                        Confidence = 0.95
                    });
                }
            }

            // Address
            foreach (Match match in AddressPattern.Matches(text))
            {
                detections.Add(new CreateDetectionRequest
                {
                    DetectionType = "address",
                    TextContent = match.Value,
                    TextStart = match.Index,
                    TextEnd = match.Index + match.Length,
                    Confidence = 0.90
                });
            }

            return detections;
        }
    }
}
