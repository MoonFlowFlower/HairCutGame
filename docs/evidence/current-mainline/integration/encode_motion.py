"""VFR video from real viewport PNGs, using the recorded wall-clock timestamps."""
import json
import subprocess
from pathlib import Path
root=Path(__file__).resolve().parent/'final-clean'
data=json.loads((root/'motion.json').read_text(encoding='utf-8-sig'))
rows=data['rows']
frames=sorted(root.glob('cut-motion-*.png'))
indices=[int(p.stem.rsplit('-',1)[1]) for p in frames]
stamps=[rows[i]['time'] for i in indices]
lines=['ffconcat version 1.0']
for i,p in enumerate(frames):
    interval=stamps[i+1]-stamps[i] if i+1<len(stamps) else rows[-1]['time']-stamps[i]
    lines.extend([f"file '{p.as_posix()}'",'option framerate 1000',f'duration {max(.001,interval):.6f}'])
lines.extend([f"file '{frames[-1].as_posix()}'",'option framerate 1000'])
playlist=root/'motion.ffconcat';playlist.write_text('\n'.join(lines)+'\n',encoding='utf8')
out=root/'cut-response.mp4'
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(playlist),'-fps_mode','vfr','-c:v','libx264','-threads','2','-crf','19','-pix_fmt','yuv420p',str(out)],check=True)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','frame=best_effort_timestamp_time','-of','json',str(out)],text=True))
pts=[float(f['best_effort_timestamp_time']) for f in probe['frames']]
assert len(pts)==len(frames)+1,(len(pts),len(frames))
max_error=max(abs(pts[i]-(t-stamps[0])) for i,t in enumerate(stamps))
assert max_error<=.0011,max_error
last_large=max((i for i,r in enumerate(rows) if r['offset']>=.0005),default=-1)
result=dict(source='real first-person viewport; timestamped VFR; readback overhead remains',
    pngCompressionOutsideMotionWindow=data['pngOutsideMotionWindow'],frames=len(frames),renderObservations=len(rows),
    capturedWallSeconds=rows[-1]['time'],videoTimestampMaxErrorSeconds=max_error,
    peakGuideMeters=max(r['offset'] for r in rows),lastGuideMeters=rows[-1]['offset'],
    continuouslyUnderPointFiveMmFrom=rows[last_large+1]['time'] if last_large+1<len(rows) else None,
    claimLimit='Guide telemetry is not per-strand tip/contact measurement. Initial cut frame is measured separately in checks.json.')
(root/'motion-analysis.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,indent=2))
