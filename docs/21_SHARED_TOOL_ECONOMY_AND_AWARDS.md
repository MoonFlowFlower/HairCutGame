# 21 — Shared Tool Economy + Personal Spotlight

## 1. Principle

Shared money creates tension because one player can make a risky spending decision that affects everyone. This is useful only if the team still has enough ordinary tools to keep playing.

Tool scarcity should create conversation, not routine waiting.

## 2. Three tool tiers

### Tier A — Core/free tools

Enough copies must exist that 1–4 players can keep doing meaningful work.

Initial examples:
- clippers/basic cutter;
- growth spray;
- glue gun;
- blower;
- vacuum/transfer tool.

Exact copy counts may be tuned, but the playtest target is: no player should regularly stand idle because a single mandatory tool is occupied.

### Tier B — Shared dangerous tools

One or few physical copies in the room; strong, fast, risky; never the only valid solution.

Examples:
- hedge trimmer;
- sniper rifle;
- flamethrower.

Scarcity here is allowed to create "give me that" moments.

### Tier C — Ordered special tools

A small order terminal/dispenser spends the **shared wallet** on dramatic temporary/one-use options.

Prototype candidates (implement only the smallest useful subset first):
- shave bomb;
- super growth bomb;
- special wig/structure bundle;
- liquid-nitrogen canister.

Do not add a large shop catalog in v0.4.

## 3. Order flow

Keep it physical/simple, not a menu-heavy economy game.

Suggested minimal interaction:
1. player interacts with terminal;
2. selects one of 2–4 special tools;
3. shared price is shown clearly;
4. order enters a short visible pending state (~2.5 seconds);
5. teammates may cancel during that window using the physical terminal/button;
6. if not cancelled, shared wallet is charged and the tool is delivered/spawned physically.

This is deliberately not a formal majority-vote UI. The point is a short social "Are you really buying this?" moment.

If the cancel interaction proves annoying, it may be removed after human test. Do not block all other work while an order is pending.

## 4. Economy guardrails

- All current goals must remain completable with Tier A/B tools already in the room.
- Special orders are faster, stranger or more flexible, not mandatory keys.
- Bankruptcy/shared shop failure remains a team outcome.
- Personal awards never reimburse or tax the shared wallet.
- A single player cannot spend money faster than the physical order cadence allows.
- Expose price/spend clearly enough that blame is understandable.

## 5. Personal spotlight, not personal victory

At the end of each job, reveal a small set of awards derived from the Impact Ledger and existing causal facts.

Candidate awards:

- **Biggest Contributor** — highest confirmed/surviving positive impact.
- **Rescue Barber** — highest valid rescue credit.
- **Biggest Accident** — largest single negative event / incident severity.
- **Chaos Magnet** — most multi-entity collateral involvement.
- **Big Spender** — highest shared-wallet spend initiated.
- **Risky Genius** — dangerous-tool action with strong positive result and no severe incident.
- **Last-Second Save** — strongest valid rescue in final 15 seconds.
- **Self-Hair Victim** — greatest own-hair loss/change, using existing barber-hair facts.

Only display a few per job. Do not guarantee every category every time.

## 6. Award rules

- Awards are presentation/storytelling. They do not create a winner.
- Hide all personal impact metrics during build.
- Prefer names/titles and replay clips over raw numbers.
- It is acceptable for awards to be approximate, but they should be explainable from visible events.
- Deterministic tie-breaking is required for network consistency.
- Do not award Rescue Barber for a crisis primarily caused by that same player.
- Do not reward repeated destroy/restore oscillation.

## 7. Result screen shape

Team result remains visually primary:

```text
JOB RESULT
- Commission quality / validation
- Incidents / damages
- Shared payout or loss
- Shared wallet after settlement
```

Then a lighter personal section:

```text
SPOTLIGHT
- Rescue Barber: P3
- Biggest Accident: P1
- Big Spender: P4
```

If a highlight replay exists, lead into or out of the spotlight with the selected clip.

## 8. Why this is preferable to four competing shops

The design target is not deliberate sabotage. The tension comes from:
- different risk tolerance;
- disagreement about the right fix;
- wanting credit/spotlight;
- spending shared money;
- limited dangerous tools;
- physical interference;
- customer reactions and accidents.

Keep those sources of conflict strong enough that a separate PvP win condition is unnecessary.
