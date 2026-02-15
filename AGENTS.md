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
- Scroll keys move selection through cards; select key copies selected card payload (text or image)
- Overlay auto-hides after a configurable timeout unless settings panel is open
- Default activation hotkey is `CapsLock+D`

## Icon Assets
- Single icon source: `brain.png`
- Generated icon set script: `scripts/generate_icons.py`
- Runtime icon path: `Membrain/assets/app.ico`
- Browser/repo favicon: `favicon.ico`

## Repo / Paths
- App project: `Membrain/Membrain.csproj`
- Pack script: `scripts/pack.ps1`
- Actions check helper: `scripts/check_actions.py`

## Development Cycle (Mandatory)
1. Make a focused change set (code + required doc updates).
2. Run relevant tests/checks before finalizing the change.
3. Commit after each completed change set.
4. Push commits automatically after each completed change set; do not ask for push confirmation.
5. After each push, poll the latest GitHub Actions run every 2-3 minutes until it finishes.
6. If the run fails, fetch failing logs, implement a fix, push again, and continue the poll/fix cycle.
7. If the run succeeds, report success and wait for next instructions.

## GitHub Actions Access
- GitHub PAT for Actions API access is stored at `/home/emillfilipov/.secrets/github_pat.env` as `GITHUB_PAT`.
- Never print or log the token value.

## Notes
- This environment may not have `dotnet`; run builds on Windows or CI.
- PAT lookup for scripts supports both `~/.secrets/github_pat.env` and `~/.secrects/github_pat.env`.
