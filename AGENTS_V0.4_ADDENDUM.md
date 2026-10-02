# AGENTS v0.4 Addendum — Co-op With Ego

This addendum applies on top of the repository's existing `AGENTS.md` and current `PROJECT_STATE.md`. If an older design document conflicts specifically on personal competition, contribution awards, shared-money ordering, or highlight attribution, this addendum and `docs/20-22` take precedence.

## Product intent

The main mode remains cooperative at the outcome level: one shared customer, one shared job, one shared wallet, one shared success/failure state.

The new layer is **social tension through ego, risk preference, shared spending and attribution**. Players should have reasons to disagree about *how* to save the job without being rewarded for simply griefing the team.

The desired player conversation is:

- "Let me do it."
- "Don't touch that."
- "Why did you buy that?!"
- "I can save this."
- "You caused this in the first place!"

The undesired dominant conversation is:

- "My optimal strategy is to destroy the team's work so I can win personally."

## Implementation ownership

Work incrementally on the current v0.3 codebase. Audit and reuse existing causal stats, validators, shared wallet, incident attribution and replay/highlight systems before adding parallel systems.

Do not ask routine questions. For reversible technical choices, choose the smallest robust design consistent with docs/20-22, record important decisions in `PROJECT_STATE.md`, implement, test, fix regressions, and continue.

## Hard rules

1. **No mandatory sequential service pipeline.** No wash -> dry -> cut -> style lockstep.
2. **No personal win score.** Personal metrics are post-job story/spotlight signals only.
3. **No contribution metric may affect authoritative job success or shop money in v0.4.** It is observational/presentation data.
4. **Core tools cannot be economically or physically scarce enough to create routine idle time.** Special tools may be scarce/risky.
5. **Contribution/rescue metrics must be host-authoritative and deterministic from gameplay facts.** Never trust a client-reported score delta.
6. **Do not infer intent.** Detect measurable state change and context only.
7. **Do not reward self-created rescue farming.** If a player causes the crisis, restoring their own damage does not earn rescue credit for that crisis.
8. **Metrics remain hidden during the 75-second build.** Reveal awards/results afterward.
9. Reuse impact events for replay/highlight selection instead of building a separate unrelated highlight heuristic.
10. Human playtest remains the authority on whether awards feel funny/plausible and whether the new tension improves the experience.
