#!/usr/bin/env python3
from PIL import Image
import os

PAL = {
    ".": None,
    "K": (15,23,42),
    "R": (123,45,57),     # đá đỏ đậm
    "r": (190,70,80),     # đá đỏ
    "G": (90,100,110),    # đá xám
    "L": (180,90,60),     # lửa
    "Y": (255,200,60),    # lửa sáng
    "B": (40,40,50),      # bóng
}

IDLE = [
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KLLLK......",
        ".....KLLLK......",
        ".....KLLLK......",
        "......KKK.......",
        ".......B........",
    ],
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KYYLK......",
        ".....KLLLK......",
        ".....KLLLK......",
        "......KKK.......",
        ".......B........",
    ],
]

RUN = [
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KLLLK......",
        "......KLLLK.....",
        "......KLLLK.....",
        ".......KKK......",
        "........B.......",
    ],
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        "......KYYLK.....",
        "......KLLLK.....",
        "......KLLLK.....",
        ".......KKK......",
        "........B.......",
    ],
]

ATTACK = [
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KLLLK......",
        ".....KYYLK......",
        ".....KLLLK......",
        "......KKK.......",
        ".......B........",
    ],
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KYYLK......",
        ".....KYYLK......",
        ".....KLLLK......",
        "......KKK.......",
        ".......B........",
    ],
    [
        "......rrrr......",
        ".....rrrrrr.....",
        "....KrrrrrKK....",
        "...KrrrrrrrK....",
        "...KrrrrrrrK....",
        "....KRrrrrRK....",
        "...KRRRRRRRK....",
        "...KRGGGGGRK....",
        "...KRGGGGGRK....",
        "...KRRRRRRRK....",
        "....KKKKKKK.....",
        ".....KYYLK......",
        ".....KYYLK......",
        ".....KYYLK......",
        "......KKK.......",
        ".......B........",
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

save('boss_idle',IDLE,PAL)
save('boss_run',RUN,PAL)
save('boss_attack',ATTACK,PAL)
print('ok')
