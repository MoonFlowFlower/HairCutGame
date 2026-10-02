# 22 — v0.4 Acceptance and Tests

## 1. Technical Definition of Done

v0.4 is technically ready for human playtest when:

1. Existing v0.3 solo/2P/4P shared-head flow still builds and completes.
2. Host records deterministic Impact Ledger events for meaningful tool actions.
3. Continuous tools/strokes are aggregated rather than logging every frame.
4. Live goal quality reuses existing goal/validator logic and does not disagree materially with final validation.
5. Crisis creation and recovery work for both player-caused and environment-caused drops.
6. Self-caused crisis rescue farming is blocked by test.
7. Positive/negative/surviving impact totals are deterministic.
8. At least three post-job spotlight awards can be produced from real play facts.
9. Existing replay system can choose a clip from Impact Ledger ranking.
10. Shared-wallet special-tool ordering works host-authoritatively, including pending/cancel/spend/delivery.
11. Core tools remain available without mandatory waiting.
12. Result screen keeps team payout/failure primary and personal awards secondary.
13. Network clients agree on result/award facts.
14. No regression to v0.3 hair sculpting, panic movement, player forces, validation or replay safety.

## 2. Pure tests to add

Minimum recommended coverage:

- positive/negative delta aggregation;
- continuous action window coalescing;
- epsilon ignores meaningless micro-deltas;
- crisis threshold creation;
- unattributed crisis allowed;
- multi-player recovery split;
- recovery capped at old baseline;
- same player causes crisis then fixes it -> no rescue credit for own caused portion;
- another player rescues the same crisis -> receives credit;
- last-15-second urgency affects highlight rank, not team score;
- provisional positive impact reduced when immediately undone;
- deterministic award ties;
- order pending/cancel/commit;
- shared wallet never goes below allowed floor / follows existing bankruptcy semantics;
- personal awards do not alter payout;
- highlight candidate derived from ledger chooses expected replay window.

## 3. Engine/integration checks

- 4P simultaneous edits around one head all generate attributable action windows.
- customer duck/turn during a precision action can produce a measurable accident with correct player/environment attribution.
- blower affects another player's body/tool/hair and ledger remains stable.
- order terminal physical interaction works for host and remote clients.
- two clients attempt order/cancel near-simultaneously; host resolves one deterministic result.
- result/spotlight facts match across all peers.
- replay remains historical/visual and does not mutate live state.

## 4. Human acceptance — the actual reason to build v0.4

Automation cannot pass these.

Run at least one 3–4 human session and observe:

### A. Concurrency
Are 3–4 players usually finding meaningful things to do, or does someone repeatedly wait for one tool/one phase?

If players are idle for 5–10 seconds because a required tool is occupied, fix availability/goal structure rather than inventing chores.

### B. Productive disagreement
Do players naturally say things like:
- "Let me do it."
- "Don't cut there."
- "We don't need to buy that."
- "I can save this."

This is a positive sign.

### C. Shared stakes
Even while arguing/stealing spotlight, do players still care whether the customer/job succeeds?

If players discover that deliberate sabotage is the dominant fun/strategy, the personal layer is too strong or team stakes too weak.

### D. Special-tool temptation
Does spending shared money on a risky tool create a real hesitation or argument?

If the special tool is always obviously correct, increase cost/risk or improve basic alternatives.
If nobody ever buys it, make it more tempting/readable.

### E. Award plausibility
After reveal, do players mostly understand why someone got Rescue Barber / Biggest Accident / Big Spender?

Perfect mathematical fairness is not required. Awards should create conversation, not confusion.

### F. Story recall
After a job, can players retell a short causal story:

> "You bought the stupid thing, he ducked, the shot went wide, then I glued the platform back with three seconds left."

This is the strongest v0.4 success signal.

## 5. Stop conditions

Do not expand into a large meta-progression, four-shop campaign, personal ranking ladder, cosmetic economy, or many new customers before the human session answers the questions above.
