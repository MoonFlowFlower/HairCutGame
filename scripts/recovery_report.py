"""Summarize a finished recovery run without inferring real-world disconnect causes."""
import argparse,json,statistics
from datetime import datetime
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('directory');a=p.parse_args();root=Path(a.directory)
def stamp(s):return datetime.fromisoformat(s.replace('Z','+00:00')).timestamp()
proxy=json.loads((root/'proxy.json').read_text())
result={'proxy':proxy,'peers':{}}
for file in sorted(root.glob('*.engine.log')):
    name=file.name.removesuffix('.engine.log');events=[]
    for line in file.read_text(encoding='utf-8',errors='replace').splitlines():
        if line.startswith('NET_EVENT '):
            try:events.append(json.loads(line[10:]))
            except json.JSONDecodeError:pass
    ready=[stamp(e['utc']) for e in events if e['kind']=='recovery_ready']
    starts=[stamp(e['utc']) for e in events if e['kind']=='recovery_begin']
    pauses=[e for e in events if e['kind']=='pause_begin']
    health=[];network=root/(name+'.network.jsonl')
    if network.exists():
        for line in network.read_text(encoding='utf-8',errors='replace').splitlines():
            try:
                sample=json.loads(line)
                if sample.get('kind')=='health':health.append(sample)
            except json.JSONDecodeError:pass
    latencies=[];fault=proxy['fault'];period=proxy.get('fault_period',0)
    count=proxy.get('fault_count',0)
    if fault!='none' and ready:
        first=proxy['started_unix']+proxy['fault_at']+proxy['fault_seconds']
        ends=[first]
        if period:
            ends=[first+i*period for i in range(count or max(1,int((max(ready)-first)/period)+1))]
        for end in ends:
            # A recovery must have begun in this fault cycle; do not count a later unrelated pause.
            begin=[s for s in starts if end-proxy['fault_seconds']<=s<=end+3]
            done=[r for r in ready if end<=r<end+(period or 30)]
            if begin and done:latencies.append(min(done)-end)
    outstanding=[peer['outstanding'] for h in health for peer in h.get('peers',[])]
    report=root/(name+'.json');data=json.loads(report.read_text(encoding='utf-8-sig')) if report.exists() else {}
    result['peers'][name]={'report':data,'recoveries':len(starts),'resumes':len(ready),'pauses':len(pauses),
        'recoveryAfterFaultSeconds':latencies,'maxRecoveryAfterFaultSeconds':max(latencies,default=0),
        'maxOutstandingPerPeer':max(outstanding,default=0),'healthSamples':len(health)}
(root/'analysis.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
