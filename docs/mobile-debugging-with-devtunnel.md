# Mobile Debugging With Dev Tunnel

This guide shows how to open the Librory web app on a phone while the app still runs locally under Aspire.

## What To Run Locally

Keep Aspire running as usual:

```powershell
dotnet run --project src/Librory.AppHost
```

The web app is still served from the local Vite dev server on port `5180`, and the API stays on the local Aspire wiring.

## Install Dev Tunnel

On Windows, install the CLI with `winget`:

```powershell
winget install Microsoft.devtunnel
```

After installation, confirm it is on the PATH:

```powershell
devtunnel --version
```

## Create A Persistent Tunnel

Create the tunnel once:

```powershell
devtunnel user login
devtunnel create --allow-anonymous
```

Then host the web port:

```powershell
devtunnel host -p 5180
```

If you do not run `devtunnel create` first, `devtunnel host` will create a temporary tunnel instead of a persistent one.

## Open On Phone

Use the tunnel URL that `devtunnel host` prints and open it on the phone browser.

The phone should load the Librory web app through the tunnel, while the browser-side requests still proxy back to the local API through Vite.

## Where Uploaded Photos Go

Shelf photos are stored under the API project root in a local `scan-uploads` folder during development.

On this repo, that is under:

```text
scan-uploads
```

The location comes from `ScanStorage:TemporaryRoot` in `src/Librory.Api/appsettings.Development.json`, so you can override it per environment if needed.
For production deployments, set the same `ScanStorage:TemporaryRoot` key through `appsettings.json` or environment variables before startup.

Each upload gets a timestamped file name, and the cleanup job deletes expired files later.

## Google Login Callback

For Google sign-in, register this redirect URI in Google Cloud Console:

```text
https://<your-devtunnel-url>/signin-google
```

The app uses `/auth/google/start` to begin login, and the Google callback path in the API is `/signin-google`.

If you also want Microsoft sign-in later, the matching callback path is `/signin-microsoft`.

## If Login Returns To Localhost

If the login flow succeeds but the browser comes back to `localhost`, the API is probably not seeing the tunnel as the real public host.

Librory handles this by reading forwarded proxy headers before authentication runs:

- `X-Forwarded-Host`
- `X-Forwarded-Proto`
- `X-Forwarded-For`

That keeps tunnel-based login on the tunnel URL while still allowing normal local development to work unchanged.

## Notes

- A persistent dev tunnel can keep the same URL across stop and start cycles as long as you recreate or reuse the same tunnel.
- Dev tunnels are private by default, so `--allow-anonymous` is the simplest choice when you want a phone browser to open the link directly.
- If the phone cannot open the tunnel URL, verify the tunnel is still hosted and that the phone is online.
- Direct local development on `localhost` still works because forwarded headers only matter when a proxy or tunnel adds them.
- The scan upload storage path is intended for local development only; production deployments should set an explicit storage root.

## Production Deployment

If you deploy Librory outside local development, make sure these settings are provided before startup:

- `ScanStorage:TemporaryRoot`: a writable, persistent directory for uploaded scan photos.
- `Scanning:PhotoRetentionDays`: how long temporary scan photos should be kept.
- `Scanning:CleanupIntervalHours`: how often the cleanup job runs.
- `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret`: required if Google login is enabled.
- `Authentication:Microsoft:ClientId` and `Authentication:Microsoft:ClientSecret`: required if Microsoft login is enabled.

Recommended storage examples:

- Windows: `C:\Librory\scan-uploads`
- Linux: `/var/lib/librory/scan-uploads`

For Google sign-in, register the public callback URL that matches your deployment host:

```text
https://<your-public-host>/signin-google
```

If you expose Microsoft sign-in, register the matching callback:

```text
https://<your-public-host>/signin-microsoft
```

The production app does not trust forwarded headers by default, so it should be reached at its real public URL rather than through the dev tunnel workflow described above.
