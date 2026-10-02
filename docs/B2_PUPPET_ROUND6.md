# B2 Puppet R6 — 光照、阴影与材质的实机对照

2026-10-01。Owner 再次否决 R5 的画面质感，并提出是否修改引擎或移植 UE 光照。R6 仍是独立 `PuppetLab.tscn` 实验；没有引擎分叉，没有更改正式 B 默认渲染器、玩法或网络。**概念图完成度仍未达到，不能将机械检查通过当作审美通过。**

## 当前判断

现有光照确实需要重做：R5 使用较强的纯色环境光、多盏不投影的补光，细毛 shader 也不接收阴影。这样的组合会冲淡发沿、眼眶、鼻下、布料褶皱的厚度。R6 的真实对照证明，恢复有方向的软光、遮挡和材料反射，已经能改善体积和材料区分；房间 GI 进一步补回暖色反弹光。这还不能证明 Godot 的质量上限，更不能证明换引擎会自动达到参考图。

不建议把整套 UE 光照搬进 Godot。Lumen 包含屏幕追踪、场景表示、Surface Cache 和跨帧更新；Virtual Shadow Maps 还涉及分页与缓存。它们都是渲染器层面的系统。这个判断是基于官方架构资料的工程推断，本轮没有实施或测量移植成本。[Epic Lumen 技术说明](https://dev.epicgames.com/documentation/unreal-engine/lumen-technical-details-in-unreal-engine)、[Virtual Shadow Maps](https://dev.epicgames.com/documentation/en-us/unreal-engine/virtual-shadow-maps-in-unreal-engine)。

更值得借鉴的是材质模型和制作方法。UE 将 Cloth 的绒光、Hair 的散射/高光与 GI 分开处理；Filament 也给布料提供单独的 sheen 模型。R6 采用 Godot 现有材质输出、纹理和阴影接收，**尚未实现 UE Hair、Charlie sheen 或物理毛发多重散射**。这些是下一步可局部替换的候选。[Epic Shading Models](https://dev.epicgames.com/documentation/en-us/unreal-engine/shading-models-in-unreal-engine)、[Filament Cloth](https://google.github.io/filament/main/filament.html)。

## 实际改动和证据

当前选定目录是 `artifacts/b2-puppet/round6-selected`，同一冻结版本的逐层对照为 `round6-review-r5`、`round6-review-direct`、`round6-review-gi`。四套配置使用同一 R6 几何、73° 相机、1.7m 眼高和相同 density。这里的 “R5” 指旧灯光/材质配置；R6 对眼球和参数曲面增加了采样，不能把它当成历史 R5 原始版本的绝对性能复测。真实头发密度、发束大形与相机位置保持 R5 设置。

- [四套灯光/材质对照](../artifacts/b2-puppet/round6-selected/lighting-comparison.png)、[前后 FPS](../artifacts/b2-puppet/round6-selected/before-after.png)、[近景对照](../artifacts/b2-puppet/round6-selected/close-comparison.png)。
- [真实 FPS](../artifacts/b2-puppet/round6-selected/gameplay_round_6.png)、[Beauty](../artifacts/b2-puppet/round6-selected/beauty_round_6.png)、[400px](../artifacts/b2-puppet/round6-selected/thumbnail_400_round_6.png)、[灰模](../artifacts/b2-puppet/round6-selected/gray_silhouette_round_6.png)、[光头](../artifacts/b2-puppet/round6-selected/bald.png)。
- [五状态，无粒子遮挡](../artifacts/b2-puppet/round6-selected/hair_states_round_6.png)、[概念参考对照](../artifacts/b2-puppet/round6-selected/reference_comparison.png)、[实际表演录像](../artifacts/b2-puppet/round6-selected/puppet_round6_realtime.mp4)。

所有游戏图片来自 Godot viewport，拼图只排版和缩放。录像由实际运行帧和真实时间戳封装，没有生成画面、补绘或补帧。仍是独立场景的脚本化短表演，不能称为正式 B 的多人事故效果。

选择的光照使用三盏带光源半径的软阴影聚光灯、两个有阴影的吊灯、低强度环境光、环境反射，以及静态房间 SDFGI。角色、可剪头发、手持物和粒子只接收 GI，不将旧位置写入静态 GI。SDFGI 不提供动态角色的完整间接遮挡或染色，这个限制保留。[Godot SDFGI](https://docs.godotengine.org/en/4.7/tutorials/3d/global_illumination/using_sdfgi.html)。

材质修正包括：细毛接收 core 和环境的阴影；毛发底层增加细尺度毛圈法线/高度变化；布料、泡棉、塑料眼睛和漆木使用不同粗糙度/反光；眼白消除过重的双重暖色；湿发保留变暗和纤维收束，使用自身粗糙度反射。头发密度 core 继续投影，细毛不单独投影。没有透明毛发 shell，也没有把高光画在最终截图上。

## 被截图否决的分支

`round6-study-a-*` 的面积光第一版偏暗；加入的屏幕空间次表面散射把短绒抹得发蜡，已移除。`round6-study-b/c/d-*` 调整了受光和毛毡底层。

灰模揭露了矩形面积灯的接收面条纹。关闭 AO、关闭 GI、增加曲面采样都未消除它；关闭面积灯阴影会消失，关闭 Omni 阴影仍存在。相同场景改用软阴影 Spot 路径后消失。相关实际证据保留在 `round6-shadow-diagnostic-*`、`round6-shadow-disabled-area/omni`、`round6-shadow-bias-e`、`round6-shadow-topology-f` 和 `round6-spot-g`。这只定位到本机此配置的 AreaLight3D 阴影路径，未证明上游引擎源码中的具体缺陷。面积灯分支仍可通过 `-LightRig area` 复现。

第一份 `round6-delivery` 的湿发清漆产生过曝白斑，已被审图否决；`round6-reel-delivery` 同属该旧候选。移除清漆后，`round6-wet-repair` 实拍确认白斑消失，再冻结为当前 `round6-selected`。

`round6-compare-r5/direct` 不用于性能结论：第一次正式 B 检查命令漏了 `--solo`，停在菜单，导致它们与另一引擎进程重叠。已结束这些自建进程，保留错误入口日志 `round6-b-world-wrong-entry.log`；正确入口完成后，单引擎重跑了当前对照。`round6-final-*` 是修正湿发前的上一组数据，也不作为当前冻结候选。

## 仍未达标的五个差距

1. 灰模发束仍规整，底沿仍有帽沿感；灯光无法补出缺失的交叠和造型。
2. 近景能看到稀疏的程序毛丝，主体与绒毛的尺度衔接仍不自然；当前没有纤维自身的投影和多重散射。
3. 脸、眼睑、嘴沿和手的结构仍偏简单，表情采用预制切换；加密曲面仅改善折面。
4. 房间窗口是图案平面，镜面和场景细节简化；当前 SDFGI 只处理静态布景。
5. 演出仍是既有固定短段，尚未证明实际多人操作会产生同等可读的节目效果。

下一次最有价值的人工检查是实际 FPS 下的毛毡感、近距离移动时毛丝与阴影的稳定性、五状态能否不看标签区分。下一阶段资源应优先投入毛束/脸部结构和专门的纤维受光模型。2026-10-01 已完成同资产 UE 小场景对照，Owner 判断差异不足以支持迁移，要求清理 UE 项目并继续 Godot；见 `B2_UE58_ENGINE_SPIKE.md`。本报告不再将 UE 对照列为下一步任务。

## 复现

```powershell
dotnet build ProjectHairball.csproj --nologo
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_lab.ps1 -Round 6 -Interactive
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_lab.ps1 -Round 6 -Lighting r5 -Probe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_lab.ps1 -Round 6 -Lighting direct -Probe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_lab.ps1 -Round 6 -Lighting gi -Probe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/puppet_lab.ps1 -Round 6 -Check
```

启动脚本兼容 Windows 自带 PowerShell 5.1，不需要 `pwsh` 或 `rg`；已有构建可直接运行，无需重复 `dotnet build`。`ExecutionPolicy Bypass` 仅作用于该进程。

Space 演出，Tab/WASD/鼠标/LMB 实际剪切，R 重置，F1/F2 Beauty/FPS，F4 基础材质与灯光，F5 灰模，F6 光头，1–5 材料状态。独立 B2 launcher 默认 R6；正式 B 仍使用 `scripts/run_b.ps1`。

最终性能和验证数值记录在同目录 `lighting-study.json`、`metrics.json`、`run-args.json` 和 `candidate.json`，并在本报告的最终验证段列出。

## 最终验证

Godot 4.7.2 Mono，RTX 5070 Ti Laptop，1280×800，无垂直同步。先丢弃启动 10 帧，再预热 4 秒，随后取至少 1.5 秒且不少于 60 帧。六次最终运行的 35 个受追踪源文件/资源/程序集哈希一致，与当前磁盘一致，见 `round6-frozen-verification.json`。这些是独立静态场景的短样本，不是正式 B、4P、低端设备或耐久测试。

| 同一 R6 几何的 FPS 视角 | 平均帧 ms | GPU ms | FPS |
|---|---:|---:|---:|
| R5 灯光和材质配置 | 3.348 | 3.298 | 298.7 |
| 新软光、阴影、环境反射 | 5.291 | 5.248 | 189.0 |
| 同一灯光 + 静态房间 GI | 6.212 | 6.163 | 161.0 |
| 同一 GI + 最终材质/细毛受阴影 | 6.781 | 6.727 | 147.5 |

GI 增量约 0.92ms；不能把新灯光组的全部代价归因于 GI。最终 Beauty 6.582ms，近景 7.052ms，四份头发 7.667ms。FPS P99 8.225ms。此次对照没有给出视觉质量的自动评分。

实际审阅了最终 FPS、400px、Beauty、灰模、光头、近景、五状态、四表情、剪切截图及录像五个阶段。湿发清漆白斑未再出现，灰模的面积灯条纹在所选 Spot 配置中消失；这仍不等于所有移动角度都通过验收。烧焦覆盖 25.54%，湿/冻各 48.38%。原有 19,424 个 core 三角形、28,004 根纤维/784,112 个纤维三角形保持不变。

- 编译与 import 成功；两条既有 nullable 警告不变。最终六个引擎运行日志无 ERROR/WARNING/泄漏报告。
- Core **248/248**，`round6-core-tests.log`。正式 B Compatibility / protocol25 的 rendered WorldChecks 成功，`round6-b-world.log`。它们使用最终相同 C# DLL；随后仅修复了 B2 湿发 shader，并重新运行全部最终截图和原生检查。没有重复网络/WAN/语音耐久测试。
- 当前 shader 版本的 **25 项原生 B2 检查通过**，`round6-native-selected`。包含 F4 关闭/恢复 GI、可动几何不注入静态 GI、灰模恢复、LMB 实际 density 剪切、后台重建、重建时重置和演出恢复。
- 实际剪切质量 470.34406 → 464.65292，切面短绒 1,253 根；主线程 25.525ms，后台细毛 243.456ms，期间 22 个引擎帧继续。仍存在短暂主线程阻塞及重建期间不接受第二次剪切的限制。
- 三组 5 机位对照 + 最终 20 机位，共 **35 张主截图**，另有 400px 图、排版与切割证据。录像来自 226 个实际 JPEG，首末帧时间跨度 13.441 秒，MP4 容器 13.52 秒，960×600、无音轨。约 16.8fps 的录像采样不能当作游戏帧率。
- 原始受保护路径 **187/187 哈希一致**，见 `round6-production-invariance.json`。正式 B、Core、默认 renderer 和发布 ZIP 均未替换。

最终 DLL SHA256：`C5FA8551E97CC6CD5D54578346784E4643B1D284ACD33AAA7E7773FA88D5EF6B`。状态：机械检查通过，Agent 视觉判断仍低于概念目标，Owner 尚未接受 R6，**不推广到正式 B**。
