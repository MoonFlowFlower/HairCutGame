"""Evidence layout only: full unretouched Godot viewport tiles, scaled to fit."""
import argparse,json
from pathlib import Path
from PIL import Image,ImageOps,ImageDraw,ImageFont
p=argparse.ArgumentParser();p.add_argument('folder');a=p.parse_args();d=Path(a.folder)
root=Path(__file__).resolve().parents[1]
ref=root/'artifacts/puppet-hair-optimization-v03-pack/PuppetHair_Optimization_Pack_v0.3/references/target/03_material_state_board.png'
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
states=['normal','trimmed','carved','burnt','wet','frozen']
def tile(canvas,path,x,y,w,h,label):
    im=ImageOps.contain(Image.open(path).convert('RGB'),(w,h-34),Image.Resampling.LANCZOS)
    canvas.paste(im,(x+(w-im.width)//2,y+34));ImageDraw.Draw(canvas).text((x+10,y+4),label,font=font,fill='white')
sheet=Image.new('RGB',(1920,1534),'#283332')
tile(sheet,ref,0,0,1920,600,'DIRECTION REFERENCE / generated concept, not runtime')
for i,s in enumerate(states):
    tile(sheet,d/f'opt-{s}-gameplay.png',(i%3)*640,600+(i//3)*467,640,434,f'REAL GODOT FPS / {s}')
sheet.save(d/'reference-comparison.png')
close=Image.new('RGB',(1920,868),'#283332')
for i,s in enumerate(states):tile(close,d/f'opt-{s}-close.png',(i%3)*640,(i//3)*434,640,434,f'REAL GODOT CLOSE / {s}')
close.save(d/'close-comparison.png')
m=json.loads((d/'metrics.json').read_text());print(json.dumps({s['shot']:{k:round(s[k],3) for k in ('gpuMs','fps','frameMs')} for s in m['shots']},indent=2))
