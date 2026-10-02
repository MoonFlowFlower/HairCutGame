# 固定体积毛束：剪切、接触与覆盖复核

2026-10-02，当前仍是实验候选。正式 B / R6 默认未替换；未宣称达到视频00:44，未进入推剪条纹或后续材质状态。参考视频完整公式未公开；四层Shell加固定体积曲线fins是独立借鉴，不能称为四层纯Shell复刻。

## 12:05 UTC 接触修正与生命周期回归

新增实际 post-WPO 像素诊断证实E243的“导向点零穿入”不能放行渲染接触。原导向响应仍有10,636个毛发像素落在真实诊断球内部超过0.5mm，关闭响应对照9,864。球只在诊断mask中隐藏，beauty没有删掉穿入毛发；其余场景的深度遮挡保留，所以这项计数也不是整个网格的穿入距离。

逐顶点球面推出仍留下391像素：两端在球外的片面可形成穿球的弦。每条持久曲线与每个Shell三角形改用一致的接触平面后，剩11像素；独立mask确认全部因6cm位移上限，没有固定根淡入残留。试验上限7cm，Normal/Trimmed/Carved固定接触mask均0，关闭响应9864。原体积裁剪、曲线/毛根身份保持，未在切面种新毛。

接触候选DLL `BFD7BC0C0B508003EBB056ADFC55DB47A9C27F3B1ABCF3D90514C68BB4DBAA17`，完整清单见 [Normal诊断](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/hybrid-bounded70-normal/candidate.json)；顺序复测在 [contact阶段](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c)。额外GPU修正有单独上限，**导向位移统计不包含全部GPU修正，不能混报**。缓存不变原始包络，并复用上传Image，减少每刀重复扫描/逐帧对象创建。

四条固定FPS实际录像共1401帧：[Normal](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c/dynamic-normal/motion.mp4)、[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c/dynamic-trimmed/motion.mp4)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c/dynamic-carved/motion.mp4)、[关闭响应](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c/dynamic-control/motion.mp4)。三态导向峰值约6.22cm、采样穿入0，对照39.613mm；风/反向脉冲停止后约0.505–0.543s恢复到0.5mm内。球逐渐退出期间已经恢复，不能把最终几个ms当独立回弹时间。剪切ready31.571–49.434ms，首个绘制帧55.176–75.952ms；每刀导向包络刷新约15.1ms，仍有停顿。原根场/fin mesh与剪后密度保持，均无NaN或包围盒裁切。

**本批整体回归失败**：新增接触包围盒代码在108刀剪空后仍通过 `Hair.VolumeFur` 访问已释放渲染器，Step与Dispose发生NullReferenceException，重置失败；原失败日志保留，末次四头复测未执行。后续DLL `3233C7D27288CA7CC9D255ED3C7054F0EE892C7B8EB9449933E3BA327B3AF694` 持有自身渲染器引用、缓存数值包围盒，并在零活动导向时关闭接触，已重新通过全部动态按键8、空头9及施力中剪空1项：[独立修复验证](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/hybrid-contact-lifecycle-fix)。不能将两构建拼成一套全通过。

本批 [候选审计](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-contact-bfd7bc0c-review/candidate-audit.json) 确認保存清单/源码一致；三张静态近景与E243的camera/density/Core及tip mask逐像素相同，所以接触修正没有改善静态叶片感。详细动态/轮廓见同目录 `dynamics-measurements.json`。

首测四头GPU13.142ms、帧p9918.759ms，性能余量未通过；Normal固定镜头GPU5.670ms/p997.646ms。零穿入像素不抵消性能和外观问题。当前仍有叶片感与短切孔隙，下一步在完成这批动态复测后检验更分散的原始毛束朝向。

## E2437D4F 冻结阶段

DLL `E2437D4FA043FDF9E2ECBE8FD9FDED0A18035F6E81E40885B10E47AC4E382634`。21次运行的59个候选文件与保存源码全部一致，26对camera/density/Core mask原像素一致，日志无引擎ERROR。完整证据在 [冻结目录](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f)，[审计与测量](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f-review/candidate-audit.json)。

原始体积的组根改为周期最远候选分布，减轻随机大空洞。卷曲幅度9→6mm，组数1024→1280；522,030条固定曲线，1,883,842个fin三角形，另有36,864个四层Shell三角形。每曲线1–4段、平均1.804段，同参数中心线简化界限3.5mm local。保留原有曲线根/尖、当前占据裁剪；不向切面重新种毛，也没有opaque beauty Core。几何量低于C3的1,915,130个fin三角形，但覆盖增加会提高像素开销。

![Normal 第一人称](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/visual-4/r7-normal-front.png)

![Trimmed 内部诊断，非FPS验收镜头](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/visual-4/r7-trimmed-top.png)

![Carved 内部诊断，非FPS验收镜头](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/visual-4/r7-carved-top.png)

小簇与散尖比早期密齐梳纹更可读；实拍仍有细碎叶状感，Trimmed中央仍透空。**这些是未通过项，不能用性能或实现检查抵消。**

## 双向轮廓与GPU

11个共同未裁切FPS镜头，各列独立取最大。cm由最近Core深度换算为局部视平面估计，非三维最近距离；向内读数包含孔隙。

|模式|向外px/cm|向内px/cm|缺失Core最大比例|
|---|---:|---:|---:|
|R6|23.77 / 2.885|0 / 0|0%|
|Hybrid4|36.40 / 3.790|10.44 / 1.983|10.223%|

缺失最大的共同FPS视图是Trimmed正面。低角度的裁切视图仍保留在原始测量中，未以排除汇总为由宣称解决。

|模式|Normal GPU ms / 缺失%|Trimmed GPU ms / 缺失%|Carved GPU ms / 缺失%|
|---|---:|---:|---:|
|R6|5.981 / 0|5.836 / 0|6.101 / 0|
|Hybrid4|5.510 / 2.362|5.511 / 4.689|5.548 / 3.277|
|Hybrid8|5.652 / 2.337|5.573 / 4.570|5.709 / 3.214|
|Hybrid12|5.709 / 2.293|5.421 / 4.478|5.688 / 3.135|
|Hybrid16|5.826 / 2.269|5.474 / 4.389|5.739 / 3.083|

均为相同近景。R6首尾Normal近景5.981/6.021ms。4→16层仅小幅改善孔隙，继续加层没有充分依据。R6与Hybrid4各17镜头，8/12/16各三态近景；这不是每一层数都完成17镜头的记录。

RTX5070Ti Laptop / Godot4.7.2 / Forward+ /1280×800 /MSAA4×+TAA，顺序运行，GPU采样不含截图读回。保存温度/时钟/功耗及进程快照。

|工况|GPU ms|帧p99 ms|响应/查询均值ms|
|---|---:|---:|---:|
|一头，普通接触|4.872|6.333|0.641|
|一头关闭响应，保留查询|4.893|6.054|0.348|
|四头，深接触，首测|12.891|16.693|2.216|
|四头，深接触，尾测|12.818|16.147|1.922|

四头是渲染与导向响应实例压力，仅一个诊断球，不是真实4P。首测p99跨过16.67ms，**不能声称稳定60fps或有充足余量**。R6四头静态GPU11.184ms，未运行同种毛发响应，不能据此比较物理性能收益。

## 真实动态与采样录像

固定FPS镜头/头部/灯光，风、撤力、球进入/保持/退出、八次反向刺激、受力中原生LMB剪切、最后恢复。四条共1406张实际帧，18.5秒/条，三态各1111物理步。原始时间戳和无重采样ffconcat保留；PNG在实时序列后压缩，MP4已按原始帧间隔编码。

- [Normal动态](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/dynamic-normal/motion.mp4)
- [Trimmed动态](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/dynamic-trimmed/motion.mp4)
- [Carved动态](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/dynamic-carved/motion.mp4)
- [关闭响应对照](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/dynamic-control/motion.mp4)

48×24导向网格，对当前占据的半段/四分之三段及毛尖做真实Godot查询。三态采样点穿入0，关闭响应39.613mm；实际碰撞体ID吻合。纯表现阶段density不变；剪后density保持、根场/fin mesh/renderer未重建。风和反向刺激后约0.510–0.536秒进入诊断阈值0.5mm，最终残差接近数值零；球退出期间已在恢复，不能把退出后几毫秒解释为独立回弹时间。

原生剪切ready25.044–49.930ms，输入后首个绘制帧51.569–89.093ms，不是显示器端到端延迟。

**复核发现E243的接触限位分支存在浮点比较缺陷：未实际超限的候选也可能被投到最大位移球面。因此7.2cm峰值不能全解释为必要的物理位移，响应自然度不放行。** 后续已加入实际长度超限判断，正在单独重测；失败阶段及全部原始录像保留。

另有 [Normal镜头移动](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/sampling-normal/motion.mp4)、[Trimmed镜头移动](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/sampling-trimmed/motion.mp4)、[Carved镜头移动](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-hybrid-e2437d4f/sampling-carved/motion.mp4)，共482张真实帧。固定开头0.2–0.75秒内部毛发像素平均帧差约0.58–0.65个8bit级别；这是静止噪声统计，不是运动补偿后的摩尔纹分数，不是物理证据，也不构成“无闪烁”结论。

## 回归与剩余边界

同构建Build成功（两条既有nullable warnings）；Core248/248、Hybrid原生34+9、R6原生32+9、动态按键8+9+1、正式B有渲染WorldChecks通过。三组原生检查均包含108刀剪空、空头操作、重置和再剪，没有旧空网格错误。本阶段未复现此前C3首次尝试的CLR崩溃，未宣称定位修复。

导向点为零穿入不能推出全部插值毛束为零穿入，正在补引擎内像素诊断。诊断球/风尚未接入全部正式工具；原生剪刀仍查询权威rest-volume；碎块仍core-only；完整渲染物理、最终松软程度和瞄准判断仍待验证。

实验启动：`./scripts/puppet_volume_fur.ps1 -Shells 4 -Hybrid -Dynamics -EdgeContact -Interactive`。F8风、F9球、F10响应，1/2/6三态、LMB剪切、R重置。正式B：`./scripts/run_b.ps1`；R6对照：`./scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive`。当前源码会继续迭代，复现本页需使用冻结目录的源码/DLL，不要把新构建冒作E243。

2026-10-02后续录像审计：上述旧`motion.mp4`采用image2默认25fps时间基，帧时间会被量化到40ms，不能作为毫秒级回弹依据。PNG/原始捕获JSON不受影响，恢复数据来自原始时序。D963阶段改用逐帧核对的`motion-verified.mp4`，误差≤0.5ms；详见`PUPPET_FUR_SURFACE_REVIEW.md`，没有覆盖旧失败证据。
