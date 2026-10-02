# 原生落刀回弹（2026-10-02）

## 当前版本：夸张回弹，Owner 要求短发也有明显 Q 弹

候选 DLL：`A38D79022A78612CD6A8D7ABEC5A9309FF18EBC37CA5140101134EB74E4A785D`。用户试玩后明确要求放开幅度，本轮只修改 `PuppetFurMotion.cs` 的落刀响应。此前短发按剩余长度衰减，Trimmed 首刀只有约4mm；现在给存活短发保留响应下限，落刀局部范围扩大、冲量增强，频率18→12 rad/s、阻尼0.30→0.22，保留2.2秒低阻尼窗口。速度限2m/s local，原位移限8cm local保持。无材料移除不触发；毛根、占据和切口不跟随弹簧恢复。

七次运行的62个候选文件与源码快照哈希全部一致，相比上一版只有该类和DLL变化。真实固定第一人称、实际LMB输入、没有风/接触球注入；三态加关闭反馈对照共729帧、四条约7.5秒实录。逐帧视频时间戳误差均≤0.5ms，无插帧。

|状态|首刀向内峰值|反向回弹峰值|降至0.5mm以下|全序列最大位移|
|---|---:|---:|---:|---:|
|Normal|6.49cm|3.20cm|1.85s|7.20cm|
|Trimmed|3.64cm|1.79cm|1.77s|5.29cm|
|Carved|6.45cm|3.18cm|1.84s|7.20cm|
|Trimmed关闭落刀反馈|0|0|无响应|0|

以上为导向点数据，不冒称每根渲染毛尖同幅度。Trimmed 首刀向内约为上版9倍，反弹约12倍；实拍能看到剪口周围明显向内收、再向外回弹。Normal/Carved 连剪会达到原7.2cm世界位移上限，随后稳定恢复；相机与头部固定。四条物理时长分别7.467/7.483/7.500/7.517秒，记录墙钟约7.51–7.53秒，保留负载导致的小幅模拟落后。

三态相机、逐刀density、剪前Core/毛尖mask与上一版逐像素一致，剪后Core亦一致。Trimmed开/关反馈的逐刀density、相机和最终Core相同；关闭反馈位移为零。根场和fin保留、剪后不重生、空剪无响应、最终残差<0.5mm均通过。动态按键8、空头9、施力剪空1、108刀剪空/重置/再剪、Core248/248通过；Build 0 error、两条既有nullable warnings。正式B/R6默认不变，未重复运行与该实验参数无关的B全套。

本轮新拍Trimmed/R6同视角：相机、density、Core完全一致。当前静止向外36.40px≈3.79cm，向内10.20px≈1.77cm，缺失Core3.48%；R6向外23.77px≈2.48cm、向内0。三态连剪后的双向轮廓、深度与叠图另存；cm均为视平面估计。**静态硬尖、短切孔隙、闪烁并未由调弹性解决，整体外观仍待验收。**

外部Python GPU工作PID39820/43888贯穿本轮。新拍Trimmed/R6原始GPU分别10.55/11.27ms，帧p99 18.02/19.07ms；native cut-ready54–165ms、首绘制帧57–182ms。**全部属于受竞争影响的数据，不能据此宣称性能收益、受控延迟或GPU验收通过。** 这轮没有重做全部受控性能矩阵；相关缺口继续保留。进程和逐秒GPU记录在本阶段根目录。

实录：[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/trimmed/motion-verified.mp4)、[Normal](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/normal/motion-verified.mp4)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/carved/motion-verified.mp4)、[关闭反馈](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/control-trimmed/motion-verified.mp4)。[短发前后回弹曲线](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/review/trimmed-recoil-comparison.png)、[真实帧](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/review/trimmed-actual-recoil.png)、[完整审计](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/cartoon-recoil/review/cartoon-audit.json)。

试玩命令保持文末入口，按2切Trimmed，分别试单刀与连剪。此轮不改自动跟进的暂停状态。下一项真人验收是夸张程度是否合适，以及连剪达到位移上限时是否显得太硬。

## 上一版原生接入记录（8E33993C，已被上方参数替代）

**实验入口已接入真实理发反馈；整体外观仍未通过。** 该轮没有修改毛束分布、层数或静态着色，硬尖、短切透空、闪烁仍需下一步处理。历史候选DLL：`8E33993C0A562D902775119073D650691B35BCE9049F4E6366F57363446B21F1`。

## 实际变化

原来`PuppetFurMotion`只接受风和诊断球，真实LMB剪切没有力输入。现在`PuppetLabHair.Cut`确认移除材料并更新剪口后发出`CutCommitted`，存活毛发收到一次向内的局部速度冲量，附近最强、远处只轻轻跟随。无材料变化和空剪不触发。

落刀后短时使用较低阻尼的弹簧，形成收缩与反向回弹；短发按剩余长度减弱。单次速度限1.4m/s local、原位移限8cm local不变，连续剪切不会无上限叠加。只变毛发位移，头部/相机姿态、原毛根、原曲线和权威占据保持。剪空先移除失效导向点，重置解绑旧回调再连接新对象。

这是PuppetLab真实LMB的表现接入。风/接触球仍属诊断刺激；正式B/R6默认、网络状态及工具命中体积保持原状，不声称正式B已换用此渲染器或完成逐根刚体模拟。

## 固定第一人称验收

三态及关闭落刀反馈对照共712张实际viewport帧、四条约7.5秒实录。固定相机/头部，不注入风或接触球；1秒一次落刀，3秒起一组快速连剪，再等待静止。Normal与关闭反馈对照逐刀density完全相同，相机和最终Core mask完全一致；对照位移为0。

|状态|实际生效刀数/尝试|首刀向内峰值|首刀反向峰值|降至0.5mm以下|
|---|---:|---:|---:|---:|
|Normal|6/6|2.32cm|0.86cm|约0.695s|
|Trimmed|3/6|0.40cm|0.15cm|约0.476s|
|Carved|6/6|2.29cm|0.85cm|约0.754s|
|Normal关闭反馈|6/6|0|0|无响应，不计恢复|

这里是最强导向点的位移，不冒称所有渲染毛尖的最大值。全序列最大导向位移2.36cm，最终残差低于0.5mm；原根资源与fin mesh不重建，剪后density不反弹，另外朝空处的真实LMB不产生反馈。短切后三次没有再移除材料，正确地没有冲量。原始rows的`phase`沿用旧诊断时间段名称；本记录实际`inputWind`全为零、`contact`全为false，以输入和剪切事件为准。

实录：[Normal](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/pilot-normal-r2/motion-verified.mp4)、[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/pilot-trimmed/motion-verified.mp4)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/pilot-carved/motion-verified.mp4)、[关闭落刀反馈](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/pilot-control-normal/motion-verified.mp4)。每条逐帧核对PTS，误差≤0.5ms、不插值。物理时长分别7.517/7.500/7.400/7.500秒；Carved在负载下比墙钟少约0.108秒，未隐去这一限制。

[回弹曲线](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/review/native-cut-rebound.png)与完整测量在[cut-audit.json](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/review/cut-audit.json)。11个当前构建运行×62文件清单及源码副本完全一致。

## 静态轮廓、性能和回归

新拍三态R6/八层同镜头对照，相机/density/Core完全一致。当前八层的Core及毛尖mask与上一D963候选逐像素相同，说明本次回弹没有改善静止外观。三态近景向内最大10.20/10.20/5.00px，约1.765/1.765/0.923cm，缺失Core2.011/3.480/2.504%；向外最大35.78/36.40/35.78px，约3.725/3.790/3.725cm。cm是视平面估计。R6有实体Core，向内为零，向外20.59/23.77/20.12px。截屏、深度图、双向叠图全部保存。

[Normal同视角](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/review/normal-same-view.png)、[Trimmed](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/review/trimmed-same-view.png)、[Carved](D:/Project/Game/Godot/project_hairball/artifacts/puppet-cut-feedback/review/carved-same-view.png)。

外部Python GPU任务仍运行，**本轮全部GPU和延迟不构成受控性能证据**。实际记录：八层GPU12.726–13.317ms，R614.181–16.767ms；不能据此宣称快于R6。原生cut-ready约60–207ms、首个绘制帧63–220ms同样受竞争影响，需要独占复测。进程及逐秒GPU记录保留在`artifacts/puppet-cut-feedback/`。

Build通过（两条既有nullable warnings）；Core248/248；动态按键8、空头9、施力中剪空1；108次真实LMB剪空、空头再点击、重置再剪通过；正式B WorldChecks通过。首个`pilot-normal`因记录器在输入真正处理前读取结果而错误结束，已将读取放到下一绘制帧，失败目录保留，不计通过。

此前D963的完整源文件/纹理/运行程序集已独立保存在`artifacts/puppet-volume-motion/frozen-d963377b-project`，61文件哈希匹配。受控复测脚本默认使用该冻结副本，避免后续实现替换旧基线；`-CheckOnly`通过。另已实际从独立副本启动Normal近景，Core/毛尖mask与原D963逐像素相同，结果在`frozen-runtime-smoke`，其GPU同样未受控。后续新构建仍需自己的受控数据，不能沿用D963的性能结论。

当前自动跟进`automation-4`在检查时已为PAUSED，更新接续记录时保持暂停；不声称后台仍在自动执行。

## 试玩

```powershell
dotnet build
./scripts/puppet_volume_fur.ps1 -Shells 8 -SurfaceSamples -Dynamics -Interactive
```

LMB剪切即有回弹，1/2/6切换Normal/Trimmed/Carved，R重置。加`-NoCutFeedback`可做同构建关闭落刀反馈对照；F10关闭全部毛发响应。最有价值的试玩是单刀与快速连剪，判断局部回弹是否够清楚、是否过软，以及短发是否需要稍强反馈。外观软化、闪烁和独占GPU/延迟仍为未通过项。
