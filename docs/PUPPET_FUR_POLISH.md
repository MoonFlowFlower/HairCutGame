# PuppetLab 整体视觉精修，2026-10-02

用户已认可 depth C 的层次与夸张 Q 弹。本轮保留该方向，改善深切内部的长梳纹、薄边覆盖，以及脸和手的布料观感。当前是可玩的 Surface 视觉候选，整体视觉仍有未通过项；正式 B/R6 默认未替换。

## 候选及回退

- DLL：`1C87DCF71FB2EA527573F3561F906C1E8ADBD830C999E911F76EF8B62EDD1E71`。
- 65文件清单SHA256：`B5D61FF77B291AD01A23C61137135E6D1373F497BED17EEB8CC58E76FC504B9B`。
- 14个完成运行的源码快照和当前文件逐项一致，48对相机/density/Core mask一致。回退入口的近景 tips mask 与获认可的depth C逐像素一致。
- 原始曲线生成源码与depth C一致，160167条原曲线/828520 fin三角形；没有增加Shell层数。`PuppetFurMotion.cs`哈希仍为`B819D52DBB4FD33312C23B0EBEA59164F9D599C280763C1F841CF117D57F3032`。
- 冻结源码：`artifacts/puppet-fur-polish/final/visual/source`；机器可读审计：`final/review/audit.json`。

```powershell
dotnet build --no-restore -v quiet
./scripts/puppet_volume_fur.ps1 -Shells 8 -SurfaceSamples -Dynamics -Interactive
# 回到用户认可的depth C，保留层次和Q弹
./scripts/puppet_volume_fur.ps1 -Shells 8 -SurfaceSamples -Dynamics -Interactive -LegacyPolish
```

1 Normal / 2 Trimmed / 6 Carved；LMB剪切并回弹，R重置。`-FlatClumps`仍只关闭遮挡层次；`-LegacyAppearance`复现更早外观。

## 原因与实现

同FPS镜头的拆层实拍在`isolate-low`。长梳纹主要出现在采样层，而原fin独立显示为短曲线；细查发现原来的二维底绒图案贯穿深度，形成连续顺纹。仅将原曲线径向分布放宽的axis-a没有解决它，且短发缺失Core增至3.47%，已撤回。

新底绒采用同一有限曲线体积模板的半尺寸第二群，固定相位与原始材料坐标，通过原始occupancy验证根支持，随后按当前occupancy裁剪。它替代二维底纹，未往新切面种毛。微小毛束按像素足迹和最低mip过滤，减轻细颗粒；仍是体积采样与原曲线fins的独立实现，不能称为作者公开源码或纯Shell复刻。

保持8层、30mm内部深度，将采样位置按`t^1.45`分布以靠近薄边；每层记录实际代表的区间，并按有界视线掠射路径积分透明度。零毛束覆盖仍为零，头发本体没有不透明紫色底壳，HairVolume仍为occupancy而非SDF。根支持与mip覆盖是经过滤的近似，未声称逐根解析精确。

脸、手和布偶躯体使用更小、更浅的布纹：非织物法线强度0.30→0.14，纹理尺度2.6→4.0，颜色变化范围收窄。保持原颜色和粗糙度。上一轮的四探针毛簇明暗与灯位修正保留。

## 实拍与双向轮廓

17个镜头完成：14个可到达的第一人称镜头加3个俯视诊断；包含三态、原生LMB切削和连续三刀深切。以下从原像素重新测量，阈值188（约50%线性覆盖），单位是屏幕轮廓距离；低角度外轮廓被视口裁掉，向外极值只表示可见部分。

| 实拍镜头 | 缺失 Core 像素 %：旧→新 | 最大向内 px：旧→新 | 最大向外 px：旧→新 |
|---|---:|---:|---:|
| r7-normal-close | 1.79 → 0.87 | 4.47 → 4.00 | 22.00 → 22.00 |
| r7-trimmed-close | 2.85 → 1.64 | 6.71 → 5.66 | 23.71 → 23.71 |
| r7-carved-close | 2.25 → 1.18 | 7.07 → 5.66 | 22.00 → 22.00 |
| r7-native-deep | 1.66 → 0.72 | 4.24 → 4.24 | 11.00 → 11.00 |
| r7-normal-low | 4.03 → 3.42 | 18.87 → 17.89 | 29.43 → 29.43 |
| r7-trimmed-low | 5.51 → 4.66 | 18.87 → 17.89 | 34.71 → 34.71 |
| r7-carved-low | 4.76 → 3.92 | 18.87 → 17.89 | 29.43 → 29.43 |

同镜头原像素对照：

- [深切前后对照](../artifacts/puppet-fur-polish/final/review/r7-native-deep-paired.png)
- [近景前后对照](../artifacts/puppet-fur-polish/final/review/r7-normal-close-paired.png)
- [低角度前后对照](../artifacts/puppet-fur-polish/final/review/r7-carved-low-paired.png)

图像只并排摆放，标题在视口外，未重绘。内部顺排长纹明显减轻，脸手更干净；外层散尖仍有硬片感，弯曲时局部仍能看出拉长纹理。低角度最大向内17.89px与3.42–4.66%缺失像素仍未通过。完整17镜头变化见`final/review/coverage-changes.json`，不以选中的近景代替全角度质量。

## 实时剪切、接触、时间稳定性

- 原生PuppetLab LMB实录195帧/7.509秒，6刀均提交；相机/头固定，无诊断风或接触注入。原根、fin和剪后density保持，空剪无冲量，最终导向残差<0.5mm。洞口没有在恢复过程中重生。
- 接触球诊断：Normal/Trimmed各2个低覆盖残留像素，对照depth C各4个；三态实心穿入像素均0，深切Carved残留0。2像素仍是未消除的诊断残留，不能写成全三态零穿入。诊断球并不代表正式所有工具碰撞接入。
- 195帧中，共同存活毛发的130574像素ROI未检出RGB全通道>245的白色爆亮。这不是所有场景闪烁通过。
- 纯相机运动实录153/159帧。静止段均值帧差旧0.365→新0.346（8bit RGB），p99均3；采样间隔37.36→35.40ms，亮度76.67→77.49。时间间隔本身不同，因此不宣称获得显著去闪烁收益。
- 真实剪切输入到首个提交画面约66.0–118.7ms；这是带截图读回、共享负载下的观察，不是受控输入延迟验收。
- 三条MP4均保留实际帧时序，逐帧PTS误差≤0.5ms，没有插帧，末帧延长以保持结束画面。

[剪切与回弹录像](../artifacts/puppet-fur-polish/final/cut-carved/motion-verified.mp4) · [新版相机移动](../artifacts/puppet-fur-polish/final/sampling-new/motion-verified.mp4) · [旧版相机移动](../artifacts/puppet-fur-polish/final/sampling-old/motion-verified.mp4)

## GPU 数据及限制

RTX5070Ti Laptop，Godot4.7.2 Mono/Forward+，1280×800、MSAA4×和TAA、关闭VSync。有效采样窗口无其他Godot/python/ffmpeg；Godot实测GPU时间与真实帧p99分别记录。静态每个镜头预热4秒后采样至少120帧，截图在耗时采样之后。

| 三态近景 | 旧版前/后夹测 GPU ms | 新版 GPU ms | 新版帧 p99 ms | R6 GPU ms |
|---|---:|---:|---:|---:|
| r7-normal-close | 7.790 / 7.663 | 7.022 | 7.893 | 6.760 |
| r7-trimmed-close | 6.836 / 7.098 | 6.638 | 7.467 | 6.127 |
| r7-carved-close | 7.164 / 6.776 | 7.141 | 8.046 | 6.753 |

这是分时段夹测：`controlled-r2`前三个运行的62次观察无外部任务；其后受干扰的accepted-after被排除。`controlled-finish/accepted-after`的23次观察无干扰，作为后夹测；随后r6-after受到外部Python任务影响，被排除。`controlled`第一次整批中断也保留。全部使用相同冻结构建，GPU温度/时钟记录随各窗口保存；没有声称一段完整连续独占长测。三态表现与原版同一量级，不能只看Normal的下降就宣称普遍性能提升。

四头受控复测尚未取得：其他项目的 Python 任务持续出现，启动守卫阻止重叠。没有使用上一版四头数据替代当前候选；也没有宣称60fps/20%余量通过。

## 实际检查和剩余工作

编译0错误、2条既有nullable警告；Core248/248；Volume34项、空头9项，包含真实108刀剪空、R重置、再剪，通过。原`array_len == 0`错误未在这些运行复现。没有新增玩法、联机协议或正式B渲染接入，未重跑未受影响的全网测。

优先真人验收：短发/深切是否还像顺排刷毛，脸手是否过于光滑，快速连剪时是否仍看见亮闪或硬片。后续最高价值工作是低角度薄边覆盖、保留散尖同时软化硬片，以及四头受控性能；实际场景中的毛发投影仍未实现，当前遮挡是原空间的风格化近似。
