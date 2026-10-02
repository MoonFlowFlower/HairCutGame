# Physical Goal Props & Validation

## Hard rule

**If the finished commission visibly uses a prop, the prop must exist in the world before validation and players must be responsible for placing/using it when appropriate.**

Validation may activate, test or animate an existing prop. It must not magically spawn the required prop directly into a success position.

This rule is specifically intended to make the build phase richer and create accidental interactions before validation.

## Prop sources

Each goal prop definition has a source:

### Stocked
Already present on a labeled shelf/tray/rack in the shop.

Examples:
- eggs,
- underwear,
- simple toy car,
- common decorations.

### Commission supplied
A crate/tray arrives at job start because the customer/job includes the item.

Examples:
- unusual trophy,
- ceremonial object,
- specific fragile cargo.

### Orderable
Players spend shared wallet money at the existing order terminal. Delivery produces a real crate/chute object in the shop during the build.

Examples:
- special rocket,
- premium reinforcement item,
- rare gimmick prop.

Required orderable props must never make a randomly offered goal mathematically unwinnable. The goal offer must ensure the team can obtain the required prop, or the commission must supply a fallback/credit.

## Player placement

Props are ordinary host-authoritative world entities using the smallest suitable physical representation.

Players should be able to:
- pick them up,
- carry them,
- place/attach them where appropriate,
- accidentally knock/blow/drop them,
- recover them if still available.

Avoid per-particle complexity. Use simplified rigid/support checks consistent with the current prototype.

## Goal examples

### Bird Nest
At build start, three actual eggs exist in a tray or delivered box.
Players physically place all three in the constructed nest.
Validation checks those exact egg entity IDs:
- currently supported by the hair/nest,
- not broken/lost,
- remain contained during the test motion.

Do not spawn eggs at validation.

### Clothesline
Underwear pieces exist before validation.
Players physically hang/attach them.
Validation applies wind/load to the same items.

### Rocket Launch Silo
Rocket exists before validation, either stocked or delivered.
Players physically place/insert it into the silo.
Validation arms/launches the same rocket entity.
If it is crooked because the players placed it badly, that is part of the result.

### Hair Bridge
Toy car/truck exists at a staging mark before validation.
Players may position it at the start. Validation drives/advances that same entity across the structure.

### Cat Structure
Cat exists in the shop before validation as a living world actor.
Players do not spawn a cat from UI. Validation may call/attract the existing cat to climb/use the structure.

### Helicopter Helipad
The RC helicopter exists on a shelf/stand/staging pad before the final test.
Validation transitions that same helicopter into approach/landing. Do not instantiate a fresh success helicopter at the end.

## Validation contract

Goal validators receive the expected prop entity IDs/state from the build phase.

They may:
- start motors/flight,
- apply wind/load,
- move an existing actor under deterministic control,
- evaluate support/contact/containment,
- record failure trajectories.

They may not:
- replace a missing prop with a new one,
- teleport a required prop into a correct position,
- silently repair player placement,
- ignore destruction/loss caused during construction.

## Replay/impact integration

Meaningful prop incidents should enter the existing event/highlight system where possible:
- egg falls out,
- rocket launches crooked,
- helicopter is knocked off staging area,
- underwear is blown away,
- cat is displaced,
- vehicle falls through bridge.
