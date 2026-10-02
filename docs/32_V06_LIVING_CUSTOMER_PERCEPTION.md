# 32 — Living Customer Perception

## Design objective

Replace “random panic motion” with a readable loop:

> event is perceived -> customer notices -> players get a window -> customer reacts physically.

For the v0.6 experiment, perception has only three important sources:

1. mirror-visible danger;
2. sudden directional game-world sound;
3. approaching helicopter / outdoor attention.

Do not add more senses until these are proven useful.

## A. Telegraph sequence

Use deliberately explicit experiment feedback:

### Stage 0 — Unaware
Normal idle behavior.

### Stage 1 — Notice (`?`)
The customer has detected something and begins orienting eyes/head toward the source.

Target window: approximately 0.8–1.5 seconds, configurable.

Players must have enough time to say “don’t look”, block the mirror, stop the tool, extinguish the fire, or brace the head.

### Stage 2 — Commit (`!`)
The customer is about to perform the full reaction.

Short final warning, e.g. 0.25–0.6 seconds.

### Stage 3 — Physical reaction
Examples already supported by the prototype can be reused:

- turn;
- duck;
- recoil;
- look upward;
- short stand/reseat only if already reliable.

The authoritative customer/head transform used for rendering must also be used for actual hair/tool queries.

The `?` and `!` are experiment aids, not locked final art direction.

## B. Mirror visibility

A dangerous event behind the customer should only be “seen in the mirror” when the mirror route is unoccluded.

Full optical reflection is unnecessary. Use a deterministic proxy that preserves the gameplay truth:

- define the customer eye / gaze origin;
- define one or more mirror observation proxy points/areas;
- determine whether the relevant dangerous event is in a mirror-visible zone;
- ray-test the eye-to-mirror observation path against valid blockers.

### Valid blockers in v0.6

At minimum:

- another player body;
- one physical magazine / cover-board prop near the chair.

If blocked, the mirror perception does not trigger.

This must be mechanically real. Do not fake it with a cosmetic animation if the customer still “sees through” the blocker.

## C. Directional sound perception

Create a small authoritative event interface for **sudden** sound attention.

Candidate events:

- initial startup of a loud tool;
- gunshot;
- impact/drop;
- explosion;
- abrupt fire burst;
- helicopter pass/approach.

The customer should orient toward the sound’s world position.

### Habituation

Do not retrigger full attention continuously from a sustained sound.

A simple prototype solution is enough:

- large novelty response on first onset;
- strong cooldown / reduced salience for repeated events from the same source class/direction;
- salience recovers after quiet time.

This prevents a continuously running blower from causing permanent head twitching and makes deliberate distraction a limited tactical window rather than a spam exploit.

## D. Customer attention may be exploited

This system must support intentional play, not only punishment.

Example:

- Player A briefly starts a loud tool on the customer’s right.
- Customer telegraphs a turn to the right.
- Player B uses the resulting left-side access window.

If players never discover a useful reason to manipulate attention, the system is probably only friction.

## E. No microphone in primary implementation

Do not implement live microphone detection for v0.6 phase 1.

Optional later experiment support:

- developer/manual trigger that creates a directional “voice/laughter attention event” for Wizard-of-Oz playtests;
- no user microphone permission required.
