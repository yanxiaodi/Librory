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
  }
}
```

## Notes

- `ScanStorage:TemporaryRoot` must point to a path the API process can write to.
- The current app only trusts forwarded headers in Development, so production should be reached at its real public URL.
- If you enable only one external login provider, you only need the matching section.
