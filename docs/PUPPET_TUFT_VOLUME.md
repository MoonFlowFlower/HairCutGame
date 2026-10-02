# 固定短毛束体积与动态候选

最新覆盖、接触与E243冻结阶段见 [剪切与接触复核](D:/Project/Game/Godot/project_hairball/docs/PUPPET_FUR_CONTACT_REVIEW.md)。下文是C3阶段的保留记录；不同构建的检查与性能不可混成同一次通过。

2026-10-02。本阶段把密齐梳纹改成更可读的小簇和散尖，并保留切开后仍然是毛的体积。**外观没有宣称达到视频 00:44，正式 B / R6 默认未替换。** 短切仍偏疏，某些小束像蕨叶；当前继续测试更明确的接触工况和内部覆盖。

## 实现与来源边界

显式 `-Hybrid` 使用四层 instanced Shell，加上原始体积内的固定短曲线薄片。毛束形态主要来自这些曲线，不能把本结果说成“四层纯 Shell 的效果”。参考视频可确认 Unlit Shell / ISM 及速度驱动表现；原作者完整公式/蓝图未公开，没有证据表明他用了 fins。Shell + fins 是依据公开毛发体积方法的独立借鉴，见 [来源记录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/SOURCES.md) 与 [原视频](https://www.bilibili.com/video/BV1aQY86REPu/)。

固定种子生成有限短曲线，映射至原始发量的物质坐标。当前 signed scalar 的六四面体占据插值裁掉已切除片段，保留原有根场、曲线与 renderer。没有重新往切面种毛，没有主头发的 opaque beauty Core，也没有把 HairVolume 当成 SDF。头皮端的形变权重为零，内部短束根随连续形变场运动；“根场保留”表示物质坐标与资源身份保留，不表示逐根微小毛丝的根都独立固定在世界中。

417,517 条原始曲线保留根和尖，自适应选择 1–6 段（平均 2.293），共 1,915,130 个薄片三角形，另有 36,864 个四层 Shell 三角形。相对原七段中心线，同参数误差最大 3.5 mm local / 3.15 mm world；这是中心线简化界限，不包括薄片朝向、宽度插值或屏幕轮廓的所有变化。

## C3F93B6F 冻结阶段

DLL `C3F93B6FAC2D863434B3DC0F4927BFDE976601A6C81E18EE01AD508529913BD3`。完整证据位于 `artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2`，分析位于同级 `stage-hybrid-c3f93b6f-review`。

17 次运行的 59 个候选文件及保存源码哈希一致。R6 / Hybrid 四层各 17 视角，8/12/16 层各三态近景，另有 R6 回测与四头压力。26 对相机、density 和 Core mask 原像素完全相同。GPU 顺序采样期间保存了 `gpu-monitor.csv` 和进程快照，测量窗口没有截图读回；不把本机短样本外推为真实 4P 或其他硬件。

![Normal 第一人称](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/visual-4/r7-normal-front.png)

![Carved 俯视诊断，仅用于检查内部](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/visual-4/r7-carved-top.png)

上图的内部是同一固定毛束体积；俯视明确是诊断视角，不计入 FPS 轮廓汇总。完整原像素、遮挡一致的 mask 与深度均保留。

### 静态图与双向轮廓

共同 11 个未裁切 FPS 镜头，各列独立取最大。cm 为最近 Core 深度换算的局部视平面估计，非三维距离。向内包括内部毛孔和漏采样，不能全解释成外轮廓退缩。

| 模式 | 向外 px / cm | 向内 px / cm | 最大缺失 Core 像素 |
|---|---:|---:|---:|
| R6 | 23.77 / 2.885 | 0 / 0 | 0% |
| Hybrid 4 | 35.74 / 3.791 | 10.30 / 1.770 | 12.94% |

低角度画面被 viewport 裁切，未纳入上表，但失败读数仍保留：Hybrid 向内最大 27.02 px / 约 2.24 cm，Normal/Trimmed/Carved 缺失 9.84/17.49/12.22%。不能因裁切而宣称这个角度已经解决。

| 模式 | Normal GPU ms / 缺失% | Trimmed GPU ms / 缺失% | Carved GPU ms / 缺失% |
|---|---:|---:|---:|
| R6 | 5.711 / 0 | 5.844 / 0 | 6.096 / 0 |
| Hybrid 4 | 5.192 / 2.99 | 5.420 / 6.62 | 5.493 / 4.55 |
| Hybrid 8 | 5.687 / 2.95 | 5.503 / 6.42 | 5.645 / 4.41 |
| Hybrid 12 | 5.739 / 2.88 | 5.591 / 6.23 | 5.747 / 4.30 |
| Hybrid 16 | 5.828 / 2.84 | 5.650 / 6.07 | 5.846 / 4.18 |

这些都是同一近景。R6 Normal 首尾为 5.711/6.046 ms，存在温度/时钟漂移，不能据小差值宣称稳定收益。8–16 层对孔隙改善有限，没有理由仅靠继续加层。Trimmed 正面缺失 12.94% 与近景 6.62% 是当前明确未通过项。

### 固定镜头动态

相机眼高 1.7 m / FOV 73°，头部、相机与灯光固定。每条约 18.5 秒，包含静止、风、停止、球进入/保持/退出、恢复、八次反向脉冲、恢复、受力中真实 LMB 剪切与最后静止。三态及关闭响应对照共 **1,406 张真实帧**，原始 wall-clock 时间戳保留，PNG 在实时序列结束后压缩，MP4 按原始帧间隔编码。

- [Normal 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/dynamic-normal/motion.mp4)
- [Trimmed 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/dynamic-trimmed/motion.mp4)
- [Carved 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/dynamic-carved/motion.mp4)
- [关闭响应对照](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-r2/dynamic-control/motion.mp4)
- [恢复曲线及原始数值](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-c3f93b6f-review/dynamics-measurements.json)

三态各 1,109–1,111 个物理步，最大导向点位移约 6.22 cm。撤去风/脉冲后约 0.515–0.521 秒回到本地诊断阈值 0.5 mm 内；剪切刺激后约 0.483–0.501 秒。球缓慢退出时已经恢复，因此“退出后几毫秒”不能解释为毛发独立恢复只需几毫秒。阈值和刺激幅度不是从参考视频测出的常量。

记录中导向点穿入为 0，关闭响应则为 28 mm；实际 `GetRestInfo` 返回的碰撞体 ID 与球一致。有限值、位移包围盒、纯表现阶段 density 不变、剪后 density 保持、根纹理/fin mesh/renderer 身份均通过。实拍发现 Normal 的接触主要在保守毛尖包络附近，视觉压毛偏浅，后续增加深接触工况，不把零穿入当成完整视觉接触通过。

原生剪切 mesh/texture ready 为 34.33–50.24 ms，输入后首个绘制帧 51.93–74.05 ms；不是显示器端到端延迟。Trimmed 动态剪后能看到保留的洞口与毛束切壁。

**物理边界：** 弹簧导向点与真实诊断球查询驱动 shader 变形；没有接入正式 B 的全部工具/ApplyForce，没有逐根微小毛丝和全部插值段的碰撞证明。原生剪刀查询仍是未变形的权威体积，没有 WPO 反向命中。碎块仍 core-only。上述数据是有界响应、查询和真实剪切的证据，不是全部游戏物理验收。

### 受控成本与回归

RTX 5070 Ti Laptop / Forward+ / 1280×800 / MSAA4× + TAA。动态暖机后至少 4 秒 / 240 帧，期间没有截图读回；四头是渲染与响应实例压力。

| 工况 | GPU ms | 帧 p99 ms | 每物理步响应/查询均值 ms |
|---|---:|---:|---:|
| 一头 | 4.826 | 5.926 | 0.560 |
| 四头 | 12.269 | 15.332 | 2.023 |
| 一头关闭响应、保留查询 | 4.871 | 5.782 | 0.425 |

同构建 Build 成功（2 条原有 nullable warnings）；Core 248/248；Hybrid 原生 34+9、R6 原生 32+9、动态按键 8+9+1 通过；正式 B 有渲染 WorldChecks 通过。三组 native 检查各包含真实 108 刀剪空、空头输入、R 重置和再剪，没有旧空网格报错。网络事实未改，没有借用这些检查宣称 WAN/4P 通过。

一次 R6 完整矩阵运行曾在第 15 张后进入原生剪切时发生 CoreCLR `0x80131506` / native `0xc0000005`。保留于 `stage-hybrid-c3f93b6f`，不计入完成结果。同构建独立三刀、完整矩阵重跑及 108 刀回归均没有复现；**没有声称已经定位或修复这次原生崩溃。**

## 试玩与后续

```powershell
dotnet build ProjectHairball.csproj --nologo -v quiet
./scripts/puppet_volume_fur.ps1 -Shells 4 -Hybrid -Dynamics -Interactive
```

F8 风、F9 接触球、F10 开关响应；1/2/6 三态，LMB 实际剪切，R 重置。所有刺激都在实验入口；正式 B 仍为 `./scripts/run_b.ps1`，R6 对照为 `./scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive`。

醒来优先验收小簇是否仍像硬叶/草、剪开内部是否连续而不过疏、实际弯曲与回弹是否自然，以及发尖外扩对瞄准判断的影响。机械检查不替代这些判断。全部拒绝试片、分段成本和下一轮进展继续记入 [ITERATIONS](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/ITERATIONS.md)。
