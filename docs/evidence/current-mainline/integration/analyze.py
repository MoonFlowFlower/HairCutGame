"""Analyze unmodified Godot captures; layout does not synthesize game imagery."""
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import distance_transform_edt

root = Path(__file__).resolve().parent
run = root / (sys.argv[1] if len(sys.argv) > 1 else 'final')
review = run / 'review'
review.mkdir(exist_ok=True)
shots = json.loads((run/'measurements.json').read_text(encoding='utf-8-sig'))
legacy = {s['name']: s for s in json.loads((root/'legacy/measurements.json').read_text(encoding='utf-8-sig'))}
def mask(path):
    return np.asarray(Image.open(path).convert('RGB')).min(axis=2) >= 188
rows = []
for s in shots:
    n = s['name']
    core, fur = mask(run/f'{n}-core-mask.png'), mask(run/f'{n}-fur-mask.png')
    outside, missing = fur & ~core, core & ~fur
    outward, inward = distance_transform_edt(~core)[outside], distance_transform_edt(~fur)[missing]
    r = dict(name=n, corePixels=int(core.sum()), furPixels=int(fur.sum()), missingPixels=int(missing.sum()),
             missingCorePercent=float(100*missing.sum()/max(1,core.sum())),
             outwardMaxPx=float(outward.max()) if outward.size else 0,
             outwardP95Px=float(np.percentile(outward,95)) if outward.size else 0,
             inwardMaxPx=float(inward.max()) if inward.size else 0,
             inwardP95Px=float(np.percentile(inward,95)) if inward.size else 0,
             touchesImageEdge=bool(core[0,:].any() or core[-1,:].any() or core[:,0].any() or core[:,-1].any()),
             gpuMeanMs=s['gpuMs'], gpuP99Ms=s['gpuP99'])
    if n in legacy:
        r['legacySameCameraAndDensity']=all(s[k]==legacy[n][k] for k in ('camera','rotation','fov','density'))
        assert r['legacySameCameraAndDensity'], n
    rows.append(r)
report=dict(threshold=188, units='pixels; no depth conversion',
    maskScope='Primary shared hair only; intrinsic coverage, not character-occluded scene silhouette. Rest pose, identical camera/density.',
    performanceScope='Real root-viewport GPU under shared desktop load, not exclusive GPU/4-renderer/60fps acceptance.', shots=rows)
(review/'silhouette.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',28)
def panel(items,out):
    # Keep all source pixels at 1:1. Captions occupy a separate band.
    width=1280; height=840
    sheet=Image.new('RGB',(width*len(items),height),(23,24,29)); d=ImageDraw.Draw(sheet)
    for i,(path,label) in enumerate(items):
        im=Image.open(path).convert('RGB'); assert im.size==(1280,800)
        sheet.paste(im,(i*width,40));d.text((i*width+16,4),label,font=font,fill='white')
    sheet.save(review/out)
panel([(root/'legacy/normal-close.png','Previous B presentation'),(run/'normal-close.png','Mainline B - accepted Puppet art')],'before-after-close.png')
panel([(run/f'{n}.png',n) for n in ('normal-close','trimmed','carved')],'three-states.png')
print(json.dumps(rows,indent=2))
