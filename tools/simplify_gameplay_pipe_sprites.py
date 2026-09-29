#!/usr/bin/env python3
"""Simplify gameplay pipe colors without changing the verified alpha geometry."""

from __future__ import annotations

import argparse
import hashlib
import io
import re
import subprocess
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
THEMES = {
    "Sakura": ((243, 233, 213), (75, 160, 194)),
    "Bamboo": ((185, 159, 93), (66, 163, 195)),
    "Moon": ((100, 110, 135), (128, 210, 226)),
}
SHAPES = ("straight", "corner", "threeway", "cross")
SEAM_START = 466
SEAM_END = 787
EXPECTED_SIZE = (1254, 1254)


def is_water(theme: str, red: int, green: int, blue: int) -> bool:
    if theme == "Sakura":
        return blue > red + 20
    if theme == "Bamboo":
        return blue > green + 20
    return blue > red + 75


def sprite_path(theme: str, shape: str) -> Path:
    return ROOT / "Assets" / "Art" / "GameplayThemes" / theme / f"pipe_{shape}.png"


def edge_alpha(alpha: Image.Image, edge: str) -> tuple[int, ...]:
    if edge == "top":
        return tuple(alpha.crop((0, 0, 1254, 1)).get_flattened_data())
    if edge == "right":
        return tuple(alpha.crop((1253, 0, 1254, 1254)).get_flattened_data())
    if edge == "bottom":
        return tuple(alpha.crop((0, 1253, 1254, 1254)).get_flattened_data())
    return tuple(alpha.crop((0, 0, 1, 1254)).get_flattened_data())


def active_edges(alpha: Image.Image) -> dict[str, tuple[int, ...]]:
    return {
        edge: profile
        for edge in ("top", "right", "bottom", "left")
        if any(profile := edge_alpha(alpha, edge))
    }


def original_image(path: Path) -> Image.Image:
    relative_path = path.relative_to(ROOT).as_posix()
    result = subprocess.run(
        ["git", "show", f"HEAD:{relative_path}"],
        check=True,
        capture_output=True,
    )
    return Image.open(io.BytesIO(result.stdout)).convert("RGBA")


def simplify(theme: str, path: Path) -> None:
    current_alpha = Image.open(path).convert("RGBA").getchannel("A")
    image = original_image(path)
    alpha = image.getchannel("A")
    if alpha.tobytes() != current_alpha.tobytes():
        raise RuntimeError(f"{path}: current alpha does not match HEAD geometry.")

    body, water = THEMES[theme]
    water_mask = Image.new("L", image.size)
    water_mask.putdata(
        [
            255 if is_water(theme, red, green, blue) else 0
            for red, green, blue, _ in image.get_flattened_data()
        ]
    )
    if theme == "Moon":
        water_mask = ImageChops.lighter(
            water_mask,
            alpha.filter(ImageFilter.MinFilter(101)),
        )
    closing_size = 1 if theme == "Sakura" else 101
    water_mask = water_mask.filter(ImageFilter.MaxFilter(closing_size))
    water_mask = water_mask.filter(ImageFilter.MinFilter(closing_size))
    pixels = []
    for (_, _, _, opacity), water_opacity in zip(
        image.get_flattened_data(), water_mask.get_flattened_data()
    ):
        if opacity == 0:
            pixels.append((0, 0, 0, 0))
            continue
        color = water if water_opacity > 0 else body
        pixels.append((*color, opacity))
    image.putdata(pixels)
    if image.getchannel("A").tobytes() != alpha.tobytes():
        raise RuntimeError(f"{path}: alpha geometry changed while simplifying.")
    image.save(path)


def validate_sprite(theme: str, shape: str) -> list[str]:
    path = sprite_path(theme, shape)
    image = Image.open(path).convert("RGBA")
    errors: list[str] = []
    if image.size != EXPECTED_SIZE:
        errors.append(f"{path}: expected {EXPECTED_SIZE}, got {image.size}.")

    alpha = image.getchannel("A")
    profiles = active_edges(alpha)
    for edge, profile in profiles.items():
        positions = [index for index, value in enumerate(profile) if value > 0]
        if positions != list(range(SEAM_START, SEAM_END + 1)):
            errors.append(f"{path}: {edge} seam is not {SEAM_START}..{SEAM_END}.")
        if any(value != 255 for value in profile[SEAM_START : SEAM_END + 1]):
            errors.append(f"{path}: {edge} seam has a terminal cap or alpha gap.")

    expected_ports = {
        "straight": {"left", "right"},
        "corner": {"bottom", "left"},
        "threeway": {"top", "right", "left"},
        "cross": {"top", "right", "bottom", "left"},
    }[shape]
    if set(profiles) != expected_ports:
        errors.append(f"{path}: active edges {set(profiles)} != {expected_ports}.")

    for quarter_turn in range(4):
        rotated = alpha.rotate(quarter_turn * 90, expand=False)
        if rotated.size != EXPECTED_SIZE:
            errors.append(f"{path}: {quarter_turn * 90} degree rotation clipped.")
        for profile in active_edges(rotated).values():
            positions = [index for index, value in enumerate(profile) if value > 0]
            if positions != list(range(SEAM_START, SEAM_END + 1)):
                errors.append(
                    f"{path}: {quarter_turn * 90} degree rotation moved a seam."
                )
    return errors


def validate_meta(path: Path) -> list[str]:
    meta = path.with_suffix(path.suffix + ".meta")
    text = meta.read_text(encoding="utf-8")
    errors: list[str] = []
    if not re.search(r"^guid: [0-9a-f]{32}$", text, re.MULTILINE):
        errors.append(f"{meta}: missing stable GUID.")
    if "spritePixelsToUnits: 1254" not in text:
        errors.append(f"{meta}: PPU is not 1254.")
    if "spritePivot: {x: 0.5, y: 0.5}" not in text:
        errors.append(f"{meta}: pivot is not centered.")
    if "spriteMode: 1" not in text or "spriteMeshType: 0" not in text:
        errors.append(f"{meta}: sprite is no longer a Full Rect single sprite.")
    return errors


def alpha_digest(path: Path) -> str:
    alpha = Image.open(path).convert("RGBA").getchannel("A")
    return hashlib.sha256(alpha.tobytes()).hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    paths = [sprite_path(theme, shape) for theme in THEMES for shape in SHAPES]
    before = {path: alpha_digest(path) for path in paths}
    if args.apply:
        for theme in THEMES:
            for shape in SHAPES:
                simplify(theme, sprite_path(theme, shape))

    errors = []
    for theme in THEMES:
        for shape in SHAPES:
            path = sprite_path(theme, shape)
            errors.extend(validate_sprite(theme, shape))
            errors.extend(validate_meta(path))
            if before[path] != alpha_digest(path):
                errors.append(f"{path}: alpha digest changed.")

    if errors:
        raise SystemExit("\n".join(errors))
    print("Validated 12 sprites: geometry, seams, rotations, PPU, pivot, and GUID meta.")


if __name__ == "__main__":
    main()
