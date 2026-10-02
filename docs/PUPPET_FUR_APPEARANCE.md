# 毛发闪光、透空、硬尖与吊灯阴影（2026-10-02）

本轮保留 Owner 已认可的夸张落刀回弹，继续修实验 Surface 外观。当前候选 DLL 为 `1C6892D43D32F30A81CB125A1326214F0E6981C80F16CEC77A3F47D84D87FDF1`；完整候选身份以 65 文件 SHA256 清单为准，因为材质代码不包含在 DLL 内。`PuppetFurMotion.cs` 与已认可的 A38D7902 逐字节一致，SHA256 `B819D52DBB4FD33312C23B0EBEA59164F9D599C280763C1F841CF117D57F3032`。

**这是可试玩的外观改进候选，整体视觉仍未通过。** 低角度缺像素、局部毛片感和截图中孤立长刺的精确复现仍开放；不把机械检查或较少闪烁写成视频参考的蓬松感已经达标。没有扩大玩法，没有启用后续毛发状态，没有替换正式 B/R6。

## 已定位的阴影原因

固定站立第一人称后侧相机，逐项关闭实际场景中的阴影、毛发层、手持物、AO、GI 后取图。隐藏全部头发或关闭手持物阴影时，天花板大圆影仍存在；关闭 Spot 阴影后消失。单独开启后方 Spot 的阴影可复现，关闭吊灯的灯罩/吊杆投影也使其消失。

后方补光原本在 `(1.85,2.85,-1.20)`，低于灯罩，向上把灯罩放大投在天花板。实验补光上移到 Y=3.65，保留真实直接阴影。其余灯、皮肤/手部材质参数不变。诊断图在 `artifacts/puppet-fur-appearance/soft-a-isolation`，修正后同视角在 `soft-b-isolation`；这些阶段分别保留自己的源码快照，不混称为最终候选。

这是已复现的大圆影修正。当前 unlit 毛发本身仍关闭实际场景投影，内部明暗是材质近似；没有将本轮写成毛发自阴影、与身体的阴影联系已全部完成。

## 外观改动与边界

- Surface 仍使用八层向内的原始毛束体积采样和有限原曲线 fins。没有加层，没有不透明紫色底壳，没有在新切面重新种毛。当前占据只裁掉原有材料；根与 fin mesh 在原生剪切中保留。
- 软边路径使用 MSAA alpha-to-coverage，替换原来的空间 alpha hash；原有微细纹的覆盖随实际采样间距积分。经纬接缝从未变形的片元坐标重新求取，并修正经度导数的周期跳变，避免跨接缝错误选取纹理层级。
- 原有限曲线变短、朝向更分散；曲线数量来源和种子不变，因筛选后的有效曲线不同，fin 三角形从 881,852 降为 828,520。末端与侧缘渐淡只作用于原来的曲线端点，不作用于新切面。
- 毛片漫反射由毛束方向和头发形体决定，减少随相机转动的明暗变化。shader 增加零向量、颜色和透明度有限值保护；有符号点积的平方改用乘法，避免 GLSL `pow(x,2.0)` 在负数底数时的未定义行为。

A 试片虽然较稳，但过平、发白，因此未保留为活动方案。B 恢复部分明暗并缩短毛尖，但实际连剪仍出现白色爆亮，亦未作为最终方案。C 在 B 上修正运算并补有限值保护，略补原材料的覆盖。`soft-b-nan-diagnostic` 没有捕获红色无效值诊断像素，因此不能将白点的全部来源断言为已证明的 NaN；目前是修复明确数学风险之后的有限不再复现证据。

## 已完成的视觉与机械证据

`final-c/visual` 完成 17 个 Normal/Trimmed/Carved/原生切割镜头，其中 14 个为真实站立第一人称、3 个俯视仅作诊断。Core/毛尖 mask 和深度取自同一实际 viewport。新拍三态 R6 近景及旧 Surface Trimmed 近景的相机、density、Core 原像素全部一致。绝不将单向外扩误差代替双向轮廓检查。

|当前镜头|向外最大 px|向内最大 px|缺失 Core 像素比例|
|---|---:|---:|---:|
|Normal close|22.00|4.47|1.79%|
|Trimmed close|23.71|6.71|2.85%|
|Carved close|22.00|7.07|2.25%|
|真实三刀深切|11.00|4.24|1.66%|

Trimmed 旧 Surface 近景向外 36.40px、向内 10.20px、缺失 3.48%；C 收短外扩并减少缺像素，但仍非完整覆盖。C 三态低角度仍有 18.87px 最大向内缺口、约 4.03–5.51% 缺失；这些视角外轮廓被画面边缘裁切，不能用其外扩最大值作完整轮廓验收。cm 仅由实际 Core 深度换算局部视平面距离，不是假设 HairVolume 是 SDF。

受控批次再次得到相同 mask。Normal 旧版向内 10.20px / 缺失2.01%，C 改善至4.47px / 1.79%；**Carved 旧版向内最大5.00px，C 反而增至7.07px**，虽然总缺失从2.50%降至2.25%，局部最坏缺口仍有退步。R6三态近景向内为0。此项明确阻止整体视觉放行，不能以平均覆盖改善掩盖。

固定相机、同一 eroded Core 像素区、0.15–0.75 秒真实相邻帧 RGB 差：旧 Surface 0.892 / P99=5，C 0.218 / P99=2（8-bit 值）。两段有 16/17 帧，原始间隔均约 34–44ms，未重采样；亮度对比也改变了，此数值不是抗锯齿单一变量的因果证明，更不是所有运动下的摩尔纹评分。

三段固定相机原生 LMB 连剪：Trimmed 后侧 187 帧、Normal 182 帧、Carved 186 帧，共 555 帧。相机/头部固定，无风和接触注入，剪后根资源、fin mesh、density 保持，空剪不发冲量，最终导向残差低于 0.5mm。在剪前/后共同存活毛发 mask 的侵蚀区域，C 未再次检测到 RGB 全通道 >245 的爆亮像素；旧后侧 195 帧中 1 帧、B 后侧 178 帧中 5 帧检测到。ROI 外和未采样运动不在此结论内。截图中的孤立紫色长刺未在该同类后侧序列稳定重现，因此不写成彻底消除。

Build 成功。Core 248/248、Surface 原生 34+9、动态按键 8+空头 9+施力剪空 1 均通过，两次都完成真实 LMB 108 刀剪空、重置与再剪，未出现空网格错误。本轮不涉及网络权威、正式 B 工具或音频，未重复声明整个 B/WAN 回归通过。

八个已完成最终运行的 65 文件清单、各源码副本和活动源码全部一致。审计：`artifacts/puppet-fur-appearance/final-c/review/appearance-audit.json`。`final-c/visual-legacy` 在两张图后停止绘制，已停止自己的进程树并保留为中断样本；不能将它视为完整对照矩阵。

四条真实录像已封装并逐帧核对时间戳，最大误差≤0.5ms，无插帧：

- [Trimmed 后侧真实剪切](D:/Project/Game/Godot/project_hairball/artifacts/puppet-fur-appearance/soft-c-rear-cut/motion-verified.mp4)
- [Normal 真实剪切](D:/Project/Game/Godot/project_hairball/artifacts/puppet-fur-appearance/final-c/cut-normal/motion-verified.mp4)
- [Carved 真实剪切](D:/Project/Game/Godot/project_hairball/artifacts/puppet-fur-appearance/final-c/cut-carved/motion-verified.mp4)
- [相机移动采样检查，非毛发物理](D:/Project/Game/Godot/project_hairball/artifacts/puppet-fur-appearance/final-c/sampling-trimmed/motion-verified.mp4)

[同一相机、同样五次原生剪切后的原像素对照](D:/Project/Game/Godot/project_hairball/artifacts/puppet-fur-appearance/final-c/review/rear-cut-before-after.png)。对照的camera、最终density与Core原像素均已核对相同。

## 性能复测

早期试片存在外部 Python GPU 竞争；GPU/帧/输入延迟不作为受控验收。任务结束后，`controlled-c-r2` 完成8个顺序运行、174次进程观察，没有外部Python/其他Godot/编码器插入；保留GPU时钟/温度记录。均为RTX5070Ti Laptop、Forward+、1280×800、MSAA4×+TAA、VSync关闭；旧Surface/R6也使用相同修正后的灯位。桌面环境并非隔离的实验室系统。

|同一近景|R6 前后夹测 GPU ms|旧 Surface 前后夹测 GPU ms|C GPU ms|C 帧 P99 ms|
|---|---:|---:|---:|---:|
|Normal|6.935–6.971|5.903–6.048|6.865|7.872|
|Trimmed|6.584–6.858|5.581–5.614|6.004|7.150|
|Carved|6.971–7.348|6.118–6.349|6.636|7.519|

新版有可见成本增加，未宣称性能收益。四个毛发实例、相同风/接触动态压力：C首尾GPU13.546/13.626ms、帧P99 **18.756/18.957ms**；旧版GPU12.961ms、P99 **18.423ms**。新旧均未通过稳定60fps的16.67ms帧时间线，更不满足20%余量；这仍是毛发压力场景，不能冒称实际四玩家性能。这批不是原生剪切输入到显示的受控延迟测试。

8个受控运行也与最终视觉阶段的65文件快照一致，15对camera/density/Core原像素全等。合计16个完成运行使用同一候选，详见 `final-c/review/controlled-audit.json`。首批 `controlled-c` 在自有Godot退出时遇到CIM命令行瞬时为空，被观察器误判外部进程；已核对同PID/父PID/创建时间的连续记录，保留该不完整批次并修正观察器，完整复测另存r2，没有覆盖旧证据。

## 试玩与回退

```powershell
dotnet build --no-restore
./scripts/puppet_volume_fur.ps1 -Shells 8 -SurfaceSamples -Dynamics -Interactive
```

1 Normal、2 Trimmed、6 Carved，LMB 剪切并回弹，R 重置。加 `-LegacyAppearance` 回看本轮之前的 Surface 外观与灯位，保留相同的夸张弹性。该开关仅在显式 Surface 实验中改变外观；正式 B/R6 入口保持原值。

下一次真人重点：沿头皮与剪口绕看时，是否还有独立长刺/亮点；低角度的小孔是否仍像透空；缩短的散尖和内部毛束是否比旧密齐毛片更合适。保留整体视觉未通过，不以本轮回归和性能数字解除它。

技术参照仅用于独立实现：[Godot alpha antialiasing](https://docs.godotengine.org/en/4.7/tutorials/3d/standard_material_3d.html#alpha-antialiasing)、[GLSL 4.60 规范](https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.60.html)。没有获得原视频作者的材质/蓝图公式，不宣称复刻其源码。
