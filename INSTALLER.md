# Windows Installer (Velopack)

## One-time setup (Windows 11)
1. Install .NET SDK 8.0.
2. Install Velopack CLI:
   `dotnet tool install -g vpk --version 0.0.1298`
   If already installed:
   `dotnet tool update -g vpk --version 0.0.1298`

## Build + package
From repo root:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/pack.ps1 -Version 1.0.0
```

This publishes the app to `publish/` and creates a Velopack release in `Releases/`.

The `Releases/` folder contains:
- `Setup.exe` (installer)
- `Membrain-<version>-full.nupkg`
- `RELEASES`

## Test install locally
Run `Releases/Setup.exe` on Windows. The app installs to `%LOCALAPPDATA%\Membrain` and auto-update support is enabled.

## Update source configuration (runtime)
Update settings are available in the app Settings panel:
- GitHub repo URL
- Access token (stored in Windows Credential Manager)
- Include pre-releases

You can also configure via env vars:
- `MEMBRAIN_GITHUB_REPO`
- `MEMBRAIN_GITHUB_TOKEN`
- `MEMBRAIN_PRERELEASE`
- `MEMBRAIN_UPDATE_URL`

## GitHub Actions release
The workflow `.github/workflows/release.yml` runs on pushes to `main` and creates version `1.0.<run_number>` automatically.

It requires one secret:
- `VELOPACK_TOKEN` (or `velopack_token`)
