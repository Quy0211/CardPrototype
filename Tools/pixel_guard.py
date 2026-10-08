#!/usr/bin/env python3
from PIL import Image
import os

PAL = {
    ".": None,
    "K": (20,20,30),
    "S": (100,110,120),
    "s": (150,160,170),
    "R": (120,40,50),
    "r": (180,60,70),
    "L": (190,150,110),
    "B": (60,40,30),
}

IDLE = [
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.....",
        ".....K..K.....",
        ".....KK.B.....",
        "......BBB.....",
        ".......B......",
    ],
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.....",
        "......K.K.....",
        "......KK.B....",
        ".......BBB....",
        "........B.....",
    ],
]

RUN = [
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        "......K.K.....",
        ".......K.K....",
        ".......KK.B...",
        "........BBB...",
        ".........B....",
    ],
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        "......K.K.....",
        ".......K.K....",
        "........KK.B..",
        ".........BBB..",
        "..........B...",
    ],
]

ATTACK = [
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K..s..",
        ".....K..K.s...",
        ".....KK.B.....",
        "......BBB.....",
        ".......B......",
    ],
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.s...",
        ".....K..K..s..",
        ".....KK.B.....",
        "......BBB.....",
        ".......B......",
    ],
    [
        ".....rrrr.....",
        "....rrrrrr....",
        "...KrrrrrK....",
        "...KSSSSSK....",
        "...KSSSSSK....",
        "....KLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "...KLLLLLK....",
        "....KKKKKK....",
        ".....KKKK.....",
        ".....K..K.s.s.",
        ".....K..K..s..",
        ".....KK.B.....",
        "......BBB.....",
        ".......B......",
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

save('guard_idle',IDLE,PAL)
save('guard_run',RUN,PAL)
save('guard_attack',ATTACK,PAL)
print('ok')
