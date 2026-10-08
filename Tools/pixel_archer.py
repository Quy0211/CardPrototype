#!/usr/bin/env python3
from PIL import Image
import os

PAL = {
    ".": None,
    "K": (15,23,42),     # outline
    "G": (34,84,61),     # áo rừng
    "g": (45,134,89),    # áo sáng
    "H": (245,158,11),   # vàng dây
    "L": (196,164,132),  # da
    "D": (90,63,37),     # tóc nâu
    "B": (120,53,15),    # giày
    "W": (226,232,240),  # tên trắng
    "w": (148,163,184),  # cánh cung xám
    "O": (194,65,12),    # mũu đỏ
}

IDLE = [
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.....",
        ".....K..K.....",
        ".....KK.KK....",
        "......BBB.....",
    ],
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.....",
        "......K.K.....",
        "......KK.K....",
        ".......BBB....",
    ],
]

RUN = [
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        "......K.K.....",
        ".......K.K....",
        ".......KK.K...",
        "........BBB...",
    ],
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        "......K.K.....",
        ".......K.K....",
        "........KK.K..",
        ".........BBB..",
    ],
]

ATTACK = [
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.....W",
        ".....K..K....W",
        ".....KK.KK...W",
        "......BBB.....",
    ],
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K....W",
        ".....K..K...W.",
        ".....KK.KK..W.",
        "......BBB.....",
    ],
    [
        "......OO......",
        ".....OOOO.....",
        "....KKKKKK....",
        "...KDDDDDK....",
        "...KDLLLDK....",
        "...KKLLLKK....",
        "....KGGGGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "...KGgggGK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K...W.",
        ".....K..K..W..",
        ".....KK.KK.W..",
        "......BBB.....",
    ],
]

def g(grid,pal,w,h=None):
    if h is None: h=len(grid)
    im=Image.new('RGBA',(w,h),(0,0,0,0)); px=im.load()
    for y,r in enumerate(grid[:h]):
        for x,ch in enumerate(r[:w]):
            c=pal.get(ch,None)
            if c is not None: px[x,y]=c+(255,)
    return im

def save(prefix,grids,pal,w=16,h=16,s=16):
    out=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','Art'); os.makedirs(out,exist_ok=True)
    for i,gr in enumerate(grids):
        im=g(gr,pal,w,h); im.save(os.path.join(out,f"{prefix}_{i+1:02d}_16.png"))
        im.resize((w*s,h*s),Image.NEAREST).save(os.path.join(out,f"{prefix}_{i+1:02d}.png"))

save('archer_idle',IDLE,PAL)
save('archer_run',RUN,PAL)
save('archer_attack',ATTACK,PAL)
print('ok')
