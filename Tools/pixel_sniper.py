#!/usr/bin/env python3
from PIL import Image
import os
PAL={".":None,"K":(15,23,42),"V":(60,40,120),"v":(90,60,150),"L":(196,164,132),"D":(90,63,37),"B":(60,50,40),"W":(226,232,240)}
IDLE=[
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....",".....K..K.....",".....K..K.....",".....KK.BK....","......BBBB....",],
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....",".....K..K.....","......K.K.....","......KK.BK...",".......BBBB...",],
]
RUN=[
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....","......K.K.....",".......K.K....",".......KK.BK..","........BBBB..",],
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....","......K.K.....",".......K.K....","........KK.BK.",".........BBBB.",],
]
ATTACK=[
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....",".....K..K....W",".....K..K...W.",".....KK.BK..W.","......BBBB....",],
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....",".....K..K...W.",".....K..K..W..",".....KK.BK.W..","......BBBB....",],
  ["......VV......",".....VVVV.....","....KKKKKK....","...KDDDDDK....","...KDLLLDK....","...KKLLLKK....","....KVVVVK....","...KVvvvVK....","...KVvvvVK....","...KVvvvVK....","....KKKKKK....",".....KKKK.....",".....K..K..W..",".....K..K.W...",".....KK.BKW...","......BBBB....",],
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
save('sniper_idle',IDLE,PAL); save('sniper_run',RUN,PAL); save('sniper_attack',ATTACK,PAL)
