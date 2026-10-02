# 02 — Game Design Spec v0.3: 4 Barbers, 1 Head

## 1. Core mode

- 1–4 players.
- One active customer in a central barber chair.
- One shared absurd target.
- One shared payout/result.
- Players freely move around and use shared tools.

Primary relationship:

> cooperation in intent, interference in execution.

## 2. Player verbs

- move around the customer,
- pick up/drop tools,
- add/remove/reshape head material,
- push/blow/suck/freeze/glue/burn/puncture compatible targets,
- reposition to get an angle,
- react to customer motion,
- repair other players' mistakes,
- accidentally create new mistakes,
- place validation props where relevant.

## 3. Controls

- WASD: move
- Mouse: look
- E: context interact / pickup / placement
- G: drop held tool
- LMB: primary tool action
- RMB: secondary tool action where relevant
- R: reload/reset where meaningful

Do not add fixed-job ability bars or a permanent inventory wheel.

## 4. Round flow

### Phase A — Customer entrance (5–8s)
Customer enters/sits. Show customer type and any special behavior through animation/one short icon, not paragraphs.

### Phase B — Shared goal vote (6s)
Show 3 visual goal cards from a roughly comparable difficulty band. Each player votes. Highest vote wins; ties random.

### Phase C — Goal preview (2–4s)
Large readable visual target/ghost/mini animation. Explain functional validator visually.

### Phase D — Build (75s default)
All players work simultaneously.

There is no individual submit/cash-out in this mode. The tension should come from the shared timer and validators, not protecting separate work.

### Phase E — Validator enters (last ~15s when appropriate)
The validator can begin affecting the room before build time ends.

Examples:
- helicopter approaches and rotor wash begins,
- cat appears and jumps toward the structure,
- eggs are delivered,
- toy rocket arms/ignites.

### Phase F — Tools lock and validation
At 0s, disable further deliberate tool edits. Run the short physical validation event.

### Phase G — Shared result
Show:
- structural/shape score,
- functional validation result,
- major incident penalties,
- shared shop-money delta.

Then show comic personal awards without affecting the main result.

## 5. Personal comic awards

Possible post-round tags:
- Biggest Save
- Biggest Accident
- Most Hair Removed
- Most Hair Added
- Most Self-Hair Lost
- Most Customer Panic Caused
- Most Expensive Mistake
- Last-Second Hero

These are for social storytelling, not competitive victory.

## 6. Customer behavior

The customer mostly cooperates, but may react to stimuli.

Low intensity:
- eye tracking,
- sweating,
- tiny head drift,
- gulp/flinch.

Medium:
- duck,
- look left/right,
- scratch head,
- recoil.

High:
- sudden larger recoil,
- brief stand-up,
- scream,
- short panic step/escape attempt.

Important: reactions should have readable pre-motion whenever practical. See customer behavior spec.

## 7. Tool philosophy

The best tool rule is broad enough to create unintended uses.

Examples:
- blower applies directional force,
- vacuum transfers material and tugs attached bodies,
- glue creates attachment constraints,
- growth spray adds material in a cone and can drift in wind,
- fire rapidly removes/burns material and can propagate,
- hedge trimmer cuts against a moving plane/volume,
- sniper performs precise puncture with dangerous line of fire.

Avoid tools that are only "do damage to another player."

## 8. Prototype tool set

Required target set:
1. Clippers — safe precise removal.
2. Growth Spray — adds head material.
3. Leaf Blower — directional force.
4. Vacuum Transfer Cannon — suck/store/redeploy material.
5. Glue Gun — join/anchor material/props.
6. Hedge Trimmer — broad planar cutting.

High-value additions once core is stable:
7. Sniper Rifle — precise puncture + line-of-fire risk.
8. Flamethrower — very fast removal + fire risk.
9. Liquid Nitrogen — stiffen/freeze + brittleness.
10. Hair Bomb / Bald Bomb — irreversible countdown hazard.

## 9. Initial shared goals

Prioritize:
1. Helicopter Helipad
2. Bird Nest
3. Hair Bridge
4. Cat Tree
5. Rocket Silo
6. Underwear Clothesline

Helipad is the first acceptance goal because it tests shape, crowding, blower interference, customer motion, and physical validation.

## 10. Customer roadmap

Baseline:
- nervous human.

Next archetypes:
- llama: panic spit briefly obscures view,
- chicken: pecks at a nearby held/dropped tool,
- alien: head material swells once under high panic.

Each special customer should initially add **one** legible signature behavior.

## 11. Shared money shell

Current prototype can use a simple shop wallet.

Example result:
- base job reward,
- shape/validation bonus,
- customer injury penalty,
- property damage penalty,
- catastrophic incident penalty.

If wallet <= 0, a simple bankrupt/result screen is sufficient. Do not build deep management yet.

## 12. What creates the comedy

Do not rely on random events for timing.

Preferred chain:
`player action -> physical side effect -> customer/world response -> another player's reaction -> escalating consequence`

A successful session should produce retellable stories rather than unexplained chaos.
