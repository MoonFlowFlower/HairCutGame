# 00 — Product Requirements Document (PRD) v0.2

## Product

**Project Hairball** — temporary codename.

## Locked player perspective

- Main gameplay: first-person 3D.
- Player stands physically near customer and aims absurd tools directly at interactive chunky hair.
- Input is deliberately simple and shared between tools.
- Tools are world objects that are picked up/dropped; no prototype hotbar/tool wheel.
- Barber/player hair can be modified by the same tool/hair rules and persists across match rounds.
- Visual references in `docs/reference/` define intent for proximity, hair readability, shared-space chaos, and absurd validation.


## Prototype objective

Build a playable 1–4 player networked prototype that can answer one question:

> Is it fun and socially explosive to manipulate low-poly hair as a physical material using absurd, dangerous, multi-purpose tools while racing to complete ridiculous head-top engineering challenges?

This prototype is not intended to prove retention, monetization, content scale, art direction, or production economics.

## Core promise

Every player should be able to understand their immediate goal quickly:

> "Make the customer's hair do this absurd thing."

The interesting part is how they do it.

Safe tools provide control. Dangerous tools provide speed and shortcuts. Because everyone shares one salon and all hair obeys compatible rules, solutions produce collateral effects, sabotage, repair, improvisation, and chain reactions.

## Primary user stories

### As a player
I want to:
- choose one of three weird but understandable hair tasks,
- manipulate my customer's hair directly,
- discover that absurd tools have legitimate uses,
- take risky shortcuts when time is low,
- recover from mistakes,
- mess with friends using the same systems,
- protect a good result by submitting early,
- watch the final physical validation,
- see the funniest moment replayed.

### As a playtester/developer
I want to:
- launch solo quickly,
- launch host + local clients quickly,
- reset hair/tools/goals rapidly,
- inspect hair state,
- reproduce interactions,
- change parameters without rebuilding architecture,
- judge gameplay without needing finished art.

## Functional requirements

### FR-1 Session
- 1–4 players.
- Offline solo.
- Host/join direct networking.
- Shared salon arena.

### FR-2 Player station
Each active player has:
- barber avatar,
- interactive barber hair,
- customer,
- customer hair,
- assigned chair/station,
- personal goal.

### FR-3 Goal choice
- 3 goal options.
- Similar difficulty band.
- choose before round starts.
- target is visually previewable.

### FR-4 Round
- 75-second simultaneous work period.
- players can cross stations and affect any unprotected hair.
- exact opponent score hidden.
- early submit/cash out available.

### FR-5 Submission
- locks customer hair,
- ends submitter's gameplay tool use that round,
- prevents post-submit harassment from becoming dominant,
- final exact score still waits for results.

### FR-6 Hair
Hair must support:
- add/grow,
- remove,
- bend/force,
- freeze/stiffen,
- glue/anchor,
- burn,
- detach,
- transfer,
- wigs,
- coarse physical response.

### FR-7 Tools
Prototype includes:
- one safe precision removal tool,
- growth spray,
- vacuum/transfer,
- blower,
- glue,
- liquid nitrogen,
- anchor/nail gun,
- sniper,
- flamethrower,
- hedge trimmer,
- wigs.

### FR-8 Accidents
- tools may affect unintended targets,
- dangerous tools can create customer incidents,
- incidents recover during round,
- source attribution exists,
- source gets score penalty so pure grief is not the optimal play.

### FR-9 Scoring
- positive target score 0–100,
- shape + function + prop/state,
- incident penalties separate,
- final results reveal together.

### FR-10 Match
- 3 rounds,
- total score,
- barber hair persists between rounds,
- customer state resets each round.

### FR-11 Validation
At least 6 absurd goals have a short physical test.

### FR-12 Replay
After core stability:
- capture recent events/state,
- pick a high-value event cluster,
- replay a short "best moment."

## Content requirements

Target goal pool:

1. Cat Tree Hair
2. Helicopter Helipad Hair
3. Bird Nest + Eggs
4. Birthday Cake + Candles
5. Rocket Silo
6. Hair Bridge + Vehicle
7. Underwear Clothesline
8. Toilet Hair + Water Test

At least 6 required for v0.2 acceptance.

## UX requirements

The player should always be able to answer:
- What is my goal?
- How much time is left?
- What tool am I holding?
- Does it have limited ammo/charge?
- Did my action change the hair?
- Did I cause an incident?
- Can I submit now?

The player should **not** need:
- a tutorial paragraph for each tool,
- precision knowledge of opponent scores,
- knowledge of networking concepts.

## Technical requirements

- Godot 4.7.2 stable .NET/C# target.
- Host-authoritative networking.
- No per-strand hair.
- Logical hair state must be compact enough for 1–4 player network play.
- Repeated setup should not require human editor work.
- Pure logic should be unit-testable outside scene runtime where practical.
- No paid middleware required.

## Quality requirements

Prototype quality means:
- interaction is readable,
- state is stable enough to play a full match,
- network clients broadly agree on game facts,
- controls allow intentional action,
- a failed interaction is understandable,
- no P0/P1 issue blocks a normal three-round session.

It does not mean:
- production graphics,
- perfect animation,
- perfect network prediction,
- perfect physics,
- content-complete game.

## Key product risks to test

### Risk A — Hair is visually funny but not satisfying to control
Mitigation:
- TestHairLab first.
- Low patch count.
- strong direct effects.
- stable rather than overly floppy baseline.

### Risk B — Dangerous tools become only grief weapons
Mitigation:
- give each dangerous tool strong goal-solving utility,
- limited charges,
- attacker incident penalties,
- safe baseline tools.

### Risk C — Multiplayer hair synchronization becomes technical sink
Mitigation:
- host authority,
- network logical facts/events,
- local cosmetic motion,
- coarse reconciliation.

### Risk D — Goal validation costs too much
Mitigation:
- use stylized proxies,
- scripted animal/vehicle paths,
- beads instead of fluids,
- preserve the joke, not realism.

### Risk E — Players finish then grief
Mitigation:
- early submit locks work and ends tool use.

### Risk F — Comedy relies on scripted content
Mitigation:
- prioritize reusable material rules and cross-tool interaction.

## Prototype success signal

A promising playtest should produce several moments per match where players can clearly recount a causal story such as:

> "I froze the bridge so the car could cross, then your blower pushed my growth spray onto it, the bridge grew into the car, I tried to fix it with the hedge trimmer, and cut your customer's tower at the same time."

That kind of story is more important than numerical balance in v0.2.
