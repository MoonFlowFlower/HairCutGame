# 02 — Game Design Spec v0.2

## Core format

- 1–4 players.
- One shared salon arena.
- Each player has:
  - one barber avatar,
  - one assigned customer chair,
  - one NPC customer,
  - one personal goal selected from three options.
- All players act simultaneously.
- Round length: **75 seconds**.
- Match length: **3 rounds**.
- Winner: highest total score after three rounds.

## Camera and control — locked prototype direction

Prototype default is **first-person 3D**. Third-person is not the normal gameplay camera; it may be used for replay/spectator shots only.

The intended feeling is that the player is physically standing next to a customer and personally operating an obviously unsafe tool near a giant readable head. Input stays simple; complexity comes from consequences and shared physics.

### Default keyboard/mouse controls

- `WASD` — move.
- Mouse — look/aim.
- `E` — context interact: pick up a tool, use a station control, place/pick a validation prop when relevant.
- Left Mouse — primary tool action.
- Right Mouse — secondary tool action when that tool needs one (e.g. vacuum eject, sniper aim).
- `G` — drop currently held primary tool.
- `R` — reload/reset only for tools that clearly need it.
- Hold `E` on the station submit control for about 0.75–1.0s — early submit/cash out.

Do **not** add jump, crouch, sprint stamina, leaning, weapon slots, or a complex inventory for v0.2 unless a concrete prototype need appears.

### Tool possession

- Players physically pick tools up from racks/tables/floor.
- A player normally holds **one primary tool at a time**.
- Do not use a permanent FPS hotbar/tool wheel for the prototype.
- Dropped tools remain shared physical objects where practical.
- Competition over where the absurd tools are located is desirable.

### First-person body/readability

- Use a simple body collider; the player is not a ghost camera.
- Simple first-person hands/tool presentation is enough; do not build sophisticated hand IK.
- Other players must see the avatar and its hair.
- Player/barber hair uses the same hair interaction family as customer hair.
- Because a player cannot normally see the top/back of their own head, salon mirrors should provide awareness. If barber hair becomes extreme, selected effects may intrude into first-person view (long fringe, smoke, frost, etc.) as readable comedy.

Do not overbuild movement. It only needs to let the player:
- move around their station and the shared salon,
- reach/steal/drop tools,
- aim at customer/player hair,
- physically interfere and observe consequences.

## Round flow

### Phase 0 — Reset
- Spawn/seat fresh NPC customers.
- Preserve barber hair state from prior rounds.
- Reset round-scoped tool charges/ammo.
- Clear previous goal props and incidents.

### Phase 1 — Goal choice (5–8 seconds)
Each player receives 3 goals from the same approximate difficulty band.

The player privately chooses 1.

Other players should not see the exact numeric score or necessarily the exact selected goal UI, but the physical construction will reveal intent naturally.

If a player does not choose before timeout, auto-select one.

### Phase 2 — Build (75 seconds)
Players manipulate hair.

Important:
- all hair can be affected by all tools unless protected by submission,
- NPC hair and player-avatar hair use the same interaction family,
- tools can create collateral effects,
- dangerous tools have limited ammo/charges or other natural constraints,
- safe tools remain available for precision.

### Phase 3 — Early submit / cash out
At any time during the build phase, a player may press a deliberate **SUBMIT** control at their station.

On submit:
- freeze/protect that customer's gameplay hair state,
- remove that player's ability to use gameplay tools for the remainder of the round,
- protect the submitted barber from new gameplay hair effects for the remainder of that round,
- allow spectating, movement/emotes if cheap,
- do not reveal final exact score yet.

Purpose:
- prevent "finish early, grief everyone for 50 seconds,"
- create risk/reward tension,
- let threatened players cash out.

Submission should take a short deliberate hold (~0.75–1.0s) so it cannot happen accidentally.

### Phase 4 — Timeout
At 0 seconds:
- all tool effects stop,
- unsubmitted states lock,
- move to validation.

### Phase 5 — Goal validation
Run each player's absurd physical test.

Examples:
- helicopter lands,
- cat climbs,
- eggs are added and chair rotates,
- rocket launches,
- cart crosses hair bridge,
- fan tests clothesline,
- water proxy tests toilet bowl.

Validation should be short and visually legible.

### Phase 6 — Results
Reveal each player one by one:

- target goal thumbnail/icon,
- hair shape score,
- function/validation score,
- prop score if applicable,
- incident penalties,
- final round score.

Do not show opponents' exact live score during play.

### Phase 7 — Highlight
Play one 5–8 second "Best Moment" replay if available.

### Phase 8 — Next round
Return to goal choice with barber hair damage/style preserved.

After round 3:
- total scoreboard,
- winner,
- final best highlight or montage if cheap.

## Scoring philosophy

Use **positive construction score + explicit incident penalties**.

Avoid starting from 100 and subtracting for every imperfect cut; that encourages conservative play.

Base target score: 0–100.

Recommended composition:
- 70% silhouette/volume match,
- 20% functional validation,
- 10% required prop placement/state.

Then subtract round incident penalties and clamp final round score to 0–100.

The exact weights should remain data-driven.

## Incident philosophy

Accidents should be costly enough to matter but not so punishing that players stop using dangerous tools.

Prototype behavior:
- serious customer incident/KO: temporary ragdoll + short recovery, not permanent removal,
- no gore,
- if caused by another player's tool, the **attacker** receives the major score penalty,
- the victim mainly loses time/disruption, not an additional large score penalty,
- self-caused incident penalizes the owner.

This prevents "kill everyone's customer" from being a dominant strategy.

## Interference philosophy

Do not add a separate combat mode.

Sabotage should emerge from the same tools used for hair:
- sniper punctures hair and can hit someone behind it,
- blower shapes hair and can blow another player's wig away,
- growth spray repairs hair and can overgrow an opponent,
- trimmer flattens platforms and can sweep through someone else's structure,
- vacuum transfers hair and can steal it,
- glue stabilizes a bridge and can attach a barber's hair to scenery.

The game should feel like reckless work, not deathmatch with a haircut skin.

## Barber hair persistence

Barber avatars have interactive hair.

Across the three rounds:
- their hair state persists,
- it can be burned, shaved, grown, frozen, glued, stolen, or covered with wigs,
- this does not directly change score unless a later design explicitly uses it,
- it acts as visual history of the match and a source of emergent interactions.

At match end, the barbers themselves should often look progressively worse.

## Customer behavior

Keep NPC behavior lightweight:
- seated,
- looks/reacts to nearby dangerous tools,
- facial/voice reaction placeholders,
- flinch/shake on incidents,
- ragdoll on catastrophic incident,
- auto-reseat/recover after a short duration.

Do not build:
- complex satisfaction AI,
- hidden preferences,
- patience systems,
- full dialogue systems.

## Information presentation

During build:
- show own goal clearly,
- show timer clearly,
- show own approximate match meter,
- do **not** show opponents' exact numeric scores,
- show tool ammo/charges,
- show submit availability.

Final exact score comes at results.
