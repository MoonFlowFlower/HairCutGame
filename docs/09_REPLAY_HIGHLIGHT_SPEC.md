# 09 — Incident Logging & Highlight Replay

Replay is valuable but lower priority than shared-head play quality.

## Record first, replay later

Even before full replay exists, log incident events with enough causal metadata to answer:
- who acted,
- which tool,
- what was initially hit,
- what secondary consequence occurred,
- which player/customer/prop was affected,
- approximate severity/time.

## High-value highlight candidates
- customer duck causes precision tool to hit teammate,
- one tool action affects 3+ entities,
- biggest head-quality drop in a short window,
- biggest last-10-second save,
- player is moved by tool and causes another incident,
- validator creates a final chain failure,
- commitment tool countdown panic,
- customer special behavior triggers a chain accident.

## Replay target
When implemented:
- rolling 8–12 second snapshot/event buffer,
- choose 5–8 second clip,
- spectator camera is allowed,
- slow motion optional,
- no need for perfect deterministic resimulation if snapshot playback is simpler.
