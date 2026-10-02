# 35 — v0.6 Human Playtest Protocol

> **状态更新（2026-09-29）**：用户已选择 B 为唯一开发主线，A 已归档。以下 A/B 实验流程和判定阈值保留作历史记录，不再是后续 B 开发的前置门槛；不代表实验已经通过。B 的设计约束仍适用，当前启动、验收与文档优先级见 [42 — B 开发基线](42_B_DEVELOPMENT_BASELINE.md)。


Automation can verify correctness. It cannot decide this experiment.

## Network recovery annotation (2026-09-29)

Record the shared package build, host/client regions, connection route, and any network warnings, pauses or retries for each round. The 30-second recovery pause does not consume construction time. Keep the existing gameplay rules and scoring; mark network-affected rounds separately instead of treating interrupted coordination or waiting time as evidence for either hypothesis. Collect both engine and network logs through `Open_Test_Logs.cmd`. See `41_NETWORK_RECOVERY_AND_TESTS.md` for the recovery checks and real-route acceptance.

## Participants

Primary test group:

- four people who already regularly play games together;
- use their normal voice-chat setup;
- developer should not occupy one of the four player slots if avoidable.

A second friend group is strongly preferred for counterbalancing.

Do not use strangers as the only evidence for teasing, blame, hiding, laughter or spontaneous coordination.

## Explain rules, not hypothesis

Tell players all information a shipped game would clearly teach, including:

- mission is to land the helicopter;
- customer can notice what happens around them;
- customer reacts directionally;
- mirror can be blocked;
- another player can brace the customer;
- helicopter will physically approach and test the work.

Do **not** tell them:

- “we are testing whether you hide things from the customer”;
- “please try distracting the customer”;
- “B is supposed to be funnier”; or
- any pre-registered success metric.

## Recommended sequence

Each version must be played at least three times by a group.

For one group:

> **A - B - B - A - A - B**

For a second group, reverse/counterbalance:

> **B - A - A - B - B - A**

This provides three rounds of each variant and makes the third exposure observable.

If available time forces a shorter session, do not claim the experiment has controlled for novelty.

## Recording

Record:

- all four first-person views;
- game audio / normal group voice chat, with participant consent;
- one overhead/spectator camera if technically practical;
- authoritative event log / timestamps.

The overhead view is analysis evidence only, not a recommendation to change normal gameplay camera.

## Observer sample every 10 seconds

For each player, label the dominant behavior:

- **Primary work** — directly shaping/solving mission;
- **Assist/customer management** — bracing, blocking mirror, deliberate distraction, fetching useful object, coordinating a dependency;
- **Repair** — fixing a concrete accident;
- **Waiting** — lacks a useful action / watching passively;
- **Novelty/chaos** — using systems mainly because it is funny, not because it plausibly serves the mission.

Do not moralize. The point is to detect whether multiplayer creates useful concurrent needs or merely spare players looking for entertainment.

## After each round — immediate independent question

Separate answers if possible. Ask only:

> “What was the funniest / most memorable moment in that round?”

Record whether players independently describe the same incident and whether they can explain a causal chain:

> because X -> Y -> Z.

## End of session

Ask:

> “If we play one more round right now, which version do you want: A or B?”

Do not ask “which design is better?”

Also ask one short reason after the choice.

## Solo baseline

Before friend-group test, a developer must complete B solo several times.

Required outcome:

- B is demonstrably winnable with reasonable safe play;
- it should not be so comfortable that the customer/approach pressure is irrelevant;
- record completion rate and approximate margin, but do not tune to force multiplayer impossibility.

The intended question is whether multiplayer creates **new behavior**, not whether solo is forbidden.

## Dangerous-tool intent coding

For each notable high-risk tool use, classify using voice/action context:

- **Task-motivated** — player states or clearly demonstrates a mission reason (“we’re out of time, burn this edge”, “I need the hole now”);
- **Novelty/trolling** — use is primarily “watch this” / unrelated destruction;
- **Ambiguous**.

Also timestamp whether the use occurs during pressure windows:

- after T=45 flyby;
- final T<=20 approach;
- active accident/recovery.

This is more informative than raw tool-use count alone.
