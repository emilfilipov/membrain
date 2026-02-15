# Membrain

Membrain is a Windows desktop clipboard strip app.

## Core behavior
- Runs in background and listens to clipboard changes.
- Opens a thin topmost vertical strip with a global activation hotkey.
- Shows clipboard entries as cards.
- Supports keyboard scroll keys for history navigation.
- Auto-hides after configured timeout (unless Settings is open).

## Settings (cogwheel)
- Activation hotkey
- Screen side (left/right)
- Scroll up/down keys
- Select key (copies selected card into clipboard)
- Retained clipboard items (default `15`)
- Auto-hide timeout in seconds
- Update repo/token configuration

## Build (Windows)
```powershell
dotnet publish Membrain/Membrain.csproj -c Release -r win-x64 --self-contained true
```

See `INSTALLER.md` for Velopack packaging and release automation.
