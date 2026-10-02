# B2 Puppet — Owner rejection 后的 Round 4–5

2026-10-01。历史候选是 `artifacts/b2-puppet/round5-delivery`。这是独立外观实验；正式 B 玩法、网络、评分和默认 Compatibility renderer 均未修改。R5 已被 Owner 再次否决画面质感，不能用以下机械检查替代。后续光照/阴影实验见 [Round 6](B2_PUPPET_ROUND6.md)。

## 先看实机证据

- [13.5 秒真实运行录像](../artifacts/b2-puppet/round5-delivery/puppet_round5_realtime.mp4)：73° FPS、1.7m 眼高，眨眼、烧焦惊慌、队友喷射补救、冻结、回头瞪人。脚本化的外观表演，不是正式 B 的事故玩法或联机录像。无音轨。
- [实际 FPS](../artifacts/b2-puppet/round5-delivery/gameplay_round_5.png)、[400px](../artifacts/b2-puppet/round5-delivery/thumbnail_400_round_5.png)、[Beauty](../artifacts/b2-puppet/round5-delivery/beauty_round_5.png)、[R3/R5 对照](../artifacts/b2-puppet/round5-delivery/before_after.png)。
- [五状态，关闭粒子](../artifacts/b2-puppet/round5-delivery/hair_states_round_5.png)、[灰模无绒毛](../artifacts/b2-puppet/round5-delivery/gray_silhouette_round_5.png)、[光头](../artifacts/b2-puppet/round5-delivery/bald.png)、[四表情](../artifacts/b2-puppet/round5-delivery/expressions.png)、[表演分镜](../artifacts/b2-puppet/round5-delivery/performance_strip.png)、[参考对照](../artifacts/b2-puppet/round5-delivery/reference_comparison.png)。

全部游戏画面来自实际 Godot viewport。拼图只缩放和排版；没有生成图、离线渲染或补绘。录像先保存真实时间戳和 252 个 viewport JPEG，再按这些时间戳封装 MP4，未补帧/加速；原始数据在 `round5-reel-delivery/reel.json` / `reel.ffconcat` / `frames/`。录像采样约 18.7fps，不能当作游戏帧率测试。静态 PNG 保持 1280×800。

FPS 保留 73°、1.7m 眼高，关闭景深；机位 Z 从 R3 的 1.95m 调到 1.70m，顾客整体缩到 0.9。Beauty 单独使用浅景深。R3/R5 因造型、灯光、距离均变化，属于视觉比较，不是同场景性能 A/B。

## 实際调整

Round 4 的三个重点是泡棉/布料结构、皮毛与光照、角色比例。新增脸部轻微不对称、偏斜小牙和鼻子、缝线、领口包边、围布补丁、手指圆头；改用 CC0 短绒/毛圈扫描的高度和法线，保留原创配色。贴图使用模型坐标，避免转头/抬手时纹理在表面滑动。嘴腔、塑料眼睛、长毛、布料和金属工具使用不同反光尺度。

Round 5 的三个重点是发束大形/纤维层次、可读的表演、交互与切换。真实 density 中的七条非对称弯曲发束替代直排刷头；毛流有长度、方向、卷曲和少量较长散丝变化。细毛保持不透明交叉窄带，零透明 shell；只关闭细毛投影，密度 core 仍投影。身体保持短绒。暖主光、冷补光、较暗的蓝绿家具、窗口天色、木纹、工作架形成层次；Forward+、4×MSAA、TAA、SSAO/SSIL、轻 bloom 仅用于 B2。

新增约 13.5 秒循环：自信 → 焦角惊慌 → 队友凑近喷射 → 冻住发呆 → 平顶后转头责怪。五套头发和四种表情缓存，头/头发一起运动，眼神和眨眼跟随表情；喷嘴、泡沫、火苗和烟的实际遮挡经过录像修正。交互启动时预建表演资产。此表演没有接入 Session，不能宣称证明真实多人游戏的节目效果。

## 审图判断与保留差距

已查看最终 FPS、400px、Beauty、五状态、灰模、光头、近景、表情、实际剪切前后和录像各阶段：身体短绒、围布纹理和毛发的区分成立；光头表情仍可读；普通/平顶差异由真实体积改变产生；焦角 25.54%、湿/冻各 48.38%，不依靠粒子遮住材质。

当前最大的五个差距是：①灰模卷束仍偏规整、底缘仍带帽沿感；②近景毛丝的程序规律和细线感尚存，缺少参考里交叠的自然毛簇；③脸型仍偏球形，表情切换是离散嘴型；④房间是简化舞台，窗口和烟火偏风格化；⑤演出是固定短段子，未验证真实玩家行为能产生同样效果。前两项仍是离概念图最远的地方。

下一次人工检视最有价值的三件事：在真实 FPS 下评价材质是否有玩偶质感；看 Space 整段表演是否真的有趣；Tab 后移动、瞄准和剪切，检查毛发边缘、切面和卡顿。不要只看 Beauty。

## 研究与许可

以下只借鉴制作原则，不复制角色或商业模型：

- [Aardman — Behind the Craft](https://www.aardman.com/latest-news/2021/may/behind-the-craft-chocapic-trust/)：实体构造、手工不规则与温暖的布景。
- [Sackboy 的官方介绍](https://blog.playstation.com/2023/04/13/platformer-multiplayer-and-music-fans-why-sackboy-a-big-adventure-is-a-must-play/)：可触摸材料和玩具尺度的细节。
- [Team Asobi / Astro Bot 的官方制作说明](https://blog.playstation.com/2024/09/09/astro-bot-how-team-asobi-created-a-unified-vision-for-fun/)：短动作、反应和可读的喜剧节奏。
- [Godot 4.7 spatial shader](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html)：原创 shader 实现。没有直接复制 Shadertoy 代码。
- [Poly Haven Caban](https://polyhaven.com/a/caban)、[Wool Boucle](https://polyhaven.com/a/wool_boucle)，[CC0](https://polyhaven.com/license)。使用四张未修改的 1K 高度/法线原图；作者、上游 MD5 和本地 SHA256 均保存在 `assets/b2_puppet_textures/`。摄影 colormass，处理 Rico Cilliers。

## 性能与实际检查

Godot 4.7.2 Mono，RTX 5070 Ti Laptop，1280×800，无垂直同步。每机位约 1 秒预热、1.5 秒取样。静态独立实验，非正式 B、4P 游戏、低端设备或长时间耐久测试。

| 场景 | 平均帧 ms | GPU ms | FPS |
|---|---:|---:|---:|
| 同几何基础材质/光照 | 1.171 | 1.098 | 854 |
| 身体微绒与布料 | 1.510 | 1.439 | 662 |
| 细毛，基础光照 | 1.920 | 1.872 | 521 |
| 完整 Beauty | 2.641 | 2.594 | 379 |
| 实际 FPS | 2.567 | 2.460 | 390 |
| 占屏毛发近景 | 2.825 | 2.772 | 354 |
| 四颗真实毛发 | 3.397 | 3.348 | 294 |

正常 core 19,424 三角形，28,004 根纤维 / 784,112 个纤维三角形。FPS 436 draws，P99 3.93ms。长散丝最长约 9.2cm，但主要沿切线卷曲；法向伸出仍受限于约 2.8cm。平顶和局部切面生成毫米级短绒。

原生 LMB 剪切：density mass 470.34406 → 464.65292，1,253 根切面短绒。复用共享 core 顶点后，主线程操作实测 **22.198ms**；细毛在后台 **242.030ms** 完成，期间经过 75 个引擎帧。R3 同步全重建 281.943ms。主线程阻塞明显降低，但不是零卡顿；约 0.24 秒细毛工作期间暂不接收第二次剪切，仍需局部增量重建才能面向连续正式工具操作。

- 最终 `dotnet build` 成功，保留两条既有 nullable 警告；新增 shader/纹理 import 成功，最终全部实际渲染通过。
- 最终 **Core 248/248**，日志 `artifacts/b2-puppet/round5-core-tests.log`。
- 最终 **22 项原生检查**，`round5-native-delivery/engine.log` / `native-check.json`：F4/F5、density/实际顶点一致、表情后恢复、真实 FPS、LMB、后台重建、重建中 R、表演开始/喷射/退出、Beauty 景深和所有房间 shader 关闭。
- 最终 **正式 B rendered WorldChecks** exit0：猫救援、受限拖动/关闭、原生 LMB 拍醒、声音唤醒、三轮照片/历史、双语；Compatibility / protocol25。日志 `round5-b-world.log`，截图在 `round5-b-world/`。本次未重跑联机/WAN/麦克风质量。
- 最终 20 个截图机位、真实时间录像、所有上述运行退出正常；保留完整原始日志。187 个原有源文件/脚本/场景/shader/配置全部 hash 相同，见 `round5-production-invariance.json`。
- 冻结 DLL SHA256：`F778E55A1A8DB5B7FA79FE67A28EF45BFF211B9ED25A07C2EB91CB6857D49969`。完整截图、最终原生检查、录像、正式 B 回归对应同一 DLL；未更换发布 ZIP。

## 保留的失败候选

`round4-a..e-scanned` 保留太光滑/橘皮纹理、规则刷头、眼睛过亮与早期布料采样。反射探针收益不明显且退出产生 7 个 Texture RID 警告，移除后最终运行日志无该问题。`round4-reel-first` 的同步 PNG 保存拖慢录制，未当作正常速度表演交付；后改真实时间驱动和 JPEG 采样。

`round5-a` 的七根竖柱、`round5-b/c` 的遮挡、`round5-reel-e/final` 的均匀泡沫珠流均保留。最终喷射调整了队友站位、表面前方落点和泡沫体积变化。`round5-check-d` 是检查脚本重置后没有重新进入 Tab 模式而失败；修复测试入口后，重建中重置在真实输入路径通过。没有用重跑成功删除失败日志。

## 运行

```powershell
Set-Location 'D:\Project\Game\Godot\project_hairball'
dotnet build ProjectHairball.csproj --nologo
pwsh -NoProfile -File scripts/puppet_lab.ps1 -Round 5 -Interactive
```

Space 播放/停止表演；F1/F2 为 Beauty/FPS；1–5 查看状态；Q 表情；F4 整体效果回退；F5 灰模；F6 光头；F7 粒子；Tab 进入鼠标观察/WASD 移动，LMB 实际剪切，R 复原，Esc 释放鼠标。第一次启动预建缓存需要数秒。正式 B 继续使用 `scripts/run_b.ps1`。

复现证据：`scripts/puppet_lab.ps1 -Round 5` 全截图；加 `-Check` 原生检查；加 `-Reel` 实时录像原始帧与时间戳；加 `-Probe` 五个主要机位。每次默认建立唯一输出目录及源文件/DLL/贴图 manifest。`scripts/compare_puppet.py <输出目录> --round 5` 只生成对照排版。旧轮次请用其保存的 source/DLL，不把当前源码的低效果档位冒充历史候选。

主要新增：`PuppetLabPerformance.cs`，共享 hair shader include、独立 fiber / set / window / flame / smoke shader，扫描纹理及下载校验脚本。修改限于已有 PuppetLab 模块和启动/排版脚本。Round 4 前源码备份为 `artifacts/b2-puppet/before-round4-source.zip`；之前 R1–3 与 Snow 路线证据均保留。
