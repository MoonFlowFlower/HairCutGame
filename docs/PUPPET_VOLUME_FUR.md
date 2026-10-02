# R7.1 原理重做：持久毛束体积

本页保留此前 `91145E...` 静态冻结记录。Owner 随后的睡前授权包含动态表现；新阶段的真实三态录像、接触修复、剩余限制与 GPU 争用说明见 [PUPPET_FUR_MOTION.md](D:/Project/Game/Godot/project_hairball/docs/PUPPET_FUR_MOTION.md)。外观仍未通过，没有替换正式 B / R6。

2026-10-02。**Gate 1 未通过；保留 R6 / 正式 B 默认，不进入推剪条纹或后续状态。** 新实现可以在实验场直接剪切，但近景仍有方向性梳纹，松散毛尖和束级蓬松感尚未达到 Owner 指定的视频 00:44。实现完成、轮廓改善和 GPU 余量都不替代视觉验收。

## 来源与可确认的原理

已阅读 Owner 提供的视频、[原页面](https://www.bilibili.com/video/BV1aQY86REPu/)、作者 Vicko图瑞的公开可见主页/动态/评论。00:02 标注 Unlit Shell 毛发（ISM）；简介说明 UE 蓝图/材质由速度求 WPO，六张 RT 的 SDF 用于球体接触/融合。公开可见范围未找到完整材质或 Blueprint 下载，剩余评论受登录限制。视频没有展示理发剖面，不能声称知道作者未公开的内部实现。

完整读取 [Acerola Shell shader](https://github.com/GarrettGunnell/Shell-Texturing/blob/main/Assets/Shell.shader) 和 [Simon Green 的 ShaderToy furball](https://www.shadertoy.com/view/XsfGWN)。前者以几何 Shell 采样共享毛根域，后者沿视线积分连续毛发密度；两者使用与高度相关的长度/弯曲和手动光照。当前保留的是 Godot MultiMesh 实例化 Shell 的独立实现，没有复制作者资产或私有公式。来源审计见 [SOURCES.md](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/reference/SOURCES.md)。

## 内部表达与修正

上一版在新的切面重新生成短毛，主体仍是实体外壳。当前 `R7_VOLUME` 把毛根固定在头皮参数域，壳层穿过原始发量；全部层读取同一个连续毛束场。切割只上传当前密度，裁掉原有毛束的一部分。主头发 beauty 不绘制 opaque Core，也不生成表面 ribbon fuzz。Core 继续服务原有编辑、射线、支撑、灰模和对照。

关系为 `可见毛发 = 持久毛束覆盖(u,v,h) ∩ 当前密度体积(p)`。HairVolume 仍是 35×31×27 的 byte signed scalar，±0.28 截断，**不是真实 SDF**。GPU 使用与现有提取相同的六四面体插值及 127.5 阈值。格点梯度只用于光照；没有用 scalar 数值做距离、AO 或安全步进。密度步长经 /3.5×0.9 后为 3.6 cm。

两项被真实截图抓到的缺陷已经修正：

- 覆盖率：先平均 Gaussian、再裁透明会让远处/掠视的毛根整片消失。改为先生成八档覆盖率、再做 mipmaps，并用稳定空间 alpha hash 采样。相同深切口从 25.08 px 向内缺失降至首版修复的 8.60 px。
- 几何范围：相邻空角度的短半径把 Shell 包络拉进了细发绺内部。现在对原始包络做相邻角度的保守最大值，超出部分仍由真实密度裁除。正面诊断从 15.26 px / 3.06 cm 降至 3.61 px / 约 0.82 cm。毛根和渲染实例在后续剪切中保留。

更长的毛尖、fins、局部/完整包围盒的视线积分对照仍呈现梳齿、硬纹或成本上升，已撤出活动代码；其源码、原图和数据保留。[试片索引](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/ITERATIONS.md)。其中 `R7_INTEGRATED` 的历史 `shells` 字段是射线采样上限，不能算作 Shell 层数。

最后的固定原始长度场对照 `length-pilot` / `length-fine` 露出了更多毛尖，但加重刷毛感和向内缺失。相同实现的 128→512 层采样诊断近景 GPU 从 4.91→9.28 ms；层纹减轻，视觉目标仍失败。512 层只是单镜头诊断，没有三态或多人放行；该分支和长度场均已撤回。完整重编译后 DLL 精确恢复下述哈希。各失败试片的原图、GPU 及双向轮廓集中在 [rejected-review](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/rejected-review/measurements.csv)。

## 当前候选证据

冻结 DLL：`91145E224E7E089A38C5165C30D08BD3535AD76B489DCDFC147C4C7F03497345`。`bounded-final` 共 26 个运行，每次 56 个源/资源/程序集哈希一致；最终分析与当前文件相符。相对上一过滤版本，运行时代码仅变更 `PuppetVolumeFur.cs` 的保守包络和 DLL；清单还包含一个 Python 分析缓存的变化。

R6 和 128 层各 17 个镜头：Normal / Trimmed / Carved 各五角度，以及真实输入一刀/三刀后。每档 14 张固定 1.7 m 眼高、73° FOV 的 FPS 图和 3 张俯视诊断。两档 17 组 Core mask 逐像素相同，camera 与 density hash 相同。4/8/12/16/64 层另各三张真实 FPS 近景。本候选总计 67 组带 GPU 和轮廓数据的捕获。

![真实 FPS 挖洞图](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-128/r7-carved-front.png)

[Normal 近景](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-128/r7-normal-close.png) · [Trimmed 近景](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-128/r7-trimmed-close.png) · [连续三刀后](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-128/r7-native-deep.png) · [R6 同镜头](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-r6/r7-carved-front.png)

## 双向 Silhouette Error

在两档共同的 11 张未裁切 FPS 图上统计。其他物体保留实际深度遮挡；mask 临时关闭 TAA、保留 MSAA，以 188 sRGB 约 50% 线性覆盖为阈值。厘米为最近完整 Core 像素深度换算的视平面估计，非精确 3D 最近距离；向内统计包括毛孔与漏采样。每列独立取最大。

| 模式 | 向外最大 px / cm | 向内最大 px / cm | 最大缺失 Core 比例 |
|---|---|---|---:|
| R6 | 23.77 / 2.88 | 0 / 0 | 0% |
| 128 Shell | 1.00 / 0.18 | 7.07 / 1.63 | 3.73% |

连续三刀镜头向内最大 4.47 px / 约 0.99 cm。R6 保留实体 Core，向内为零是其表示方式带来的结果，不能因此替代内部材质观感比较。不能只用新方案向外误差接近零就判合格。

## GPU

RTX 5070 Ti Laptop / Godot 4.7.2 Mono / Forward+ / 1280×800 / MSAA 4× + TAA / VSync off。每镜头暖机 4 秒，再至少 2 秒且 ≥120 帧。截图读回和视频写盘不进入 GPU 采样。CPU 指标仅为 render + frame setup；显存为 Godot 分配监视值，均在逐镜头表中。

| 模式 | Normal / Trimmed / Carved 近景 GPU ms | 单头压力 GPU ms | 四头压力 GPU ms |
|---|---|---:|---:|
| R6 | 5.68 / 5.88 / 6.12 | 5.95–6.03 | 11.09–11.15 |
| 4 Shell | 3.61 / 3.66 / 3.63 | 2.84 | 4.28 |
| 8 Shell | 3.62 / 3.64 / 3.63 | 2.79 | 4.38 |
| 12 Shell | 3.65 / 3.67 / 3.65 | 2.80 | 4.53 |
| 16 Shell | 3.68 / 3.70 / 3.68 | 2.84 | 4.70 |
| 64 Shell | 4.20 / 4.18 / 4.19 | 3.66 | 6.78 |
| 128 Shell | 4.78 / 4.75 / 4.81 | 4.83 | 9.52 |

单头压力为最近 Core 约 30.5 cm、1.7 m 视高；128 层整头占屏 72.81%。四头为一名完整顾客加额外完整毛发实例，均为所列层数；不是四顾客玩法或实际 4P/WAN 验收。128 层四头 frame p99 为 10.20 ms；两头为 7.18 ms，R6 两头 8.27 ms。R6 首尾锚点显示时钟波动，因此不以跨批次最低数值宣称收益。

4/8/12/16 都明显透空，性能通过不能抵消视觉失败。64/128 是低档失败后的诊断，不是默认推荐；正式默认仍为 R6。

## 剪切、录像与回归

三次相同原生 LMB 切割的 density hash 与 R6 完全一致，渲染实例、毛根场保持不动，密度纹理每刀更新。当前主线程 mesh/texture 提交 24.16–24.98 ms，R6 异步毛丝 ready 467.78–522.33 ms；这不是按下到画面更新的延迟测量。

[当前真实移动录像](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-final/visual-128/motion.mp4)：159 个实际帧、约 5.78 秒，真实时间戳编码，无补帧，末帧只作结束点重复。先恢复 GI/TAA 并暖机 4 秒。静止 ROI 通道标准差均值 0.476（0–255），不能推出移动中无摩尔纹或快转视角稳定。

当前构建 Build 0 error / 2 条原有 nullable warnings；Volume 原生 46+9=55 项通过，包括连续 108 刀剪空、空头切换、R 重置再剪，未见 `array_len == 0`。本轮前一过滤构建的 Core 248/248、R6 原生 32+9=41（同为 108 刀）、正式 B WorldChecks 均通过；之后仅修改新 Volume 模式的包络，此处不把它们伪装成新 DLL 上重跑的结果。

[候选审计](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-review/candidate-audit.json) · [逐镜头 CSV](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-review/measurements.csv) · [完整 JSON](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-review/measurements.json) · [移动审计](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-principle/bounded-review/motion-audit.json)

## 运行与剩余差距

在 `D:\Project\Game\Godot\project_hairball`：

```powershell
dotnet build ProjectHairball.csproj --nologo
./scripts/puppet_volume_fur.ps1 -Shells 128 -Interactive
./scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive
```

Tab 进入 FPS，WASD / 鼠标，LMB 剪切；1/2/6 为三态，F4 原 Core，F5 灰模，R 重置。第一条实验命令显式使用 128 层，只供比较；启动器默认 4 层用于低档诊断。

当前主要差距是近景毛束仍偏成排、松散毛尖不足，尚未复现视频的蓬松观感。脱落碎块仍使用既有 core-only 低 LOD；超出原始包络的新生长、动态 grooming、其他状态未接入。单独增加毛尖长短与层数已试过，不能作为已知有效的后续解法。剩余问题是毛束尺度、方向分布与体积采样的共同表达，必须继续用相同三态镜头验证。真人检查应聚焦连续剪深后是否仍读成毛、近景梳纹/移动爬纹，以及轮廓和实际落刀位置是否一致。**本轮不放行 R7.1。**
