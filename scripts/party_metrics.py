"""Summarize host experiment JSONL. Missing facts remain unknown, never zero.
Usage: python scripts/party_metrics.py LOG --exclude-first 20 --output report.json
"""
import argparse, json, re
from collections import defaultdict, Counter
from pathlib import Path

def summarize(rows, exclude=0):
    rounds = defaultdict(list)
    for row in rows:
        r = row.get('event', row)
        if 'Kind' in r: rounds[r['Round']].append(r)
    result = []
    for number, events in sorted(rounds.items()):
        events.sort(key=lambda r: r['Time'])
        starts = [r['Time'] for r in events if r['Kind']=='phase' and r['Detail']=='Build']
        if not starts: continue
        start = starts[0] + exclude
        ends = [r['Time'] for r in events if r['Kind']=='result']
        end = ends[0] if ends else max(r['Time'] for r in events)
        players = defaultdict(list)
        for r in events:
            if r['Kind']=='player' and start <= r['Time'] <= end:
                fields = dict(re.findall(r'(\w+)=([^;]+)',r['Detail']))
                players[r['Actor']].append((r['Time'],int(fields.get('buttons',0)),int(fields.get('tool',-1)),fields.get('brace','False')=='True'))
        metrics = {}
        for actor, samples in players.items():
            held = active = longest = idle = observed = 0
            inactive_over_20s = 0
            for i, (t, buttons, tool, brace) in enumerate(samples):
                dt = min(.75, max(0,(samples[i+1][0] if i+1<len(samples) else end)-t))
                observed += dt
                held += dt * (tool>=0)
                working = (tool>=0 and bool(buttons&3)) or brace or bool(buttons&4)
                active += dt * working
                old_idle = idle
                idle = 0 if working else idle+dt
                inactive_over_20s += old_idle <= 20 < idle
                longest = max(longest,idle)
            metrics[str(actor)] = dict(held_ratio=held/observed if observed else None, active_ratio=active/observed if observed else None, longest_observed_idle=longest, input_inactive_over_20s=inactive_over_20s, observed_seconds=observed)
        rescues = []
        action_rows = [(r, r['Detail'].split(';')[0], re.search(r'target=(-?\d+)',r['Detail'])) for r in events if r['Kind']=='action']
        remedies = dict(Ignite='Extinguish',FreezeFriend='ThawFriend',GlueFriend='ReleaseFriend',FireFriend='WaterFriend')
        for incident, kind, target in action_rows:
            if kind not in remedies: continue
            rescue = next((r for r,k,t in action_rows if r['Time']>=incident['Time'] and k==remedies[kind] and target and t and target[1]==t[1]),None)
            rescues.append(dict(time=incident['Time'],kind=kind,target=int(target[1]) if target else None,actor=incident['Actor'],rescuer=rescue['Actor'] if rescue else None,latency=rescue['Time']-incident['Time'] if rescue else None))
        has_actions = any(r['Kind']=='action' for r in events)
        result.append(dict(round=number, excluded_seconds=exclude, players=metrics, tools=dict(Counter(r['Detail'].split(';')[0] for r in events if r['Kind'] in ('tool_start','dangerous_tool'))), rescue_latency=rescues if has_actions else None, placements=sum(r['Kind']=='action' and r['Detail'].startswith('PlaceCommission') for r in events) if has_actions else None, exit_result=next((r['Detail'] for r in events if r['Kind']=='result'),None)))
    return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('log');p.add_argument('--exclude-first',type=float,default=0);p.add_argument('--output');a=p.parse_args()
    rows=[json.loads(line) for line in Path(a.log).read_text(encoding='utf-8-sig').splitlines() if line.strip()]
    data=json.dumps(summarize(rows,a.exclude_first),ensure_ascii=False,indent=2)
    if a.output: Path(a.output).write_text(data,encoding='utf-8')
    else: print(data)
