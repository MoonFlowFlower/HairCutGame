# R7.1 — 按视频 00:44 修正蓬松方向

2026-10-01。本轮承接 Owner 对 `41736931027-1-192.mp4` 00:44 蓬松目标的确认。此前贴面 Shell 实验及其失败结论保留在 [原 R7 报告](PUPPET_HAIR_SHELL_R7.md)。包内技术建议是参考；本轮具体授权是修正外观、继续 R7.1 对照，不扩玩法。

## 改动与目视判断

Normal 现在使用浅层 Shell 毛根覆盖，加上五根一束、独立变长、弯曲和收尖的几何毛丝。外轮廓明显蓬起，同时保留原有大卷的方向。Trimmed / Carved 使用更短、更密的毛束，真实切面和洞壁保留同色绒面。脸和手继续原短布绒。

这是一条 **Shell 根部覆盖 + 几何毛束** 的混合路线，不能把结果归功于增加 Shell。4 / 8 / 12 / 16 层共用相同毛束；增加层数主要影响靠近表面的覆盖，不能改善长毛的形状。

当前方向比旧贴面候选更接近视频；但 48 cm 近看仍偏细丝，30 cm 压力近景的根部颗粒更明显，底部转折还露出较平的绒面，洞口也保留密度网格的台阶。视频中更松软、成簇的毛尖还没有完全达到。**本轮候选可试玩，Gate 1 不作为已通过处理，不进入 R7.2 推剪条纹或后续状态，也未替换正式 B。**

- [真实 FPS 总览对比](../artifacts/puppet-plush-0044/review/normal-comparison.png)：R6 / 旧贴面 4 层 / 新毛束 4 层，整帧缩小索引。
- [新 4 层完整正面](../artifacts/puppet-plush-0044/final/visual-4/r7-normal-front.png)、[48 cm 近景](../artifacts/puppet-plush-0044/final/visual-4/r7-normal-close.png)、[洞壁低角度](../artifacts/puppet-plush-0044/final/visual-4/r7-carved-low.png)、[Trimmed 俯视诊断](../artifacts/puppet-plush-0044/final/visual-4/r7-trimmed-top.png)。
- [六档三状态原像素裁切](../artifacts/puppet-plush-0044/review/gate1-original-pixels.png)：所有模式使用同一固定矩形，未经重绘、降噪或锐化。

## 保持的真实路径

`HairVolume` 仍是量化 signed scalar，不把它当 SDF。原密度、marching tetrahedra 网格、Normal / Trimmed / Interior 分类、原生 LMB、异步毛丝重建、DetachUnsupported、空网格保护均复用。没有扩大可剪体积去迎合外层毛尖，也没有改网络、玩法、房间灯光或身体材质。

Normal 的名义毛束长度为本地 0.040 m，根部 Shell 0.012 m；Trimmed 毛束 0.009 m、Shell 0.003 m；Carved 毛束 0.007 m、Shell 0.0025 m。长度有随机变化并按 Root 0.9 缩放，参数不是最终世界位移。几何检查分别记录长毛和短绒的实际顶点距根部上限。检查中的 6 cm / 1.7 cm 只是防意外生成过长几何的保护线，**不是 Silhouette Error 或瞄准手感通过线**。

## 保留的试片

所有失败候选均保存在 `artifacts/puppet-plush-0044`，不与最后冻结矩阵混算。

- p1：加高 Shell，配双片毛束纹理；毛片被底层覆盖，出现厚片和颗粒，失败。
- p2：缩浅 Shell，试用纹理毛片与 alpha-to-coverage；近处细纹仍合并为纸片状粗条，GPU 成本上升，失败。该 shader 已从当前源码移除。
- p3：改成五根真实几何毛丝，0.055 m 名义长毛；偏长、偏稀、像针丝，未采用。
- p4：收至 0.040 m、增加根部密度与弯曲。最后冻结版本进一步收细根部纹理比例，避免大颗粒。

## 取证方法

Godot 4.7.2 Mono，RTX 5070 Ti Laptop，Forward+，1280×800，MSAA 4× + TAA，VSync off。每镜头暖机 4 秒、测量至少 2 秒 / 120 帧，GPU 采样在 PNG 读回前完成。各运行顺序执行，期间不改源码/DLL；视频编码和像素分析放在 GPU 批次之后。

同一最终构建比较 R6、新 4/8/12/16 层以及旧贴面 4 层。每档 Normal / Trimmed / Carved 各五角度，加真实 LMB 后截图，共 16 个镜头；其中每档 13 个固定 1.7 m 视高的 FPS 镜头，另 3 个俯视诊断。每档还有原生剪切前图。

Silhouette Error 来自真实 viewport 的 core / tips / depth 输出，保留场景遮挡，只有 mask 阶段关闭 TAA。测量最外可见毛尖到 core mask 的像素距离；厘米用最近完整 core 像素深度换算为局部视平面估计，**不是精确三维距离或 SDF**。越出屏幕边缘的镜头标记 censored，不用于完整轮廓通过声明。长外毛和新露出短绒的根部几何距离另列。

CPU 只记录 viewport render + frame setup，非完整游戏 CPU 帧。显存为引擎分配监视值，非进程驻留显存。多头样例是 1 个完整顾客加完整毛发实例，不包含附加人物身体、玩家逻辑或联网；只用于同屏渲染压力。

## 最终截图、轮廓与实际剪切

最终六档共 96 个镜头，每档 10 张未裁切 FPS 图用于完整轮廓统计。所有 **80 组** 新 Shell / 旧贴面 Shell 对 R6 的 core mask 逐像素一致，density hash、camera 和核心三角形数相同。R6 与上次冻结矩阵的 16 张毛尖 mask 也逐像素相同；旧贴面 Shell 的几何统计相同，16 张中仅一个像素颜色差，未隐瞒或清理该差异，见 [回退审计](../artifacts/puppet-plush-0044/review/fallback-comparison.json)。

| 模式 | 未裁切 FPS 最大 px | 最大视平面估计 cm | Normal / Trimmed / Carved 近景最大 cm |
|---|---:|---:|---|
| R6 | 23.77 | 2.48 | 2.30 / 2.48 / 2.23 |
| 旧贴面 4 Shell | 8.54 | 1.68 | 0.85 / 0.88 / 0.85 |
| 新 4 Shell + 毛束 | 39.05 | 4.30 | 4.15 / 4.30 / 4.03 |
| 新 8 Shell + 毛束 | 39.05 | 4.30 | 4.15 / 4.30 / 4.03 |
| 新 12 Shell + 毛束 | 39.05 | 4.30 | 4.15 / 4.30 / 4.03 |
| 新 16 Shell + 毛束 | 39.05 | 4.30 | 4.15 / 4.30 / 4.03 |

px 与 cm 的最大值独立统计，未必来自同一镜头。四种新 Shell 使用同一毛束，因此外轮廓结果相同；并非将一种层数的测量复制给其他层。厘米最坏点在 Trimmed 近景仍未剪短的外侧长毛；**这不是剪平面厚度 4.30 cm**。短绒实际顶点至根部最大距离：Trimmed 1.21 cm、Carved 0.94 cm、原生剪后 1.19 cm；整个 Normal 毛尖最大根部距离 5.27 cm。根部 Shell 几何保守上限 1.1232 cm。

蓬松增加了可见外轮廓与真正可剪 core 的差距，因此没有宣称瞄准手感或 cut boundary gate 通过。没有移动 core 或扩大 raycast 来掩盖误差。[逐镜头 CSV](../artifacts/puppet-plush-0044/review/visual-measurements/measurements.csv)、[JSON](../artifacts/puppet-plush-0044/review/visual-measurements/measurements.json) 保存像素位置、深度换算位置、p95、裁切标志及全部 GPU 值；[最坏厘米镜头标线](../artifacts/puppet-plush-0044/review/visual-measurements/visual-4--r7-trimmed-close-silhouette.png) 可直接查看。

新 4 层的实际几何：

| 状态 | Core 三角形 | 毛丝根数 | 毛丝三角形 | Shell 三角形 |
|---|---:|---:|---:|---:|
| Normal | 19,424 | 90,730 | 644,380 | 77,696 |
| Trimmed | 15,460 | 115,300 | 593,040 | 61,840 |
| Carved | 19,716 | 110,475 | 707,500 | 78,864 |

新版本的根数指五根一束中的每条单 ribbon；R6 的每根由两片交叉 ribbon 构成，不能只拿“根数”比较成本。六档相同原生 LMB 的前后密度 hash 一致。此刀 R6 / 新 4 层的主线程 build 为 38.61 / 41.66 ms，全部毛绒 ready 为 558.29 / 441.96 ms；这是一次剪切样本，不是稳定延迟收益证明。数百毫秒重建仍是可感知的潜在问题。

## 最终 GPU 压力

单头相机距离最近 core 顶点约 30.5 cm。新 4 层的整头可见覆盖 80.16%、毛发覆盖 52.82%；R6 分别为 76.63% / 49.13%，均满足整头占屏 50%+ 的口径。2 / 4 头使用相同的 1.7 m FPS 站位、73° FOV，主头最近约 81 cm；所有附加毛发实例使用所列层数，没有远景 LOD 降成本。

| 模式 | 头数 | GPU ms | FPS | 帧 p99 ms | 整头 / 毛发覆盖 % |
|---|---:|---:|---:|---:|---|
| R6，交错 a | 1 | 7.401 | 133.6 | 8.863 | 76.63 / 49.13 |
| 新 4 | 1 | 7.924 | 125.6 | 9.450 | 80.16 / 52.82 |
| 新 8 | 1 | 7.637 | 124.4 | 9.434 | 80.19 / 52.87 |
| R6，交错 b | 1 | 7.384 | 134.3 | 8.750 | 76.63 / 49.13 |
| 新 12 | 1 | 9.026 | 110.4 | 10.189 | 80.20 / 52.89 |
| 新 16 | 1 | 9.627 | 103.4 | 11.303 | 80.21 / 52.90 |
| R6，交错 c | 1 | 8.352 | 119.2 | 9.628 | 76.63 / 49.13 |
| 旧贴面 4 | 1 | 5.702 | 174.0 | 7.146 | 75.29 / 47.81 |
| R6 | 2 | 11.266 | 88.5 | 12.677 | 41.52 / 23.79 |
| 新 8 | 2 | 10.490 | 95.0 | 13.157 | 42.47 / 24.77 |
| 新 12 | 2 | 12.308 | 80.1 | 14.151 | 42.47 / 24.78 |
| R6 | 4 | 15.637 | 63.7 | 16.940 | 57.49 / 39.77 |
| 新 4 | 4 | 14.394 | 68.7 | 16.250 | 58.66 / 40.98 |
| 新 8 | 4 | 15.674 | 63.7 | 16.886 | 58.67 / 40.99 |
| 新 12 | 4 | 16.569 | 60.2 | 17.599 | 58.67 / 40.99 |

4×12 已没有充足余量，按包中条件跳过 4×16；[决策记录](../artifacts/puppet-plush-0044/final/four16-decision.json) 包含事先采用的余量标准。4 层是继续视觉迭代的候选：12 / 16 层没有相称的外观收益。新方案没有保住旧贴面 Shell 的大幅省时收益，单头与 R6 在相近量级；四头 4 层样本好于 R6，但仍接近 60 FPS 的预算，不能外推正式多人场景。

R6 三次交错 GPU 为 7.401 / 7.384 / 8.352 ms，说明运行条件存在波动。8 层单次 GPU 略低于 4 层不能证明它更便宜；这里没有挑最低样本宣称优化比例。完整 [压力 CSV](../artifacts/puppet-plush-0044/review/perf-measurements/measurements.csv) 还包含 Render CPU、draw calls、visible primitives、引擎显存分配等。

## 移动、回归和候选身份

R6 / 4 / 8 层各约 5.8 秒实际移动，分别 155 / 165 / 161 帧，共 **481 帧**。mask 恢复后额外等候约 4 秒再录制；视频按实测时间戳编码，保留全部帧，只重复最后一帧以表达末帧时长，无补帧或降噪。PNG 读回影响录制帧率，所以录像不是性能基准。抽帧仍能看出细丝，未宣称摩尔纹消失或无闪烁。

- [新 4 层真实移动](../artifacts/puppet-plush-0044/final/motion-4/motion.mp4)、[R6](../artifacts/puppet-plush-0044/final/motion-0/motion.mp4)、[新 8 层](../artifacts/puppet-plush-0044/final/motion-8/motion.mp4)、[移动原像素抽帧](../artifacts/puppet-plush-0044/review/motion-original-pixels.png)。
- 构建成功，0 error、2 条已有 nullable warning（GesturePropsView / PartyMoldView）。`dotnet run --project tests/Core/CoreTests.csproj`：**248/248**。
- R7 原生 **32 + 9 = 41**、R6 原生 **25 + 13 + 9 = 47** 项通过；两者均真实 LMB 连剪 108 刀至零 core / 零毛丝，空头操作、R 恢复及再剪通过，未出现 `array_len == 0` / `ERR_INVALID_DATA`。
- 正式 B 有渲染 WorldChecks：exit 0，`EXPANSION_13_ENGINE_OK`。没有修改网络，也没有运行新的 WAN / 真人 4P 验收。
- [候选审计](../artifacts/puppet-plush-0044/review/candidate-audit.json)：24 次取证 + 2 次原生检查，共 **26 个运行的 51 个候选文件**与当前源码/DLL 一致。相对上一版仅 9 个表现/实验文件及 DLL 变化，其余 41 个候选文件相同。
- 冻结 DLL：`BD5AF2EDD4A7A6BE4488980A8D2BF629117E28265AAFCDEEFC41C89A7CBD4864`。最终证据在 `artifacts/puppet-plush-0044/final`，p1–p4 不混入最终结果。以上机械通过不能覆盖视觉和瞄准判断的未通过项。
- [最终验证汇总](../artifacts/puppet-plush-0044/review/final-validation.json) 再次核对全部 51 个当前文件及 27 份引擎日志：候选未变，取证均结束，无引擎错误记录。验证进程已退出。

## 可运行入口

在仓库根目录执行：

```powershell
dotnet build ProjectHairball.csproj --nologo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_shell_r7.ps1 -Shells 4 -Interactive
```

Tab 自由移动，WASD / 鼠标；LMB 实际剪切，R 恢复。1 / 2 / 6 切换 Normal / Trimmed / Carved。可以改为 8 / 12 / 16 层作相同头型对照；`-Flat` 恢复上次被否决的贴面 Shell 候选，`-Mode R6_FIBERS` 对照当前 R6。原 `puppet_hair_optimization.ps1 -Round 3 -Interactive` 继续使用 R6。

下一次真人判断优先看：绕头移动时是否仍显毛刷/细丝；近看短绒与外部厚毛是否属于同一材质；沿轮廓剪切时是否因毛尖超出 core 而误判。画面更蓬松不等于这三项自动通过。
