# 45 — B 派对核心循环：实施计划

> 设计意图见 [43](43_B_PARTY_LOOP_DESIGN.md)，需求条目见 [44](44_B_PARTY_LOOP_REQUIREMENTS.md)。本文规定顺序、依赖、代码切入点、测试和每阶段的退出条件。
>
> 代码位置是 2026-09-30 规划时核对过的。开工前先用 `rg --files` 和 `rg` 确认，不要按类名猜文件名（见 `AGENTS.md`）。

## 0. 工作方式

- 按阶段 1 → 7 的顺序实施。每个阶段内部小步提交，每一步都要可编译、可测试。
- 仓库**没有 Git**。每个阶段开工前，先打一个源码快照：`artifacts/b-party-p<N>-before-<日期>.zip`，只包含 `src/ tests/ scenes/ scripts/ docs/ project.godot *.csproj *.sln`。
- 每个阶段结束时：
  1. 在 `PROJECT_STATE.md` 追加一条记录：做了什么、哪些“初值”改成了多少、实际跑过的测试和结果、剩下的缺口。
  2. 若快照结构有变化，提升 `NetProtocol`。
  3. 按第 9 节跑验证。
- 阶段 2、4、6 结束后各打一个试玩包（`scripts/package_b.ps1` 加 `verify_b_package.ps1`），作为 owner 真人试玩的检查点。
  - 试玩结果不阻塞下一阶段的**实现**。
  - 但如果试玩结果和假设相反，要先回到 owner 那里确认，再继续。
- 不做需求文档以外的扩展。遇到需求没有覆盖的细节时，选最简单、可以撤回的做法，并记录在 `PROJECT_STATE.md`。

## 阶段 0 — 准备

1. 打快照。
2. **扩展试玩日志分析**：扩展 `scripts/b_playtest_review.ps1`，或新增一个脚本，从真人会话的 JSONL（位置见 `PROJECT_STATE.md` 或打包说明）里统计以下指标，作为后续阶段对比的基线：
   - 每名玩家按住工具的时间占比
   - 最长闲置时长（可以排除开局讲解的前 N 秒，N 作为参数）
   - 事故发生后第一次他人补救的延迟
   - 放置委托物的次数
   - 离店终检的结果
   - 各工具的使用次数
3. **记录当前 B 的关键常量**，便于回退：
   - `ExperimentState.GrowthCapacity=3`
   - `Dwell=3`
   - `BuildDuration=150`
   - 火灾规则：烧着超过 12 块时 `Incident(25)`
   - `Penalty>=50` 阻止成功

## 阶段 1 — 看懂目标与边界（R1-x）

| 项 | 切入点 |
|---|---|
| R1-1 伤害与成败脱钩 | `src/Core/Experiment.cs`：`LandingBlockers` 删除 `Job.Penalty>=50`。`LandingIssue.CustomerDamage` 保留为枚举值，但 B 不再触发。`src/UI/ExperimentHud.cs` 和 `src/Core/LandingFeedback.cs` 的文案同步修改 |
| R1-2 火灾改为顾客忍耐 | `src/Core/Session.cs` 的顾客循环（约第 265–271 行，烧着超过 12 块时 `Incident(source,c.Slot,25)`）：B 走新的忍耐逻辑，A 和旧模式保持原样。在 `CustomerState` 或 `ExperimentState` 加 `Tolerance` 字段。满值时调用 `ResolveExperiment(false,"customer fled")` 或其等价物，写入新的 `LandingIssue` 或结果原因，并做双语 |
| R1-3 预警 | `src/Presentation/ExperimentView.cs` 负责顾客头顶的环形指示和表情；`src/UI/ExperimentHud.cs` 负责辅助文字 |
| R1-4 顾客实时反应 | 新增 `src/Core/CustomerRemarks.cs`：纯逻辑，房主根据 `Patch` 的温度、湿度、燃烧、冰冻以及道具事件选出台词 ID，带冷却和优先级。台词 ID 进快照，表现层本地显示气泡 |
| R1-5 移除狙击枪 | 生成 B 工具架的位置（先用 `rg -n "ResetTools"` 定位）。B 不再生成 `sniper`；`Definitions.cs` 保留定义 |
| R1-6 实时缺陷显示 | 复用 `LandingFeedback`、`ContactFeedback`，以及现有的探针标记 |

**测试**：

- Core：
  - 伤害很高时，仍然可以成功。
  - 模拟一场火灾：忍耐值上升和下降的曲线；满值所需时间 ≥ 8 秒。
  - 在 X 秒内喷水灭火，委托不会终止。
  - 台词的冷却和优先级。
- 引擎 B 检查（`src/Bootstrap/ExperimentChecks.cs`）：
  - 真实火焰工具点火后，忍耐值上升，灭火后回落。
  - 不再出现“伤害超过 50 就不能成功”。

**退出条件**：`verify_b.ps1` 全部通过；上述新测试通过；截图里能看到预警和台词气泡（中英文各一张）。

## 阶段 2 — 委托物上桌与离店终检（R2-x）

这是风险最高的阶段，按下面的顺序做：

1. **探针跟随委托物（R2-2）**：`LandingProbes(WorldState, PropState)` 目前以 `heli.Position` 为中心、轴向固定。改成按 `prop.Rotation.Y` 旋转网格。Core 测试：同一块平台，道具偏心放置和旋转放置都能被正确评估。
2. **桌面拿放（R2-1）**：
   - `PhysicalProps.Tick`、`Session.HandleProp`：B 目前把 `Goal==0` 的直升机排除在拿放之外，要取消这个排除。
   - `StageExperimentProps`：初始位置改到桌上。
   - `ReleaseProp`：放置时要保存 yaw。
3. **试放与滑落（R2-3）**：
   - 委托物附着在头上时，每 tick 计算支撑。连续不合格超过阈值，就把 `Attached` 设为 false、`Released` 设为 true，让它落到地上。
   - 把稳定秒数写进 `PropState.HeldTime`，或者单独建一个字段。
   - **不要**在施工期间自动结束。
4. **离店预兆与按回座位（R2-4、R2-5）**：
   - 离店状态放进 `ExperimentState`：`Leave` 阶段、开始时间、剩余可延长时间。
   - 按回座位复用 `CanBrace`/`SetBrace`。
5. **离店终检（R2-6）**：
   - 新增一个“走动”阶段：共享头的权威位置沿固定路线插值（椅子 → 镜子 → 门），叠加颠簸和摆动。
   - **要检查**：`Session.WorkCenter`、`AttentionPose`、`CustomerMotion.Apply`、`SyncFaces`、`ChairHeight`、镜子遮挡、碎屑支撑（`DebrisSystem.ReleaseSupport`）、梯子，以及所有假设“头在椅子上”的代码。
   - 建议集中用一个“顾客根变换”来驱动，不要到处加特殊判断。
   - 扶头时要求玩家在头的 1.9 米内（沿用 `CanBrace` 的距离规则），走动中同样有效。
   - 玩家自己的相机绝不被带着走（G-4）。
6. **完工铃（R2-7）**：铃是一个固定的交互点，参照 `HelicopterCallButton` 的交互判定。
7. **退役旧流程（R2-8）**：
   - B 不再走 `TickFlight` 的呼叫、低空飞过和进近排程，也不再使用 `CallHelicopter`、`HelicopterCallButton`。
   - 新流程验证通过后，删除这些 B 专用代码，或把它们改写为新流程的等价测试：
     - `tests/Core/FlightCallTests.cs`
     - `src/Bootstrap/BCallNetworkChecks.cs`
     - `scripts/smoke.ps1 -CallTrials` 和 `--b-call-smoke`
     - 打包脚本里的呼叫重试检查
   - 用新的“试放加离店”网络检查取代它们。
   - 修改后的验证脚本要保证 A 归档路径不受影响（`verify_v06.ps1 -ArchivedA` 仍然可用）。

**测试**：

- Core：
  - 偏心放置和旋转放置的评估。
  - 滑落阈值。
  - 离店路线上的完成与未完成判定。
  - 按回座位的次数上限。
  - 完工铃只在委托物已放上时生效。
- 引擎：
  - 用真实的 E 拿起、放下。
  - 走动中工具仍能命中移动中的头。
  - 走动中扶头能降低摆动。
- 网络：
  - 远端玩家拿放委托物、敲铃，以及离店全过程，各端的最终快照一致。
  - 2P 和 4P 都要跑。
  - 做带种子的 UDP 劣化测试，同时检查“走动中的头”和玩家移动的平滑度指标（见 `docs/40`、`docs/41`，以及 `AGENTS.md` 的网络纪律）。

**退出条件**：新流程的完整 2P/4P 局通过；劣化测试没有硬校正回退；打试玩包（检查点 A）。

## 阶段 3 — 胡闹可挽回（R3-x）

- **R3-1 生长剂按瓶计量**：
  - `GrowthSpray.cs`、`Session.UpdateGrowthHold`、`Experiment.SpendGrowth`、`CanAffordGrowth` 目前读写 `State.Experiment.Growth`，改为读写所持瓶子的容量。
  - `Growth` 工具在 B 中生成多个实例。
  - 液面显示在 `src/Presentation` 的工具模型上。
  - HUD 上的“共享剩余”改为“本瓶剩余”。
- **R3-2 容量验证**：参考现有的单人校准脚本 `scripts/v06_solo_calibration.ps1`，新增“浪费一瓶”场景。
- **R3-3 假发补丁**：
  - `Session.SpawnWig` 在 B 中生成假发架。
  - 假发贴上后的支撑，由 `LandingProbes` 的 `h.AttachedTo==0` 路径覆盖；补一个测试：假发贴上并上胶后通过支撑判定，不上胶则判为太软。
- **R3-4 碎发回收**：
  - 已有代码：`Session.cs` 约第 358–376 行，吸取调用 `DebrisSystem.Vacuum` 存进 `Reservoir`，右键用 `EffectKind.Transfer` 喷回，**不消耗生长剂**。
  - 需求是确认它在 B 里可用、可读（给出提示和液面或储量显示），再按 R3-4 调整效率。
  - 如果现有行为已经满足，只补测试。

**测试**：瓶子各自独立扣量；某一瓶空了不影响其他瓶；假发补丁；回收之后能修补被烧掉的角。另外做一次网络检查，确认瓶子的容量同步。

## 阶段 4 — 揭晓与留念（R4-x）

- **R4-3 事实动作记录**：房主维护一个有长度上限的 `ActionLog` 列表，来源是明确的事件点：
  - 工具触发（`ExperimentTool`）
  - 引燃（`EffectKind.Ignite` 的首次触发）
  - 灭火（水作用于燃烧块，使燃烧从有变无）
  - 道具拿放
  - 敲铃
  - 按回座位
  - 瓶子用空
  - 其他要求：
    - 结算时进快照，施工期间不必同步。
    - **禁止**从 `Patch.Source`/`BurnSource` 推断。
    - `ImpactLedger`、`Replay` 可以提供镜头，但文字只用明确的事件。
- **R4-1 谢幕、R4-5 结算分层**：
  - `SharedJob.Settle` 目前对 B 走 `Result.Final*1.5` 这类旧公式。B 改为“基本工钱 + 小费”（R4-5），小费规则写成数据表。
  - `ExperimentHud` 结算页展示：完成与否、顾客反应、收益、最多 6 条动作记录。
- **R4-2 拍照、R4-4 照片房间**：
  - 用 Godot 的 `SubViewport` 做固定机位渲染，存 PNG 和 JSON 元数据到 `user://gallery/`。
  - 房间放在大厅或店内墙面，读取最近 24 张。
  - 不走网络。
  - 清空或损坏的图库不能影响启动。

**测试**：Core 测试小费规则，确认小费不会让收益变成负数；动作记录的顺序和上限。引擎测试照片文件生成、元数据正确，以及图库损坏时能正常降级。另做结算页截图，中英文各一张。**退出条件**：打试玩包（检查点 B）。

## 阶段 5 — 协作道具与头发物理（R5-x）

- **R5-1 至 R5-3 模具**：
  - 在生长的体素写入路径上加一个“允许区域遮罩”：模具内部，加上破洞外侧的延伸柱。相关代码是 `VolumeEditing.cs`、`HairVolume.Brush` 的 `AddHair`/`Young` 路径，以及 `Session` 里调用生长的地方。
  - 模具是一种 `PropState`（新的 `Goal` 值或类型字段），要同步姿态、是否被钉住和破洞列表。
  - 体素分辨率约 0.14，破洞半径至少 0.3。
  - 先在 Lab 里做可视化验证，再进 B。
- **R5-4 重心**：
  - 新增纯函数：输入共享头的体积和附着物的质量分布，输出横向重心偏移。
  - 倾斜角作为权威状态进快照，并叠加在同一个头部变换上（和注意力姿态同一条路径）。
  - 用 Core 测试确认：对称作品不会倾斜。
- **R5-5 组合发现**：在现有组合的触发处（冰冻后受冲击碎裂、热让胶失效、风与水雾或火、湿头发阻止引燃）发出“首次发现”事件，持久化到图库目录。

**测试**：模具先填满内部、再从破洞外溢；没有扶住或钉住时会掉落；破洞的网络同步。重心歪头的阈值和速率上限，以及歪头对委托物支撑的影响。

## 阶段 6 — 队友小事故与秘密任务（R6-x）

- **R6-1 队友事故**：
  - 在玩家受工具影响的路径上加入三种短状态：冰冻、粘手、头发着火。可以参考 `SharedRound.cs` 约第 137 行附近的工具撞人逻辑，以及玩家头发（`Barber` 头）的现有处理。
  - 状态放在 `PlayerState`，里面记录剩余时间。
  - 要遵守 G-4：可以转视角，镜头不被移动，时间到自动恢复。
- **R6-3、R6-4 秘密任务**：
  - 任务池写成数据表（`Definitions.cs` 或新文件）。房主在开始施工时分配，**只把任务发给对应的玩家**。实现上要确认快照不会把别人的任务泄露给所有客户端；如果快照是全员共享的，任务内容就在结算前不进快照，只下发给本人。
  - 完成由房主根据明确事件判定。
- **R6-5**：菜单开关。

**测试**：三种事故的持续时间和解除方式；控制限制符合 G-4。秘密任务不会在别的客户端上被看到；完成判定正确。网络 2P/4P。**退出条件**：打试玩包（检查点 C）。

## 阶段 7 — 内容扩展

实施前，先把 44 号文档的 R7-x 细化成可以测试的需求，交 owner 确认。建议顺序：R7-1 鸟巢（用现有顾客）→ R7-2 随机初始状态 → R7-3 第一个带特性的顾客 → R7-4 随机 Boss。

## 8. 风险

| 风险 | 缓解 |
|---|---|
| 离店走动需要移动共享头，很多代码假设头固定在椅子上 | 在阶段 2 集中用“顾客根变换”驱动；先用 `rg` 列出所有 `WorkCenter`、`SharedHead.Position` 的使用处，逐个处理 |
| 走动中的头在跨境网络下出现抖动 | 头的变换由服务器端的时间和固定路线决定，客户端可以按局内时间本地插值；走动过程要纳入运动平滑度指标 |
| 模具遮罩在 0.14 分辨率下效果不好 | 先在 Lab 做可视化；破洞半径设下限；必要时把模具做大 |
| 删掉呼叫流程影响已有的打包和网络检查 | 按 G-6，先让新检查通过，再删旧检查；检查点 A 的包必须带新的网络检查 |
| 秘密任务泄露 | 见阶段 6 的实现要求 |

## 9. 每阶段的验证

在仓库根目录用 PowerShell 7：

```powershell
./scripts/build.ps1
./scripts/test.ps1                 # Core 测试
./scripts/verify_b.ps1             # B 与共享引擎回归
./scripts/verify_b.ps1 -Network    # 网络状态有变化时
./scripts/smoke.ps1 -Players 2 -Variant B -FullDuration
./scripts/smoke.ps1 -Players 4 -Variant B -FullDuration
./scripts/net_scenario.ps1 ...     # 带种子的 UDP 劣化测试（参数见 docs/40、41）
```

- 并发运行网络检查时，每个运行要用不同的 UDP 端口对和不同的日志名。
- 一次构建完成之后，再开始它的回归批次；不要在已经运行着的节点之间混用新旧程序集。
- 打包检查点：`./scripts/package_b.ps1`，然后 `./scripts/verify_b_package.ps1`。
- 报告里只写实际跑过的检查。

## 10. 试玩观察点（给 owner）

| 阶段 | 看什么 |
|---|---|
| 1 | 顾客喊“好烫”之后，玩家是否停手或去救；有没有出现玩家说不清原因的失败 |
| 2 | 试放了几次；有没有人提前偷放或敲铃；离店时有几个人跟着走、扶头或补救 |
| 3 | 有人浪费了一瓶之后，队伍是否仍能完成；有没有用假发或碎发补救 |
| 4 | 看结算时是在笑还是在抱怨；会不会主动去看照片房间 |
| 5 | 模具是否由两个人配合使用；拿模具的人是否在调整位置，而不是站着不动；歪头之后是否有人配重 |
| 6 | 队友中招后，5 秒内有没有人去救；语音里笑声和抱怨（“我又动不了”）哪个多；秘密任务揭晓时的反应 |

每个阶段再额外统计：闲置 20 秒以上的次数（排除开局讲解）。如果阶段 4 之后仍然频繁出现，就按 43 号文档第 6 节，把“短单”试验提前。
