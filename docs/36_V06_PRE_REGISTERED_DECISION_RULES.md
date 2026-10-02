# 36 — Pre-Registered Decision Rules

> **状态更新（2026-09-29）**：用户已选择 B 为唯一开发主线，A 已归档。以下 A/B 实验流程和判定阈值保留作历史记录，不再是后续 B 开发的前置门槛；不代表实验已经通过。B 的设计约束仍适用，当前启动、验收与文档优先级见 [42 — B 开发基线](42_B_DEVELOPMENT_BASELINE.md)。


Write these rules into the repository before the friend-group A/B session and do not retroactively redefine them after seeing results.

Network-recovery annotation: retain these decision thresholds. Report network-affected sessions explicitly and separate pause/wait behavior from the social gameplay observations. Successful reconnection or synthetic network tests do not constitute evidence that B is the stronger game, or that the Canada–China route is comfortable. Use the same package on both ends and retain both peers' logs.

## Primary question

Does Variant B create sustained, socially meaningful behavior that is absent/weaker in A, rather than merely adding difficulty or novelty?

## B continuation criteria

After at least three B rounds with the same friend group, count the four criteria below.

Variant B earns a strong “continue investing” signal if **at least 3 of 4** are met **and the behavior is still present on the third B exposure**.

### Criterion 1 — Customer management becomes gameplay

Across the three B rounds:

- at least two distinct spontaneous behavior types occur repeatedly, such as:
  - bracing;
  - blocking the mirror;
  - deliberate directional sound distraction;
  - warning teammates to stop/hide something;
  - physically covering/removing evidence before the customer sees it.

At least one of these behaviors must still occur spontaneously in the third B round.

If players know the rules but never use these systems unless prompted, perception is probably only punishment.

### Criterion 2 — Dangerous tools are useful bets, not boredom toys

The majority of notable dangerous-tool uses should be judged **task-motivated** rather than novelty/trolling, especially during:

- post-flyby recovery;
- final 20 seconds;
- active rescue.

Do not require a specific numeric tool frequency before observing the first test; classify intent and timing first.

### Criterion 3 — Multiplayer creates new behavior, not only faster work

In 4P B, the group should repeatedly exhibit interactions unavailable to the solo baseline, such as:

- brace + tool use;
- attention manipulation + opposite-side work;
- mirror blocking + hidden repair;
- simultaneous response to flyby/landing pressure.

Red flag:

- `Waiting + Novelty/chaos` exceeds roughly one-third of sampled player-time and does not improve by the third B round.

The one-third threshold is a diagnostic, not a statistical proof.

### Criterion 4 — Shared causal story

In at least two of the three B rounds:

- at least three of four players independently identify the same memorable incident;
- their descriptions contain a recognizable causal chain rather than “everything was random”.

Example acceptable form:

> “He turned because Dan started the blower, then my cut went wide, then Mei grabbed his head while the helicopter came down.”

## Supporting signals

These do not decide B by themselves:

- end-of-session “one more round” preference;
- laughter count;
- raw score;
- raw failure count;
- total dangerous-tool count.

A harder version is not automatically better. A funnier first round is not automatically replayable.

## Strong reasons to stop/pivot B

Reconsider the living-customer direction if, after fair tuning and three exposures:

- perception-management actions remain unused unless explicitly prompted;
- players describe customer attention mainly as annoying randomness;
- B simply lowers success rate without adding coordination;
- high-risk tools remain mostly novelty/trolling;
- four players still collapse into one main worker plus spectators;
- stories remain fragmented/non-causal (“it was just chaos”).

## Interpretation discipline

If B loses, preserve reusable implementation pieces only when they independently improve the game (e.g. live landing control, better reaction telegraphs). Do not rationalize the result into automatic support for B.
