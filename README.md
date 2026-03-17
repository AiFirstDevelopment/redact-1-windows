# Redact-1 for Windows

A Windows desktop application for police records redaction, designed for FOIA/public records requests. This is the Windows port of the iOS Redact-1 application.

## Features

- **Records Request Management**: Create, edit, and track FOIA records requests
- **File Upload**: Support for images (JPG, PNG) and PDF documents
- **Auto-Detection**: Automatically detect sensitive information:
  - Faces (using OpenCV)
  - License plates
  - Social Security Numbers (SSN)
  - Phone numbers
  - Email addresses
  - Dates of Birth
- **Manual Redaction**: Draw custom redaction boxes on documents
- **Review Workflow**: Approve or reject detected items before redaction
- **Export**: Generate redacted files with audit trails
- **User Management**: Role-based access (Clerk and Supervisor roles)
- **Multi-Tenancy**: Support for multiple agencies

## Requirements

- Windows 10 or later
- .NET 8.0 SDK
- Visual Studio 2022 (recommended) or VS Code with C# extension

## Dependencies

The application uses the following NuGet packages:

- **CommunityToolkit.Mvvm** - MVVM framework
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **PdfSharp** - PDF manipulation
- **Emgu.CV** - OpenCV wrapper for face detection
- **Tesseract** - OCR for text extraction
- **System.IdentityModel.Tokens.Jwt** - JWT authentication

## Building

1. Clone the repository
2. Open `Redact1.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution

```bash
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project Redact1
```

Or press F5 in Visual Studio.

## Configuration

The application connects to the same backend as the iOS app:

```
https://redact-1-worker.joelstevick.workers.dev
```

This can be configured in `appsettings.json`.

## Test Credentials

For development/testing:
- **Email**: clerk@pd.local
- **Password**: test-password

## Project Structure

```
Redact1/
├── Models/           # Data models (User, Request, File, Detection, etc.)
├── Services/         # Business logic services
│   ├── ApiService    # REST API client
│   ├── AuthService   # Authentication handling
│   ├── DetectionService  # Face/PII detection
│   └── RedactionService  # Image/PDF redaction
├── ViewModels/       # MVVM ViewModels
├── Views/            # WPF XAML views
├── Styles/           # UI styles and themes
├── Converters/       # XAML value converters
└── Assets/           # Application assets
```

## Backend

The Windows app uses the same Cloudflare Workers backend as the iOS app, providing:
- D1 database for data storage
- R2 for file storage
- JWT authentication

## API Endpoints

All API endpoints are documented in the iOS repository. Key endpoints:

- `POST /api/auth/login` - User authentication
- `GET /api/requests` - List records requests
- `POST /api/requests/:id/files` - Upload files
- `GET /api/files/:id/detections` - Get detections
- `POST /api/requests/:id/export` - Create export

## Detection Patterns

The following PII patterns are detected:

| Type | Pattern Example |
|------|-----------------|
| SSN | 123-45-6789 |
| Phone | (555) 123-4567 |
| Email | user@example.com |
| DOB | 01/15/1990 |
| License Plate | ABC1234 |

## License

Proprietary - All rights reserved
