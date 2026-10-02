# 原始短毛体积的边界采样与碎块连续性（阶段记录，2026-10-02）

**视觉仍未通过。** 当前近景有小簇与不齐毛尖，内部不再依赖不透明紫色Core填充；但细绒、叶片感和局部孔隙仍与视频00:44不同。独立同类实现：原作者完整私有公式未获得。没有进入额外玩法或后续材质状态，正式B/R6保留默认。

## 原理与变化边界

`-SurfaceSamples` 仍使用原始128³有限曲线毛束场和固定根坐标。它将4/8/12/16层代理放在当前提取边界内30mm，采样原来就存在的体积；代理在剪切后更新，原根场和显式曲线不重建。HairVolume只提供六四面体占据分类，不当作SDF。显式几何保留原边界跨出采样的曲线及内部1/4组，共170060条，881852三角形。中心线相对原曲线的简化上界2mm local（0.9缩放后1.8mm）。这是Shell加曲线薄片，不冒称纯四层Shell。

宽角度试片`.72..1→.25..1`增加透空，已拒绝并恢复；不能从“更随机”推断更蓬松。

## 已完成的局部证据

|候选/目录|证据|边界|
|---|---|---|
|B611D897… / `surface-smooth-curves-8`|真实近景N/T/C，缺失Core2.011/3.480/2.504%；GPU5.436/5.029/5.294ms|仅三近景，不是完整矩阵；实际画面已查看，视觉未通过|
|同构建 / `surface-smooth-native-check`、`surface-smooth-dynamic-check`|静态34+9，动态8+9+1；108次实际LMB剪空、重置、再剪|机械回归，不替代美术/性能验收|
|E21D0994… / `surface-cut-wall-contact`、`surface-cut-wall-control`|真正Carved内壁，原体积径向暴露21.74cm；接触查询1443命中；开启响应0穿入像素，关闭4756|径向暴露不是SDF距离；像素诊断只涵盖可见栅格，不是全网格碰撞证明|
|DC506B5E… / `surface-contact-calibrated`|最终post-WPO原毛尖(t≥.85)位移p99 4.747cm、max6.389cm；5cm已知值编码p99误差0.390mm、max0.659mm|按可见像素统计，不等于所有顶点最大位移；导向位移另记|
|同构建 / `surface-smooth-perf4`|四头动态GPU11.268ms，帧p9915.993ms|有限短样本，60fps余量小，不是实际4P|

每个运行目录在 `artifacts/puppet-volume-motion/` 下，含源码/DLL/Shader/纹理哈希、原始JSON及实拍。完整长哈希见各`candidate.json`，不把不同构建合并为一次通过。

## 全矩阵中断

`stage-surface-dc506b5e` 在2026-10-02 12:57UTC中断。无关Python进程PID43888于12:54:46UTC启动，GPU Engine计数器观察到93%。首轮四头和R6与其重叠，受控性能无效；停止的只有自己持有的runner、Godot、GPU监视进程。详见该目录`INTERRUPTED_GPU_CONTENTION.txt`、`contention-processes.json`和逐秒GPU记录。待空闲后使用新目录重测，不覆盖失败记录。

## D963377B完整静态比较（13:33 UTC）

五档各17张，共85张实际viewport截图；5×61候选文件/保存副本哈希一致，68对相机、密度与Core mask逐像素完全一致。每档14个FPS镜头（其中低角度有裁边）与3个俯视诊断；共同11个未裁切FPS用于最大轮廓比较。实际原图、无缩放双图、双向测量在 `artifacts/puppet-volume-motion/stage-surface-d963377b-review/`；原始数据 `static-audit.json` / `measurements.json`。

|档位|Normal近景缺失Core|Trimmed近景|Carved近景|共同FPS最坏缺失Core|向内最大px / 约cm|
|---|---:|---:|---:|---:|---:|
|R6|0%|0%|0%|0%|0 / 0|
|4|2.907%|5.908%|3.884%|12.105%|10.20 / 1.765|
|8|2.011%|3.480%|2.504%|7.681%|10.20 / 1.765|
|12|1.569%|2.672%|1.964%|5.842%|8.06 / 1.396|
|16|1.317%|2.294%|1.670%|4.852%|7.81 / 1.352|

共同FPS向外最大：R6 23.77px /约2.885cm；Surface各档36.40px /约3.790cm。px与cm分别取最大，cm为最近Core深度换算的视平面估计，不是3D距离。R6有opaqueCore；新候选的向内数据同时揭示了真实孔隙/漏采样，不能省略这一半来美化结果。

实际看图：四层短切面透空，八层有改善，十二/十六继续降低缺像素但仍偏硬尖/叶片，不能仅凭遮盖率提高放行。更大的外轮廓也增加了相对原Core瞄准边界的差异，未声称原生切削手感已等价。85张中的GPU数字因外部工作**全部排除受控比较**；报告JSON将它们标成`Uncontrolled`，等待单独真实性能窗口。

## 碎块缺口与下一次冻结

脱落块原来仅显示opaqueCore。E086F6F1…候选让Surface模式下的碎块以最多8层采样同一个原始毛束/根纹理；保留原head-local坐标，运动只改变Root变换。当前块的占据裁剪原毛体积，不在断面长毛；不复制整头fin。原12块上限和原有prefall/下落行为保留，后者是既有装饰运动，不是新增刚体仿真。

普通4刀产生的小块仅72个Core三角形，且被主体遮挡；这组材质/字段检查通过，**不足以验证大断面外观**。尝试32刀仍无足够大碎块的失败保留在`fragment-large-pilot`。

后续明确诊断fixture只从原发量减去水平窄带、保留一条真实支撑桥；先验证连接，再由4次原生LMB断桥。它不是普通完整发型剪切产生大块的证明。断下来的原发量约1.10×0.46×0.80m local，Core9752三角形，实际FPS下录16帧。仅8层采样显得稀薄，仍未视觉放行；首次记录器在释放Mesh后读尺寸异常，已改为释放前复制数值并重跑，失败保留。

`fragment-retained-curves` / 完整DLL `D963377B98FC0D86A3069B7E5B347B5058ABA77F07A262D286FB86F0253C0749`：碎块从原始曲线缓存按空间格筛选，稳定散列选取，每块fin≤60000三角形；局部位置、法线、宽度、原根颜色、UV直接复制原曲线，当前占据继续裁剪。原根/毛束纹理与头部同一资源。真实断桥、12块上限、清理、运动不改头部/碎块密度通过；截图已查看，加入细尖有改善，稀薄区域仍待修。切割ready130.5ms是受干扰试片值，不能当受控延迟。最大压力为12份真实“大冠块”，额外几何1656192三角形，远重于12份小屑。

`stage-surface-d963377b` 视觉/功能阶段已完成。24个运行各保存61文件，清单与副本哈希一致；85张静态、68对同视角逐像素Core对照、1311张施力动态帧、902张纯相机帧、16张脱落帧均已审计。引擎日志未出现ERROR。候选哈希仍为上面的D963377B完整值。

**本阶段GPU、帧耗时及剪切延迟全部不是受控性能证据。** PID43888的无关GPU工作从12:54:46UTC持续到本次14:06UTC复查；复查GPU99%、87°C。`candidate-audit.json`明确`controlledPerformance:false`。原始诊断字段`afterControlledPerf`仅表示在fixture的计时窗口之后拍摄，不代表GPU独占。准备好的`run_surface_controlled.ps1`尚未执行：将同构建交错R6/4/8/12/16、三态近景、静态1/2/4头、动态一/四头和12块压力另存，不复用这批受干扰数字。

## 固定镜头下的动态与内部接触

实际FPS相机固定，执行风、撤风、接触球进入/离开、反向和重复脉冲、施力中真实LMB剪切、恢复。三态各约18.5秒，1110–1111个物理步；另有关闭响应的Carved对照。原根场、原fin mesh/renderer保持，剪后占据散列稳定，洞口未在恢复时重生。三态最大导向位移6.22/6.22/6.73cm；撤风/重复脉冲后0.511–0.534秒进入0.5mm阈值，最终静止残差低于阈值。接触球退出期间已在恢复，退出后的几毫秒不能单列为独立回弹时间。

渲染后的像素诊断另测真正post-WPO表面：三态在球内超过0.5mm的可见毛发像素均为0；关闭响应Normal为10035、Carved内壁为4756。Carved目标处原体积径向暴露21.74cm，是真正剪出的内部，不是HairVolume的SDF距离。可见原毛尖（原曲线t≥.85）位移p99为4.87/4.50/4.75cm；5cm编码校准p99误差0.390mm、最大0.659mm。这是可见像素与显式球的诊断，不是整个网格或逐根毛发碰撞证明。

剪切ready为68.7/55.1/103.0ms，首个绘制帧114.2/83.4/132.5ms；接触命令延迟p95约33.5–40.0ms。它们保留为受GPU竞争影响的原始观察，不是受控延迟，也不是显示器端到端延迟。

实录：[Normal](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b/dynamic-normal/motion-verified.mp4)、[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b/dynamic-trimmed/motion-verified.mp4)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b/dynamic-carved/motion-verified.mp4)、[关闭响应](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b/dynamic-control-carved/motion-verified.mp4)、[真实断桥脱落](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b/qa-fragments/motion-verified.mp4)。

11条`motion-verified.mp4`已逐帧ffprobe核对：输入PNG不插值，最后一帧重复一次保留时长，PTS单调，与原始捕获时间的误差不超过0.500ms。旧`motion.mp4`使用image2默认25fps时间基，时间被量化到40ms；旧预览不再用来精确判断恢复时间。PNG、原始时间JSON和失败版本保留。

## 仍然未通过：静止闪烁、毛尖与覆盖

重新取R6/Surface共有的侵蚀后毛发ROI；三态camera/density/Core相同，均为0.2–0.75秒固定相机。R6平均相邻帧8bit差0.493–0.517，新候选0.791–0.875；超过3级差的比例分别1.065–1.159%和2.207–2.908%。共同ROI排除了之前mask不同的偏差，仍有短样本、不同捕获间隔/亮度和GPU竞争的限制。结果见`sampling-common/sampling-common.json`，**未证明摩尔纹改善**。相机录像与受力物理分开。

全尺寸同视角原像素：[Normal](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b-review/normal-same-view.png)、[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b-review/trimmed-same-view.png)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b-review/carved-same-view.png)、[原生深切](D:/Project/Game/Godot/project_hairball/artifacts/puppet-volume-motion/stage-surface-d963377b-review/native-deep-same-view.png)。硬尖/叶片感、短切透空和大碎块稀薄仍保留为失败项。

下一步先完成独占GPU复测，再以同层数、同根场的小步检查alpha边缘采样与毛尖软化。Godot4.7官方文档确认MSAA的alpha-to-coverage接口，但能否改善本例尚未试验，不预先改判。另一待测问题是碎块原曲线筛选的CPU耗时。正式B/R6继续默认，实验不直接合入。

## 实际回归与运行

最终增量Build成功且DLL不变；Core248/248，Surface34+空头9，R632+空头9，动态按键8+空头9+施力剪空1，碎块原字段/12块上限/清理，正式B WorldChecks全部完成。三组空头检查均含108刀真实LMB剪空、重置、再剪。没有复现旧空网格错误；此前独立CLR崩溃根因仍未宣称解决。完整初次编译的两条既有nullable warnings仍记录在案，增量0warnings不代表已修复它们。

试玩实验：`./scripts/puppet_volume_fur.ps1 -Shells 8 -SurfaceSamples -Dynamics -Interactive`。F8风、F9诊断接触球、F10响应开关、1/2/6状态、R重置、LMB原生剪切。它们是局部诊断刺激，正式工具接入尚未宣称完成。

构建：`dotnet build`。正式B：`./scripts/run_b.ps1`；R6：`./scripts/puppet_shell_r7.ps1 -Mode R6_FIBERS -Interactive`。独占矩阵：`./artifacts/puppet-volume-motion/run_surface_controlled.ps1`（须先核对冻结清单/无并发Godot或GPU任务）。

14:12UTC持续跟进：当前线程已启用15分钟heartbeat（`automation-4`），先等待外部GPU工作结束，再执行冻结复测和下一轮；不是阶段验收结束。runner保留每秒进程记录，中途出现竞争会将整批性能标无效，后续用新的`-Stage`保留重跑。新guard仅完成PowerShell语法检查，未冒称已跑完受控矩阵。
