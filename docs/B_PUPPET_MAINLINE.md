# B 主线：已认可 Puppet 美术接入

> 2026-10-02 备份/清理更新：用户要求备份当前版本到 GitHub 后删除历史版本产物。下文旧 `artifacts/` 路径是当时的原始出处；当前保留的实拍、视频、mask、性能样本和回归日志已迁至 [精简验收证据](evidence/current-mainline/README.md)。旧源码快照和重复安装包不再承诺保留，见 [清理记录](REPOSITORY_BACKUP_AND_CLEANUP.md)。此后仅隐藏自己镜像 HUD 的改动及其独立检查见 `PROJECT_STATE.md`。

2026-10-02，Owner：“美术效果先这样吧，把这样的美术效果应用到主线”。该授权取代这套已认可外观此前的 lab-only / no-install 边界。本次完成的是正式 B 的接入，不重开美术方向实验，不增加玩法。

## 入口与回退

从项目目录运行：

```powershell
dotnet build --no-restore
./scripts/run_b.ps1
./scripts/run_b.ps1 -Mode Host -Port 7777
./scripts/run_b.ps1 -Mode Join -Address 127.0.0.1 -Port 7777
```

编辑器 F5 / 默认菜单及 B 启动脚本均选用新画面。主场景仍为 `res://scenes/Main.tscn`，新美术使用 Forward+、MSAA 4x、TAA。旧画面可运行 `./scripts/run_b.ps1 -LegacyArt`，该入口同时切回 Compatibility。尚未生成新的可分发导出包。

当前候选 DLL SHA256：`07D254C0D4DE72616F80BCE8FDC46B94E434929D84B7FC2A6C87B58FE43D9E9E`。

- 当前运行源码、shader、场景、配置、生成资源和 DLL：`artifacts/b-puppet-mainline/frozen-mask-clean/manifest.json`。258 文件最终复核零漂移；清单 SHA256 `2F384C6482A09FAD085BBB2F4E083B1B7081A97CDFF9AB7E356EA1C2A676AD4E`。
- 接入前完整源码备份：`artifacts/b-puppet-mainline/before/files.json`；接入前 DLL `1C87DCF71FB2EA527573F3561F906C1E8ADBD830C999E911F76EF8B62EDD1E71`。
- 中途候选与失败记录均保留，不能把不同构建的运行合并成一个“全通过”构建。

## 实际接入范围

`Main → SalonView → HeadView → MainlineFur` 使用真实 B 的共享头、玩家头、眉毛/胡须、假发与碎块。保持 B 的原发型体积和巨大共享头尺度；实验室的发型预设没有替换玩法数据。

毛发仍是已认可的固定有限曲线场、八次向内采样和原曲线外缘。B 的占据格子用于真实裁剪，它不是 SDF。新增坐标适配使渲染采样与权威体积逐点一致。切削面继续显示体积内原有毛场，没有给新切面添加实心紫色底壳，也没有在切面重新种毛根。短眉/胡须及小型展示使用同源体积场；镜像、假发和碎块用较低成本的四次采样。生长可重新显露预先存在于有界材料域内的毛场；超出初始发帽的新增长部分没有另造外缘曲线网格，这部分外轮廓细节与原始发帽有差别。

正式剪切/工具接触、吹风以及头部惯性驱动本地弹簧导向点；保留夸张回弹，最大位移约 7.2 cm，湿度、冰冻和胶水影响阻尼/硬度。弯曲是本地表现，网络不逐根同步。没有把实验室诊断球接触测试冒充正式逐根物理碰撞。

顾客和玩家使用布偶脸、手和圆润四肢；保留原有表情、坐姿/站立、注视等命名控制节点。第一人称手和推剪接入已认可模型/材质。房间保留主线布局与碰撞，采用相同材质方向和适合主房间的灯光。剪下的碎发使用同色短毛团，继续复用原碎屑预算、质量守恒、落地与吸取机制。

`src/Core` 的 47 个 C# 文件与接入前哈希相同，证据 `core-source-audit.json`。工具规则、打分、共享头权威、秘密信息过滤、协议 25 都保留。

## 联机加载修复

真实四人渲染测试暴露了首次加载导致的连接失败。单纯把资源准备移到 ENet 之前仍会失败：客户端拨号等了较久后，连接成功继续使用旧的 no-progress 计时，马上丢弃刚建立的传输，留下临时席位并报“full”。`ConnectedRecovery` 现在从传输成功时开始 welcome 阶段计时，没有放宽游戏中的失联阈值。

失败运行：`smoke-4-v06-B-20261002-171426-098`（原始接入）、`smoke-4-v06-B-20261002-172402-731`（只有预加载，已中止）。修复后 `smoke-4-v06-B-20261002-172700-340` 四端完成，全体最终状态一致；该次 DLL `BBE13D7B83D721B404C568F29B652E4DB278EAF78E38D0D2B3327CCC47AD0968`，随后仅改进 QA 录像保存方式。启动/首轮资源加载仍会造成短暂同步恢复，不能称为完全无停顿启动。

联网/世界回归 DLL `DFE35CDBA2D15073B57EA437AD2988BBF0A390F9139DF9083A7CC00CEAE1742B` 的受控网络：`artifacts/net-cross-border-new-20261002-172915-624-18230-37084`。固定 seed 729，单向 120 ms 延迟、35 ms 抖动、2% 丢包、4 Mbps；一个真实渲染客户端、一个 headless 主机，219.4 s 墙钟运行通过。最终 hash `5AE3331E3671E449`，顾客运动 3620 样本、6.279 m，最高速度 1.037 m/s；行走 6806 tick，反向 tick 0，硬纠正 0。不是加拿大—中国真实链路验收，也不是四个渲染实例性能证明。

## 证据口径与限制

静态验收运行在真实 Main 场景与 B Session，1280×800、73°、1.7 m 眼高，保存 Normal / Trimmed / Carved、实际原生 LMB 剪切、生长和湿/冻/焦状态。Trimmed/Carved 是确定性体积夹具；湿/冻/焦是正式材料函数驱动的诊断状态。原生 LMB 测试另外经过真实输入事件、Session 输入、工具和权威密度。三个额外热剪切经过真实 UseTool/EndStroke。

轮廓用主头内禀 fur/core mask 双向计算，阈值 188 sRGB，对照的是相同摄像机下权威体积的提取表面；不包含人物遮挡，不存在 depth 数据，因此只报告像素。近景 mask 接触画面边缘，不能把截图外的轮廓当作已测。旧 B 的同机位对照验证了 camera/rotation/FOV/density 相同；它在 Forward+ 中使用旧表现，用于视觉对比，不能称为旧 Compatibility 渲染器的等条件性能比较。

每个静态 GPU 窗口先预热 80 帧，再采 120 帧；PNG 读回在窗口之外。机器上有用户的游戏和其他任务，记录了进程，未停止无关任务。GPU 数据是真实的主视口耗时，但不是独占 GPU 基准，也不放行低配或四窗口 60 fps。

最终动态采样缓冲真实帧，PNG 压缩放到运动窗口之后，保存单调墙钟时间。导向点位移属于表现遥测，不是所有渲染毛尖的逐根接触证明；GPU 读回本身仍有开销。旧 `final/motion.json` 在采样间同步压缩 PNG，不能用于实时恢复时间结论。

## 最终复核

编译零错误、两个既有可空警告。Core 248/248 通过；共享引擎集成 `ENGINE_INTEGRATION_OK`。两者日志保存在 `artifacts/b-puppet-mainline`，Core 源码哈希不变。

DLL `DFE35...` 的顺序回归全部通过，日志 `final-checks.log`：实际视觉 19 条事实、WorldChecks 的猫/拖拽/拍醒/三轮照片/双语、真实 Compatibility 旧画面启动、四人完整局。四人证据 `artifacts/smoke-4-v06-B-20261002-173502-597`，60.565 s，最终状态 hash `E50E2CA2C657DE96`，一个渲染主机和三个 headless 客户端。该组不是四个渲染窗口压力测试。Compatibility 启动检查使用 `--quit-after 120` 结束，退出码 0，但日志有两个 ObjectDB 实例泄漏警告；该启动检查不证明旧画面的正常退出生命周期无警告。新视觉与 WorldChecks 的 stderr 为空。

随后发现语音 CanvasLayer 混入 silhouette mask；当前 `07D254...` 唯一源码差异是在 **QA 入口**隐藏该 UI，`qa-only-diff.json` 证明生产源文件/资源均与联网、世界和四人回归候选相同。当前 DLL 单独重跑完整视觉，证据 `final-clean`；没有声称前述网络套件在两个 DLL 上都跑过。旧受 UI 污染的 `final-verified/review/silhouette.json` 不作为轮廓结果。

`final-clean/review/silhouette.json` 的有效结果：

| 第一人称镜头 | Core 缺失 | 向内最大 px | 向外最大 px | GPU 均值 / p99 ms |
|---|---:|---:|---:|---:|
| Normal 普通距离 | 0.111% | 3.61 | 5.00 | 6.427 / 8.509 |
| Normal 近景 | 0.125% | 5.00 | 13.00 | 7.610 / 10.261 |
| Trimmed 近景 | 0.215% | 5.00 | 14.14 | 7.425 / 9.173 |
| Carved 近景 | 0.128% | 5.00 | 14.14 | 8.581 / 10.636 |

全十镜头 GPU 均值 6.072–8.581 ms；桌面并行任务限制如上。第一刀输入到首次画面 173.465 ms，后续三刀 52.299 / 49.347 / 45.659 ms；这仍是可感知的剪切停顿，不能用稳态 GPU 时间遮盖。主头首次资源建立约 1.3–1.6 s，实际联网预加载约 7.5 s，首轮仍可能触发同步恢复。

实际原生 LMB 剪后 360 次渲染观察、72 张真实采样帧、3.304 s 墙钟。记录窗口内导向最大位移 5.612 cm，约 1.754 s 后持续低于 0.5 mm，末帧 0.011 mm；约束上限 7.2 cm。视频 `final-clean/cut-response.mp4` 逐帧使用实际时间戳，PTS 差小于 1 ms。没有新增逐根碰撞或多人相互梳理物理验收。

普通实拍 `final-clean/normal-close.png`；同镜头前后对照 `final-clean/review/before-after-close.png`；三态 `final-clean/review/three-states.png`。静态状态夹具暂停世界模拟，所以早先真实剪下的碎发可能停留在后续状态截图中；不据此判定正常游戏中的碎屑悬空。

下一次人类试玩优先检查正式 B 中近距离深剪的内部、连剪手感、湿/冻后的回弹以及多人进入/首轮加载。继续优化的主要工程余项是首次加载和剪切尖峰；原先已知低角度硬尖/薄边仍属于已接受外观的限制，没有宣称全面消除。
