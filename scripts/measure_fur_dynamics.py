"""Analyze real fixed-camera fur records; thresholds are local diagnostics.

No derived plot or resampled movie is a replacement for original frame times.
"""
import argparse
import json
import statistics
from pathlib import Path

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
from measure_puppet_shells import measure


def analyze(folder, output):
    perf = (folder / 'dynamics-perf.json').exists()
    data = json.loads((folder / ('dynamics-perf.json' if perf else 'dynamics.json')).read_text())
    rows = data['rows']
    contact = [r for r in rows if r['contact']]
    manifest = json.loads((folder / 'candidate.json').read_text(encoding='utf-8-sig'))
    dll = next(r['Sha256'] for r in manifest if r['Path'].endswith('ProjectHairball.dll'))
    result = dict(run=folder.name, dll=dll, response=data['response'], heads=data.get('heads', 1),
                  wallSeconds=rows[-1]['wallTime'], simulationSeconds=rows[-1]['physicsTime'],
                  physicsTicks=len(rows), allFinite=all(r['finite'] for r in rows),
                  fullMotionBoundsContained=all(r.get('fullMotionBoundsContained',False) for r in rows) if 'fullMotionBoundsContained' in rows[0] else None,
                  maxGuideOffsetCm=max(r['maxTipOffsetWorld'] for r in rows)*100,
                  maxContactPenetrationMm=max((r['contactPenetrationWorld'] for r in contact), default=0)*1000,
                  contactTicks=sum(r['contacts'] > 0 for r in rows),
                  colliderIdentityVerified=all(r['hitColliderId'] == r['contactBodyId'] for r in rows if r['contacts']),
                  queryCount=sum(r['queries'] for r in rows),
                  updateMeanMs=statistics.mean(r['updateMs'] for r in rows),
                  updateP99Ms=float(np.percentile([r['updateMs'] for r in rows], 99)),
                  probeRefreshMaxMs=max(r['probeRefreshMs'] for r in rows),
                  diagnosticOnly=True)
    result['contactMetricScope'] = data.get('coordinates', 'Occupied guide samples only; see exact candidate source, not all visible strands')
    if 'boundedContacts' in rows[0]:
        result['boundedContactProjections'] = sum(r['boundedContacts'] for r in rows)
        result['infeasibleContactPlanes'] = sum(r['infeasibleContactPlanes'] for r in rows)
        result['maxRequiredContactOffsetLocalCm'] = max(r['maxRequiredContactOffsetLocal'] for r in rows)*100
    if 'colliderCommandDelay' in rows[0]:
        result['contactCommandDelayP95Ms'] = float(np.percentile([r['colliderCommandDelay']*1000 for r in contact], 95)) if contact else None
    if perf:
        for key in ('gpuMs','renderCpuMs','frameMs','p95Ms','p99Ms','samples'):
            result[key] = data[key]
    else:
        result.update(state=data['state'], cutPreserved=data['afterDensity'] == data['finalDensity'] != data['beforeDensity'],
                      rootAndRendererRetained=data['rootAndRendererRetained'], poseFixed=data['poseFixed'],
                      finsRetained=data.get('finsRetained'),finRendererId=data.get('finRendererId'),finMeshId=data.get('finMeshId'),
                      cutBuildMs=data['LastCutBuildMs'], cutReadyMs=data['LastCutReadyMs'],
                      cutToFirstDrawMs=data.get('cutToDrawMs'), recoveryThresholdMm=.5)
        recovery = {}
        for name, begin, end in [('wind', 2.2, 4.7), ('contact', 7.3, 9.8), ('pulses', 11.4, 14.4), ('cut', 15.4, 18.5)]:
            window = [r for r in rows if begin <= r['wallTime'] < end]
            valid = [r['wallTime'] for i,r in enumerate(window) if all(x['maxTipOffsetWorld'] < .0005 for x in window[i:])]
            recovery[name] = dict(settleToHalfMmSeconds=valid[0]-begin if valid else None,
                                  finalResidualMm=window[-1]['maxTipOffsetWorld']*1000 if window else None)
        result['recovery'] = recovery
        movie = json.loads((folder / 'motion.json').read_text())
        result['recordedFrames'] = movie['frames']
        result['captureTimesMonotonic'] = all(b['time'] > a['time'] for a,b in zip(movie['times'], movie['times'][1:])) if len(movie['times'])>1 else None
        times = [r['wallTime'] for r in rows]
        fig, axes = plt.subplots(3,1,figsize=(11,7),sharex=True)
        axes[0].plot(times,[r['maxTipOffsetWorld']*1000 for r in rows],label='maximum guide point')
        axes[0].plot(times,[r['rmsTipOffsetWorld']*1000 for r in rows],label='RMS')
        axes[0].set_ylabel('Deflection (mm)');axes[0].legend()
        axes[1].plot(times,[r['contactPenetrationWorld']*1000 for r in rows]);axes[1].set_ylabel('Sphere overlap (mm)')
        axes[2].plot(times,[r['inputWind']['x'] for r in rows],label='diagnostic wind X')
        axes[2].plot(times,[float(r['contact']) for r in rows],label='visible collider')
        axes[2].set_xlabel('Actual monotonic wall time (s)');axes[2].legend()
        for ax in axes:
            ax.axvline(data['nativeCutTime'],color='red',alpha=.5,linestyle='--')
            ax.grid(alpha=.2)
        fig.suptitle(folder.name+' — fixed FPS camera; cosmetic guides, native query + native cut')
        fig.tight_layout();fig.savefig(output/(folder.name+'-curves.png'),dpi=135);plt.close(fig)
    shot=dict(shot='r7-dynamic-perf' if perf else 'r7-dynamic-after', mode=data['settings']['VisualMode'],shells=data['settings']['Shells'],
              heads=data.get('heads',1),firstPerson=True,nearestCoreMeters=None,gpuMs=data.get('gpuMs'),fps=1000/data['frameMs'] if perf else None,
              p99Ms=data.get('p99Ms'),renderCpuMs=data.get('renderCpuMs'),engineVideoMemoryMiB=None,drawCalls=None,visiblePrimitives=None,
              fov=data['fov'],silhouette=data['silhouette'])
    result['silhouette'] = measure(folder, shot, 1280, 800, output)
    return result


if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('runs', nargs='+', type=Path)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();args.output.mkdir(exist_ok=True,parents=True)
    results=[analyze(folder,args.output) for folder in args.runs]
    (args.output/'dynamics-measurements.json').write_text(json.dumps(results,indent=2))
    print(json.dumps([{k:v for k,v in r.items() if k not in ['silhouette','recovery']} for r in results],indent=2))
