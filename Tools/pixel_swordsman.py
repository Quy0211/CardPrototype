#!/usr/bin/env python3
"""Vẽ pixel art Kiếm Sĩ 16x16 bằng Pillow.

Cách hoạt động:
  - Mỗi dòng trong GRID là một dòng pixel (16 ký tự).
  - Mỗi ký tự ánh xạ 1 màu trong PALETTE ('.' = trong suốt).
  - Ảnh 16x16 được scale lên x16 (nearest-neighbor) để dễ nhìn.
Chạy: python3 Tools/pixel_swordsman.py
"""
import os
from PIL import Image

# ---------------------------------------------------------------- palette
PALETTE = {
    ".": None,            # trong suốt
    "K": (15, 23, 42),    # outline đen xanh
    "A": (37, 99, 235),   # giáp xanh đậm
    "a": (96, 165, 250),  # giáp xanh sáng (bị đèn chiếu)
    "V": (30, 41, 59),    # khe mũ giáp
    "W": (226, 232, 240), # thép sáng (lưỡi kiếm / tay nắm)
    "w": (148, 163, 184), # thép xám (tay nắm, chuôi)
    "G": (245, 158, 11),  # vàng (tay kiếm)
    "R": (220, 38, 38),   # lông vũ đỏ trên mũ
    "B": (120, 53, 15),   # giày nâu
}

# ------------------------------------------------------------- lưới 16x16
GRID = [
    "......RR...Ww...",  # lông vũ + mũi kiếm
    ".....RRRR..Ww...",  # lông vũ lan
    "....KKKKK..Ww...",  # đỉnh mũ
    "...KaVVaK..Ww...",  # mặt: khe nhìn
    "...KaVVaK..Ww...",
    "...KKKKKK..Ww...",  # cằm mũ
    "....KKK..GGGGG..",  # cổ + tay kiếm (chuôi)
    ".KKaAAAAKKwK....",  # vai + giáp + tay cầm chuôi
    ".KKaAAAAKKwK....",
    ".KKaAAAAKKwK....",
    "..KKKKKKKKK.....",  # thắt lưng
    "...KKKKKK.......",  # hông
    "...KA..AK.......",  # đùi
    "...KA..AK.......",
    "...KK..KK.......",  # ống chân
    "..BBB..BBB......",  # giày
]

SCALE = 16  # 16x16 -> 256x256


def build_image():
    h = len(GRID)
    w = max(len(row) for row in GRID)
    for i, row in enumerate(GRID):
        if len(row) != w:
            raise SystemExit(f"Dòng {i} có {len(row)} ký tự (cần {w}): {row!r}")

    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(GRID):
        for x, ch in enumerate(row):
            if ch not in PALETTE:
                raise SystemExit(f"Ký tự lạ '{ch}' tại ({x},{y})")
            color = PALETTE[ch]
            if color is not None:
                px[x, y] = color + (255,)
    return img


def main():
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Art")
    out_dir = os.path.normpath(out_dir)
    os.makedirs(out_dir, exist_ok=True)

    img = build_image()
    img16 = os.path.join(out_dir, "swordsman_16.png")
    big = img.resize((img.width * SCALE, img.height * SCALE), Image.NEAREST)
    img16x = os.path.join(out_dir, "swordsman.png")

    img.save(img16)
    big.save(img16x)
    print(f"Đã lưu: {img16}")
    print(f"Đã lưu: {img16x} ({big.width}x{big.height})")


if __name__ == "__main__":
    main()
