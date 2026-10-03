"""Deterministic pipe artwork. Run with Pillow/numpy; no image service is used.

627 px / 410 PPU preserves the existing renderer bounds. Open ends meet at
exactly half a board cell. Base art directions match TileView's legacy offsets.
"""
from pathlib import Path
import uuid
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/GameplayThemes/Channels"
SIZE, PPU, CENTER = 627, 410, 313
ARMS = {"straight": "EW", "corner": "SW", "threeway": "NEW", "cross": "NESW"}
DIRECTIONS = {"N": (0, -1), "E": (1, 0), "S": (0, 1), "W": (-1, 0)}
PALETTES = {
    "Sakura": ((240, 219, 211), (153, 119, 130), (91, 112, 118), (247, 170, 190), (84, 191, 215)),
    "Bamboo": ((150, 164, 99), (82, 96, 59), (53, 80, 69), (191, 155, 92), (72, 182, 170)),
    "Moon": ((180, 185, 220), (94, 101, 146), (44, 52, 87), (222, 215, 250), (140, 194, 247)),
}


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, "PipeMuzzle/" + str(path.relative_to(ROOT)).replace("\\", "/")).hex


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def save(image, path):
    image.save(path)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid(path)}\nTextureImporter:\n  textureType: 8\n  spriteMode: 1\n  spritePixelsToUnits: 410\n  spritePivot: {{x: 0.5, y: 0.5}}\n  alphaIsTransparency: 1\n  isReadable: 0\n  mipmaps:\n    enableMipMap: 0\n")


def mask(arms, width):
    image = Image.new("L", (SIZE, SIZE))
    draw = ImageDraw.Draw(image)
    for arm in arms:
        dx, dy = DIRECTIONS[arm]
        draw.line([(CENTER, CENTER), (CENTER + dx * PPU // 2, CENTER + dy * PPU // 2)],
                  fill=255, width=width)
    half = width / 2
    draw.ellipse((CENTER-half, CENTER-half, CENTER+half, CENTER+half), fill=255)
    return image


def layer(image, color, alpha):
    painted = Image.new("RGBA", image.size, color)
    painted.putalpha(alpha)
    image.alpha_composite(painted)


def pipe(world, shape):
    body, edge, floor, accent, water = PALETTES[world]
    arms = ARMS[shape]
    outer = mask(arms, 164)
    image = Image.new("RGBA", (SIZE, SIZE))
    shadow = Image.new("L", image.size)
    shadow.paste(outer, (2, 5))
    layer(image, (*edge, 75), shadow.filter(ImageFilter.GaussianBlur(3)).point(lambda x: x*.25))
    layer(image, edge, outer)
    layer(image, body, mask(arms, 156))
    # Soft ceramic/wood/stone grain, clipped to the body; empty channels stay dark.
    rng = np.random.default_rng(list(PALETTES).index(world) * 11 + list(ARMS).index(shape))
    yy, xx = np.mgrid[:SIZE, :SIZE]
    noise = rng.normal(0, 1.2, (SIZE, SIZE))
    if world == "Bamboo": noise += np.sin(xx*.16 + np.sin(yy*.02))*2.5
    shade = 8 * (CENTER-xx)/PPU + 7 * (CENTER-yy)/PPU + noise
    rgb = np.clip(np.array(body)[None, None, :] + shade[..., None], 0, 255).astype("uint8")
    texture = Image.fromarray(np.dstack((rgb, np.asarray(mask(arms, 150)))))
    image.alpha_composite(texture)
    layer(image, tuple(min(255, c+20) for c in body), mask(arms, 110))
    layer(image, tuple(max(0, c-14) for c in floor), mask(arms, 99))
    layer(image, floor, mask(arms, 89))
    # Split collars stay on the sidewalls. Never paint a bar across an open channel.
    collars = Image.new("RGBA", image.size)
    draw = ImageDraw.Draw(collars)
    for arm in arms:
        dx, dy = DIRECTIONS[arm]
        for side in (-1, 1):
            if dx:
                x, y = CENTER + dx*186, CENTER + side*65
                draw.rounded_rectangle((x-12, y-14, x+12, y+14), radius=4, fill=accent, outline=edge, width=2)
                draw.line((x-7, y-10, x-7, y+8), fill=tuple(min(255, c+28) for c in accent), width=2)
            else:
                x, y = CENTER + side*65, CENTER + dy*186
                draw.rounded_rectangle((x-14, y-12, x+14, y+12), radius=4, fill=accent, outline=edge, width=2)
                draw.line((x-10, y-7, x+8, y-7), fill=tuple(min(255, c+28) for c in accent), width=2)
    image.alpha_composite(collars)
    return image


def marker(world, source):
    body, edge, floor, accent, water = PALETTES[world]
    image = Image.new("RGBA", (SIZE, SIZE))
    draw = ImageDraw.Draw(image)
    c = CENTER
    if world == "Sakura":
        for dx, dy in ((-1,-1), (-1,1), (1,-1), (1,1)):
            x, y = c+dx*43, c+dy*43
            draw.ellipse((x-20, y-20, x+20, y+20), fill=(*accent, 235), outline=(*edge, 255), width=2)
    elif world == "Moon":
        for dx, dy in ((-1,-1), (-1,1), (1,-1), (1,1)):
            x, y = c+dx*52, c+dy*52
            draw.polygon([(x,y-11),(x+5,y-3),(x+11,y),(x+4,y+4),(x,y+11),(x-4,y+3),(x-11,y),(x-4,y-3)], fill=accent)
    draw.ellipse((c-52,c-52,c+52,c+52), outline=edge, width=12)
    draw.ellipse((c-48,c-48,c+48,c+48), outline=accent, width=7)
    draw.arc((c-44,c-44,c+44,c+44), 190, 295, fill=tuple(min(255,v+25) for v in body), width=3)
    if source:
        draw.polygon([(c,c-30),(c-17,c+1),(c+17,c+1)], fill=water)
        draw.ellipse((c-17,c-10,c+17,c+24), fill=water)
        draw.arc((c-11,c-4,c+11,c+17), 110, 240, fill=(226,249,255), width=3)
    else:
        # Hollow basin: the arriving water remains visible through its center.
        draw.arc((c-35,c-35,c+35,c+35), 10, 170, fill=accent, width=4)
    if world == "Bamboo":
        draw.line((c-28,c-57,c+28,c-57), fill=edge, width=9)
        draw.line((c-24,c-57,c+24,c-57), fill=accent, width=5)
    return image


def main():
    folder(OUT)
    preview = Image.new("RGB", (4*230, 3*240), (244, 238, 228))
    labels = ImageDraw.Draw(preview)
    for row, world in enumerate(PALETTES):
        folder(OUT/world)
        for col, shape in enumerate(ARMS):
            image = pipe(world, shape)
            save(image, OUT/world/f"pipe_{shape}.png")
            thumb = image.resize((280,280), Image.Resampling.LANCZOS)
            preview.paste(thumb, (col*230-25, row*240-5), thumb)
            labels.text((col*230+12,row*240+215), f"{world} / {shape}", fill=(56,52,64))
        for source, name in ((True,"source"),(False,"target")):
            save(marker(world, source), OUT/world/f"{name}.png")
    (ROOT/".utmp").mkdir(exist_ok=True)
    preview.save(ROOT/".utmp/pipe-art-preview.png")
    print("Created 12 empty-channel pipe sprites and 6 source/target motifs.")


if __name__ == "__main__":
    main()
