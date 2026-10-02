# 05 — Hair / Head Material Spec

## Design goal

The head material must be a toy, not decoration.

It should be easy to read when players:
- add it,
- remove it,
- stretch/redirect it,
- blow it,
- glue it,
- freeze it,
- burn it,
- puncture it,
- transfer it.

Prefer chunky low-poly blocks/tufts over realistic strand simulation.

## Baseline representation

Use a moderate number of logical scalp regions (roughly 24–40 for baseline human) with procedural/low-poly visual chunks.

Each region stores gameplay facts such as:
- volume/amount,
- length/extent,
- direction,
- stiffness,
- attachment state,
- wetness,
- thermal state,
- burning/charred state,
- material type.

## Important behavior

### Material conservation where useful
Removed/transferred material may become a detached bundle that vacuum/transfer tools can store/redeploy.

### Stable by default
Hair/material should not constantly collapse due to tiny physics noise. Instability should usually come from clear causes or risky states.

### State combinations
Useful combinations include:
- long + flexible -> blower has strong effect,
- glued -> shape holds but is harder to correct,
- frozen -> rigid but brittle,
- wet -> heavier, less flammable, more droop,
- burning -> volume decreases over time and may spread.

### Shared interaction family
Player/barber hair should reuse as much logic as practical. It may use fewer patches than customer hair.

## Future material types

Do not fully implement now, but preserve seams for:
- wool,
- feathers,
- tentacles,
- plant growth,
- foam/jelly.

A material type can customize coefficients and one or two special responses without replacing the entire system.
