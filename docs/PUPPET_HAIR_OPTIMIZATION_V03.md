# Puppet Hair Optimization v0.3 — 三轮实机外观修正

当前结论：Owner 在 Round 3 后指出明显摩尔纹，原 R3 的采样质量未通过。随后已移除头发材质里重复的法线层和周期条带，并按像素覆盖范围过滤织物细节。同构建真实 FPS 对照及移动序列显示切面条带减弱，短绒与洞壁毛感保留；静止帧闪烁指标仅小幅改善，不能宣称完全无闪烁。当前可试玩候选与证据在 `artifacts/puppet-hair-moire-20261001`，见文末补充。全部改动仍限定在 B2 PuppetLab 外观实验场，真人动态观感、深洞暗部及概念品质验收仍开放。

后续试玩修复：连续剪切耗尽表面触发的空网格错误已复现并修正，最新二进制和证据为 `artifacts/puppet-empty-mesh-20261001`；材质保持上述摩尔纹候选，详见本文末尾。

2026-10-01. Scope: the current Godot B2 PuppetLab only. No new gameplay, formal B rollout, score/network/density-rule changes. Pack entry documents, all ten current technical documents and all seven current/target images were read/inspected. The legacy references are historical. Source recovery is in `artifacts/puppet-hair-v03/before-source.zip`.

## Pipeline audit

`PuppetLab` → `PuppetLabHair.Volume` → unchanged `HairShell.Build` → indexed opaque core + bounded opaque crossed-ribbon fibers → shared hair shader. Forward+ / existing R6 fabric lighting, SDFGI for static room, moving actors receive only. No transparent shell stack. The current core carried a binary short-cut alpha; inward-normal rejection removed cavity fibers. The shader also multiplied scanned normal/albedo variation by `(1-cut)`, explicitly suppressing detail on the newly exposed plane. Surface classification can use pre-edit volume samples plus local edit planes; new render-only alpha encodes outer/trim/interior. Density and vertex positions stay authoritative.

Formal B: `Session.UseSculpt` → `FinalizeHair` (`src/Core/Sculpting.cs`) → `HairVolume.DetachUnsupported` (`src/Core/VolumeEditing.cs`). Connectivity uses 14 lattice neighbours, scalp and anchors; fragments are removed immediately. New `Head.Fragment` with a new ID is a presentation observation seam, not a dedicated event. 12 large loose fragments maximum; small/excess pieces use `DebrisSystem.Emit`, whose separate flight cap is 64. `debris_flocked.gdshader` still exists; it is a cheap opaque material and does not provide a structural warning. The B2 lab previously had no detach hook. These Core rules must remain byte-identical.

## Round 1 — visually rejected, retained

Evidence: `artifacts/puppet-hair-v03/round1-a`. Fifteen full engine viewport captures, including all six states at 73°/1.7 m FPS and a closer diagnostic camera, plus actual mouse-input cut. Warning/fall images in this first iteration are explicitly inactive placeholders (`warningImplemented=false`), not detach evidence. GPU timer is Godot RenderingServer's measured viewport render time, warmed for 4 seconds and sampled for at least 1.5 seconds/60 frames; device RTX 5070 Ti Laptop, 1280×800, Forward+, VSync off. See [Godot measurement API](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html#class-renderingserver-method-viewport-get-measured-render-time-gpu).

Changes: explicit newly exposed surface classes; include cavity fibers; retain microfiber substrate on cut surfaces. Compile succeeded with the two existing unrelated nullable warnings. Actual mouse input changed mass 470.34406 → 464.65292; 1,407 short fibers, 24.57 ms main-thread cut / 317.82 ms ready, 36 engine frames continued. This proves the path, not tactile quality.

Review of the actual screenshots: (1) cut/carve still reads as smooth purple clay at FPS distance; (2) normal surface is sparse and threadlike close up; (3) wet is too dark and regular; (4) fixed trim plane is hard to see from below; (5) warning/fall presentation absent. Round 2 addresses the first three: sampled fiber scale/density, continuous plush core, wet/state separation. Round 3 must finish anticipation and cost/LOD evidence.

Round 1 normal/carved/native-cut GPU: 6.556 / 6.128 / 5.829 ms, 151.6 / 162.3 / 170.6 FPS. Zero shells, one visible hair; not multiplayer performance. Full raw per-shot data in metrics.json. Do not call Round 1 a visual pass.

## Round 2 — surface improvement, further correction required

Evidence: `artifacts/puppet-hair-v03/round2-a`, with all fifteen captures, raw measurements, native-input record, and reference/close comparison sheets. The cavity now visibly retains dense short nap at FPS distance. The earlier texture repeated 12–22 times per metre and lost most structure to mip filtering. R2 uses 1.15–1.75 repeats and 95,000 short roots per square metre, with fewer segments per short fiber. Original volume and the real mouse cut hashes remain identical to Round 1.

Largest remaining five gaps: (1) wet specular islands look waxy; (2) 1.3–1.5 million fiber triangles and 588 ms asynchronous cut rebuild; (3) no anticipation yet; (4) authored cap silhouette and sparse individual curly lines remain unlike the concept; (5) a horizontal top plane cannot be fully inspected from the lower FPS eye. The next three scoped corrections are wet response, bounded fiber cost/rebuild and warning/release. No camera-height or gameplay change to disguise the last two gaps.

R2 normal / carved / native-cut: GPU 6.65 / 5.82 / 6.01 ms; 149.6 / 171.0 / 165.4 FPS. Native cut 22.21 ms main thread and 588.06 ms ready, 71 frames progressed. Round 2 does not establish final visual acceptance.

## Round 3 — 最终外观候选，真人验收仍开放

The estimator reads a density copy using the existing 14-neighbour connectivity, identifying only single-cell articulations with a root-free subtree of at least 64 positive cells. It does not claim general stress/neck-width prediction. Redundant paths or an anchor on the load suppress the warning. Maximum local warning displacement is under 7 mm, without moving core mesh vertices or density. Detach reuses `DetachUnsupported`; cosmetic fragments never rejoin the head. They use a 0.32 s presentation beat and a monotonic wall clock, cap at 12, and core-only materials.

The first detach probe is retained as a failure: its fixture had two adjacent connection rows, so the conservative estimator correctly did not warn; PNG compression during capture also distorted recorded times, and accumulated engine delta lagged wall time. The corrected fixture is aligned to one lattice row. Capture now buffers actual images before writing them and fragment age uses a monotonic clock. These fixture/timing repairs do not change Core.

本轮继续修正 Round 2 的三个最大问题：湿发蜡状高光、过重的毛丝生成、缺少断裂预兆。长绒改为三段、短绒一段；法线复用顶点插值。湿发保留较高粗糙度，并靠伏毛和有限亮度变化区分。边缘平滑只调整已知切面的法线，所有核心顶点位置与 density 保持原值。默认零层 shell；Hero 保留完整绒毛，Nearby 降到 38% 毛根、最多一层 shell，Background 和 Debris 只用带微纤维纹理的 core。当前 B2 单头始终使用 Hero；多头压力场景显式分配层级，尚未接正式 B 的动态距离切换。

实际视图：[六状态 FPS](../artifacts/puppet-hair-v03/round3-release/six-states-gameplay.png)、[同一鼠标切割前后](../artifacts/puppet-hair-v03/round3-release/native-cut-comparison.png)、[内壁开关对照](../artifacts/puppet-hair-v03/round3-release/interior-ab.png)、[概念参考与实机并排](../artifacts/puppet-hair-v03/round3-release/reference-comparison.png)、[近景诊断](../artifacts/puppet-hair-v03/round3-release/close-comparison.png)、[实际掉落时序](../artifacts/puppet-hair-v03/round3-release/detach-sequence.png)。拼图只排列、等比缩放完整截图；没有补画或修图。原始 PNG、GPU 原始值、源码及 DLL 快照都在同目录。目录名 release 表示本轮最终候选，不表示正式 B 发布。

目视判断：真实鼠标剪出的表面和预制洞壁均有连续短绒；关闭 fuzz 后仍能看见基底织物纹理。相同镜头切回 old interior，会重新出现光滑内胆。普通表面仍保留较长绒，湿区伏下，焦区明显最暗，霜区发白。洞壁没有额外涂黑，深处偏暗来自实际几何与照明。这些截图支持材料连续性改善；无人参加本轮盲测，不据此宣称所有状态或预警已经通过真人辨认。

当前最大的五项剩余差距：

1. 原有体块仍有帽檐感，近景部分毛丝过于规整；core-only 碎块的绒感也明显弱于主头发。本轮没有重新雕整颗头型。
2. 深洞阴影仍重，最低亮度处的短绒不够醒目。
3. 玩家从 1.7 m 视点仰看水平顶部时，推平范围和推剪方向仍不够明确；没有提高 FPS 相机掩盖问题。
4. 预警仅覆盖单格承重点，对宽颈会漏报；约 6 mm 世界空间位移是否足够醒目尚未真人验证。本轮没有新增断裂音效。
5. 普通真实剪切主线程 26.90 ms，完整毛丝就绪 475.50 ms，期间推进 46 帧；仍只允许一个待完成剪切，连续操作手感未达零等待。

断裂时另发现碎块构造会重复计算整颗初始头型，主线程曾达 595.56 ms。改为共享只读初始体积，所有可编辑体积仍独立；专项复测 22.72 ms，最终整轮复测 30.37 ms，完整毛丝就绪 374.31 ms。未选取较快的单次结果掩盖整轮结果。逻辑在最后一刀立即移除碎块；最终时序在约 0.170 s 下沉 8 mm、0.430 s 下沉 85 mm、0.608 s 下落 458 mm，所有采样主头发 density 哈希一致。

Round 3 的十五张视图、原生检查和 A/B 对照已采集；完整 GPU 矩阵与冻结构建一致性复核见下文。

## GPU 与剪切实测

同一台 RTX 5070 Ti Laptop，Godot 4.7.2 Mono，Forward+，1280×800，VSync 关闭，原有 R6 灯光与 SDFGI。每个视图预热 4 秒，再记录至少 1.5 秒且不少于 60 帧。GPU 值为整个 viewport 的实测渲染时间，FPS 为实际帧间隔，包含场景其他内容。每轮源码/DLL 冻结后顺序运行，每次只开一个引擎。以下是短样本结果，GPU 时钟与系统负载会波动，不能把小幅差异当确定的性能收益。

| 实际 FPS 视图 | Round 1 GPU ms / FPS | Round 2 GPU ms / FPS | Round 3 GPU ms / FPS |
| --- | --- | --- | --- |
| 正常头发 | 6.556 / 151.6 | 6.647 / 149.6 | 6.771 / 147.0 |
| 挖洞后 | 6.128 / 162.3 | 5.816 / 171.0 | 6.810 / 146.1 |
| 真实鼠标修剪后 | 5.829 / 170.6 | 6.015 / 165.4 | 6.350 / 156.7 |

三轮均为 1 颗活动头发、零层 shell。Round 3 的 GPU 时间没有比前两轮全面下降；普通长绒三角形由 Round 2 的 1,304,720 降到 782,832，提升的是几何预算，GPU 总时间仍受覆盖面积、材质与场景影响。相同构建的原 R6 鼠标修剪对照为 5.971 ms / 166.6 FPS，当前候选为 6.350 ms / 156.7 FPS；其切割前后 density 哈希完全相同。

最终 16 组矩阵：[原始 CSV](../artifacts/puppet-hair-v03/matrix-release/matrix.csv)。单元格为 GPU ms / FPS。

| 可见负载 | shell 0 | shell 1 | shell 2 | shell 4 |
| --- | --- | --- | --- | --- |
| 1 Hero | 6.781 / 146.7 | 6.887 / 144.5 | 7.137 / 139.3 | 7.061 / 140.9 |
| 4 Hair | 5.789 / 171.8 | 6.272 / 158.6 | 6.045 / 164.5 | 6.062 / 164.1 |
| 4 Hair + 6 大碎块 | 6.124 / 162.4 | 6.276 / 158.4 | 6.191 / 160.6 | 6.272 / 158.4 |
| 4 Hair + 12 大碎块 | 6.304 / 157.7 | 6.320 / 157.3 | 6.376 / 155.9 | 6.630 / 150.0 |

4 Hair 是 1 Hero + 3 Nearby，四档实际总 shell draw 数为 0 / 4 / 5 / 7，碎块均为 core-only。四头场景相机更远，隐藏了角色与手；只能在同一行比较 shell 设置，不能据此说四头比单头便宜。这是渲染压力场景，未运行真实 4P/WAN。最大 12 块按当前正式系统上限选取；没有更改正式预算。

选择零层默认：当前核心材质与短绒已承担切面辨认，四层近景主要增加蓬松度，对切面帮助有限，额外放大轮廓；[同镜头对照](../artifacts/puppet-hair-v03/round3-release/shell-close-ab.png)保留。零层近景 7.005 ms，四层 7.327 ms，未观察到靠近镜头后 GPU 数倍增长。没有使用透明混合 shell，避免引入透明排序问题。所有采样帧数和 p99 帧间隔保存在各 metrics.json；未做低端 GPU 或长时间热稳定验收。

## 实际验证与证据边界

- 最终构建成功，0 error；保留 GesturePropsView.cs 与 PartyMoldView.cs 两处既有 nullable warning。引擎导入成功。
- Core 248/248 通过。本轮之后没有改动 Core；最终构建又通过 25 项原生 B2 检查及 13 项定向检查，涵盖真实 Tab/LMB/R、异步重建、只读预警、稳定/冗余/锚定结构、立即断裂、视觉缓冲、12 块上限，以及开关对 density/核心顶点位置的不变性。
- 正式 B 的有渲染 WorldChecks 以 Compatibility / protocol 25 运行，`EXPANSION_13_ENGINE_OK`，正常退出。没有重复 WAN、语音或真实多人回归；这些路径未修改。
- [最终证据审计](../artifacts/puppet-hair-v03/release-evidence-audit.json)：26 次有效运行，42 个候选文件的源码、材质、贴图和 DLL 哈希完全一致；39 个有效 GPU 视图；16 组完整矩阵；378 个受保护原文件哈希未变。最终 DLL SHA256：`689621F086B31125183699A09D09147CEC2C5F0E6FBFBE40DA02A50B92736A6F`。
- 相同鼠标切割在旧版、三轮候选及关闭平滑时得到同一 density；关闭 warning 或 prefall 也得到相同断裂前后 density。没有修改正式 B 的评分、联网、density 算法、DetachUnsupported 或常规入口。
- 每轮 6 状态 FPS、6 状态近景、实际鼠标切割及参考并排均保留。R1/R2 的 warning/fall 文件当时是未实现的静态占位，明确不计为掉落证据；R3 才有实际断裂与单调时钟采样。

失败及中间候选没有删除：`round3-detach-probe` 记录了两行连接导致未预警及截图计时问题；`matrix-final`、`round3-final` 是被中断的旧构建验证，不计为完成；`round3-selected` / `matrix-selected` 是完整但存在 595.56 ms 碎块构造卡顿的旧候选。最后一轮 `release-ab-old-interior` 因窗口被最小化而停止绘制，没有产出有效测量；标为 INVALID，停止确认归属的进程后以同一构建重跑到 `release-ab-old-interior-retry`。最终审计只使用有效重跑；没有混用不同构建补齐矩阵。

保留源码备份 `artifacts/puppet-hair-v03/before-source.zip`、原始哈希清单和每次运行的 source 快照。工作区不是 Git 仓库。本轮没有替换正式发布 ZIP，也没有把实验外观默认安装到 B。

## 可运行内容与复查方式

在仓库根目录构建，随后运行新候选入口（Windows PowerShell 5.1 可用）：

```powershell
dotnet build ProjectHairball.csproj --nologo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Interactive
```

Tab 进入真实 FPS；WASD / 鼠标移动观察，LMB 剪切，R 重置。数字 6 查看挖洞样例，F11 查看单格细颈预警样例，F12 执行样例最后一刀。F11/F12 是可复现的外观诊断入口。正常 B 仍通过 run_b.ps1；既有 puppet_lab.ps1 默认仍为原 R6，新外观从上述 optimization 入口进入。

无交互截图、原生验证与 GPU 矩阵：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_benchmark.ps1
dotnet run --project tests/Core/CoreTests.csproj
```

同一入口支持 `-FuzzOff`、`-OldInterior`、`-NoMicrofiber`、`-NoSmoothing`、`-NoWarning`、`-NoPrefall`、`-Shells 0|1|2|4`；可与 `-Interactive` 或固定 `-Shot opt-carved-gameplay` 配合。`-Round 0` 是原 R6 材质/绒毛对照。所有开关只影响本地表现；关闭预警或 prefall 使用同一刀和同一断裂结果。

最高价值的下一次真人试玩：在正常 FPS 距离连剪数刀、转动看洞壁，判断是否仍像内胆；不看标签区分焦/湿/霜；F11 后观察能否自然察觉危险，再按 F12 判断掉落节奏。预警是否促使停手、掉落是否有趣，必须留下人的观察记录。

## Round 3 后续 — 摩尔纹采样修正（2026-10-01）

Owner 明确指出摩尔纹并要求优化。此前以细节丰富和切面连续性作主要判断，漏掉采样稳定性；原 R3 不视为视觉通过。此次没有再扩玩法或改变毛丝几何，用同一编译产物的 `-LegacySampling` 开关做旧/新材质对照。

因果定位：旧 core 同时叠加 `p*5` 的织物法线和 `p*1.15–1.75` 的优化法线，另有 `hash(floor(p*430))` 的不连续信号、周期 flow/bundle 与切面梳纹。多个频率共用一个衰减量，难以稳定覆盖它们。新默认只保留一层织物扫描材质，取消这些周期/不连续信号，对三个投影分别使用实际导数的 `textureGrad`；覆盖范围放宽至 1.6 倍，法线幅度降到 0.30，微小明暗对比适当收敛。保留原有 MSAA 4×、TAA、1280×800 和零层 shell。使用的 API 依据：[Godot shader textureGrad](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shader_functions.html#texturegrad)。

源码仅变更头发 shared shader、材质参数、局部设置与 B2 采样/启动入口；新增 `PuppetLabSampling.cs` 记录真实渲染帧。Round 0–2 及原 R6 路径保持旧采样。普通头发仍为 65,236 根、782,832 个 fiber triangles；同一真实鼠标切面仍有 13,053 根短绒，所有新旧对照得到相同 density。没有通过减少短绒、降低渲染分辨率或改变视角来掩盖条带。

证据目录：`artifacts/puppet-hair-moire-20261001`。

- [切面原像素并排](../artifacts/puppet-hair-moire-20261001/native-cut-original-pixel-comparison.png)与[正常近景并排](../artifacts/puppet-hair-moire-20261001/close-original-pixel-comparison.png)：只裁切、排列原始截图，没有缩放或修图。
- [鼠标切面移动序列](../artifacts/puppet-hair-moire-20261001/native-motion-original-pixel-crops.png)与[洞壁移动序列](../artifacts/puppet-hair-moire-20261001/carve-motion-original-pixel-crops.png)：旧版在不同距离出现的竖向带状明暗明显减弱；新版表面细节更克制，洞壁仍有短绒。这里是逐帧目视检查，不等于完整动态舒适度验收。
- 四条原始移动记录：鼠标切面[旧](../artifacts/puppet-hair-moire-20261001/b-legacy-native-retry/motion.mp4)/[新](../artifacts/puppet-hair-moire-20261001/b-filtered-native/motion.mp4)，洞壁[旧](../artifacts/puppet-hair-moire-20261001/legacy-carve-motion/motion.mp4)/[新](../artifacts/puppet-hair-moire-20261001/candidate-carve-motion/motion.mp4)。每条约 5.8 秒，合计 629 张引擎实渲帧；前 0.8 秒静止，随后侧向和深度小幅移动。保持 FPS 73°/1.7 m，使用实际时间戳编码，没有插帧。视频采样约 27 帧/秒，受 GPU 读回影响，不能把录像采样率当作游戏 FPS。性能测量在读回与 PNG 写入前完成。
- [六状态完整 FPS 原图拼排](../artifacts/puppet-hair-moire-20261001/six-states-original-pixels.png)及 `candidate-full` 下十五张完整视图。正常、推平、挖洞、焦、湿、霜都复核；洞内最暗处仍难辨，近平面的短绒仍很细，不能据此说所有材质观感已达标。

静止段另做有限诊断：在切面内部固定区域 `(574,358)–(698,418)`，各取 16 张相机静止的原始帧。8-bit 亮度逐像素时间标准差均值由 0.395 降到 0.372；额头控制区域为 0.219 → 0.231。差异很小，没有重复样本显著性依据，不支持“闪烁已根除”。[完整诊断及局限](../artifacts/puppet-hair-moire-20261001/sampling-diagnostics.json)保留；主要正向证据是原尺寸条带与移动位置对照，不能拿这个数值代替人的观感。

GPU 仍为 RTX 5070 Ti Laptop / Forward+，每视图预热 4 秒，再取至少 1.5 秒且不少于 60 帧。同一编译产物顺序运行：

| 视图 | 旧采样 GPU ms / FPS | 新采样 GPU ms / FPS |
| --- | --- | --- |
| 相同真实鼠标切面 | 6.675 / 149.1 | 6.696 / 148.7 |
| 正常近景 | 7.24 / 137.5 | 7.30 / 136.2 |
| 洞壁 FPS | 7.50 / 132.7 | 7.29 / 136.5 |

这些短样本显示成本相近，没有稳定性能收益的结论。独立完整状态采样正常 / 洞壁 / 真实鼠标修剪后为 7.036 / 7.144 / 6.782 ms；正常近景 7.326 ms，洞壁近景 7.822 ms。4 颗头发（1 Hero + 3 Nearby）+ 12 个 core-only 碎块、零层 shell 为 6.280 ms / 158.2 FPS；属于相机较远的渲染压力样例。此次没有重跑旧版全部 shell 矩阵或真实 4P/WAN，不用这些数字推断多人体验。毛丝几何和 LOD 未改。

验证：编译成功，0 error、2 条既有 nullable warning；Core 248/248；原生 B2 25 + 优化检查 13 项通过；有渲染正式 B WorldChecks 正常退出并报告 `EXPANSION_13_ENGINE_OK`。这些机械检查验证回归边界，不给视觉自动打勾。[冻结候选审计](../artifacts/puppet-hair-moire-20261001/candidate-audit.json)核对 11 次有效 B2 运行的 43 个文件哈希一致，378 个原有受保护文件不变；相对前一版另有 36 个候选文件不变。DLL SHA256 `7FFA15014DA73EC4AB890B2CCFC0878CFB11BF88283586CD1B3703ACD49B7878`。完整轮真实切割主线程 41.57 ms、毛丝就绪 467.86 ms，推进 47 帧；没有宣称本轮改善剪切等待。

失败保留：`a-legacy-native` 因窗口最小化而无完整渲染证据；`a-legacy-native-retry` 的早期移动采集循环未通过完整性检查。修正为先等待 ProcessFrame 再连接 FramePostDraw，同构建重跑得到有效序列；两次失败都有 STATUS，未混入最终对照。旧源码备份和全部失败日志保留。

当前默认入口仍是 `scripts/puppet_hair_optimization.ps1 -Round 3 -Interactive`。在旧启动命令后加 `-LegacySampling` 可做人工对照；固定镜头加 `-Motion` 可复采真实帧，例如：

```powershell
dotnet build ProjectHairball.csproj --nologo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Interactive
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Shot opt-native-trim -Motion
```

本轮结论为“头发周期条带改善，可复核候选”。下次真人重点是在正常 FPS 距离一边缓慢前后移动一边剪切，看新切面是否仍有爬纹、细节是否过弱；近景长细丝、深洞暗部和场景其他织物的采样噪声仍需分别判断。没有正式 B 安装或新玩法交付。

## 试玩修复 — 连续剪切耗尽表面后的空数组错误

Owner 报告 `Condition "array_len == 0" is true. Returning: ERR_INVALID_DATA`。完整本地 Godot 日志定位为 LMB → `PuppetLabHair.Cut` → `BuildMesh`，以及随后 `RebuildFuzzAsync` → `InstallFuzz` 的两次空顶点上传；PowerShell 启动行只是显示引擎错误。前一轮的一刀重建和截图没有覆盖剪到空，这是之前验证的缺口。

修复范围仅表现层：空核心返回合法的零 surface ArrayMesh，替换旧几何；空绒毛结果释放旧节点、清空引用及统计，不再调用 AddSurfaceFromArrays；空核心也不生成 shell。不修改 density、DetachUnsupported、材质或毛丝生成参数，不屏蔽引擎 stderr。

回归从完整正常头型开始，以 73°/1.7 m 的真实 FPS 相机和原生 LMB 连剪；没有注入空密度。旧版在第 108 刀复现两处相同堆栈。修正版同样 108 刀得到空核心/零毛丝，随后点击、F4/F5 切换、R 重置和重新剪切均通过。另覆盖“有核心但零毛丝”和“空体积启用四层 shell”。9 项边界检查已加入原有 `-Check`，合计 25+13+9=47 项。

证据：[原始错误](../artifacts/puppet-empty-mesh-20261001/owner-error.log)、[旧版重现](../artifacts/puppet-empty-mesh-20261001/red-retry-native.log)、[修正版检查](../artifacts/puppet-empty-mesh-20261001/verified-native/engine.log)、[剪空实机截图](../artifacts/puppet-empty-mesh-20261001/verified-native/empty-check-shaved.png)、[R 恢复截图](../artifacts/puppet-empty-mesh-20261001/verified-native/empty-check-reset.png)。首版回归在后侧选点未命中处停止，保留在 `red`；使用较近且可行走范围内的 FPS 位置后完成旧版失败与新版通过的同路径对照。截图观察确认空头没有旧毛残留，R 后完整恢复。

编译成功（两条既有 nullable warning），Core 248/248、原生 47 项、正式 B 有渲染 WorldChecks 全部通过。[正常真实鼠标切面](../artifacts/puppet-empty-mesh-20261001/visual-gpu/opt-native-trim.png)及 GPU 复采：RTX5070Ti Laptop / Forward+ / 1280×800、246 帧样本，6.097 ms / 163.1 FPS。相同切面的 density、核心与毛丝几何数量匹配前一版；材质源码及纹理哈希相同。单次 GPU 数字不作为性能提升结论。

[冻结审计](../artifacts/puppet-empty-mesh-20261001/fix-audit.json)核对检查与截图的 44 个候选文件一致，上一版 40 个候选文件不变。最新 DLL `FA232C4E0C8A58B65317CB3FAA143906A4DE797B862BE0F218794BB037863F66`。已构建，重开原 optimization 入口即可使用修复；连续操作手感与前述视觉差距仍按原验收边界保留。

## Round 3 后续 — 毛发黑边、手脸斑驳修正（2026-10-01）

Owner 指出毛发边缘过黑、人物材质脏，尤其手部像有斑点。当前授权扩至人物材质，仍只修改 B2 optimization Round 3。先保留未修改截图，再用关闭 AO / 关闭直接阴影的实机图定位：手部斑驳在两种情况下都存在；毛发下沿既受真实遮挡影响，也叠加了根部与毛丝顶点颜色的染暗。没有采用关闭阴影或提高全场环境亮度作为最终方案。

人物的扫描贴图此前以 `.66 + .78*h` 调制底色，法线幅度 0.65/0.75、fabric profile 高光强度 0.45，较大的织物起伏被读成污渍和凹坑。新版手脸底色调制为 `.90 + .22*h`，衣物为 `.86 + .30*h`；法线降到 0.30/0.38，粗糙度 0.94、高光 0.16。略缩小纹理颗粒，各投影按像素导数进行 1.5 倍范围过滤，去掉人物身上的额外周期线纹与深色条纹。保留原本颜色、网格和细纹，不增加磨皮后处理。

毛发根部底色下限由 0.77 改为 0.91，毛丝顶点色向未染暗值收敛 35%，并保留全部 core 阴影。用 `0.22*ALBEDO` 的有界局部补偿减轻纤维间过暗，通过 `EMISSION` 输出，网格仍不向 GI 注入光。这是当前棚光中的艺术近似，不是物理多次散射。早期尝试 `IRRADIANCE` 效果不足；[Godot 官方 API](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html)定义其混合环境辐照度，[Forward+ 源码](https://github.com/godotengine/godot/blob/4.7-stable/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl)显示后续 GI 路径会覆盖该项，不能仅以写入参数视为补光生效。最终以真实图选择参数；未来若换完全不同的灯光，应重新验收这个近似。

证据根目录：`artifacts/puppet-clean-materials-20261001`。

- [手脸与毛发下沿原像素对照](../artifacts/puppet-clean-materials-20261001/materials-detail-before-after.png)、[完整同镜头 FPS](../artifacts/puppet-clean-materials-20261001/fps-before-after.png)、[洞壁对照](../artifacts/puppet-clean-materials-20261001/cavity-original-pixels.png)。左旧右新；仅裁切和排列引擎截图，没有缩放、补画或后期改色。
- [六状态完整 FPS](../artifacts/puppet-clean-materials-20261001/six-states-original-pixels.png)；`final-full` 保留十五张原图，包括六个近景、原生 LMB 切面及断裂诊断。毛发数量、相同切面的 density/网格数量、73°/1.7 m 相机、MSAA4×+TAA、零 shell 均保持原值。短绒未减量。
- [实际移动帧对照](../artifacts/puppet-clean-materials-20261001/motion-original-pixels.png)；[旧版视频](../artifacts/puppet-clean-materials-20261001/legacy-native/motion.mp4) / [新版视频](../artifacts/puppet-clean-materials-20261001/final-native-motion/motion.mp4)，分别 159/160 张原始帧，约 5.8 秒。按真实时间戳编码，不插帧；最后一帧仅重复以保留显示时长。GPU 在读回和 PNG 编码前独立测量，约 27 Hz 的记录频率不等于游戏 FPS。

目视判断：正常毛发下沿由近黑变为暗紫，轮廓不再像涂了一圈黑色；洞壁有所提亮但仍保留深度，焦黑/湿发/霜白仍可区分。手脸原有的大块污渍感明显减弱，较近时仍可见轻微细纹。第一版人物过平滑，第二版黑边改善不足，均保留为被替换的候选。最终较干净的材质也更容易看清手指网格接缝；这项、帽檐式大形和近景硬直毛丝仍待后续几何处理，不把本轮当成整体艺术验收通过。

固定像素区域仅作诊断：毛发下沿 `(504,456)–(582,470)` 的平均 8-bit 亮度 21.26 → 41.51；右拇指 `(906,620)–(940,660)` 的均值 184.48 → 182.90，而相对 2px Gaussian 局部均值的亮度残差标准差 1.91 → 0.39。这说明手部主要减少局部斑驳，并非单纯提白。区域与原始数据在审计中；这些数字不代表观感改善百分比或动态舒适度结论。

同一冻结源码/DLL、单引擎顺序运行，RTX5070Ti Laptop / Forward+ / 1280×800，每视图预热 4 秒，再取至少 1.5 秒/60 帧：

| 实际视图 | 旧材质 GPU ms / FPS | 新材质 GPU ms / FPS |
| --- | --- | --- |
| 相同原生鼠标切面 | 6.409 / 155.3 | 6.401 / 155.3 |
| 洞壁 FPS | 6.834 / 145.8 | 7.133 / 139.4 |

另一次完整十五视图采样中，正常/鼠标切后为 6.960/6.350 ms；六状态 FPS 为 6.47–7.13 ms。四颗头（1 Hero + 3 Nearby）+12 个 core-only 碎块、零 shell 为 6.265 ms /158.8 FPS，相机较远且无人物/手，只用于渲染压力检查。相同鼠标切面成本相当，洞壁短样本新版略慢；没有性能收益或真实多人结论。[全部原始 GPU 数据](../artifacts/puppet-clean-materials-20261001/gpu.csv)保留。完整轮实际剪切仍为主线程 39.55 ms、毛丝就绪 438.04 ms，41 帧继续运行，未声称改善剪切等待。

最终构建成功；首轮编译仍见两条既有 nullable warning，最终增量构建 0 error。Core 248/248、原生 B2 25 + 优化 13 + 空网格 9 =47 项全通过；连续 108 刀剪空、空头点击与 F4/F5、R 恢复后再剪都通过。正式 B 有渲染 WorldChecks 输出 `EXPANSION_13_ENGINE_OK` 并正常退出；未重复 WAN/语音或真实 4P。[冻结审计](../artifacts/puppet-clean-materials-20261001/final-audit.json)验证 6 次 B2 运行的 44 个候选文件一致，19 个有效 GPU 视图；前次空网格修复版本 38 个文件不变，仅六项材质/参数/启动器/DLL 改动。DLL：`9152CDF8C5A740B18DFFE9A57AADFC101DCD6A6346D176B1D05CE873C8987483`。

已构建，可直接重开；在下面第二条后加 `-LegacyMaterials` 做同构建旧外观对照。`-LegacySampling` 仍是独立的旧摩尔纹对照开关。

```powershell
dotnet build ProjectHairball.csproj --nologo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Interactive
```

下一次真人重点是在实际 FPS 移动并剪切时看毛发边缘是否仍过暗，手部是否干净且保有布偶质感。旧候选、未修改基线及关 AO/阴影诊断都保留；本轮没有扩玩法、重新运行 Round 1–2、改正式 B 入口或替换发布包。
