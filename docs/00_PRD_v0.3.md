# 00 — Product Requirements Document v0.3: Shared Head

## Product hypothesis

The strongest version of the prototype is not four players independently styling four customers. It is **1–4 players physically surrounding the same living customer and trying to save the same ridiculous head-engineering job**.

The fun should emerge from causal escalation rather than scripted jokes:

1. A shared goal is easy to understand.
2. Different players choose different reasonable methods.
3. Their methods interfere physically.
4. The customer/world reacts.
5. One small error creates a chain reaction.
6. Everyone scrambles to repair the same result.
7. A physical validation event tests the construction.
8. Players can retell exactly how the disaster happened.

## Primary player fantasy

> "We are four extremely unqualified barbers trying to make one impossible haircut work with industrial equipment while the customer increasingly regrets coming here."

## Target player count

- 1–4 players.
- Solo must remain technically playable for debugging and basic fun.
- The desired experience peaks with friends in voice chat.

## Current prototype goal

Prove that **shared-object cooperative chaos** is fun before investing in content breadth.

The prototype succeeds if playtests naturally generate statements such as:
- "Don't move!"
- "Who did that?!"
- "I was trying to fix it!"
- "Why is the helicopter already here?!"
- "He ducked and I shot you!"
- "Don't bring that thing over here!"

## Functional requirements

### FR-1 First-person shared workspace
- One central active customer.
- 1–4 player bodies can occupy space around the customer.
- Players can move around each other and collide sufficiently to create readable crowding.
- Third-person only for spectator/highlight presentation.

### FR-2 Shared head/material
- All players act on the same authoritative head-material state.
- Hair/material is chunky, deformable, readable, and interaction-first.
- It can grow, shrink, deform, transfer, glue, burn, puncture, and receive force.
- Player hair also participates in the same interaction family where practical.

### FR-3 Shared goal
- At round start, present 3 absurd shared goal choices.
- Players vote for one goal during a short vote window.
- Ties may resolve randomly.
- The selected goal is visible/readable to everyone.

### FR-4 Free-form build phase
- Default build time: 75 seconds.
- No mandatory shampoo/cut/dry phases.
- Any player can use any available tool.
- Tools physically exist in the room and are picked up/dropped.
- One main held tool at a time.

### FR-5 Customer life and panic
- Customer is not static.
- Stimuli increase hidden panic/arousal.
- Reactions include small head/body motions.
- Reactions should have anticipation cues where practical.
- Reactions can cause real tool misses and chain accidents.

### FR-6 World-reactive tools
- Tools are not hair-only UI abilities.
- Their physical rule should apply to compatible world targets.
- Tool side effects should be causally understandable.

### FR-7 Validation event
- Final 10–15 seconds may introduce the goal's validator into the world.
- After time expires, a short physical validation test runs.
- Example: tiny helicopter lands on the hair helipad.
- Validation is part of gameplay comedy, not only score presentation.

### FR-8 Shared result/economy
- Team receives one shared result and shop-money delta.
- Major accidents reduce payout.
- Individual awards are comic post-round attribution only.
- No primary PvP winner in current prototype.

### FR-9 Networking
- 1–4 host/client networking.
- Host authoritative.
- Replicate game facts rather than every hair cosmetic transform.

### FR-10 Incident story capture
- Log causally meaningful incidents: actor, tool, target, effect, resulting major event.
- These logs support debugging and future highlight selection.

## Non-functional requirements

- Prototype must be runnable without paid middleware.
- Repeated development setup should be scriptable.
- C# project should compile outside the editor where practical.
- Avoid architecture that requires one network transform per hair clump.
- Maintain playable framerate on ordinary development hardware with 4 local instances at reduced settings where feasible.

## Explicit non-goals for the current acceptance slice

- Full salon economy.
- Shop decoration/upgrades.
- Final art.
- Dozens of customers.
- Realistic animal AI.
- Complex injury system.
- Professional hair rendering.
- Competitive ranked balance.
- Fixed barber roles.
- Mandatory realistic salon process.

## Prototype success questions

1. Is four people sharing one head funnier/more social than separate stations?
2. Can players understand who/what caused an accident?
3. Do customer micro-movements produce laughter rather than irritation?
4. Do dangerous tools feel useful enough that players voluntarily risk them?
5. Does the validation event create anticipation and final-second chaos?
6. Can players retell a causal story after the round?
