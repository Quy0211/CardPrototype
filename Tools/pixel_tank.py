#!/usr/bin/env python3
from PIL import Image
import os

PAL = {
    ".": None,
    "K": (15,23,42),
    "S": (71,85,105),     # giáp thép xám
    "s": (148,163,184),   # sáng
    "R": (185,28,28),     # khiên đỏ
    "r": (239,68,68),
    "L": (196,164,132),   # da
    "H": (120,53,15),     # tóc nâu
    "B": (62,36,20),      # giày
    "Y": (250,204,21),    # vàng
}

IDLE = [
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        ".....K..KK......",
        ".....K..KK......",
        ".....KK.BB......",
        "......BBBB......",
    ],
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        ".....K..KK......",
        "......K.KK......",
        "......KKBB......",
        ".......BBBB.....",
    ],
]

RUN = [
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        "......K.KK......",
        ".......K.KK.....",
        ".......KKBB.....",
        "........BBBB....",
    ],
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        "......K.KK......",
        ".......K.KK.....",
        "........KKBB....",
        ".........BBBB...",
    ],
]

ATTACK = [
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        ".....K..KK.Y....",
        ".....K..KKYY....",
        ".....KK.BB......",
        "......BBBB......",
    ],
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        ".....K..KKYY....",
        ".....K..KK.Y....",
        ".....KK.BB......",
        "......BBBB......",
    ],
    [
        "......RRRR......",
        ".....RRRRRR.....",
        "....KKKKKKKK....",
        "...KSssssSKK....",
        "...KSSSSSSKK....",
        "...KSSSSSSKK....",
        "....KLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "...KLLLLLLKK....",
        "....KKKKKKKK....",
        ".....KKHHKK.....",
        ".....K..KK.YY...",
        ".....K..KK.Y....",
        ".....KK.BB......",
        "......BBBB......",
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

save('tank_idle',IDLE,PAL)
save('tank_run',RUN,PAL)
save('tank_attack',ATTACK,PAL)
print('ok')
