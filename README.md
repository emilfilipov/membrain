# Membrain

Membrain is a Windows desktop clipboard strip app.

## Core behavior
- Runs in background and listens to clipboard changes.
- Opens a thin topmost vertical strip with a global activation hotkey.
- Shows clipboard entries as cards (text and images with thumbnails).
- Supports keyboard scroll keys for history navigation.
- Supports configurable strip/settings shortcuts (default strip: `A` settings, `S` select, `D` hide; settings: `A` save, `S` update, `D` back).
- Auto-hides after configured timeout (unless Settings is open).

## Settings (cogwheel)
- Activation hotkey
- Screen side (left/right)
- Scroll up/down keys
- Select key (copies selected card into clipboard)
- Open settings key (strip)
- Hide strip key (strip)
- Save/update/back keys (settings panel)
- Retained clipboard items (default `15`)
- Auto-hide timeout in seconds
- Update repo/token configuration

## Build (Windows)
```powershell
dotnet publish Membrain/Membrain.csproj -c Release -r win-x64 --self-contained true
```

## Icon assets
- Source image: `brain.png`
- Generated assets: `Membrain/assets/app.ico`, `Membrain/assets/app-*.png`, `favicon.ico`
- Regenerate after changing `brain.png`:
```bash
python3 scripts/generate_icons.py
```

See `INSTALLER.md` for Velopack packaging and release automation.
