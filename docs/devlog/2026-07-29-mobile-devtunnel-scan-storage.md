# 2026-07-29 Mobile Dev Tunnel and Scan Storage

- Added a mobile debugging guide for Aspire + dev tunnel flows, including Windows installation, persistent tunnel setup, and Google redirect URI notes.
- Fixed the login return path when the app is accessed through a tunnel by enabling forwarded headers before authentication so the API respects the public host and scheme.
- Moved temporary shelf photo storage from the system temp folder to the repo-root `scan-uploads/` directory so uploaded files are easier to inspect during local debugging.
- Added `scan-uploads/` to `.gitignore` and updated the scan upload/cleanup tests to assert the new storage location.
- Verified the scan upload and cleanup tests pass after the storage path change.
