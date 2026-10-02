# 14 — License & Asset Notes

- In-game voice: Concentus 2.2.2 (NuGet, https://www.nuget.org/packages/Concentus/2.2.2), permissive BSD redistribution terms. The codec factory is forced to managed C#; no native codec is loaded. Full package license ships in `docs/packaging/CONCENTUS_LICENSE.txt` and package `licenses/`. Species syllables are original code-generated audio; none is recorded or saved.

- Godot engine code is MIT licensed; retain required notices in distribution as appropriate.
- Keep game code/assets owned by the project unless third-party licenses explicitly apply.
- Avoid pulling random internet models/textures into the prototype.
- Prefer generated primitives, original low-poly placeholders, permissively licensed assets with recorded attribution, or user-provided assets.
- The images in `docs/reference/` are development intent references. Do not extract, trace, or ship them as final game assets.
- Do not introduce paid/proprietary runtime dependencies unless the human explicitly approves them.
