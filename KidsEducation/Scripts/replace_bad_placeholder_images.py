#!/usr/bin/env python3
"""Replace bad placeholder images with simple polished 3D-style assets."""

from __future__ import annotations

import math
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
IMG_DIR = ROOT / "Resources" / "Images"
BACKUP_DIR = IMG_DIR / "_backup_bad_placeholders"
SIZE = 1024
CURRENT_IMAGE: Image.Image | None = None


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def shade(color: tuple[int, int, int], amount: int) -> tuple[int, int, int]:
    return tuple(max(0, min(255, c + amount)) for c in color)


def canvas(bg1="#F6F0FF", bg2="#E8FAF2") -> Image.Image:
    top = hex_to_rgb(bg1)
    bottom = hex_to_rgb(bg2)
    img = Image.new("RGB", (SIZE, SIZE), top)
    px = img.load()
    for y in range(SIZE):
        t = y / (SIZE - 1)
        col = tuple(int(top[i] * (1 - t) + bottom[i] * t) for i in range(3))
        for x in range(SIZE):
            px[x, y] = col
    return img.convert("RGBA")


def shadow(base: Image.Image, bbox, radius=34, offset=(0, 34), alpha=95) -> None:
    if not hasattr(base, "alpha_composite"):
        if CURRENT_IMAGE is None:
            return
        base = CURRENT_IMAGE

    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    moved = (bbox[0] + offset[0], bbox[1] + offset[1], bbox[2] + offset[0], bbox[3] + offset[1])
    d.ellipse(moved, fill=(0, 0, 0, alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(radius))
    base.alpha_composite(layer)


def rounded_box(d: ImageDraw.ImageDraw, xy, color, radius=45, depth=22):
    x1, y1, x2, y2 = xy
    c = hex_to_rgb(color) if isinstance(color, str) else color
    d.rounded_rectangle((x1, y1 + depth, x2, y2 + depth), radius=radius, fill=shade(c, -45))
    d.rounded_rectangle(xy, radius=radius, fill=c)
    d.rounded_rectangle((x1 + 22, y1 + 16, x2 - 22, y1 + 44), radius=18, fill=shade(c, 35))


def circle(d: ImageDraw.ImageDraw, xy, color, depth=20):
    c = hex_to_rgb(color) if isinstance(color, str) else color
    x1, y1, x2, y2 = xy
    d.ellipse((x1, y1 + depth, x2, y2 + depth), fill=shade(c, -45))
    d.ellipse(xy, fill=c)
    d.ellipse((x1 + 58, y1 + 45, x1 + 165, y1 + 130), fill=shade(c, 45))


def polygon_shadow(d: ImageDraw.ImageDraw, pts, color, depth=18):
    c = hex_to_rgb(color) if isinstance(color, str) else color
    d.polygon([(x, y + depth) for x, y in pts], fill=shade(c, -45))
    d.polygon(pts, fill=c)


def regular_polygon(cx, cy, r, n, rotation=-math.pi / 2):
    return [
        (cx + r * math.cos(rotation + 2 * math.pi * i / n), cy + r * math.sin(rotation + 2 * math.pi * i / n))
        for i in range(n)
    ]


def save(name: str, img: Image.Image) -> None:
    target = IMG_DIR / name
    if target.exists():
        BACKUP_DIR.mkdir(exist_ok=True)
        backup = BACKUP_DIR / name
        if not backup.exists():
            shutil.copy2(target, backup)
    img.convert("RGBA").save(target)


def asset(subject: str, draw_func, bg1="#F7F1FF", bg2="#EAF9FF"):
    global CURRENT_IMAGE
    img = canvas(bg1, bg2)
    CURRENT_IMAGE = img
    draw_func(ImageDraw.Draw(img))
    CURRENT_IMAGE = None
    return img


def draw_book(d):
    shadow(d.im, (235, 300, 790, 720))
    rounded_box(d, (245, 300, 760, 690), "#2F7DF6", 42)
    d.rounded_rectangle((340, 355, 755, 660), radius=28, fill="#F8E7C0")
    for y in range(400, 625, 44):
        d.line((390, y, 710, y), fill="#D9B983", width=6)
    d.polygon([(320, 690), (390, 690), (390, 805), (355, 765), (320, 805)], fill="#F0445E")


def draw_pencil(d):
    shadow(d.im, (270, 300, 745, 720))
    d.polygon([(315, 665), (620, 235), (735, 315), (430, 745)], fill="#FFD33D")
    d.polygon([(620, 235), (690, 135), (735, 315)], fill="#F4D0A2")
    d.polygon([(690, 135), (715, 235), (735, 315)], fill="#25262B")
    d.polygon([(315, 665), (255, 745), (350, 815), (430, 745)], fill="#FF6A6A")
    d.line((370, 695, 670, 275), fill="#F6A31A", width=24)
    d.line((305, 730, 390, 790), fill="#E4E7EB", width=28)


def draw_bag(d):
    shadow(d.im, (250, 260, 785, 750))
    rounded_box(d, (290, 245, 735, 750), "#1DA1D2", 70)
    d.arc((410, 110, 620, 330), 180, 360, fill="#FFCA3A", width=42)
    d.rounded_rectangle((330, 425, 700, 745), radius=58, fill="#157CA6")
    rounded_box(d, (360, 545, 670, 735), "#FFB72B", 38, 14)
    d.rounded_rectangle((420, 270, 610, 340), radius=25, fill="#FFE66D")


def draw_clock(d):
    shadow(d.im, (270, 255, 770, 770))
    d.ellipse((300, 170, 430, 300), fill="#F0445E")
    d.ellipse((595, 170, 725, 300), fill="#F0445E")
    circle(d, (260, 260, 780, 780), "#F0445E", 28)
    d.ellipse((340, 340, 700, 700), fill="#FFF3D1")
    for i in range(12):
        a = -math.pi / 2 + i * math.pi / 6
        x1, y1 = 520 + 138 * math.cos(a), 520 + 138 * math.sin(a)
        x2, y2 = 520 + 162 * math.cos(a), 520 + 162 * math.sin(a)
        d.line((x1, y1, x2, y2), fill="#333333", width=8)
    d.line((520, 520, 520, 410), fill="#222222", width=18)
    d.line((520, 520, 620, 570), fill="#222222", width=18)
    d.ellipse((495, 495, 545, 545), fill="#222222")


def draw_lamp(d):
    shadow(d.im, (260, 200, 790, 760))
    d.ellipse((320, 690, 750, 805), fill="#2389DA")
    d.rounded_rectangle((505, 455, 570, 710), radius=30, fill="#F2B62D")
    d.arc((430, 245, 730, 560), 210, 350, fill="#F2B62D", width=48)
    d.pieslice((260, 210, 700, 560), 190, 350, fill="#2196E8")
    d.ellipse((385, 390, 590, 510), fill="#FFF0A8")
    d.rounded_rectangle((470, 665, 635, 735), radius=28, fill="#35A4F2")


def draw_chair(d):
    shadow(d.im, (220, 210, 780, 790))
    rounded_box(d, (310, 220, 720, 430), "#FF7A2F", 55)
    rounded_box(d, (260, 500, 780, 690), "#FF7A2F", 50)
    for x in (345, 645):
        d.rounded_rectangle((x, 405, x + 72, 570), radius=32, fill="#D84F1A")
        d.rounded_rectangle((x + 15, 415, x + 45, 560), radius=18, fill="#FF8C4C")
    for x in (320, 660):
        d.rounded_rectangle((x, 670, x + 68, 850), radius=30, fill="#D84F1A")


def draw_table(d):
    shadow(d.im, (210, 280, 825, 765))
    rounded_box(d, (205, 335, 830, 550), "#D98A36", 45)
    d.rounded_rectangle((250, 400, 785, 475), radius=30, fill="#F6A54E")
    for x in (275, 675):
        d.rounded_rectangle((x, 520, x + 75, 820), radius=32, fill="#B76724")
        d.rounded_rectangle((x + 13, 535, x + 42, 800), radius=15, fill="#DF9145")


def draw_key(d):
    shadow(d.im, (235, 330, 805, 680))
    circle(d, (235, 330, 535, 630), "#FFD12E", 16)
    d.ellipse((330, 425, 440, 535), fill="#FFF0A8")
    rounded_box(d, (475, 445, 815, 520), "#FFD12E", 35, 15)
    d.rounded_rectangle((690, 505, 745, 610), radius=16, fill="#D99B00")
    d.rounded_rectangle((760, 505, 815, 585), radius=16, fill="#D99B00")


def draw_stop(d):
    shadow(d.im, (250, 230, 775, 760))
    pts = regular_polygon(512, 505, 280, 8)
    polygon_shadow(d, pts, "#E42C32", 22)
    pts2 = regular_polygon(512, 505, 210, 8)
    d.polygon(pts2, fill="#FFFFFF")
    pts3 = regular_polygon(512, 505, 170, 8)
    d.polygon(pts3, fill="#E42C32")
    d.rounded_rectangle((430, 375, 595, 615), radius=60, fill="#FFFFFF")
    d.rectangle((390, 485, 635, 570), fill="#FFFFFF")


def draw_traffic_light(d):
    shadow(d.im, (340, 175, 690, 820))
    rounded_box(d, (345, 170, 680, 765), "#222A35", 65)
    for y, c in [(285, "#F94144"), (465, "#FFD23F"), (645, "#43B85C")]:
        circle(d, (430, y - 75, 595, y + 90), c, 10)
    d.rounded_rectangle((485, 760, 540, 865), radius=22, fill="#343C48")


def draw_crosswalk(d):
    shadow(d.im, (190, 310, 840, 760))
    d.polygon([(240, 760), (415, 295), (610, 295), (790, 760)], fill="#4B5563")
    for y in range(680, 355, -75):
        w = 80 + (690 - y) * 0.25
        d.polygon([(512 - w, y), (512 + w, y), (560 + w * .7, y + 36), (464 - w * .7, y + 36)], fill="#FFFFFF")
    circle(d, (430, 170, 560, 300), "#FFD166", 8)
    d.line((495, 300, 470, 455), fill="#FFD166", width=35)
    d.line((470, 370, 390, 460), fill="#FFD166", width=28)
    d.line((480, 450, 420, 610), fill="#FFD166", width=30)
    d.line((480, 450, 575, 610), fill="#FFD166", width=30)


def draw_school(d):
    shadow(d.im, (230, 185, 790, 775))
    pts = regular_polygon(512, 505, 300, 3, -math.pi / 2)
    polygon_shadow(d, pts, "#F7C948", 20)
    pts2 = regular_polygon(512, 505, 230, 3, -math.pi / 2)
    d.polygon(pts2, fill="#20242A")
    circle(d, (360, 350, 455, 445), "#FFFFFF", 4)
    circle(d, (570, 350, 665, 445), "#FFFFFF", 4)
    d.line((405, 455, 510, 555), fill="#FFFFFF", width=30)
    d.line((615, 455, 510, 555), fill="#FFFFFF", width=30)
    d.line((510, 555, 455, 680), fill="#FFFFFF", width=30)
    d.line((510, 555, 595, 680), fill="#FFFFFF", width=30)


def draw_bike(d):
    shadow(d.im, (225, 280, 815, 720))
    circle(d, (225, 475, 425, 675), "#2F80ED", 8)
    circle(d, (610, 475, 810, 675), "#2F80ED", 8)
    d.line((325, 570, 470, 430, 610, 570, 420, 570, 520, 570, 470, 430), fill="#FFFFFF", width=24)
    d.line((610, 570, 665, 420), fill="#FFFFFF", width=22)
    d.line((665, 420, 725, 420), fill="#FFFFFF", width=18)
    d.line((470, 430, 450, 365), fill="#FFFFFF", width=18)
    d.line((425, 365, 505, 365), fill="#FFFFFF", width=18)


def draw_no_parking(d):
    shadow(d.im, (250, 225, 775, 770))
    circle(d, (245, 220, 775, 750), "#E42C32", 22)
    d.ellipse((330, 305, 690, 665), fill="#F7FBFF")
    d.line((335, 655, 685, 315), fill="#E42C32", width=48)
    d.rounded_rectangle((440, 365, 535, 610), radius=30, fill="#2F80ED")
    d.pieslice((505, 365, 655, 520), 270, 90, fill="#2F80ED")
    d.rectangle((505, 365, 575, 520), fill="#2F80ED")


def draw_speed(d):
    shadow(d.im, (250, 225, 775, 770))
    circle(d, (245, 220, 775, 750), "#E42C32", 22)
    d.ellipse((330, 305, 690, 665), fill="#FFFFFF")
    # Draw "50" using thick segments, because this traffic sign convention needs numbers.
    d.rounded_rectangle((405, 390, 500, 425), radius=15, fill="#222222")
    d.rounded_rectangle((405, 390, 440, 505), radius=15, fill="#222222")
    d.rounded_rectangle((405, 500, 500, 535), radius=15, fill="#222222")
    d.rounded_rectangle((465, 500, 500, 615), radius=15, fill="#222222")
    d.rounded_rectangle((405, 610, 500, 645), radius=15, fill="#222222")
    d.ellipse((540, 390, 655, 645), outline="#222222", width=32)


def draw_warning(d):
    shadow(d.im, (230, 190, 790, 760))
    pts = regular_polygon(512, 505, 300, 3, -math.pi / 2)
    polygon_shadow(d, pts, "#E42C32", 20)
    pts2 = regular_polygon(512, 505, 225, 3, -math.pi / 2)
    d.polygon(pts2, fill="#FFD23F")
    d.rounded_rectangle((488, 380, 536, 560), radius=22, fill="#222222")
    d.ellipse((485, 600, 540, 655), fill="#222222")


def draw_big(d):
    shadow(d.im, (185, 190, 840, 790))
    circle(d, (260, 205, 760, 705), "#FF7A2F", 28)
    circle(d, (660, 615, 805, 760), "#39C77F", 12)


def draw_small(d):
    shadow(d.im, (215, 320, 805, 760))
    circle(d, (250, 445, 395, 590), "#39C77F", 10)
    circle(d, (500, 270, 755, 525), "#FF7A2F", 18)


def draw_hot(d):
    shadow(d.im, (220, 235, 805, 780))
    circle(d, (285, 210, 735, 660), "#FFD23F", 22)
    for i in range(12):
        a = i * math.pi / 6
        x1, y1 = 510 + 280 * math.cos(a), 435 + 280 * math.sin(a)
        x2, y2 = 510 + 360 * math.cos(a), 435 + 360 * math.sin(a)
        d.line((x1, y1, x2, y2), fill="#FF9F1C", width=28)
    d.polygon([(460, 760), (525, 570), (610, 760), (540, 720), (500, 825)], fill="#F94144")
    d.polygon([(510, 755), (548, 655), (590, 755)], fill="#FFD23F")


def draw_cold(d):
    shadow(d.im, (245, 245, 780, 780))
    circle(d, (330, 330, 695, 695), "#8BD3FF", 20)
    for i in range(6):
        a = i * math.pi / 3
        x2, y2 = 512 + 280 * math.cos(a), 512 + 280 * math.sin(a)
        d.line((512, 512, x2, y2), fill="#FFFFFF", width=28)
        bx, by = 512 + 190 * math.cos(a), 512 + 190 * math.sin(a)
        for off in (-0.55, 0.55):
            d.line((bx, by, bx + 70 * math.cos(a + off), by + 70 * math.sin(a + off)), fill="#FFFFFF", width=18)


def draw_fast(d):
    shadow(d.im, (175, 290, 850, 735))
    for y in (370, 475, 580):
        d.line((160, y, 395, y), fill="#FFB703", width=28)
    d.polygon([(370, 335), (720, 230), (855, 490), (550, 665)], fill="#F94144")
    d.polygon([(550, 665), (370, 335), (350, 705)], fill="#2F80ED")
    d.ellipse((660, 345, 750, 435), fill="#BDE0FE")
    d.polygon([(350, 480), (220, 565), (350, 630)], fill="#FFD23F")


def draw_slow(d):
    shadow(d.im, (190, 340, 835, 750))
    rounded_box(d, (245, 445, 760, 645), "#88C057", 95)
    circle(d, (690, 390, 840, 540), "#88C057", 8)
    for x in (310, 460, 610):
        d.ellipse((x, 620, x + 115, 735), fill="#5A8E36")
    d.arc((330, 250, 700, 560), 180, 360, fill="#F4A261", width=45)


def draw_open(d):
    shadow(d.im, (255, 210, 795, 800))
    rounded_box(d, (300, 225, 715, 805), "#E8C38C", 25)
    d.polygon([(395, 280), (725, 210), (725, 790), (395, 705)], fill="#FFB703")
    d.line((395, 280, 395, 705), fill="#80543A", width=18)
    d.ellipse((655, 510, 700, 555), fill="#7A4F2B")
    d.polygon([(300, 225), (300, 805), (395, 705), (395, 280)], fill="#8D5A3B")


def draw_closed(d):
    shadow(d.im, (270, 205, 770, 805))
    rounded_box(d, (315, 210, 710, 805), "#FFB703", 32)
    d.rounded_rectangle((360, 260, 665, 750), radius=24, outline="#D88700", width=16)
    d.ellipse((610, 500, 660, 550), fill="#7A4F2B")


def draw_category_traffic(d):
    draw_traffic_light(d)
    d2 = d
    pts = regular_polygon(250, 725, 115, 8)
    polygon_shadow(d2, pts, "#E42C32", 10)
    d2.polygon(regular_polygon(250, 725, 70, 8), fill="#FFFFFF")
    d2.polygon(regular_polygon(250, 725, 50, 8), fill="#E42C32")
    pts = regular_polygon(800, 710, 130, 3, -math.pi / 2)
    polygon_shadow(d2, pts, "#F7C948", 10)
    d2.polygon(regular_polygon(800, 710, 90, 3, -math.pi / 2), fill="#222222")


def draw_category_opposites(d):
    shadow(d.im, (160, 250, 870, 775))
    circle(d, (180, 360, 500, 680), "#FF7A2F", 18)
    circle(d, (650, 505, 800, 655), "#39C77F", 10)
    d.line((525, 510, 630, 510), fill="#2F80ED", width=22)
    d.polygon([(640, 510), (600, 475), (600, 545)], fill="#2F80ED")
    d.line((630, 595, 525, 595), fill="#F94144", width=22)
    d.polygon([(515, 595), (555, 560), (555, 630)], fill="#F94144")


ASSETS = {
    "object_book.png": draw_book,
    "object_pencil.png": draw_pencil,
    "object_bag.png": draw_bag,
    "object_clock.png": draw_clock,
    "object_lamp.png": draw_lamp,
    "object_chair.png": draw_chair,
    "object_table.png": draw_table,
    "object_key.png": draw_key,
    "traffic_stop.png": draw_stop,
    "traffic_light.png": draw_traffic_light,
    "traffic_crosswalk.png": draw_crosswalk,
    "traffic_school.png": draw_school,
    "traffic_bike.png": draw_bike,
    "traffic_no_parking.png": draw_no_parking,
    "traffic_speed.png": draw_speed,
    "traffic_warning.png": draw_warning,
    "opposite_big.png": draw_big,
    "opposite_small.png": draw_small,
    "opposite_hot.png": draw_hot,
    "opposite_cold.png": draw_cold,
    "opposite_fast.png": draw_fast,
    "opposite_slow.png": draw_slow,
    "opposite_open.png": draw_open,
    "opposite_closed.png": draw_closed,
    "category_traffic.png": draw_category_traffic,
    "category_opposites.png": draw_category_opposites,
}


def main() -> int:
    for name, drawer in ASSETS.items():
        img = asset(name, drawer)
        save(name, img)
        print(f"replaced {name}")

    old = IMG_DIR / "category_objects.old.png"
    if old.exists():
        BACKUP_DIR.mkdir(exist_ok=True)
        shutil.move(str(old), str(BACKUP_DIR / old.name))
        print("moved category_objects.old.png to backup folder")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
