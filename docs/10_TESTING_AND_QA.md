# 10 — Testing & QA v0.3

## Automated logic tests

Where practical test:
- goal voting and tie handling,
- round state transitions,
- shared money calculation,
- panic accumulation/decay/cooldowns,
- reaction eligibility/rate limiting,
- head-material effect application,
- tool ownership validation,
- helipad score calculations,
- incident event attribution.

## Engine smoke tests

### Solo
- enter shared salon,
- pick up each required tool,
- edit central head,
- customer reacts,
- complete helipad round,
- validator runs,
- result appears.

### Host + client
Verify:
- both manipulate same head,
- no duplicate customer,
- tool ownership is consistent,
- reaction pose is consistent enough for precision,
- shared timer/goal/result match,
- player body impulses visible on both.

### Host + 3 clients
Attempt when hardware permits:
- central customer remains readable,
- no major replication flood,
- tool collisions do not diverge catastrophically,
- disconnect one client and continue.

## Migration regression checks

Specifically search for old behavior:
- four customers spawning,
- player-specific station ownership,
- individual submit controls,
- separate per-player goals,
- individual primary winner logic.

These must not remain on the main mode path.

## Human playtest observation sheet

Watch for:
- Do players cluster around the same head naturally?
- Do they ask each other to move/stop/wait?
- Can observers tell who caused a visible accident?
- Are tools selected to solve problems, not only to troll?
- Does panic animation provide enough warning?
- Is customer motion rare enough to remain funny?
- Does the final validator create a shared "please hold" moment?
- Can players retell the round as a causal story?

## P0 defects
- session cannot complete,
- head state diverges badly between host/client,
- customer collision pose differs enough to invalidate precision,
- tool use crashes/desyncs,
- main flow still spawns independent player customers.

## P1 defects
- customer reacts too frequently to play,
- one tool trivializes all goals,
- unavoidable chaos obscures causality,
- players cannot physically access useful angles,
- validator fails for reasons players cannot understand.
