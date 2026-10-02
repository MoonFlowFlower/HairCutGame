# 17 — Customer Behavior & Panic Spec

## Purpose

The customer should create tiny unpredictable-but-understandable perturbations that turn precision work into stories.

The customer is not an enemy. The player should usually think:

> "He moved because we terrified him," not "the game randomly cheated."

## Hidden panic model

Maintain hidden normalized panic/arousal, e.g. 0..1.

Stimuli examples:
- loud gunshot,
- bullet/fast object near head,
- nearby flame,
- actual burning,
- strong blower in face,
- collision/impact,
- large explosion,
- teammate/customer scream,
- validator approaching (small increase),
- repeated tool mistakes.

Panic should decay over time when danger stops.

## Reaction bands

### Low
Mostly cosmetic:
- sweating,
- eyes tracking tool,
- gulp,
- tiny head jitter/drift.

### Medium
Short gameplay movement:
- duck,
- turn head,
- recoil,
- scratch head.

### High
Rare stronger action:
- sharp recoil,
- brief stand-up,
- scream,
- one or two panic steps.

Do not implement constant running in the baseline.

## Anticipation requirement

Gameplay-relevant reactions should generally telegraph 0.3–1.0 seconds beforehand.

Examples:
- duck: eyes widen + shoulders tense + inhale -> duck,
- scratch: glance upward + hand begins lifting -> scratch,
- turn: look toward sound -> head follows,
- sneeze future option: nose twitch + inhale -> sneeze.

This lets skilled players react while preserving accidents.

## Rate limiting

Use cooldowns/hysteresis. The customer should not chain medium reactions every second.

Suggested starting values are implementation-tunable; prioritize human playtest feel over exact numbers.

## Collision truth

If head moves for gameplay, the interaction/collision target must move with it. Do not fake only the visible animation.

## Special customer hooks

### Llama
Signature: spit under medium/high panic.
- short cone/projectile,
- can briefly obscure player view or wet nearby surface,
- avoid long stun.

### Chicken
Signature: peck nearby tool when curious/agitated.
- may knock it from hand or nudge dropped tool,
- rate limited.

### Alien
Signature: one localized head-material swelling event at high panic.
- creates a new bump/volume,
- not continuous regeneration.

Keep each first-pass archetype to one special rule.
