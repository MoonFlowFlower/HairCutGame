"""Measure actual R7 viewport masks; never synthesize a beauty render.

Usage: python scripts/measure_puppet_shells.py RUN_DIR [...] --output REPORT_DIR
cm is a local view-plane estimate from rendered core depth, not a 3D SDF.
"""
import argparse
import csv
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import binary_erosion, distance_transform_edt


def rgb(path):
    return np.asarray(Image.open(path).convert("RGB"))


def vector(v):
    return np.array([v[k] for k in ("x", "y", "z")], dtype=float)


def measure(folder, shot, width, height, output):
    name = shot["shot"]
    # 188 sRGB is approximately 50% linear MSAA coverage. All scene
    # occluders remain black with their real depth; no background removal.
    core = rgb(folder / (name + "-core-mask.png")).min(2) >= 188
    tips = rgb(folder / (name + "-tips-mask.png")).min(2) >= 188
    if not core.any() or not tips.any():
        raise ValueError(f"empty measurement mask: {folder}/{name}")
    distance, nearest = distance_transform_edt(~core, return_indices=True)
    outside = tips & ~core
    edge = tips & ~binary_erosion(tips)
    srgb = rgb(folder / (name + "-core-depth.png"))[:, :, 0] / 255.0
    linear = np.where(srgb <= .04045, srgb / 12.92, ((srgb + .055) / 1.055) ** 2.4)
    depth = linear * shot["silhouette"]["depthImageRangeMeters"]
    # Obtain depth from a fully covered neighboring core pixel, avoiding
    # MSAA depth dilution at the contour; contour distance still uses core.
    _, inner = distance_transform_edt(~binary_erosion(core), return_indices=True)
    depth_at_core = depth[inner[0], inner[1]]
    z = depth_at_core[nearest[0], nearest[1]]
    focal = height / (2 * math.tan(math.radians(shot["fov"]) / 2))
    cm = distance * z / focal * 100
    # Outward-only distance can make a hollow/underfilled shell look perfect.
    missing = core & ~tips
    inward = distance_transform_edt(~tips)
    inward_cm = inward * depth_at_core / focal * 100
    where = np.unravel_index(np.argmax(np.where(outside, cm, 0)), cm.shape)
    yy, xx = map(int, where)
    cy, cx = int(nearest[0, yy, xx]), int(nearest[1, yy, xx])
    sil = shot["silhouette"]
    camera, right, up, forward = (vector(sil[k]) for k in ("camera", "right", "up", "forward"))
    def at(x, y):
        return (camera + forward*z[yy, xx] + right*(x+.5-width/2)*z[yy, xx]/focal - up*(y+.5-height/2)*z[yy, xx]/focal).tolist()
    result = dict(
        run=folder.name, shot=name, mode=shot["mode"], shells=shot["shells"], heads=shot["heads"],
        firstPerson=shot["firstPerson"], nearestCoreMeters=shot["nearestCoreMeters"],
        gpuMs=shot["gpuMs"], fps=shot["fps"], p99Ms=shot["p99Ms"],
        renderCpuMs=shot["renderCpuMs"], engineVideoMemoryMiB=shot["engineVideoMemoryMiB"],
        drawCalls=shot["drawCalls"], visiblePrimitives=shot["visiblePrimitives"],
        coreCoveragePercent=float(core.mean()*100), tipsCoveragePercent=float(tips.mean()*100),
        wholeHeadCoveragePercent=float((rgb(folder / (name + "-head-mask.png")).min(2)>=188).mean()*100) if (folder / (name + "-head-mask.png")).exists() else None,
        maxPixels=float(distance[outside].max()) if outside.any() else 0,
        p95Pixels=float(np.percentile(distance[outside], 95)) if outside.any() else 0,
        maxLocalViewPlaneCm=float(cm[outside].max()) if outside.any() else 0,
        p95LocalViewPlaneCm=float(np.percentile(cm[outside], 95)) if outside.any() else 0,
        viewportClipped=bool(tips[0].any() or tips[-1].any() or tips[:, 0].any() or tips[:, -1].any()),
        missingCorePixels=int(missing.sum()),
        missingCorePercent=float(missing.sum()/core.sum()*100),
        inwardMaxPixels=float(inward[missing].max()) if missing.any() else 0,
        inwardP95Pixels=float(np.percentile(inward[missing],95)) if missing.any() else 0,
        inwardMaxLocalViewPlaneCm=float(inward_cm[missing].max()) if missing.any() else 0,
        shellRootBoundCm=sil["shellOffsetUpperBoundMeters"]*100,
        actualFiberRootMaxCm=sil["actualFiberRootOffsetMaxMeters"]*100 if sil["actualFiberRootOffsetMaxMeters"] is not None else None,
        corePixelAtMax=[cx, cy], visibleTipPixelAtMax=[xx, yy],
        coreWorldAtMaxEstimate=at(cx, cy), tipOnCoreViewPlaneEstimate=at(xx, yy),
        projectedMeasurement="Euclidean tip-mask-to-core-mask distance; cm uses nearest core depth in view plane, not exact 3D distance. Clipped contours are censored.",
    )
    overlay = rgb(folder / (name + ".png")).copy()
    core_edge = core & ~binary_erosion(core)
    overlay[core_edge] = [30, 240, 220]
    overlay[edge & ~core] = [255, 120, 50]
    image = Image.fromarray(overlay)
    draw = ImageDraw.Draw(image)
    draw.line((cx, cy, xx, yy), fill=(255, 255, 0), width=2)
    draw.rectangle((0, 0, width, 28), fill=(12, 16, 22))
    draw.text((8, 8), f"{folder.name} | {name} | cyan: core / orange: visible tips | max {result['maxPixels']:.2f}px / ~{result['maxLocalViewPlaneCm']:.2f}cm", fill="white")
    image.save(output / (folder.name + "--" + name + "-silhouette.png"))
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("runs", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    results = []
    for folder in args.runs:
        data = json.loads((folder / "r7-metrics.json").read_text(encoding="utf-8-sig"))
        for shot in data["shots"]:
            results.append(measure(folder, shot, int(data["width"]), int(data["height"]), args.output))
    (args.output / "measurements.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    columns = [k for k, v in results[0].items() if not isinstance(v, (list, dict))]
    with (args.output / "measurements.csv").open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=columns, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(results)
    print(json.dumps([{k:r[k] for k in ("run", "shot", "gpuMs", "tipsCoveragePercent", "maxLocalViewPlaneCm", "viewportClipped")} for r in results], indent=2))


if __name__ == "__main__":
    main()
