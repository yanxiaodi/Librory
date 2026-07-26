# 2026-07-26 Aspire API Link

- Switched the API app host registration to external HTTP endpoints so Aspire can expose the service directly instead of relying on the old fixed endpoint wiring.
- Aligned the web dev server port with the AppHost NPM endpoint and updated the Vite proxy target to the API's local launch URL.
- Added launch settings for the API and AppHost so local runs use the same ports as the Aspire wiring.
- Verified the API Scalar documentation opens successfully after the change.
