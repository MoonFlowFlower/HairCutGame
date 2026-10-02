# Third-party assets — VisualTargetLab

Last checked/downloaded: **2026-10-01**. Scope: opt-in VisualTargetLab only. The production B customer, hair density/cut/growth/scoring/network code is not replaced. Previews used in the scouting report are research references, not game textures.

## Imported assets

| Asset Name | Creator | Source URL | License | Commercial use | Attribution | Modification | Download Date |
|---|---|---|---|---|---|---|---|
| Snow v4 (download contains snow_v4.2.blend) | Blender Studio / Blender Foundation | https://studio.blender.org/characters/snow/v4/ | CC-BY 4.0 | Yes | Required: Snow Rig (CC) Blender Foundation, studio.blender.org; link license and disclose modifications | Yes | 2026-10-01 |
| Ultimate Animated Animals — Alpaca | Quaternius | https://quaternius.com/packs/ultimateanimatedanimals.html | CC0 1.0 | Yes | Not required | Yes | 2026-10-01 |
| Ultimate House Interior Pack — selected props | Quaternius | https://quaternius.com/packs/ultimatehomeinterior.html | CC0 1.0 | Yes | Not required | Yes | 2026-10-01 |
| Barber Shop Chair 01 | Fernando Quinn / Poly Haven | https://polyhaven.com/a/BarberShopChair_01 | CC0 1.0 | Yes | Not required | Yes | 2026-10-01 |

Original notices and primary-source license records: `third_party/licenses/Blender-Snow-ReadMe.txt` (extracted verbatim from the blend's ReadMe text), `Quaternius-Ultimate-Animated-Animals-License.txt`, `Quaternius-Ultimate-Home-Interior-License.txt`, `PolyHaven-License.html`, `CC-BY-4.0-legalcode.html`. Poly Haven supplied no separate per-model license file in the glTF download: its original license webpage is archived instead. See [CC-BY legal terms](https://creativecommons.org/licenses/by/4.0/) and [Poly Haven license](https://polyhaven.com/license).

### Adaptations and runtime location

`assets/third_party/visual_target/` contains the selected derived GLBs. `scripts/prepare_target_assets.py` regenerates them with the official portable Blender 4.5.3 runtime. Source downloads remain intact under `artifacts/asset-search-r1/downloads/` (excluded from Godot import/export). No external script from a downloaded blend was enabled.

- **Snow:** evaluated seated / working poses; proportion adjustment; removed static hair and render-only eye corneas; one subdivision level; albedo reduced to 1024; UDIM skin split into ordinary glTF materials; simplified opaque PBR; separate posed right forearm/hand. The original full facial/body rig remains in the original blend. These exported poses are static visual fixtures, **not a production animation/retarget integration**. No third-party hairstyle becomes customer hair truth.
- **Alpaca:** original authored mesh/rig, welded glTF split boundaries, face/ear/eye proportion adjustments, smooth shading and one subdivision; debug bone helper mesh excluded. Source animation clips are not exported. The fleece is a separate HairVolume / HairShell surface. No animal gameplay or network message is added.
- **Interior:** selected chair/counter/shelf/mirror/light/plant/stool models, bevel treatment and unified palette. The early armchair experiment is superseded by the real barber chair; unused converted candidates are not quality acceptance.
- **Barber chair:** original mesh; worn texture replaced by burgundy upholstery / warm wood / brass materials, preserving its mechanical structure. Source textures remain in the download archive.

**Required distribution credit:**

> Snow Rig (CC) Blender Foundation | https://studio.blender.org/characters/snow/v4/ — CC-BY 4.0, https://creativecommons.org/licenses/by/4.0/. Modified for Hairball: proportions, poses, materials, texture resolution, hair removal and arm extraction. Blender Foundation does not endorse this project.

The same credit is retained in `assets/third_party/visual_target/ATTRIBUTION.txt` and the Lab's credits display. Keep it with any distributed build/screenshots containing Snow. Do not apply a license restriction to the underlying CC-BY asset that contradicts CC-BY.

The existing Windows export preset explicitly includes this attribution text and `third_party/licenses/*`. No paid content is included. Lab screenshots in this round are accompanied by this ledger and the same attribution; store-view screenshots hide HUD, so retain the credit in their distribution caption or accompanying credits.

## Downloaded for evaluation; not selected as runtime art

| Asset | Creator / source | License and rights | Original license |
|---|---|---|---|
| Universal Base Characters Standard | [Quaternius](https://quaternius.itch.io/universal-base-characters) | CC0; commercial + modifications allowed; no attribution required; 2026-10-01 | `Quaternius-Universal-Base-Characters-License.txt` |
| Furniture Kit 2.0 | [Kenney](https://kenney.nl/assets/furniture-kit) | CC0; commercial + modifications allowed; no attribution required; 2026-10-01 | `Kenney-Furniture-Kit-License.txt` |
| Rain v3 (archive blend v3.2) | [Blender Studio](https://studio.blender.org/characters/rain/v3/) | CC-BY 4.0; commercial + modifications allowed; Rain Rig / Blender Foundation credit required; 2026-10-01 | Original blend and source-page license retained; not currently distributed |

Paid, login-blocked, and unselected alternatives are in [Asset Search Round 1](ASSET_SEARCH_ROUND_1.md). No purchase was made. No NC, ND, ripped, or provenance-ambiguous model is imported.
