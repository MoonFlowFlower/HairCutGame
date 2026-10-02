"""Evidence layout only. Every game tile is an unretouched runtime viewport capture."""
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageFont
import argparse, json
p=argparse.ArgumentParser();p.add_argument('folder');p.add_argument('--round',type=int,required=True);a=p.parse_args()
d=Path(a.folder);r=a.round
root=Path(__file__).resolve().parents[1]
ref=root/'artifacts/puppethair-artdirection-pack-v02/PuppetHair_ArtDirection_Pack_v0.2/references/positive/B_puppet_crop_01.png'
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
def tile(canvas,path,box,label):
    im=Image.open(path).convert('RGB');im=ImageOps.contain(im,(box[2],box[3]-35),Image.Resampling.LANCZOS)
    canvas.paste(im,(box[0]+(box[2]-im.width)//2,box[1]+35));ImageDraw.Draw(canvas).text((box[0]+10,box[1]+5),label,font=font,fill='#e2d8c9')
states=Image.new('RGB',(2000,285),'#233332')
for i,s in enumerate(['normal','trimmed','burnt','wet','frozen']):tile(states,d/f'state-{s}.png',(i*400,0,400,285),s.upper())
states.save(d/f'hair_states_round_{r}.png')
sheet=Image.new('RGB',(1760,1053),'#233332')
tile(sheet,ref,(0,0,480,1080),'REFERENCE - concept, not engine')
tile(sheet,d/f'beauty_round_{r}.png',(480,0,640,435),f'ROUND {r} - REAL GODOT / BEAUTY')
tile(sheet,d/f'gameplay_round_{r}.png',(1120,0,640,435),'REAL GODOT / 73 degree FPS')
tile(sheet,d/f'gray_silhouette_round_{r}.png',(480,435,640,435),'GRAY CORE / no fuzz')
tile(sheet,d/'bald.png',(1120,435,640,435),'BALD / expression and proportion')
mini=states.resize((1280,183),Image.Resampling.LANCZOS);sheet.paste(mini,(480,870))
sheet.save(d/'reference_comparison.png')
if (d/'expr-happy.png').exists():
    expressions=Image.new('RGB',(1600,285),'#233332')
    for i,s in enumerate(['happy','panic','angry','dazed']):tile(expressions,d/f'expr-{s}.png',(i*400,0,400,285),s.upper())
    expressions.save(d/'expressions.png')
metrics=json.loads((d/'metrics.json').read_text())
print(json.dumps({s['shot']:{'frameMs':round(s['frameMs'],3),'fps':round(s['fps'],1),'gpuMs':round(s['gpuMs'],3)} for s in metrics['shots']},indent=2))
