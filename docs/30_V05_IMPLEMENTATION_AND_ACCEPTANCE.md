# v0.5 Implementation Plan & Acceptance

## Strategy

Do not expand content broadly. Use a small set of existing commissions—primarily Helipad, then Bird Nest / Rocket—to prove the material/tool/reference language.

Continue autonomously through milestones unless truly blocked.

## M0 — Audit current repo

- Read latest `PROJECT_STATE.md`.
- Detect whether v0.4 impact ledger/shared ordering has already landed; preserve it if present.
- Build and run current core tests before changing source.
- Record baseline evidence.

## M1 — Diegetic target references

Implement:
- per-player off-hand reference board,
- front/side/top images generated from the real target definition,
- material/function cues where used,
- shared anchored miniature target at a shop Target Station,
- spatial ghost demoted to optional assist.

Acceptance:
- one player can inspect board while another continues working,
- 4 players never fight over a single reference object,
- board/maquette update correctly when job changes,
- front/side/top agree with scored geometry.

## M2 — Material readability lab

Create/extend Hair Lab with Normal, Young, Wet, Frozen, Glued, Burnt states.

Implement state-specific:
- material/shader appearance,
- secondary motion response,
- at least one interaction/sound/particle difference.

Acceptance:
- rendered captures make states distinguishable without debug labels,
- head turn and blower demonstrate clearly different motion,
- no full soft-body or strand rewrite.

## M3 — Sculpting previews and precision

Implement consequence previews for at least:
- clipper,
- growth,
- hedge trimmer,
- blower/vacuum direction,
- glue join.

Improve surface protection/stability as documented.

Acceptance:
- previewed clipper layer matches actual removed region within prototype tolerance,
- growth direction is predictable,
- no back-side accidental edit from shallow clipper unless geometry genuinely wraps into the effect,
- precision mode feels stable in rendered/manual fixture.

## M4 — Tool grammar separation

Implement/refine first set:
- clipper = shallow remove,
- growth = directional young-hair add,
- blower = push existing mass,
- vacuum = pull/transfer with conservation,
- hedge trimmer = broad plane/sever,
- glue = join/set,
- sniper = puncture,
- flamethrower = heat/char/irregular erosion.

Do not balance by making clipper/growth sloppy.

Acceptance challenge: start from an uneven helipad and demonstrate at least four distinct viable correction approaches in fixtures/tests, not all reducible to repeated cut/grow.

## M5 — Physical goal props

Convert at least Bird Nest and one other goal from magic validation props to build-phase world props.

Minimum:
- three real eggs available before validation,
- players can physically place them,
- validation checks the same IDs/placements,
- no validation spawn/teleport replacement.

Also migrate helicopter staging to pre-existing entity if the current implementation still instantiates it only at validation.

## M6 — Shape + State + Function scoring

Add goal-state zones and goal-specific score weighting.

Minimum demonstration:
- one target visibly requires a material state (e.g. charred zone),
- one target uses real placed props as function score,
- shape-only approximation cannot earn near-perfect score when material/function requirement fails.

## M7 — Multiplayer / replay / regression

- authoritative state/prop placement remains host-owned,
- reference boards remain lightweight presentation,
- secondary motion is local cosmetic where appropriate,
- late join receives authoritative material/prop state,
- replay can reproduce enough state to explain relevant material/prop incidents,
- no regression to 1–4 shared-head flow.

Run current repository build/test/integration/smoke/load suites appropriate to changed systems.

## Human playtest acceptance questions

Do not claim these from automation:

1. Without target ghost, can a player understand the job from board + mini model?
2. Can a player predict the next clip/grow action with confidence?
3. Does normal hair feel alive/Q-like without sabotaging precision?
4. Can a player visually identify soft/wet/frozen/glued/burnt regions?
5. When a shape is wrong, do players spontaneously consider push/pull/plane-cut/fix-state routes rather than only cut/grow?
6. Do physical eggs/props create useful pre-validation comedy rather than busywork?
7. Does a burnt/frozen/glued target image naturally suggest the need for state-changing tools?

## Stop condition for this pass

Do not add more customers or dozens of new tools until the above questions can be playtested in the existing small goal set.
