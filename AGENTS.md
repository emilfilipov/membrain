# Membrain — Agent Notes

## Stack / Runtime
- WPF on .NET 8 (`net8.0-windows`)
- Self-contained `win-x64` publish
- Velopack for updates + installer

## Build / Publish (Windows)
```powershell
dotnet publish Membrain/Membrain.csproj -c Release -o publish -r win-x64 --self-contained true -p:Version=1.0.<build>
```

## Velopack (Windows)
- Package and upload uses channel `win`
- GitHub Actions uses `VELOPACK_TOKEN` secret

## Updates / Auth
- Update settings are editable from the in-app Settings panel
- Token is stored in Windows Credential Manager (`Membrain.UpdateToken`)

## App Behavior
- Background clipboard listener via `AddClipboardFormatListener`
- Overlay strip is topmost, side-pinned, and toggled by global hotkey
- Scroll keys move selection through cards; select key copies selected card text
- Overlay auto-hides after a configurable timeout unless settings panel is open

## Repo / Paths
- App project: `Membrain/Membrain.csproj`
- Pack script: `scripts/pack.ps1`
- Actions check helper: `scripts/check_actions.py`

## Notes
- This environment may not have `dotnet`; run builds on Windows or CI.
- PAT lookup for scripts supports both `~/.secrets/github_pat.env` and `~/.secrects/github_pat.env`.
