"""Seeded UDP impairment on the real ENet datagrams; no admin/drivers required.
These profiles are synthetic scenarios, not measurements of any country's network.
"""
import argparse, heapq, json, random, selectors, socket, time
from pathlib import Path

PROFILES = {
    "lan": dict(delay_ms=2, jitter_ms=1, loss_percent=0, mbps=100, burst_ms=0),
    "domestic": dict(delay_ms=25, jitter_ms=8, loss_percent=.5, mbps=8, burst_ms=0),
    "cross-border": dict(delay_ms=120, jitter_ms=35, loss_percent=2, mbps=4, burst_ms=0),
    "relay-stress": dict(delay_ms=180, jitter_ms=60, loss_percent=3, mbps=2, burst_ms=200),
}
p=argparse.ArgumentParser()
p.add_argument('--listen',type=int,required=True);p.add_argument('--server',type=int,required=True)
p.add_argument('--profile',choices=PROFILES,required=True);p.add_argument('--seed',type=int,default=729)
p.add_argument('--duration',type=float,default=160);p.add_argument('--report',required=True)
p.add_argument('--fault',choices=['none','blackout','uplink','downlink','large','chunks','reliable','ack'],default='none')
p.add_argument('--fault-at',type=float,default=35);p.add_argument('--fault-seconds',type=float,default=5)
p.add_argument('--fault-period',type=float,default=0)
p.add_argument('--fault-count',type=int,default=0)
a=p.parse_args();settings=PROFILES[a.profile];rng=random.Random(a.seed)
selector=selectors.DefaultSelector();listener=socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
listener.bind(('127.0.0.1',a.listen));listener.setblocking(False);selector.register(listener,selectors.EVENT_READ,None)
clients={};queue=[];serial=0;start=time.monotonic();next_free=[start,start]
stats=dict(profile=a.profile,settings=settings,seed=a.seed,received=[0,0],forwarded=[0,0],dropped=[0,0],bytes=[0,0],max_queue=0)
stats.update(fault=a.fault,fault_at=a.fault_at,fault_seconds=a.fault_seconds,fault_dropped=0,started_unix=time.time(),fault_period=a.fault_period,fault_count=a.fault_count)

def enet_commands(payload):
    # ENet network-order header (peer id, optional sent time); no encryption/compression in this fixture.
    if len(payload)<2:return []
    header=int.from_bytes(payload[:2],'big');offset=4 if header & 0x8000 else 2
    if header & 0x4000:return []
    sizes={1:8,2:48,3:44,4:8,5:4,6:6,7:8,8:24,9:8,10:12,11:16,12:24}
    commands=[]
    while offset+4<=len(payload):
        cmd=payload[offset]&15;size=sizes.get(cmd)
        if size is None or offset+size>len(payload):break
        commands.append(cmd)
        data_at={6:4,7:6,8:6,9:6,12:6}.get(cmd)
        length=int.from_bytes(payload[offset+data_at:offset+data_at+2],'big') if data_at is not None else 0
        offset+=size+length
    return commands
def report():
    stats['elapsed_seconds']=time.monotonic()-start
    Path(a.report).write_text(json.dumps(stats,indent=2),encoding='utf-8')
def schedule(payload,sock,dest,direction):
    global serial
    now=time.monotonic();stats['received'][direction]+=1;stats['bytes'][direction]+=len(payload)
    age=now-start-a.fault_at
    active=age>=0 and (age%a.fault_period if a.fault_period else age)<a.fault_seconds
    if a.fault_period and a.fault_count and age>=a.fault_period*a.fault_count:active=False
    commands=enet_commands(payload) if active and a.fault in ('reliable','ack') else []
    reject=active and (a.fault=='blackout' or a.fault=='uplink' and direction==0 or a.fault=='downlink' and direction==1 or a.fault=='large' and len(payload)>1200 or a.fault=='chunks' and direction==1 and len(payload)>800 or a.fault=='reliable' and direction==1 and any(c in (6,8) for c in commands) or a.fault=='ack' and direction==0 and 1 in commands)
    if reject:stats['dropped'][direction]+=1;stats['fault_dropped']+=1;return
    burst=settings['burst_ms'] and 8 <= (now-start)%15 < 8+settings['burst_ms']/1000
    if burst or rng.random()*100<settings['loss_percent']:
        stats['dropped'][direction]+=1;return
    finish=max(now,next_free[direction])+len(payload)*8/(settings['mbps']*1e6)
    if finish-now>1:
        stats['dropped'][direction]+=1;return
    next_free[direction]=finish
    due=next_free[direction]+max(0,settings['delay_ms']+rng.uniform(-settings['jitter_ms'],settings['jitter_ms']))/1000
    serial+=1;heapq.heappush(queue,(due,serial,sock,dest,payload,direction));stats['max_queue']=max(stats['max_queue'],len(queue))
print('PROXY_READY',flush=True);report();last_report=start
try:
    while time.monotonic()-start<a.duration:
        now=time.monotonic()
        while queue and queue[0][0]<=now:
            _,_,sock,dest,payload,direction=heapq.heappop(queue)
            try:sock.sendto(payload,dest);stats['forwarded'][direction]+=1
            except OSError:stats['dropped'][direction]+=1
        timeout=min(.01,max(0,queue[0][0]-now)) if queue else .01
        for key,_ in selector.select(timeout):
            try:payload,addr=key.fileobj.recvfrom(65535)
            except (BlockingIOError,ConnectionResetError):continue
            if key.data is None:
                if addr not in clients:
                    sock=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);sock.bind(('127.0.0.1',0));sock.setblocking(False)
                    clients[addr]=sock;selector.register(sock,selectors.EVENT_READ,addr)
                schedule(payload,clients[addr],('127.0.0.1',a.server),0)
            else:schedule(payload,listener,key.data,1)
        if now-last_report>1:report();last_report=now
finally:
    report();listener.close()
    for sock in clients.values():sock.close()
