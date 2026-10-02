# 联机恢复与断线体验

2026-09-29。基于 v0.6 的增量修改；保留 A、B、普通模式及 ENet / ZeroTier。没有接入 Steam，没有修改网络设备或防火墙。

## 体验与边界

运动数据超过 1.5 秒或完整状态超过 2 秒未更新时显示网络警告。有效接收进度停滞 3 秒或收到意外断开事件后进入恢复。房主最多暂停全队 30 秒；界面、网络、单调时钟继续运行，游戏模拟不运行。房主可以提前继续。恢复窗口中的后续失联不延长截止时间，恢复后需连续健康 10 秒才再次允许自动暂停。

重连保留原局与逻辑玩家身份，拿到校验和解码成功的完整现场后才能操作。输入预测、持续工具操作与扶头状态清空，必须松开操作键后重新操作。自动重连和同步界面也可手动重试，保留原恢复凭证和截止时间，不把 30 秒窗口重新拉长。主动离房有确认和取消。连接恢复失败保留最后场景和遮罩，提供重试、离开房间；重试超过保留期按新加入处理。房主正常关房通知成功送达时说明房间已关闭；未知断开只能说明暂时无法连接，不能判定房主崩溃或 ZeroTier 出错。

房主提前继续时，失联者放下工具和搬运物，取消扶头、移除阻挡，席位最多保留到原截止时间。全部等待者完成同步后解除暂停；到期仍在线者继续原局。没有主机迁移或跨进程存档恢复。

## 同步实现

- 协议版本 **9**，双方必须更新。握手显式检查版本，旧协议客户端不允许进入游戏。保留既有三参数握手入口，协议检查先于新字段解析；已用真实 r11/协议 8 客户端验证新房主返回版本不一致提示。
- `src/Bootstrap/NetworkRecovery.cs` 管理健康状态、逻辑身份、传输与恢复；`Main.cs` 接入连接生命周期和游戏暂停；`NetworkMotion.cs` 保留现有预测和插值。
- 完整快照与回放按最多 **900 字节应用载荷**分块。每玩家最多 **32 块未确认**，总发送预算 192,000 字节/秒，突发 28,800 字节；轮转玩家，每轮最多发送 8 块。这里的 900 字节不包含 RPC/ENet/UDP 头。
- 块走 ENet 独立不可靠通道，由应用层有界确认和补发实现完整传输。避免把全部分块再次放进同一条 ENet 可靠有序队列。补发间隔根据 RTT 限在 0.35–1 秒；运动、健康控制使用独立通道。回放排在现场同步之后。
- 单玩家只保留一个活动传输和一份待发送回放；新快照在可发送时取最新权威世界，不积累历史快照。收包确认与完整应用确认分开；心跳重复累计确认，防止单个确认丢失导致永久等待。
- 传输编号、连接代次、块序号、完整 SHA256 校验；压缩载荷上限 8 MiB，解压上限 128 MiB。损坏、旧代次、重复与超界数据不能替换现场。完整收齐并成功解码后原子替换。
- 房间身份与 ENet peer ID 分开；恢复凭证为进程内随机 32 字节，不写入世界快照或日志。重新绑定递增连接代次，旧确认和输入代次不能继续生效。客户端握手带单调递增的尝试序号，旧尝试不能替换新绑定；握手成功后复位失败退避，若同步中再次断开则立即开始下一次重连，但不重置 30 秒总期限。
- 逻辑房间仍只有 3 个远端席位。底层预留最多 32 个临时传输连接，未完成的 ENet 握手在 8 秒后回收；已注册连接仍经 SceneMultiplayer 清理，避免直接重置后留下高层 peer 引用。逐秒记录底层连接状态数量，检查是否随重连次数累积。程序主动清理连接记录 `transport_close_requested` 及来源；底层断开回调单独记录，不能把回调当作已知根因。诊断用的帧耗时、纠正量与包间隔采样均有固定容量。
- 网络暂停不推进施工时间，实验事件记录 `pause_begin`、`pause_end` / `pause_continue`。B 试玩报告（以及历史 A/B 对照报告）都需标注受网络影响的场次。

## 可复现命令

使用 PowerShell 7 (`pwsh`)，从仓库根目录运行。不要在源代码测试运行途中重新编译它正在加载的程序集；长期网络测试固定使用已导出的 exe。

耐久夹具使用单调时钟控制真实运行时长，报告同时保留 `wallSeconds` 和物理帧累计 `seconds`。同机多进程负载会影响物理帧耗时，这类测试不是朋友电脑的性能基准。被主动中断的长测不计作完成。

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/verify_b.ps1 -Network
./scripts/run_b.ps1
./scripts/package_b.ps1 -BuildId 新的唯一标识
./scripts/verify_b_package.ps1 -Archive artifacts/packages/ProjectHairball-B-Windows-20260929-recovery-r15.zip

$game = 'artifacts/packages/ProjectHairball-B-Windows-20260929-recovery-r15/ProjectHairball-B/ProjectHairball-B.exe'
python scripts/recovery_matrix.py --executable $game
python scripts/recovery_scenario.py --executable $game --exported --players 2 --fault blackout --active-retry --rendered --seconds 75 --port 20300
python scripts/recovery_scenario.py --executable $game --exported --players 4 --fault blackout --fault-at 45 --fault-seconds 5 --period 90 --seconds 1800 --port 19000
python scripts/recovery_scenario.py --executable $game --exported --players 2 --fault chunks --fault-seconds 15 --seconds 130 --port 18660
./scripts/net_scenario.ps1 -Profile cross-border -Players 4 -RequireReplay -Port 18670 -Executable $game
```

`recovery_scenario.py` 还支持 `--continue-early`、`--expect-failure`、`--manual-retry`、`--host-close`、`--user-leave`、`--version-mismatch`、`--bad-token`、`--host-exit 6`。`--rendered --language zh` / `en` 保存实际恢复界面截图。`--period 6 --fault-count 2 --fault-seconds 5` 模拟恢复过程再次失联。

UDP 代理不需管理员权限。固定随机种子 729；同一机器上的故障解除至恢复时间可以直接比较。`domestic`、`cross-border`、`relay-stress` 是合成档位名称，不是中国或加拿大网络实测值：分别为单向基础延迟 25/120/180 ms、随机丢包 0.5/2/3%、带宽 8/4/2 Mbps，另有抖动和压力档短突发丢包。

`blackout` 为双向断流，`uplink` / `downlink` 为单向断流；`chunks` 丢弃下行大于 800 字节的数据报，保留小运动包；`large` 丢弃大于 1200 字节的数据报；`reliable` / `ack` 按 ENet 命令过滤可靠数据/分片和确认。一个 UDP 数据报可能包含多类命令，过滤不能解释为真实 MTU 根因证明。

## 诊断与证据

原始基线见 `artifacts/disconnect-audit-20260929-logs1/FINDINGS.md`：完整状态停滞，存在 21,544 字节未完成交换；移动和输入继续流动一段时间；之后断开。没有异常堆栈，双方时钟有偏差。**可靠传输/确认停滞是有依据的假设，原始根因仍未知。**

旧交付包保留在 `artifacts/packages/ProjectHairball-B-Windows-20260929-smooth.zip`。其大小包过滤对照 `artifacts/recovery-baseline-chunks-20260929-113831-18600/` 重现了状态停滞后断线的症状；不能据此证明朋友那次事故的原始根因。

新包最终验证结果以本次交付证据索引和 `PROJECT_STATE.md` 最新条目为准；中间 candidate/recovery2/final 包及被打断的测试不作为最终通过证据。

中间 r8 包的耐久测试在约 7 分钟时失败：周期性短断网后两名客户端恢复，第三名在 30 秒窗口内一直未完成 ENet 重连。该证据保存在 `artifacts/recovery-blackout-20260929-120755-18900/`。临时握手占用原先 8 个传输槽位是待验证的解释，后续版本增加有界握手容量、回收未完成握手并记录底层状态；不能将这次合成失败的解释当作朋友原始掉线的已证实根因。

双击包内 `Open_Test_Logs.cmd` 打开用户数据目录，同时收集 `logs/` 引擎日志和 `playtests/` 网络/实验日志。双方分别打包并标注房主/加入者、包版本、所在地和大致故障时间。程序不自动上传。日志包含 UTC、各端单调时间、连接代次、健康状态、收包与应用进度、RTT/丢包统计；RTT 过期有标记。跨机器 UTC 不能直接换算单向延迟。

r10 完整 30 分钟长测的状态一致性通过，但其中一次恢复耗时 **8.064 秒**，超过 8 秒验收线，因此没有按工程完成交付。事件中可确认同步途中再次断开，尚不能确认原因；不能据此断言是旧握手抢占。后续版本增加重试序号保护、握手成功后复位退避和更细的关闭来源日志，并重新运行耐久测试。测试脚本现在对每个可恢复周期执行 8 秒检查；若下次故障本身在 8 秒内再次发生，则从最后一次故障解除计时。

## 本次验证结果

工程检查通过，证据和构建边界见根目录 `NETWORK_RECOVERY_VERIFICATION.md`。26/26 网络场景通过；冻结 r14 的 30 分钟测试窗口完成 14 局、20 次周期断网，最慢恢复 5.2325 秒。最终 r15 补充恢复中手动重试和逐玩家 RTT 过期标记，专门验证截止时间不延长、恢复成功及实际导出包。不要把 r14 长测记成 r15 可执行文件的长测。

## 人工验收仍必需

加拿大房主与中国加入者使用同一个新包，通过原有 ZeroTier 地址完成至少 3 局，并做一次约 5 秒短断网。确认保留现场、没有无提示退回菜单、恢复没有明显镜头猛拉，同时保存双方日志。

网络恢复不等于消除所有眩晕。旧线路日志约 1.2 米的位置纠正必须在新包上单独观察；合成测试没有超过 2 米并不代表舒适。没有这轮真人确认，不能宣称真实跨境故障彻底解决。



