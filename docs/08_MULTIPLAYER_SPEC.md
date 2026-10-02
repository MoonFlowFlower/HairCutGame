# 08 — Multiplayer Spec v0.3

## Topology
- 1 host + up to 3 clients.
- Host authoritative.
- ENet for prototype.

## Shared authoritative entities
- one active customer,
- customer head material,
- shared round/goal,
- tool ownership/use,
- validation props,
- shared shop wallet/result,
- panic/reaction state.

## Player-owned entities
- movement/input intent,
- held-tool request state,
- player hair presentation/state authority still resolved by host.

## Client requests
Examples:
- RequestPickup(toolId)
- RequestDrop(toolId)
- RequestUseTool(toolId, origin, direction, secondaryMode)
- RequestGoalVote(goalId)
- RequestPropInteract(propId)

Host validates proximity/ownership/cooldowns/ammo and computes results.

## State replication strategy

Replicate:
- compact logical head-material state/deltas,
- authoritative reaction phase/parameters,
- tool held/active facts,
- shared timer/goal,
- relevant prop state,
- incident events.

Avoid:
- per-frame replication of every visual tuft,
- networked debris spam,
- networked full ragdoll bones unless absolutely necessary.

## Customer reactions
Reaction selection/trigger should be authoritative so clients do not see different head poses during precision actions.

Replicate reaction id/start time/intensity/parameter seed and drive deterministic/close-enough motion.

## Disconnect behavior
- If a client leaves, their player/tool is cleaned up or dropped safely.
- Round continues with remaining players.
- Shared goal/customer does not disappear.

## Local test support
Provide scripts to start:
- solo,
- host + 1 client,
- host + 3 clients where resources permit.
