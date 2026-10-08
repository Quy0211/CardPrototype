#!/usr/bin/env python3
import json,sys,os
sheet='Assets/Resources/Sprites/swordsman_anim_sheet.png'
# read sheet meta guid
with open('Assets/Resources/Sprites/swordsman_anim_sheet.meta') as f:
  for line in f:
    if line.strip().startswith('guid:'): guid=line.split(':')[1].strip(); break
# build 7 frames 256x256
frames=[]
x=0; names=['sw_idle1','sw_idle2','sw_run1','sw_run2','sw_slash1','sw_slash2','sw_slash3']
for i,n in enumerate(names):
  frames.append({
    "name": n,
    "rect": {"x":x,"y":0,"width":256,"height":256},
    "pivot": {"x":0.5,"y":0.5},
    "border": {"x":0,"y":0,"z":0,"w":0}
  })
  x+=256
meta={
  "fileFormatVersion": 2,
  "guid": guid,
  "TextureImporter": {
    "internalIDToNameTable": [],
    "spriteMode": 2,
    "defaultPivot": {"x":0.5,"y":0.5},
    "spritePixelsToUnits": 100,
    "spriteBorder": {"x":0,"y":0,"z":0,"w":0},
    "mipMaps": {"enableMipMap": False},
    "isReadable": False,
    "wrapMode": 1,
    "filterMode": 0,
    "alphaIsTransparency": True,
    "textureType": 1,
    "compressionQuality": 0,
    "spriteSheet": {
      "sprites": frames,
      "serializedVersion": 2
    },
    "maxTextureSize": 2048
  }
}
open(sheet.replace('.png','.meta'),'w').write(json.dumps(meta,indent=2))
print('ok')
