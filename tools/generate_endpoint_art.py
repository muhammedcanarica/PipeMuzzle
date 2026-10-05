"""Local, deterministic endpoint painting. Uses the project's Pillow/numpy tools.

Only source/target art is written: pipe sprites and level assets stay untouched.
627px, centered pivot and 410 PPU match the existing pipe coordinate system.
"""
from pathlib import Path
import math
import uuid

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Art/GameplayThemes/Channels"
WORLDS = ("Sakura", "Bamboo", "Moon")
SIZE, C = 627, 313
COLORS = {
    "Sakura": ((227, 216, 201), (139, 120, 123), (250, 180, 196), (122, 197, 204)),
    "Bamboo": ((184, 160, 103), (100, 93, 66), (139, 160, 98), (91, 170, 161)),
    "Moon": ((189, 193, 212), (109, 110, 145), (226, 219, 244), (158, 194, 220)),
}


def ellipse(draw, box, fill, outline=None, width=1):
    draw.ellipse(tuple(C + value for value in box), fill=fill, outline=outline, width=width)


def polygon(draw, points, fill, outline=None, width=1):
    draw.polygon([(C + x, C + y) for x, y in points], fill=fill, outline=outline, width=width)


def line(draw, points, fill, width=1):
    draw.line([(C + x, C + y) for x, y in points], fill=fill, width=width, joint="curve")


def petal(draw, x, y, tint):
    for angle in range(0, 360, 72):
        dx, dy = 8 * math.cos(math.radians(angle)), 8 * math.sin(math.radians(angle))
        ellipse(draw, (x+dx-5, y+dy-5, x+dx+5, y+dy+5), tint)
    ellipse(draw, (x-3, y-3, x+3, y+3), (240, 217, 162, 255))


def wash(image, seed):
    """Low contrast paper grain and soft edge; deterministic, not a network call."""
    rgba = np.asarray(image).copy()
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[:SIZE, :SIZE]
    grain = rng.normal(0, 1.3, (SIZE, SIZE))
    grain += np.sin(xx * .036 + yy * .015) * 1.5
    grain += (C - xx) * .012 + (C - yy) * .009
    rgba[:, :, :3] = np.clip(rgba[:, :, :3].astype(float) + grain[:, :, None], 0, 255)
    painted = Image.fromarray(rgba)
    return painted.filter(ImageFilter.GaussianBlur(.45))


def target_water(world):
    _, _, accent, water = COLORS[world]
    image = Image.new("RGBA", (SIZE, SIZE))
    draw = ImageDraw.Draw(image)
    ellipse(draw, (-99, -99, 99, 99), (*water, 245))
    ellipse(draw, (-90, -89, 91, 93), tuple(min(255, c+13) for c in water) + (245,))
    draw.arc((C-79, C-73, C+78, C+77), 195, 277, fill=(236, 246, 247, 210), width=4)
    draw.arc((C-54, C-38, C+53, C+45), 14, 152, fill=(*accent, 140), width=3)
    line(draw, [(-46, 18), (-21, 20), (-5, 17)], (236, 247, 247, 145), 2)
    return wash(image, 90 + WORLDS.index(world))


def endpoint(world, source):
    stone, edge, accent, water = COLORS[world]
    image = Image.new("RGBA", (SIZE, SIZE))
    draw = ImageDraw.Draw(image)

    if world == "Sakura":
        # Rounded garden stones and a blossom basin have a low organic silhouette.
        ellipse(draw, (-148, -141, 149, 143), (*edge, 60))
        ellipse(draw, (-140, -137, 139, 134), edge)
        ellipse(draw, (-134, -131, 134, 128), stone)
        for i in range(10):
            angle = i * math.pi * 2 / 10
            x, y = math.cos(angle) * 117, math.sin(angle) * 112
            ellipse(draw, (x-25, y-21, x+25, y+21), tuple(min(255, c+8) for c in stone), edge, 2)
        if source:
            # A small bamboo spout feeds the spring, visibly distinct from the receiver.
            polygon(draw, [(-56,-158),(16,-164),(52,-126),(27,-102),(-5,-125),(-55,-122)], (162, 178, 113), edge, 3)
            line(draw, [(-45,-139),(-2,-144),(33,-116)], (205, 215, 159), 7)
            line(draw, [(-28,-155),(-26,-127)], (105, 127, 78), 4)
            line(draw, [(17,-103),(13,-83),(5,-69)], (*water, 215), 13)
            petal(draw, -111, 79, (*accent, 255))
        else:
            petal(draw, -113, -81, (*accent, 255))
            petal(draw, 112, 77, (*accent, 255))
            polygon(draw, [(89,-103),(121,-130),(130,-109),(103,-88)], (157, 177, 139), edge, 2)
    elif world == "Bamboo":
        if source:
            # A broad barrel/tank, stave grain, two dark hoops and a small brass valve.
            polygon(draw, [(-128,-128),(-110,-155),(111,-155),(132,-125),(127,120),(106,145),(-108,145),(-130,121)], edge)
            polygon(draw, [(-122,-122),(-103,-148),(104,-148),(125,-122),(120,115),(101,138),(-103,138),(-123,115)], stone)
            for x in range(-105, 115, 35):
                line(draw, [(x,-130),(x-3,-44),(x+2,75),(x,125)], (142, 127, 80), 3)
            for y in (-119, 113):
                line(draw, [(-126,y),(-92,y+4),(90,y+4),(125,y)], edge, 12)
                line(draw, [(-120,y-3),(118,y-3)], (188, 181, 140), 3)
            ellipse(draw, (90,-74,147,-20), (189, 161, 94), edge, 4)
            line(draw, [(104,-61),(131,-34)], edge, 5)
            line(draw, [(105,-33),(130,-61)], edge, 5)
        else:
            # Receiving reservoir with a wooden waterwheel at its upper left corner.
            polygon(draw, [(-131,-132),(124,-132),(144,-104),(139,114),(112,142),(-122,140),(-145,110),(-144,-100)], edge)
            polygon(draw, [(-125,-125),(119,-125),(136,-99),(132,109),(106,135),(-115,133),(-137,105),(-137,-96)], stone)
            line(draw, [(-127,-116),(126,-116)], accent, 8)
            ellipse(draw, (-164,-171,-55,-62), (155, 129, 81), edge, 5)
            ellipse(draw, (-151,-158,-68,-75), (195, 175, 120), edge, 3)
            for i in range(8):
                angle = i * math.pi / 4
                x, y = -109 + math.cos(angle)*49, -116 + math.sin(angle)*49
                line(draw, [(-109,-116),(x,y)], edge, 7)
            ellipse(draw, (-122,-129,-96,-103), accent, edge, 3)
    else:
        if source:
            # Shrine spring: stepped hexagonal stone and a crescent ornament.
            polygon(draw, [(-154,-74),(-99,-141),(92,-141),(152,-79),(151,80),(93,144),(-93,144),(-152,82)], edge)
            polygon(draw, [(-145,-70),(-94,-133),(87,-133),(144,-74),(143,76),(88,136),(-88,136),(-144,78)], stone)
            line(draw, [(-137,92),(-88,148),(85,148),(138,93)], (151, 154, 180), 9)
            ellipse(draw, (-36,-177,37,-104), accent, edge, 2)
            ellipse(draw, (-10,-184,52,-121), (0, 0, 0, 0))
            polygon(draw, [(-8,-130),(28,-130),(33,-110),(-13,-110)], stone, edge, 2)
        else:
            # An asymmetric crescent ledge around the quiet shrine pool.
            ellipse(draw, (-146,-144,144,144), edge)
            ellipse(draw, (-139,-137,137,137), stone)
            draw.arc((C-154, C-154, C+154, C+154), 30, 275, fill=(*accent,255), width=18)
            polygon(draw, [(98,-131),(136,-161),(163,-129),(134,-101)], stone, edge, 3)
            polygon(draw, [(114,-132),(134,-147),(147,-128),(134,-114)], accent, edge, 2)
            line(draw, [(-88,115),(-44,131),(15,133)], accent, 5)

    # Every object receives the same centered aperture. Connector geometry is reused
    # by Unity above this rim; it reaches inside this opening in all orientations.
    ellipse(draw, (-107,-107,107,107), edge)
    ellipse(draw, (-101,-101,101,101), tuple(max(0, c-18) for c in stone))
    ellipse(draw, (-93,-94,95,94), tuple(max(0, c-8) for c in stone))
    draw.arc((C-102, C-102, C+102, C+102), 205, 296, fill=tuple(min(255, c+24) for c in stone), width=4)
    if source:
        image.alpha_composite(target_water(world))
        # The source remains visibly wet before flow starts.
        draw = ImageDraw.Draw(image)
        ellipse(draw, (-12,-20,13,5), (*water, 255))
        draw.arc((C-16, C-24, C+18, C+11), 195, 290, fill=(244, 250, 246, 215), width=3)
    return wash(image, WORLDS.index(world)*2 + int(source))


def save(image, path):
    image.save(path)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        reference = "PipeMuzzle/" + path.relative_to(ROOT).as_posix()
        guid = uuid.uuid5(uuid.NAMESPACE_URL, reference).hex
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\nTextureImporter:\n  textureType: 8\n  spriteMode: 1\n  spriteMeshType: 0\n  spritePixelsToUnits: 410\n  spritePivot: {{x: 0.5, y: 0.5}}\n  alphaIsTransparency: 1\n  textureCompression: 0\n  mipmaps:\n    enableMipMap: 0\n", encoding="utf-8")


def main():
    preview = Image.new("RGB", (3*340, 2*330), (248, 241, 229))
    label = ImageDraw.Draw(preview)
    for col, world in enumerate(WORLDS):
        folder = OUT / world
        for row, source in enumerate((True, False)):
            image = endpoint(world, source)
            save(image, folder / ("source.png" if source else "target.png"))
            thumb = image.resize((560,560), Image.Resampling.LANCZOS)
            preview.paste(thumb, (col*340-110, row*330-120), thumb)
            label.text((col*340+18, row*330+297), world + (" / Source" if source else " / Target"), fill=(67, 61, 69))
        save(target_water(world), folder / "target_water.png")
    destination = ROOT / ".utmp"
    destination.mkdir(exist_ok=True)
    preview.save(destination / "endpoint-art-preview.png")
    print("Painted 6 endpoint objects and 3 arrival-water layers locally.")


if __name__ == "__main__":
    main()
