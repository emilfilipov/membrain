#!/usr/bin/env python3
"""Generate all Membrain icon assets from a single source PNG."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def build_icons(source: Path, out_dir: Path, favicon_path: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    image = Image.open(source).convert("RGBA")
    size = max(image.size)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(image, ((size - image.width) // 2, (size - image.height) // 2), image)

    png_sizes = [16, 20, 24, 32, 40, 48, 64, 72, 96, 128, 256, 512]
    for px in png_sizes:
        resized = canvas.resize((px, px), Image.Resampling.LANCZOS)
        resized.save(out_dir / f"app-{px}.png", format="PNG")

    ico_sizes = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (72, 72), (96, 96), (128, 128), (256, 256)]
    canvas.save(out_dir / "app.ico", format="ICO", sizes=ico_sizes)
    canvas.save(favicon_path, format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64)])


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate icon assets from brain.png")
    parser.add_argument("--source", default="brain.png", help="Source PNG file path")
    parser.add_argument("--out-dir", default="Membrain/assets", help="Output directory for app icons")
    parser.add_argument("--favicon", default="favicon.ico", help="Output path for favicon")
    args = parser.parse_args()

    build_icons(Path(args.source), Path(args.out_dir), Path(args.favicon))
    print(f"Generated icon set from {args.source}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
