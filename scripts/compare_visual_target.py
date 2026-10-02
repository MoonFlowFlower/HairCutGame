"""QA layout only: unaltered runtime captures beside supplied reference images."""
from PIL import Image, ImageDraw
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
folder=root/sys.argv[1]
animal=len(sys.argv)>2 and sys.argv[2]=='animal'
refs=root/'artifacts/hairvisual-pack-v01/HairVisual_Implementation_Pack_v0.1/references'
out=Image.new('RGB',(1920,1350),'#192323');d=ImageDraw.Draw(out)
def slot(path,x,y,w,h,label):
    im=Image.open(path).convert('RGB');im.thumbnail((w,h-30),Image.Resampling.LANCZOS)
    out.paste(im,(x+(w-im.width)//2,y+30+(h-30-im.height)//2));d.text((x+12,y+8),label,fill='white')
slot(refs/('03_alpaca_concept.png' if animal else '01_human_concept.png'),0,0,960,440,'REFERENCE / animal shapes' if animal else 'REFERENCE / human shapes')
slot(refs/('04_alpaca_gameplay.png' if animal else '02_human_gameplay.png'),960,0,960,440,'REFERENCE / gameplay staging')
for row,style in enumerate(['ordinary','giant']):
    for col,camera in enumerate('ABC'):
        slot(folder/f'{style}-{camera}.png',col*640,440+row*455,640,445,f'{folder.name} / {style} / {camera}')
out.save(folder/'comparison.jpg',quality=94)
print(folder/'comparison.jpg')
