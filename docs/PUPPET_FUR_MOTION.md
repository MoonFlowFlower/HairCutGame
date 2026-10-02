# 持久毛束与动态实验阶段记录

本页是 `9F538890...` 的历史冻结记录。后续固定短曲线体积、低层 Shell + fins 对照、自适应分段、真实动态和未通过项见 [PUPPET_TUFT_VOLUME.md](D:/Project/Game/Godot/project_hairball/docs/PUPPET_TUFT_VOLUME.md)。各阶段数据不互相冒充同一构建。

2026-10-02，冻结 DLL `9F53889018DD6E5B0DD987C8B80FE95DB43E9839090982B4F4EDF6B5E61412CF`。这是可回退的 PuppetLab 候选，**外观仍未达到视频 00:44，不替换正式 B / R6，不宣称整体物理验收通过**。Owner 睡前授权包含物理表现，因此继续实施有界响应；后续外观试片另存，不覆盖本阶段。

毛束场在原始发量中固定，当前 occupancy 裁掉原有毛束。七根曲线纤维组成小束，原始外侧毛尖长短错开，局部上限 4 cm；新切面不生成毛根或新外衣。主头发 beauty 没有 opaque Core。HairVolume 仍是 signed scalar，以现有六四面体插值做占据裁剪，没有当作真实 SDF。

17 次运行的 58 个候选文件及各自源码快照逐项一致。R6 与 128 层共 17 对镜头，相机位置/朝向/FOV、density hash、Core mask 像素完全相同。R6 / 128 各 17 镜头，4/8/12/16/64 各三态近景，共 49 组静态实拍。完整数据见 [stage-motion-review](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion-review/candidate-audit.json)。

![Normal 真实第一人称](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/visual-128/r7-normal-front.png)

![Carved 同镜头](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/visual-128/r7-carved-front.png)

## 静态判定

整齐梳纹有所减轻，内部保持同一毛束场；但仍偏密绒，散尖与小簇的松软程度不足，掠视仍可见层纹。粗束、低卷曲次数的试验分别增加漏采样或绳纹，已撤回，原图和源码留在 `bundle-coarse` / `bundle-continuous`。

共同 11 个未裁切 FPS 视角的双向轮廓，每列独立取最大。cm 为最近完整 Core 像素深度换算的视平面估计，非 3D 距离；向内包含毛孔和漏采样。

| 模式 | 向外 px / cm | 向内 px / cm | 最大缺失 Core 像素比例 |
|---|---:|---:|---:|
| R6 | 23.77 / 2.885 | 0 / 0 | 0% |
| 小束 128 层 | 26.83 / 3.159 | 9.43 / 1.408 | 4.52% |

Normal / Trimmed / Carved 近景向内均为 9.43 px，缺失 Core 比例 2.60 / 3.85 / 3.00%。4/8/12/16 仍明显透空；128 层只是保留的诊断档，不是默认推荐。轮廓变化也意味着外观与原有瞄准体积的距离仍需真人判断。

## 动态证据与边界

固定 1.7 m 眼高、73° FOV，相机、头部、灯光不移动。每条约 18.5 秒：静止 → 风 → 撤力 → 球进入/保持/退出 → 恢复 → 八次反向脉冲 → 恢复 → 施力中实际 LMB 剪切 → 静止恢复。正常三态及无响应对照共 1405 张真实帧，原始时间戳保留，PNG 在模拟结束后压缩，MP4 使用原时间间隔编码。

- [Normal 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/dynamic-normal/motion.mp4)
- [Trimmed 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/dynamic-trimmed/motion.mp4)
- [Carved 实录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/dynamic-carved/motion.mp4)
- [无响应对照](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion/dynamic-control/motion.mp4)
- [原始测量与恢复曲线](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-motion-review/dynamics-measurements.json)

各态记录 1110–1111 个物理步，模拟时间 18.50–18.52 秒。毛束导向点最大位移约 6.22 cm；撤去风、脉冲、剪切时刺激后，约 0.49–0.54 秒回到本地诊断阈值 0.5 mm 内。该阈值不是从参考视频测得。三态导向点穿入为 0；关闭响应后位移为 0，球穿入约 28 mm。返回的碰撞体 ID 与实际诊断球相符。

没有 NaN，完整位移范围在渲染 AABB 内；纯表现阶段 density 不变，真实剪切后 density 改变并保持，毛根场资源和 Renderer 实例没有重建。剪切 mesh/texture 提交耗时 54.6–73.1 ms；输入后首个绘制帧 77.6–95.1 ms，这不是显示器端到端延迟。

真实查询为 Godot `GetRestInfo`，一个 StaticBody3D 球驱动固定根的弹簧导向点，再由 shader 弯曲原有 Shell。每束仍按剩余长度保留根部不动，剪短后使用新的端点归一化。旧版本把短毛的接触纠正压小，Trimmed 穿入 15.84 mm，失败数据保留于 `checkpoint`；同步 PNG 压缩、碰撞查询滞后一帧的早期失败也全部保留。

**这些是导向点与诊断接触球的证据，不是每根微小毛丝、所有插值段和所有真实工具的碰撞证明。** 风是诊断驱动，没有接入正式 B 的 ApplyForce / 吹风工具。剪刀仍查询未变形的权威体积，没有做 WPO 反向映射。头发碎块仍为原有 core-only 表现。观察到的毛发弯曲不能直接外推为完整游戏物理达标。

## 性能与回归

RTX5070Ti Laptop / Forward+ / 1280×800 / MSAA4× + TAA。动态性能暖机 4 秒，再至少 4 秒且 240 帧，期间无截图读回；附加头仅为渲染和响应实例压力。

| 动态压力 | GPU ms | 帧 p99 ms | 每物理步响应/查询平均 ms |
|---|---:|---:|---:|
| 一头 | 6.500 | 7.997 | 0.703 |
| 四头 | 12.591 | 16.133 | 2.176 |
| 一头关闭响应，保留查询 | 6.216 | 7.356 | 0.519 |

以上单/四头样本完成于 01:33–01:34。另一个后台 GPU 任务于 01:37:49 启动，随后静态矩阵出现约 40% 的明确 GPU 争用。**静态矩阵 GPU 不用于性能放行或收益比较**；需在独占窗口交错复测 R6。动态样本早于该进程创建，未被这个进程影响，但短样本不能替代持续运行/真实 4P。详见 [GPU_CONTENTION.md](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/GPU_CONTENTION.md)。

补测：07:08–07:10 UTC，后台 GPU 任务已结束，恢复本节精确 9F538890 构建后交错测三态近景。R6 GPU 为 5.880 / 5.784 / 6.018 ms，小束 128 为 5.470 / 5.370 / 5.535 ms；三对相机、density、Core mask 像素及候选清单一致。仅作为本机短样本，视觉否决不变。详见 `controlled-static/paired-audit.json` 和 `controlled-static-review/measurements.json`。

本冻结构建：Build 0 error / 2 条既有 nullable warnings；Core 248/248；Volume 原生 46+9、R6 原生 32+9、动态实际按键 7 项、正式 B 有渲染 WorldChecks 通过。两种毛发模式均完成真实 108 刀剪空、空头操作、重置再剪，没有旧空网格报错。未改网络事实，因此没有重跑 WAN 测试。

试玩实验响应：

```powershell
./scripts/puppet_volume_fur.ps1 -Shells 128 -Bundles -Dynamics -Interactive
```

F8 风开关，F9 接触球，F10 响应开关；1/2/6 切换三态，LMB 实际剪切，R 重置，Tab 捕获鼠标，Esc 释放。界面明确标为诊断实验。R6 对照仍使用 `./scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive`，正式游戏入口 `./scripts/run_b.ps1` 保留。

下一步继续改善规则层纹与真正可读的小簇散尖，并补独占性能复测。最终审美与实际接触手感留给 Owner 醒来验收。[独立借鉴来源](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/SOURCES.md)。
