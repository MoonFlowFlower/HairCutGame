# Phase 8: static bake pipeline fork (reviewable scope)

Runtime audit: artifacts/lookdev-p7-floor/bake-audit.json. Art.Shop constructs163 primitive meshes at runtime;0 UV2 and no loaded lightmap. Current collider/query, moving chair, head and props are code generated. Installed4.7.2 CLI has no lightmap-bake command; its C# LightmapGI API exposes data/configuration, not an editor bake entry. A LightmapGI node alone would not produce baked lighting.

Proposed optional bake route (requires owner's asset-pipeline decision):

1. Add an export helper that copies only stable floor/walls/counters/ceiling into an artifact-only static scene, converts primitive geometry to UV2 ArrayMesh and gives stable node paths. Exclude the chair lift, all tools/props/customers/player heads/colliders.
2. Bake this scene offline through Godot editor with an RD-capable renderer in an isolated bake project. Main project and runtime stay Compatibility. Store generated scene/lightmap textures with a generator version and source fingerprint.
3. Load these resources only with an opt-in visual setting. Retain Art.Shop's collider/query generation; use the old mesh path if resources are missing or stale. Candidate switches continue to restore legacy presentation.
4. Compare actual captures and GPU/frame time, moving-chair/round-twist behavior and4P regressions before adopting the resource. Never infer success from creating a LightmapGI node.

Expected added files: scripts/export_visual_room.ps1, a bootstrap export helper, scenes/visual/ShopStatic.tscn and generated assets/visual lightmap resources. Small presentation integration in Art/SceneLook; no Core, scoring or wire changes. This introduces a new generated-asset/bake/update workflow and requires refresh when room geometry or static lighting changes.

Alternative: retain runtime construction, explicitly defer LightmapGI; finish Phase8 using a small reflection probe, conservative tone/glow and color adjustment, then run Phase9. This avoids the new asset workflow but does not satisfy the pack's LightmapGI item. Both routes remain opt-in until visual acceptance.

Recommendation: permit the isolated optional bake route if matching the toy-ad lighting target is the priority; adopt only after measured visual improvement. Otherwise finish the current runtime-only candidate and label LightmapGI deferred.
