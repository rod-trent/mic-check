#!/usr/bin/env python3
"""Generate placeholder Teams app icons (color 192x192, outline 32x32).

Pure standard library — no Pillow needed. Draws a rounded-square badge with a
simple microphone glyph. Replace with real brand artwork before publishing.
"""
import math
import os
import struct
import zlib

OUT = os.path.join(os.path.dirname(__file__), "..", "appPackage")

BRAND_TOP = (0x6B, 0x6F, 0xD8)
BRAND_BOT = (0x4F, 0x52, 0xB2)
WHITE = (255, 255, 255)


def write_png(path, w, h, pixels):
    raw = bytearray()
    for y in range(h):
        raw.append(0)  # filter type 0 per scanline
        for x in range(w):
            raw += bytes(pixels[y * w + x])
    comp = zlib.compress(bytes(raw), 9)

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data +
                struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff))

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)  # 8-bit RGBA
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", ihdr))
        f.write(chunk(b"IDAT", comp))
        f.write(chunk(b"IEND", b""))


def blend(dst, src, a):
    return tuple(round(dst[i] * (1 - a) + src[i] * a) for i in range(3))


def in_rounded_rect(x, y, x0, y0, x1, y1, r):
    if x < x0 or x > x1 or y < y0 or y > y1:
        return False
    cx = min(max(x, x0 + r), x1 - r)
    cy = min(max(y, y0 + r), y1 - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def mic_glyph(x, y, w, h, alpha_scale=1.0):
    """Return coverage 0..1 for a microphone shape centered in a wxh field."""
    cx = w / 2
    body_w = w * 0.22
    body_top = h * 0.20
    body_bot = h * 0.55
    r = body_w / 2
    cov = 0.0
    # Capsule body
    if in_rounded_rect(x, y, cx - r, body_top, cx + r, body_bot, r):
        cov = 1.0
    # U-shaped cradle (arc) below the body
    arc_r = w * 0.20
    dx, dy = x - cx, y - (body_bot - w * 0.02)
    d = math.hypot(dx, dy)
    if dy >= 0 and abs(d - arc_r) <= w * 0.035:
        cov = 1.0
    # Stem
    if abs(x - cx) <= w * 0.018 and body_bot <= y <= h * 0.80:
        cov = 1.0
    # Base
    if abs(x - cx) <= w * 0.14 and abs(y - h * 0.80) <= w * 0.02:
        cov = 1.0
    return cov * alpha_scale


def make_color(w=192, h=192):
    px = []
    r = w * 0.18
    for y in range(h):
        t = y / (h - 1)
        bg = tuple(round(BRAND_TOP[i] * (1 - t) + BRAND_BOT[i] * t) for i in range(3))
        for x in range(w):
            if in_rounded_rect(x, y, 0, 0, w - 1, h - 1, r):
                c = bg
                g = mic_glyph(x, y, w, h)
                if g > 0:
                    c = blend(c, WHITE, min(1.0, g))
                px.append((c[0], c[1], c[2], 255))
            else:
                px.append((0, 0, 0, 0))  # transparent outside badge
    write_png(os.path.join(OUT, "color.png"), w, h, px)


def make_outline(w=32, h=32):
    # Transparent background, white monochrome glyph (Teams outline spec).
    px = []
    for y in range(h):
        for x in range(w):
            g = mic_glyph(x, y, w, h)
            a = round(255 * min(1.0, g))
            px.append((255, 255, 255, a))
    write_png(os.path.join(OUT, "outline.png"), w, h, px)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    make_color()
    make_outline()
    print("Wrote color.png (192x192) and outline.png (32x32) to appPackage/")
