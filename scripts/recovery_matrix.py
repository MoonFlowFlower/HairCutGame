"""Run the frozen exported recovery candidate; save every command and outcome."""
import argparse, concurrent.futures, json, subprocess, sys, time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--executable',required=True);p.add_argument('--port',type=int,default=18700);p.add_argument('--only',default='');p.add_argument('--workers',type=int,choices=[1,2],default=2)
a=p.parse_args();root=Path(__file__).resolve().parent.parent
out=root/'artifacts'/('recovery-matrix-'+time.strftime('%Y%m%d-%H%M%S'));out.mkdir()
cases=[]
for profile in ('lan','domestic','cross-border','relay-stress'):
    for players in (2,4):cases.append((f'{profile}-{players}', ['--profile',profile,'--players',str(players),'--fault','none','--seconds','125']))
for fault,seconds in [('blackout',2),('blackout',5),('blackout',15),('uplink',5),('downlink',15),('chunks',15),('large',5),('reliable',15),('ack',5)]:
    cases.append((f'{fault}-{seconds}', ['--players','4','--fault',fault,'--fault-seconds',str(seconds),'--seconds','110']))
cases += [
    ('relapse',['--players','4','--fault','blackout','--fault-seconds','5','--period','6','--fault-count','2','--seconds','110']),
    ('continue',['--players','4','--fault','blackout','--fault-seconds','15','--continue-early','--seconds','110']),
    ('timeout',['--fault','blackout','--fault-seconds','40','--expect-failure','--rendered','--language','en','--seconds','90']),
    ('host-close',['--profile','lan','--fault','none','--host-close','--expect-failure','--seconds','70']),
    ('user-leave',['--profile','lan','--fault','none','--user-leave','--expect-failure','--seconds','70']),
    ('bad-version',['--profile','lan','--fault','none','--version-mismatch','--expect-failure','--seconds','45']),
    ('bad-token',['--profile','lan','--fault','none','--bad-token','--expect-failure','--seconds','80']),
    ('manual-retry',['--fault','blackout','--fault-seconds','40','--manual-retry','--seconds','155']),
    ('zh-render',['--fault','chunks','--fault-seconds','15','--rendered','--seconds','100']),
]
if a.only:
    requested=set(a.only.split(','));known={name for name,_ in cases}
    if requested-known:raise ValueError('Unknown cases: '+str(requested-known))
    cases=[case for case in cases if case[0] in requested]
def run(item):
    i,(name,opts)=item
    cmd=[sys.executable,str(root/'scripts/recovery_scenario.py'),'--executable',a.executable,'--exported','--port',str(a.port+i*2)]+opts
    with open(out/(name+'.log'),'w',encoding='utf-8') as f:
        result=subprocess.run(cmd,cwd=root,stdout=f,stderr=subprocess.STDOUT)
    report=dict(name=name,command=cmd,exitCode=result.returncode)
    (out/(name+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(name,result.returncode,flush=True);return report
with concurrent.futures.ThreadPoolExecutor(max_workers=a.workers) as pool:results=list(pool.map(run,enumerate(cases)))
(out/'summary.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(out,flush=True)
sys.exit(0 if all(r['exitCode']==0 for r in results) else 1)
