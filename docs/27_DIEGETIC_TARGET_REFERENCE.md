# Diegetic Target Reference — Personal Board + Shared Miniature

## Purpose

Players must understand a 3D target without turning the game into a permanent CAD overlay.

Use two complementary reference objects:

1. **Personal hand-held reference board** — precise orthographic understanding.
2. **Shared physical miniature maquette** — intuitive 3D understanding.

The existing spatial ghost remains optional assistance.

## Personal reference board

Each active player has their own board for the current commission. It is a reference presentation item, not a scarce gameplay tool.

### Interaction

Recommended default:
- `Tab` toggles/raises the board in the off-hand.
- The board does **not** consume the primary tool inventory slot.
- One-handed held tools may remain visible in the other hand but should not accidentally fire through the board.
- Heavy/two-handed tools can auto-lower the board while actively used; the player can re-raise it immediately afterward.
- If existing bindings conflict, preserve the product intent and choose the nearest simple control, documenting it.

Other players should see a lightweight replicated board pose if practical, but its image/content is client presentation and need not be networked as large textures.

### Board content

Required:
- target name,
- front orthographic render,
- side orthographic render,
- top orthographic render.

Optional per-goal, maximum 1–3 concise cues:
- "char this rim",
- "keep center soft",
- "through-hole",
- "support eggs",
- "glue/anchor allowed here" only when the *result* matters, not to prescribe one exact tool.

The board should show material states with the same visual language as gameplay hair.

### Source of truth

Board renders must be produced from the same target geometry/material-state definition used by scoring. Prefer runtime/editor-generated orthographic renders rather than manually painted approximations.

## Shared miniature target maquette

At a Target Station near the job area, spawn/show a small physical model of the final target.

Requirements:
- same target geometry proportions,
- same material-state pattern where feasible,
- clearly readable low-poly surface,
- mounted on a small pedestal/plinth,
- players can physically walk around and inspect it from arbitrary angles,
- anchored/protected in v0.5 so it cannot be lost/destroyed and soft-lock understanding.

The maquette is not scored and does not count as a source of usable hair/props.

## Reference update timing

When a goal vote/job changes:
- all personal boards switch to the selected goal,
- Target Station maquette updates/replaces itself,
- no stale model/board may remain from the previous job.

## Concept references

See `docs/reference_v05/`:
- `01_helipad_board_and_maquette.png`
- `03_bird_nest_board_and_maquette.png`
- `04_target_station_rocket_silo.png`

These show intent, not pixel-perfect UI/art requirements.
