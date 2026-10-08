#!/usr/bin/env python3
from PIL import Image
import os

PALETTE_SWORDSMAN = {
    ".": None,
    "K": (15,23,42),
    "A": (37,99,235),
    "a": (96,165,250),
    "V": (30,41,59),
    "W": (226,232,240),
    "w": (148,163,184),
    "G": (245,158,11),
    "R": (220,38,38),
    "B": (120,53,15),
}

def grid_to_img(grid, pal, w,h=None):
    if h is None: h = len(grid)
    # pad
    img = Image.new("RGBA",(w,h),(0,0,0,0)); px=img.load()
    for y,row in enumerate(grid[:h]):
        for x,ch in enumerate(row[:w]):
            c=pal.get(ch,None)
            if c is not None: px[x,y]=c+(255,)
    return img

def save_seq(prefix, grids, pal, w,h,s=16):
    out=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','Art'); os.makedirs(out,exist_ok=True)
    for i,g in enumerate(grids):
        im=grid_to_img(g,pal,w,h); im.save(os.path.join(out,f"{prefix}_{i+1:02d}_16.png"))
        im.resize((w*s,h*s),Image.NEAREST).save(os.path.join(out,f"{prefix}_{i+1:02d}.png"))

# IDLE 2 frames
IDLE = [
    [
        "......RR...Ww...",
        ".....RRRR..Ww...",
        "....KKKKK..Ww...",
        "...KaVVaK..Ww...",
        "...KaVVaK..Ww...",
        "...KKKKKK..Ww...",
        "....KKK..GGGGG..",
        ".KKaAAAAKKwK....",
        ".KKaAAAAKKwK....",
        ".KKaAAAAKKwK....",
        "..KKKKKKKKK.....",
        "...KKKKKK.......",
        "...KA..AK.......",
        "...KA..AK.......",
        "...KK..KK.......",
        "..BBB..BBB......",
    ],
    [
        "......RR...Ww...",
        ".....RRRR..Ww...",
        "....KKKKK..Ww...",
        "...KaVVaK..Ww...",
        "...KaVVaK..Ww...",
        "...KKKKKK..Ww...",
        "....KKK..GGGGG..",
        ".KKaAAAAKKwK....",
        ".KKaAAAAKKwK....",
        ".KKaAAAAKKwK....",
        "..KKKKKKKKK.....",
        "...KKKKKK.......",
        "...KA..AK.......",
        "....KA.AK......",
        "....KK.KK......",
        "...BBB.BBB.....",
    ],
]
# SLASH 3 frames
SLASH = [
    [
        "......RR...Ww...",
        ".....RRRR..Ww...",
        "....KKKKK..Ww...",
        "...KaVVaK..Ww...",
        "...KaVVaK..Ww...",
        "...KKKKKK..Ww...",
        "....KKK........",
        ".KKaAAAA........",
        ".KKaAAAA.....GGG",
        ".KKaAAAA....wK..",
        "..KKKKKKKKKwK...",
        "...KKKKKK..wK...",
        "...KA..AK.......",
        "...KA..AK.......",
        "...KK..KK.......",
        "..BBB..BBB......",
    ],
    [
        "......RR...Ww...",
        ".....RRRR..Ww...",
        "....KKKKK..Ww...",
        "...KaVVaK..Ww...",
        "...KaVVaK..Ww...",
        "...KKKKKK..Ww...",
        "....KKK.........",
        ".KKaAAAA........",
        ".KKaAAAA...GGGGG",
        ".KKaAAAA...wK...",
        "..KKKKKKKKKwK...",
        "...KKKKKK..wK...",
        "...KA..AK.......",
        "...KA..AK.......",
        "...KK..KK.......",
        "..BBB..BBB......",
    ],
    [
        "......RR...Ww...",
        ".....RRRR..Ww...",
        "....KKKKK..Ww...",
        "...KaVVaK..Ww...",
        "...KaVVaK..Ww...",
        "...KKKKKK..Ww...",
        "....KKK.....GGGG",
        ".KKaAAAA....wK..",
        ".KKaAAAA...wK...",
        ".KKaAAAA..wK....",
        "..KKKKKKKKK.....",
        "...KKKKKK.......",
        "...KA..AK.......",
        "...KA..AK.......",
        "...KK..KK.......",
        "..BBB..BBB......",
    ],
]
save_seq('swordsman_idle', IDLE, PALETTE_SWORDSMAN,16,16)
save_seq('swordsman_slash', SLASH, PALETTE_SWORDSMAN,16,16)
print('done')
