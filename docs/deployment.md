# Deployment

This document lists the configuration Librory needs when you deploy it outside local development.

## Required Configuration

### Database

Provide one of these before startup:

- `ConnectionStrings:LibroryDb`
- `LIBRORY_DATABASE_URL`

### Scan Storage

Provide a writable, persistent directory for temporary shelf photos:

- `ScanStorage:TemporaryRoot`

Recommended examples:

- Windows: `C:\Librory\scan-uploads`
- Linux: `/var/lib/librory/scan-uploads`

### Scan Cleanup

These control retention and cleanup cadence:

- `Scanning:PhotoRetentionDays`
- `Scanning:CleanupIntervalHours`

### Authentication

If Google sign-in is enabled, provide:

- `Authentication:Google:ClientId`
- `Authentication:Google:ClientSecret`

If Microsoft sign-in is enabled, provide:

- `Authentication:Microsoft:ClientId`
- `Authentication:Microsoft:ClientSecret`

### Google Books

If you want the book metadata search endpoint to use Google Books reliably in deployment, provide:

- `GoogleBooks:ApiKey`

If the key is omitted, the app will still start, but the provider may be subject to anonymous request limits or rejection depending on Google Books API policy.

### Book Recognition

If you want the book recognition job flow to extract text and fall back to vision-based interpretation, provide:

- `Recognition:AzureVision:Endpoint`
- `Recognition:AzureVision:ApiKey`
- `Recognition:AzureOpenAI:Endpoint`
- `Recognition:AzureOpenAI:ApiKey`
- `Recognition:AzureOpenAI:DeploymentName`

Required Azure resources:

- One Azure AI Vision resource for OCR
- One Azure OpenAI resource with a vision-capable chat model deployed for fallback interpretation

For local development, put the same keys in user secrets instead of checking real values into `appsettings.json`.

## Redirect URIs

Register the public callback URLs that match your deployment host:

```text
https://<your-public-host>/signin-google
https://<your-public-host>/signin-microsoft
```

## Example appsettings override

Use environment-specific configuration or environment variables to override the defaults.

```json
{
  "ConnectionStrings": {
    "LibroryDb": "Host=...;Database=...;Username=...;Password=..."
  },
  "ScanStorage": {
    "TemporaryRoot": "/var/lib/librory/scan-uploads"
  },
  "Scanning": {
    "PhotoRetentionDays": 7,
    "CleanupIntervalHours": 24
  },
  "Authentication": {
    "Google": {
      "ClientId": "...",
      "ClientSecret": "..."
    }
  },
  "GoogleBooks": {
    "ApiKey": "..."
  },
  "Recognition": {
    "AzureVision": {
      "Endpoint": "https://<vision-resource>.cognitiveservices.azure.com/",
      "ApiKey": "..."
    },
    "AzureOpenAI": {
      "Endpoint": "https://<openai-resource>.openai.azure.com/",
      "ApiKey": "...",
      "DeploymentName": "..."
    }
  }
}
```

## Notes

- `ScanStorage:TemporaryRoot` must point to a path the API process can write to.
- The current app only trusts forwarded headers in Development, so production should be reached at its real public URL.
- If you enable only one external login provider, you only need the matching section.
- Keep the recognition configuration and Azure resource names together in this file so deployment checklists stay in one place.
