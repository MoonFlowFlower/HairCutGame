# Puppet Hair v0.4 — R7.1 实测记录

此处保留首次贴面 Shell 实验的历史数据。Owner 随后确认视频 00:44 的蓬松目标，新一轮见 [蓬松毛束报告](PUPPET_HAIR_PLUSH_0044.md)。当前启动器附加 `-Flat` 才回到本文候选；本文的失败结论及冻结证据不变。

2026-10-01。Owner 的当前要求优先：只按 R7.1 → Gate 1 → R7.2 → Gate 2 的顺序推进。本文件记录当前实验，不代表正式 B 或 Owner 视觉验收通过。

## 输入与基线

已解压并完整阅读增量包 README、CODEX_KICKOFF、REFERENCE_INDEX、docs/00–09，查看八张参考图。包保存在 `artifacts/puppet-shell-r7-v04/pack/PuppetHair_ShellFur_R7_Delta_v0.4`。文件内技术建议只在 Owner 授权的本轮范围内采用。

R6 对照是当前 v0.3 Round 3 加上摩尔纹、空网格、黑边及手脸材质修正的 independent fibers。不是重新启用早期有缺陷的 R6。修改前源码/清单在 `before-source.zip`、`before-candidate.json` 和 `before-all-source.json`，三状态实机基线在 `audit-baseline`。此后的最终 A/B 使用同一新构建切换渲染模式，保持身体、灯光和密度一致。

## 审计结论

- `HairVolume` 为 35×31×27 byte 量化 signed scalar，正值在内，解码范围 ±0.28。椭球近似、平滑 CSG、重采样、侵蚀都不保证真实距离，没有 reinitialization。不能直接按 SDF 做 AO、深度或安全步进。
- `HairShell.Build` 使用每格六个 tetrahedra 的零等值面提取、共享边索引、梯度法线。B2 顶点先除以 3.5，Root 再缩放 0.9；0.14 的密度步长在本场景实际为 **3.6 cm**，不是 14 cm。
- COLOR.a 的 0/0.5/1 表示 Normal/Trimmed/Interior，来源是原始体积采样和真实 cut plane。它不是距离。R7 复用现有分类，所有壳层沿同一真实 core 更新。
- R6 每根毛是两片交叉 ribbon，长毛三段、短毛一段，固定随机种子。Normal 65,236 根、782,832 毛丝三角形；Trimmed 123,275 根、800,404 三角形；Carved 96,482 根、879,192 三角形。
- 既有少量 shell 使用逐层独立 hash，不能直接加到 16 层冒充 coherent fur。新增 R7 使用共享毛根纹理、连续 taper 和有限倾斜，配少量只在掠射角可见的毛丝。
- core density、切割、DetachUnsupported、支撑判定、网络、评分、正式 B 入口保持原路径。`debris_flocked.gdshader` 不在当前 B2 碎块路径上，本轮没有修改它。碎块仍使用已有 core-only 路径。

## R7.1 候选及测量

显式 `R6_FIBERS` / `R7_SHELL` 模式；比较 0 / 4 / 8 / 12 / 16 层。最终候选的 Normal pile 0.012、Trimmed 0.0038、Carved 0.0026 为本地米，世界尺度再乘 0.9。层位移公式的保守上限为 1.1232 cm；稀疏毛丝实际生成顶点到根部的最大距离另行记录。内侧原有短毛限制保留，避免穿过脸面。未增加动态 groom field、SDF AO 或玩法。

环境：Godot 4.7.2 Mono、RTX 5070 Ti Laptop、Forward+、1280×800、MSAA 4×、TAA、现有 fabric/SDFGI 灯光，VSync off。每镜头暖机 4 秒，采样至少 2 秒 / 120 帧；截图和像素读回不混入性能采样。CPU 数字仅为 viewport render + frame setup，非完整 CPU 游戏帧；显存数字为 Godot allocation monitor，非进程驻留显存。短样本可能受笔记本 GPU 时钟影响，不外推其他硬件或联网表现。

每种模式保存 Normal / Trimmed / Carved 各五个镜头：正面约 1 m、侧前方最近毛面 48 cm、侧面约 1 m、俯视、低角度 50 cm。除俯视诊断外均固定 1.7 m 视高、73° FOV、真实 FPS 手持物和场景遮挡，无景深。另有通过原生 LMB 路径完成的同一刀前后截图及密度 hash。R7 试玩可绕到侧面，平面半径至少 0.55 m；原 R6 启动器的行走范围不变。

Silhouette Error 使用同一真实 viewport 的三次输出：可见毛尖 mask、core mask、core view-depth。自动采集锁定头部 pose；仅 mask/depth 阶段关闭 TAA，保留 MSAA 和其他物体深度遮挡。`measure_puppet_shells.py` 对可见毛尖到 core 的距离做逐像素 Euclidean distance transform，用最近完整 core 像素的深度换算局部视平面厘米估计。JSON 同时保存核心位置估计、实际毛尖像素位置、二者距离及根部几何上限。

这个厘米值不是精确 3D 最近点距离，也不是 SDF。PNG 深度有量化误差，毛尖越出 viewport 的镜头标记为 censored；轮廓判定优先使用未裁切的固定镜头。GPU 压力镜头的裁切可以用于性能测试，不能用于宣称完整轮廓误差。

Compatibility 未作为等价 A/B：当前 fabric profile 明确要求 Forward+，关闭其 GI/材质环境去凑一行数据会改变比较对象。

## 保留的失败试片

- `pilot-*`：最近距离求解把相机推进面部，不是有效 FPS 验收。保留，不计入最终矩阵。
- `pilot2-*`：镜头修复后，4/8 层表面偏平滑，凹壁仍有泥块感；没有通过 Gate 1。
- `pilot3-*` / `pilot4-*`：增加覆盖、有限毛根法线和根尖明暗后的试片。检查时发现自动采集允许 idle pose 变化，随后锁定 pose；这些早期 mask 不用于最终 Silhouette 结论。
- `matrix/perf-8-one`：48.48% 可见毛发覆盖的压力镜头预检。当时混淆了整头和毛发 coverage，这条预检不能用于证明最终负载；不与最终冻结构建合并。
- `final-matrix` / `final-analysis` 名称保留原样，但这是被否决的候选 A，而非最终通过证据：完整 80 镜头及 GPU 测量抓到四档 Shell 共用的侧脸孤立毛点，25.46 px / 约 5.99 cm 的可见核心轮廓距离。这里的厘米含遮挡/显露效应，不能当作毛丝物理长度。候选 B 恢复内侧 4 mm 上限、收短外毛、改为 Poisson 毛根位置；修正后侧面预检为约 0.76 cm。
- `candidate-b`：保留修正和压力镜头预检。最初错误地把包中的“头占屏 50%+”解释成“毛发像素 50%+”，做了不必要的站位搜索；已修正口径。最终分别输出整头和毛发 coverage，主视高、FOV、头部尺寸均不改变。504 个真实相机姿势的搜索记录只是镜头选择，不是性能通过证据。

最终冻结矩阵在 `accepted-matrix`，目录名表示进入最终评审的数据集，不表示视觉通过。不得把候选 A、早期预检与该矩阵拼成同构建通过。

## 最终结果

**R7.1 Gate 1 未通过，保留 R6 默认入口。** 五档均已完成三状态、五角度和真实 LMB 剪切；Shell 降低了毛边偏移和渲染成本，Normal 的外表面也比 R6 的交叉细丝更紧实。但 Trimmed 大平面仍偏像绒面泡沫，Carved 的内壁缺少可读的短绒厚度，洞口的 core 网格台阶反而更显眼。4 → 8 → 12 → 16 层主要改善表层颗粒的连续性，没有解决内部材质目标。不能用 GPU 收益抵消这个视觉失败。

不进入 R7.2 推剪条纹、R7.3 状态或 R7.4 LOD/多人接入。现有 R7 启动器只用于回看本次候选，正式 B 和原有 optimization Round 3 入口继续原路径。失败候选、原始截图、mask、GPU 样本和视频全部保留。

### 对比图与测量口径

- [五档三状态原像素裁切](../artifacts/puppet-shell-r7-v04/review/gate1-original-pixels.png)：每列 R6 / 4 / 8 / 12 / 16，三行 Normal 近景 / Trimmed 俯视诊断 / Carved 低角度。裁切不缩放、不重绘。
- [R6 完整镜头目录](../artifacts/puppet-shell-r7-v04/review/contact-0.png)、[8 层](../artifacts/puppet-shell-r7-v04/review/contact-8.png)、[16 层](../artifacts/puppet-shell-r7-v04/review/contact-16.png)：缩略索引，原始 1280×800 PNG 在各自 `accepted-matrix/visual-*` 目录。
- [逐镜头轮廓 CSV](../artifacts/puppet-shell-r7-v04/review/visual-measurements/measurements.csv) 与 [JSON](../artifacts/puppet-shell-r7-v04/review/visual-measurements/measurements.json) 包含毛尖/core 像素与位置估计、p95、裁切标记、几何位移上限和 GPU。没有过滤孤立毛点以改善结果。

每档有 13 张第一人称验收图、3 张俯视诊断图，另有原生剪切前图。所有 64 组 Shell/R6 配对的 core mask 逐像素相同，density hash 和 camera 也相同。三张低角度图均碰到屏幕边缘，完整轮廓统计使用其余每档 10 张未裁切 FPS 图。厘米值是局部视平面估计；相机距离通过最近 core 顶点估计，不是精确三角面最近点。

### Silhouette Error

| 模式 | 未裁切 FPS 最大 px | 最大视平面估计 cm | Normal / Trimmed / Carved 近景最大 cm |
|---|---:|---:|---|
| R6 fibers | 23.77 | 2.48 | 2.30 / 2.48 / 2.23 |
| 4 Shell | 8.54 | 1.68 | 0.85 / 0.88 / 0.85 |
| 8 Shell | 9.00 | 1.68 | 0.87 / 0.91 / 0.87 |
| 12 Shell | 9.06 | 1.68 | 0.87 / 0.96 / 0.94 |
| 16 Shell | 9.00 | 1.68 | 0.87 / 0.96 / 0.94 |

每列最大值独立统计，最大 px 与最大 cm 不一定发生在同一镜头。Shell 四档共同的最坏厘米位置在 Carved 正面：7.28 px / 约 1.68 cm；不是忽略异常点后的平均值。Shell 几何根部位移上限 1.1232 cm，稀疏毛丝实际最大根尖距离 1.458 cm，均与可见核心轮廓测量分开。R6 实际根尖最大约 4.34 cm。

同一 LMB 切割的五档前后 density hash 一致，Shell 随实际切面更新。此结果支持“没有另造可剪轮廓”，不等于玩家瞄准手感通过。主线程切割仍为单次几十毫秒；R6 / 8 Shell 此刀 build 38.77 / 51.02 ms，毛丝 ready 581.62 / 134.72 ms，单次结果不能充当稳定延迟改善结论。

### GPU 交错复测：同一 48 cm Normal 镜头

顺序 R6 → 4 → 8 → R6 → 12 → 16 → R6，用来检查长矩阵期间 GPU 时钟/温度变化的影响。三次 R6 为 6.647 / 6.605 / 6.636 ms。下表 R6 显示范围，其余为一次短样本。

| 模式 | GPU ms | FPS |
|---|---:|---:|
| R6 fibers | 6.605–6.647 | 149.9 |
| 4 Shell | 5.161 | 192.4 |
| 8 Shell | 5.546 | 179.2 |
| 12 Shell | 5.787 | 171.5 |
| 16 Shell | 5.983 | 166.0 |

完整三状态矩阵的原始 GPU 数字保存在逐镜头 CSV 中；其中部分运行发生时钟变化，例如 4 层某些镜头比后跑的 8 层慢。不能混选最低样本作为“8 层更便宜”的证据。

### 1 / 2 / 4 头实际渲染压力

1 头在 30.5 cm 最近 core 顶点距离、1.7 m 视高下，整头可见覆盖 75.3–76.6%，毛发覆盖 47.8–49.1%，满足包中整头占屏 50%+。2 / 4 头保持相同 1.7 m FPS、73° FOV、房间及手持物；主头最近约 81 cm，其他完整毛发实例处于同屏。所有实例均使用所列层数，不用远景 LOD 隐藏成本。4×12 的 GPU 9.319 ms、帧 p99 10.466 ms 留有 60 FPS 余量后，才补测 4×16。

这是 1 个完整顾客加额外完整毛发实例的渲染压力样例，附加实例没有角色身体/网络逻辑；不是四顾客玩法或真实四玩家/WAN 验收。

| 模式 | 头数 | 整头/毛发覆盖 % | GPU ms | FPS | 帧 p99 ms | Render CPU ms | Draws | Visible primitives | 引擎显存分配 MiB |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| R6 fibers | 1 | 76.63/49.13 | 7.068 | 140.6 | 8.058 | 1.235 | 138 | 1,301,244 | 760.3 |
| R6 fibers | 2 | 41.52/23.79 | 9.594 | 103.7 | 10.536 | 1.923 | 283 | 2,466,628 | 801.5 |
| R6 fibers | 4 | 57.49/39.77 | 12.920 | 77.2 | 13.853 | 2.028 | 287 | 4,071,140 | 884.0 |
| 4 Shell | 1 | 75.29/47.81 | 5.001 | 198.4 | 5.803 | 1.375 | 142 | 623,708 | 698.1 |
| 4 Shell | 4 | 56.93/39.23 | 7.480 | 132.9 | 8.381 | 1.734 | 303 | 1,360,996 | 703.2 |
| 8 Shell | 1 | 75.60/48.15 | 5.519 | 179.9 | 6.151 | 1.223 | 146 | 701,404 | 698.1 |
| 8 Shell | 2 | 41.08/23.37 | 7.153 | 139.0 | 8.520 | 1.921 | 299 | 1,266,948 | 699.8 |
| 8 Shell | 4 | 57.04/39.35 | 8.599 | 115.8 | 9.923 | 1.807 | 319 | 1,671,780 | 703.2 |
| 12 Shell | 1 | 75.68/48.24 | 6.136 | 161.9 | 7.123 | 1.418 | 150 | 779,100 | 698.1 |
| 12 Shell | 2 | 41.08/23.37 | 7.580 | 131.3 | 8.648 | 2.078 | 307 | 1,422,340 | 699.8 |
| 12 Shell | 4 | 57.04/39.34 | 9.319 | 106.9 | 10.466 | 2.445 | 335 | 1,982,564 | 703.2 |
| 16 Shell | 1 | 75.75/48.31 | 6.571 | 151.4 | 7.572 | 1.478 | 154 | 856,796 | 698.1 |
| 16 Shell | 4 | 57.06/39.37 | 10.020 | 99.4 | 11.298 | 2.339 | 351 | 2,293,348 | 703.2 |

[压力测试完整 CSV](../artifacts/puppet-shell-r7-v04/review/perf-measurements/measurements.csv)。多头“整头”coverage 包括主顾客脸和全部毛发实例。Visible primitives 是引擎可见绘制统计；真实 core/fiber/shell 三角形计数另见每次 `r7-metrics.json`，二者不混用。

### 移动、回归与冻结

R6 / 4 / 8 层各录制约 5.8 秒固定视高的真实相机移动，共 156 + 164 + 163 = **483 帧**。视频按实际时间戳编码，保留全部帧，无补帧/降噪。检查了 1.2 / 2.4 / 3.6 / 5.0 秒附近的原像素抽帧；Shell 仍有颗粒和方向性细纹，未给出“摩尔纹完全消失”或“无闪烁”结论。前 0.8 秒紧接 mask 之后的 GI 恢复，不用于静止时间方差通过声明。

- [R6 视频](../artifacts/puppet-shell-r7-v04/accepted-matrix/motion-0/motion.mp4)、[4 层视频](../artifacts/puppet-shell-r7-v04/accepted-matrix/motion-4/motion.mp4)、[8 层视频](../artifacts/puppet-shell-r7-v04/accepted-matrix/motion-8/motion.mp4)、[实际抽帧](../artifacts/puppet-shell-r7-v04/review/motion-original-pixels.png)。
- `dotnet build ProjectHairball.csproj --nologo` 成功，0 error、2 条既有 nullable warnings。Core 测试本轮 248/248；后续仅修改表现层/实验工具，Core 文件审计保持原值。
- 最终冻结构建：R7 原生 32 + 空网格 9 = **41 项通过**；R6 原生 25 + optimization 13 + 空网格 9 = **47 项通过**。两种模式各经真实 LMB 连剪 108 刀到零 core/零毛丝，空头操作、R 恢复和再剪通过；未出现 `array_len == 0` / `ERR_INVALID_DATA`。
- 正式 B 有渲染 WorldChecks exit 0 / `EXPANSION_13_ENGINE_OK`。没有更改网络或运行新 WAN/真实 4P 验收。
- [最终冻结审计](../artifacts/puppet-shell-r7-v04/review/final-audit.json)：28 次取证 + 2 次原生检查，共 30 个运行、51 个候选文件完全一致；原有 215 个源文件中 6 个表现/实验入口文件变化、209 个未变，新增 7 个 R7 文件。DLL `8D823D3C94F0DC3AA615512426936D174748109886203F7EA375E783AC8A77A9`。正式 B world check 在该构建上运行。

### 可运行入口与下次判断

从仓库根目录：

```powershell
dotnet build ProjectHairball.csproj --nologo
# 当前默认保留的 R6（含先前摩尔纹、空网格、手脸/黑边修复）
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_hair_optimization.ps1 -Round 3 -Interactive
# 仅回看本轮未通过的 Shell 候选；可将 4 改成 8/12/16
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_shell_r7.ps1 -Mode R7_SHELL -Shells 4 -Flat -Interactive
# 同一 R7 测试相机/站位规则下的 R6 对照
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive
```

Tab 进入 FPS，WASD/鼠标，LMB 真实剪切，R 重置；R7 的 1 / 2 / 6 分别为 Normal / Trimmed / Carved 并保持站位，F4 看 baseline，F5 看灰色核心。最有价值的真人检查仍是 2/6 和实际 LMB 切出的表面能否读成短密毛绒、洞口台阶是否干扰剪切。

若继续 R7.1，下一项应只验证切面/内壁的有限短绒覆盖和发流，让正视凹壁也有可读的绒层；当前 edge-only 的稀疏毛丝主要帮助外轮廓。先用 R6 与 4/8 层同镜头验证这项因果判断，再考虑层数。这个方向尚未实现或通过，不作为 Gate 1 放行理由。

