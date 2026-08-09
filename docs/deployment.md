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

The book recognition job flow uses a Microsoft Agent Framework vision agent, running in-process in the API, to extract
structured book candidates directly from the shelf photo, then enriches and re-ranks them with Google Books. There is no
separate OCR service in this flow.

To enable it, provide:

- `AgentFramework:AzureOpenAI:Endpoint`
- `AgentFramework:AzureOpenAI:ApiKey`
- `AgentFramework:AzureOpenAI:DeploymentName`

Required Azure resources:

- One Azure OpenAI resource with a vision-capable chat model deployed (used for structured candidate extraction)

For local development, put the same keys in user secrets instead of checking real values into `appsettings.json`. If these
values are left empty, the API still starts; recognition jobs simply return zero candidates and the API logs a warning.

### Local Recognition Test Setup

If you want to test the recognition flow locally with a real Azure OpenAI resource:

- Deploy a vision-capable chat model (e.g. a GPT-4o-class model) and record the deployment name
- Store the endpoint, API key, and deployment name in user secrets or environment variables, not in source control

Minimum values to set locally:

- `AgentFramework:AzureOpenAI:Endpoint`
- `AgentFramework:AzureOpenAI:ApiKey`
- `AgentFramework:AzureOpenAI:DeploymentName`

If you do not want to create an Azure OpenAI resource yet, you can still test the scan-session and intake flow by supplying
candidates manually in API tests or by using the existing UI after recognition results are mocked.

If a photo upload succeeds but the UI keeps polling and never shows completed results, check the API logs for the recognition job processor. A queued job should normally move to running, then either succeed with candidates or fail with a visible failure message.

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
  "AgentFramework": {
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
