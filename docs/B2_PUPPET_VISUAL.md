# B2 Puppet visual experiment — 2026-10-01

最新续作：**[Round 6 光照/阴影实机报告](B2_PUPPET_ROUND6.md)**，当前候选 `artifacts/b2-puppet/round6-selected`，仍未达到概念图完成度。R5 已被 Owner 否决。下文保留 R1–3 的历史结果，不代表当前源版本。

**Round 1–3 的实机试验、截图比较与修正已完成；概念图级视觉验收未通过，剪切重建存在卡顿，不推广到正式 B。**

R1–3 阶段候选：`artifacts/b2-puppet/round3-delivery`。早先目录 `round3-final` 是已被替代的候选，不能当作交付版。独立实验场支持五状态、四表情、灰模、光头、效果开关、第一人称检视和实际 density 剪切；正式 B 玩法、评分、网络、关卡和默认 renderer 未改。

## Contract and audit (before Round 1)

The owner explicitly selected the supplied PuppetHair v0.2 kickoff, superseding the Snow route. All pack text and nine reference images were read. Positive boards are visual targets; negative Snow and primitive characters are rejection examples. Only this standalone experiment is authorized; no production rollout.

- Existing `VisualTargetLab` directly loads Snow customer, teammates and FPS arms. Preserve it as historical evidence; do not extend it.
- `HairVolume` is the density authority, `HairShell.Build` extracts the rendered surface. Reuse both unchanged. B2 has its own fixture density, no Session, scoring, RPCs or audio.
- `debris_flocked.gdshader` is a tiny opaque debris shader: sinusoidal normal/grain and rim, not long fibers or a groom. Reuse derivative filtering as an idea; do not apply the debris material wholesale to hero hair.
- `HeadLook` already interpolates wet/frozen/char/glue weights into RGBA. B2 tests spatial visual masks without changing those fields or production states. Flat cuts must edit the local density and regenerate its mesh.
- Existing WorldEnvironment is Compatibility and has no effective SSAO there. B2 starts with Compatibility; rendering changes, if needed, are isolated launch overrides. `project.godot` remains unchanged.
- Existing chair is licensed Poly Haven CC0. Reuse chair only; create original puppet/hand geometry. Existing notices remain at `third_party/licenses` and `docs/THIRD_PARTY_ASSETS.md`.

## Minimal seam and planned files

Directly launch `scenes/PuppetLab.tscn`, bypassing Main. New `src/Bootstrap/PuppetLab.cs`, `src/Presentation/PuppetLab*.cs`, `shaders/puppet_*.gdshader`, `scripts/puppet_lab.ps1`, `scripts/compare_puppet.py`. This file and PROJECT_STATE document the experiment. Source recovery and pre-change hashes: `artifacts/b2-puppet/before-source.zip`, `baseline-sha256.json`.

## Review rules

Three sequential runtime rounds: R1 proportions/face/core/camera; R2 microfleece/fuzz/cut; R3 states/light/hands/performance. Each saves beauty, actual 73 degree FPS at 1.7m, 400px thumbnail, five states, gray/bald checks, reference comparison, source manifest and timings. Only the largest three gaps drive the next revision. Failed candidates remain on disk. No generated image or offline render substitutes for runtime evidence.

Actual inspection, controls and verdicts are recorded below. Owner acceptance and concept match are not inferred from compilation.

## Round 1 — runtime reviewed, 1-b retained

Evidence: `artifacts/b2-puppet/round1-b/reference_comparison.png`, all five required images, bald, close and four-head captures; source/DLL snapshots and metrics alongside. Baseline and current are the same material tier in R1 (do not imply the future micro/fuzz/state features are present).

Largest five gaps: (1) smooth toy-plastic surfaces, (2) no long fiber edge on hair, (3) cut plane lacks contrasting short nap, (4) flat lighting/background, (5) hands are too large and tube-like. R1 addressed proportions/face, merged swept density silhouette, and 73-degree/1.7m first-person framing. R2 will address only the first three material gaps.

Failed 1-a retained: incorrect parametric face normals, overexposure and shadow acne. Fixed winding/normals and erroneous light intensity/bias; 1-b visibly restores surface shading. Removed paired buck teeth. Hair/face/tool all remain visible in FPS. The different pear-shaped stylist avoids three identical human heads. Bald face readable but still simple. No concept-match claim.

Sources consulted for the rendering choice: [Godot 4.7 spatial shader reference](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html), [NVIDIA Shells and Fins](https://developer.download.nvidia.com/SDK/10/direct3d/Source/Fur/doc/FurShellsAndFins.pdf). Shader code is original. R2 tests bounded opaque fiber ribbons in a single mesh instead of transparent shell stacks; performance and aliasing still require screenshots/measurements.

## Round 2 — 实机审图，保留 round2-b

最大五个差距：①毛丝仍偏均匀、像短地毯；②core 前缘像软帽；③脸/围布过亮、层次被吃掉；④手太大、抓握生硬；⑤背景与主体缺少参考的暖冷/软硬对比。

本轮解决 R1 遗留的前三个材质问题：身体使用高粗糙度、过滤微颗粒的短绒表面；头发改为弯曲且有方向的独立纤维；切面使用明显缩短的绒毛和更哑光的 core。身体保持干净，不覆盖长毛。F4 能关闭 shader、纤维与候选光照，保留同一个 density/core。

`round2-a` 的约 49,496 根密集短丝像刺猬/地毯，未接受。保留版 `round2-b` 降低密度并增加弯曲、长度差异；近景仍有规则细线感，未声称已成为自然 groom。`round2-forward-probe` 只换 Forward+ 后仍暗、平，未把换渲染器当成改善证据。

## Round 3 — 状态、灯光、手与性能

本轮解决三组：①烧焦/湿/冻结材质及纤维变化；②主体、队友、房间分层光照；③FPS 手/工具比例、抓握和验证开关。灰模复查又暴露了发帽大形，做一次有限的卷束修正，并保留前后证据。

- 焦角缩短毛丝、降低反光并变黑，最终按外表面积加权约 **26%**；湿/冻结约 **51%**。这些是遮罩面积估计，截图仍是视觉判断依据。
- 湿毛缩短、贴伏、变深，有束状方向；冻结减少弯曲、偏白蓝。五状态拼图关闭粒子，保证不靠烟/冰掩盖材质问题。8 个小烟团、20 个冰晶单独开关并测量。
- 暖 key、冷 fill、接触遮蔽、较暗背景；仅实验启动参数选择 Forward+。小手和推子在 73° FPS 内有清楚层次。四种表情有真实截图，见 `round3-delivery/expressions.png`。

最大的五个剩余视觉差距：①灰模仍像简化发帽，普通发型也偏高；②长毛太均匀，缺少成束、交叠和局部杂乱；③脸、手、围布仍偏光滑 CG，缺少手工泡棉的不规则；④房间面材和光照太规整，缺少参考的摄影层次；⑤状态边界明显呈测试遮罩，湿区条纹过整齐，焦角缺少更有说服力的焦脆细节。**这些是未达标原因，不能用测试通过覆盖。**

保留的失败和修正：

- `round3-a` 过曝、湿区像亮黏液；后续提高湿区粗糙度、收窄亮部，截图改善。
- `round3-b` / `round3-c` 保留中间光照、手和表情证据。灰模退出曾漏恢复导入椅子的 null material override，已修正并通过实际按键检查。
- `round3-final` 为早期暂定名，已被替代。首张 Beauty 有启动 CPU 抖动，约 4.29ms，原始日志/数值保留。
- `round3-groom-probe` 卷束形成穿孔/拱形，弃用。`round3-groom2-probe` 补足底部体积后大形有所改善，但仍未达到参考；当时焦角随造型变为 37%，超规格。
- `round3-delivery` 保留第二次卷束轮廓，并把焦角恢复到 26%。最终结论只对应此目录及下方 DLL hash。

## 每轮文件与证据清单

R1 新增：`scenes/PuppetLab.tscn`、`src/Bootstrap/PuppetLab.cs`、`src/Presentation/PuppetLabModels.cs`、`PuppetLabHair.cs`、`PuppetLabRoom.cs`、`scripts/puppet_lab.ps1`、`scripts/compare_puppet.py`。

R2 新增：`shaders/puppet_microfleece.gdshader`、`shaders/puppet_hair.gdshader`、`src/Presentation/PuppetLabSurface.cs`。修改：`PuppetLabHair.cs`、`PuppetLab.cs`。

R3 新增：`src/Presentation/PuppetLabEffects.cs`。修改：`PuppetLab.cs`、`PuppetLabModels.cs`、`PuppetLabHair.cs`、`PuppetLabRoom.cs`、`PuppetLabSurface.cs`、`puppet_hair.gdshader` 和两个实验脚本。同步更新本文件及 `PROJECT_STATE.md`。187 个既有源文件/脚本/场景/shader/配置 SHA256 未变化，见 `artifacts/b2-puppet/production-invariance.json`。

| 轮次 | 保留候选与对照图 | baseline / current Beauty FPS |
|---|---|---:|
| R1 | [round1-b](../artifacts/b2-puppet/round1-b/reference_comparison.png) | 1222 / 1319 |
| R2 | [round2-b](../artifacts/b2-puppet/round2-b/reference_comparison.png) | 1075 / 646 |
| R3 | [round3-delivery](../artifacts/b2-puppet/round3-delivery/reference_comparison.png) | 898 / 479 |

每目录含五张规定图片、原始 viewport PNG、`metrics.json`、`engine.log`、`candidate.json` 和当时源文件/DLL 副本。图片名为 `beauty_round_N.png`、`gameplay_round_N.png`、`thumbnail_400_round_N.png`、`hair_states_round_N.png`、`gray_silhouette_round_N.png`。拼图只做缩放/排版，无美化或补绘。

R1 两项是相同效果档位的短样本，差异不能解释为优化收益。R1/R2 的状态拼图保留阶段事实：平顶已改几何，烧焦/湿/冻结的材质差异直到 R3 才实现。三轮 renderer 与造型不同，不是严格的跨轮性能实验。所有原始捕获、失败候选仍在磁盘。

## 最终实机性能

Godot 4.7.2 Mono，RTX 5070 Ti Laptop，1280×800，R3 Forward+，4×MSAA，关闭垂直同步。每镜头约 1 秒预热、1.5 秒采样。均为独立静态实验场，**不是正式 B/联机负载，也不是低端硬件或长时稳定性测试**。

| 场景 | 平均帧 ms | GPU ms | FPS |
|---|---:|---:|---:|
| 相同 B2 几何、基础材质/光照 | 1.114 | 1.058 | 898 |
| 仅身体微绒 | 1.147 | 1.103 | 872 |
| 微绒 + 纤维，基础光照 | 1.671 | 1.635 | 598 |
| 完整 Beauty | 2.087 | 2.048 | 479 |
| 完整真实 FPS | 2.080 | 2.040 | 481 |
| 头发占屏超过一半的近景 | 2.947 | 2.909 | 339 |
| 四颗毛发同屏 | 3.563 | 3.526 | 281 |
| 烧焦 + 8 烟团 | 2.051 | 2.011 | 488 |
| 冻结 + 20 冰晶 | 2.081 | 2.041 | 481 |

正常 core 7,928 三角形，34,390 根纤维，纤维 962,920 三角形。每根由两张交叉不透明窄带构成，七段弯曲；**shell 层数 0**，没有透明 shell 堆叠。Beauty 259 draws、FPS 290 draws。四头是四份真实毛发网格，未运行四玩家游戏模拟。GPU 短样本没有灾难性下降，但近景仍有细线与闪烁风险。

长绒法向伸出上限约 3.75cm，切面约 2.34mm。原生 LMB 剪切 density mass 从 139.24826 降到 136.82877，切面生成 1,690 根短绒。**该次同步 density/core/全头纤维重建耗时 281.943ms，存在明显操作卡顿。** 因而不能以静态 481 FPS 宣称剪发性能达标。继续需要局部重建、复用梳理数据及 LOD，不能继续靠增加毛丝逼近参考。

## 实际验证及结论边界

- 最后 `dotnet build ProjectHairball.csproj --nologo` 成功；仍有两个既有 nullable 警告，位于 `GesturePropsView.cs`、`PartyMoldView.cs`。日志 `artifacts/b2-puppet/delivery-build.log`。此前完整 build/import 成功；最终实际运行完成新 shader 加载/渲染。
- Core **248/248**，`final-core-tests.log`。它发生在最后 B2-only 造型/诊断修订前；Core 始终未改，不冒充最终 DLL 的额外全套重跑。
- 最终 DLL 的原生输入检查 **12 项通过**，`native-check-release/engine.log`：F4 双向切换、density/真实顶点不变、SSAO 关闭、F5 恢复 null override、表情后回 baseline、Tab/FOV/眼高、LMB 修改 density/生成短绒、R 恢复。前后剪切 PNG 和 `native-check.json` 同目录。
- 最终 DLL 的正式 B **实际渲染 WorldChecks** exit0，`b-world-delivery.log`：猫救援/重量、受限拖动及 opt-out、原生 LMB 拍醒、工作唤醒、三轮合照/历史、双语。仍为 Compatibility、protocol25。本轮未重跑 WAN/4P，未借用旧结果充数。
- 最终 20 个机位/状态捕获 exit0。已查看真实 FPS、400px、灰模、光头、五状态、四表情、近景和参考拼图。
- 最终 DLL SHA256：`ECCEE32FFA69892D2C52EAACCC2E03A4C4725DB2206E42F570E0EBF2777EF562`。未打包或覆盖正式发布 ZIP。

## 运行与人工检视

```powershell
Set-Location 'D:\Project\Game\Godot\project_hairball'
pwsh -NoProfile -File scripts/build.ps1
pwsh -NoProfile -File scripts/puppet_lab.ps1 -Round 3 -Interactive
```

`GODOT_BIN` 可指定 4.7.2 Mono，否则由既有 `env.ps1` 查找。正式 B 继续 `pwsh -NoProfile -File scripts/run_b.ps1`。

| 输入 | 实验功能 |
|---|---|
| F1 / F2 | Beauty / 73°、1.7m 真实 FPS 固定机位 |
| 1–5 | 正常、平顶、焦角、湿半边、冻结半边检视图 |
| Q | 表情 |
| F4 | 关闭/恢复新材质、绒毛、候选光照 |
| F5 / F6 | 灰模 / 光头 |
| F7 | 状态粒子 |
| Tab，WASD，鼠标 | 固定 1.7m 眼高的自由第一人称 |
| LMB | 自由检视中瞄准头发，真实局部剪切 |
| R / Esc | 重置 / 释放鼠标 |

切换预设机位/状态会重置切割。F4 baseline 是同一个 B2 几何的普通材质和基础光照，不改变 density，不在运行中切换 renderer。该场景是外观实验交互，未接正式工具系统。

复现截图、检查、拼图：

```powershell
pwsh -NoProfile -File scripts/puppet_lab.ps1 -Round 3
pwsh -NoProfile -File scripts/puppet_lab.ps1 -Round 3 -Check
python scripts/compare_puppet.py artifacts/b2-puppet/round3-delivery --round 3
```

启动脚本默认使用带毫秒/PID 的新输出目录。当前 `-Round 1/2` 只关闭后续效果档位，历史轮次应查看各自源快照和图片，不把现代码重跑冒充旧候选。

人工优先看 [400px](../artifacts/b2-puppet/round3-delivery/thumbnail_400_round_3.png)、[光头](../artifacts/b2-puppet/round3-delivery/bald.png)、[五状态](../artifacts/b2-puppet/round3-delivery/hair_states_round_3.png)，然后按 F5 检查发型大形、LMB 感受卡顿。脸/头发/工具同屏、材质分离和状态辨识已有改善；“是否讨喜”和“是否达到参考”仍需要 Owner 感知验收。我的审图结论为 **视觉未达标、交互性能未达标、继续保持独立实验**。

接近参考的下一步应先处理毛束结构和角色表面造型，再修局部纤维更新。增加 shader 层和后处理不能补上这些结构差距。
