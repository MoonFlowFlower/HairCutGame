"""Run real Godot peers through a seeded UDP fault. No admin privileges required."""
import argparse,json,subprocess,sys,time,re,os,shutil
from pathlib import Path
p=argparse.ArgumentParser()
p.add_argument('--executable',required=True);p.add_argument('--exported',action='store_true');p.add_argument('--baseline',action='store_true')
p.add_argument('--active-retry',action='store_true')
p.add_argument('--voice',action='store_true');p.add_argument('--voice-filtered',action='store_true')
p.add_argument('--real-audio',action='store_true')
p.add_argument('--client-executable',default='')
p.add_argument('--players',type=int,choices=[2,4],default=2);p.add_argument('--port',type=int,default=18400)
p.add_argument('--variant',choices=['Off','A','B'],default='B');p.add_argument('--profile',default='cross-border');p.add_argument('--fault',default='blackout');p.add_argument('--fault-at',type=float,default=30)
p.add_argument('--fault-seconds',type=float,default=5);p.add_argument('--period',type=float,default=0);p.add_argument('--seconds',type=float,default=110)
p.add_argument('--fault-count',type=int,default=0)
p.add_argument('--continue-early',action='store_true');p.add_argument('--rendered',action='store_true');p.add_argument('--language',default='zh')
p.add_argument('--expect-failure',action='store_true');p.add_argument('--version-mismatch',action='store_true');p.add_argument('--host-exit',type=float,default=0)
p.add_argument('--host-close',action='store_true');p.add_argument('--user-leave',action='store_true');p.add_argument('--bad-token',action='store_true');p.add_argument('--manual-retry',action='store_true')
a=p.parse_args();root=Path(__file__).resolve().parent.parent
out=root/'artifacts'/('recovery-'+('baseline-' if a.baseline else '')+a.fault+'-'+time.strftime('%Y%m%d-%H%M%S')+'-'+str(a.port));out.mkdir()
procs=[];logs=[];games=[]
def run(args,name):
    f=open(out/(name+'.log'),'w',encoding='utf-8');logs.append(f)
    proc=subprocess.Popen(args,stdout=f,stderr=subprocess.STDOUT,cwd=root,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0));procs.append(proc);return proc
try:
    proxy=run([sys.executable,str(root/'scripts/net_proxy.py'),'--listen',str(a.port+1),'--server',str(a.port),'--profile',a.profile,'--report',str(out/'proxy.json'),'--duration',str(a.seconds+80),'--fault',a.fault,'--fault-at',str(a.fault_at),'--fault-seconds',str(a.fault_seconds),'--fault-period',str(a.period),'--fault-count',str(a.fault_count)],'proxy')
    until=time.monotonic()+10
    while not (out/'proxy.json').exists():
        if time.monotonic()>until:raise RuntimeError('Proxy did not start')
        time.sleep(.1)
    for i in range(a.players):
        name='host' if i==0 else 'client'+str(i)
        cmd=[a.client_executable if i and a.client_executable else a.executable]
        if not(a.rendered and i==1):cmd+=['--headless']
        if a.real_audio:cmd+=['--audio-driver','WASAPI']
        if not a.exported:cmd+=['--path',str(root)]
        cmd+=['--log-file',str(out/(name+'.engine.log')),'--','--host' if i==0 else '--join']
        if i:cmd+=['127.0.0.1']
        cmd+=['--port',str(a.port if i==0 else a.port+1),'--expected',str(a.players),'--report',str(out/(name+'.json')),'--net-walk','--language',a.language]
        if a.variant!='Off':cmd+=['--v06-variant-'+a.variant.lower()]
        if a.baseline:cmd+=['--full-smoke']
        else:cmd+=['--recovery-test',str(a.seconds)]
        if a.continue_early and i==0:cmd+=['--test-host-continue']
        if a.expect_failure and i:cmd+=['--test-expect-failure']
        if a.version_mismatch and i:cmd+=['--test-protocol','999']
        if a.host_exit and i==0:cmd+=['--quit-after-seconds',str(a.host_exit)]
        if a.host_close and i==0:cmd+=['--test-host-close']
        if a.user_leave and i:cmd+=['--test-user-leave']
        if a.bad_token and i:cmd+=['--test-bad-token']
        if a.manual_retry and i:cmd+=['--test-manual-retry']
        if a.active_retry and i:cmd+=['--test-active-retry']
        if a.voice:cmd+=['--voice-inject']
        if a.voice_filtered:cmd+=['--voice-filter-test','2']
        if a.real_audio:cmd+=['--voice-test-mute']
        if a.rendered and i==1:cmd+=['--capture',str(out/'gameplay.png'),'--capture-phase','Build','--capture-delay','10']
        games.append(run(cmd,name))
        if i==0:time.sleep(2)
    start=time.monotonic();deadline=start+a.seconds+75
    while any(g.poll() is None for g in games):
        if time.monotonic()>deadline:raise RuntimeError('Peer deadline exceeded')
        if a.expect_failure and all(g.poll() is not None for g in games[1:]):time.sleep(4);break
        if not a.baseline and not a.expect_failure and any(g.poll() not in (None,0) for g in games):raise RuntimeError('Peer failed')
        time.sleep(.3)
    reports={f.stem:json.loads(f.read_text(encoding='utf-8-sig')) for f in out.glob('*.json') if f.stem!='proxy' and not f.stem.endswith(('.net','.voice'))}
    # Keep the exported peers' raw per-second diagnostics beside engine logs.
    telemetry=Path(os.environ.get('APPDATA',''))/'Godot/app_userdata/Project Hairball/playtests'
    for i,g in enumerate(games):
        name='host' if i==0 else 'client'+str(i)
        for source in telemetry.glob('network-*-'+str(g.pid)+'.jsonl'):
            shutil.copy2(source,out/(name+'.network.jsonl'))
    summary=dict(baseline=a.baseline,settings=vars(a),reports=reports,processIds=[g.pid for g in games],exitCodes=[g.poll() for g in games],output=str(out))
    if a.voice:
        summary['voiceReports']={f.stem:json.loads(f.read_text(encoding='utf-8-sig')) for f in out.glob('*.voice.json')}
        for i in range(1,a.players):
            events=[json.loads(line[10:]) for line in (out/('client'+str(i)+'.engine.log')).read_text(encoding='utf-8',errors='replace').splitlines() if line.startswith('NET_EVENT ')]
            resumed=[e for e in events if e['kind']=='recovery_ready']
            if not resumed:raise RuntimeError('Voice test requires an actual recovered client')
            ready=resumed[-1]['monotonicSeconds']
            before=[e for e in events if e['kind']=='voice_metrics' and e['monotonicSeconds']<ready]
            after=[e for e in events if e['kind']=='voice_metrics' and e['monotonicSeconds']>ready+5]
            if not before or not after:raise RuntimeError('Missing pre/post recovery voice observations')
            prior={s['actor']:s for s in before[-1]['detail']['streams']}
            final=after[-1]['detail']
            if len(final['streams'])<a.players-1 or any(s['decoded']-prior.get(s['actor'],{}).get('decoded',0)<100 for s in final['streams']):raise RuntimeError('Voice did not resume every remote speaker')
            if a.voice_filtered and any(e['detail']['opusPackets']!=0 for e in events if e['kind']=='voice_metrics' and e['detail']['featurePackets']>0):raise RuntimeError('Filtered sender encoded raw Opus')
            if a.real_audio and any((s['playbackDiscarded']-prior.get(s['actor'],{}).get('playbackDiscarded',0))/max(1,(s['decoded']-prior.get(s['actor'],{}).get('decoded',0))*(960 if s['opus'] else 1920))>.01 for s in final['streams']):raise RuntimeError('Voice playback dropped samples after reconnect')
    if not a.baseline:
        names=['client'+str(i) for i in range(1,a.players)]+([] if a.expect_failure else ['host'])
        if any(not reports.get(n,{}).get('success') for n in names):raise RuntimeError('Missing/passing report required')
        if not a.expect_failure and len({reports[n]['hash'] for n in names})!=1:raise RuntimeError('Different final states')
        for f in out.glob('*.engine.log'):
            if re.search(r'ERROR:|Unhandled exception|RECOVERY_TEST_FAIL',f.read_text(encoding='utf-8',errors='replace')):raise RuntimeError('Engine error '+f.name)
        if not a.expect_failure:
            if reports['host']['players']!=a.players:raise RuntimeError('Unexpected active player count')
            if not reports['host']['pauseVerified']:raise RuntimeError('Gameplay changed during pause')
        elif not a.user_leave:
            wanted='version' if a.version_mismatch or a.client_executable else 'expired' if a.bad_token else 'room_closed' if a.host_close else 'timeout'
            if any(reports[n]['reason']!='expected_failure:'+wanted for n in names):raise RuntimeError('Wrong failure explanation; expected '+wanted)
        if a.active_retry:
            for i in range(1,a.players):
                if 'manual_retry_deadline_preserved' not in (out/('client'+str(i)+'.engine.log')).read_text(encoding='utf-8',errors='replace'):raise RuntimeError('Active retry was not exercised')
        if a.user_leave:
            host_text=(out/'host.engine.log').read_text(encoding='utf-8',errors='replace')
            if (out/'host.network.jsonl').exists():host_text+=(out/'host.network.jsonl').read_text(encoding='utf-8',errors='replace')
            if 'peer_left_intentionally' not in host_text:raise RuntimeError('Intentional leave was not received')
        # All peers share this test machine's clock. Production cross-machine clock skew is not inferred here.
        if not a.expect_failure and not a.manual_retry and a.fault!='none':
            from datetime import datetime
            proxy_data=json.loads((out/'proxy.json').read_text())
            if proxy_data['fault_dropped']==0:
                if a.fault not in ('reliable','ack','large'):raise RuntimeError('Fault did not affect any packet')
                # The new bulk path deliberately avoids ENet reliable messages and large
                # application payloads. Zero matching packets is recorded, not called an
                # injected outage. The separate chunks/blackout cases must actually drop data.
                summary['faultCoverage']='No matching datagram; flow passed but this filter injected no loss'
            else:summary['faultCoverage']='Injected '+str(proxy_data['fault_dropped'])+' datagram drops'
            first_end=proxy_data['started_unix']+a.fault_at+a.fault_seconds
            cycle_count=(a.fault_count or max(1,int((a.seconds-a.fault_at-a.fault_seconds-8)/a.period)+1)) if a.period else 1
            latencies={}
            for i in range(1,a.players):
                events=[]
                for line in (out/('client'+str(i)+'.engine.log')).read_text(encoding='utf-8',errors='replace').splitlines():
                    if line.startswith('NET_EVENT '):
                        event=json.loads(line[10:]);events.append((event['kind'],datetime.fromisoformat(event['utc'].replace('Z','+00:00')).timestamp()))
                samples=[]
                for cycle in range(cycle_count):
                    end=first_end+cycle*a.period
                    next_fault=first_end+(cycle+1)*a.period-a.fault_seconds if a.period and cycle+1<cycle_count else float('inf')
                    # A second deliberate outage inside the recovery budget supersedes this deadline.
                    if next_fault<end+8:continue
                    began=any(k=='recovery_begin' and end-a.fault_seconds<=t<=end+3 for k,t in events)
                    ready=[t-end for k,t in events if k=='recovery_ready' and end<=t<min(next_fault,end+30)]
                    if began and not ready:raise RuntimeError('No recovery after fault cycle '+str(cycle)+' client '+str(i))
                    if ready:samples.append(min(ready))
                latencies['client'+str(i)]=samples
            summary['recoveryAfterFaultSeconds']=latencies
            if any(t>8 for samples in latencies.values() for t in samples):
                (out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
                raise RuntimeError('Recovery exceeded 8 seconds: '+str(latencies))
    (out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print('RECOVERY_SCENARIO_DONE',out,flush=True)
finally:
    for proc in procs:
        if proc.poll() is None:proc.terminate()
    for proc in procs:
        try:proc.wait(timeout=5)
        except subprocess.TimeoutExpired:proc.kill()
    for f in logs:f.close()
