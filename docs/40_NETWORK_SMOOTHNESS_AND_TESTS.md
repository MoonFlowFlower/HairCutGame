# B 版跨国联机抖动修正（2026-09-29）

实际反馈：加拿大房主、中国加入者，经 ZeroTier 连接；加入者严重卡顿、抖动并眩晕。提供的视频为约 6.94 秒手机拍摄，能看到不连续运动，但不能由它独立确定帧率、丢包率或根因。

## 最终交付与实测

交付 `artifacts/packages/ProjectHairball-B-Windows-20260929-smooth.zip`（75,448,257 B），SHA256 `CFBD4A98FD9696C1775E54FDE7D468E3ABBFB0E7BB4B0DD325E253A20E97E58C`。其余 netfix/netfix2/netfix3 为调查中的中间包，不作为最终交付。双方都须更新；继续使用原有 ZeroTier 房主地址即可。

以下全部通过**最终导出 exe**执行一整局，实际 UDP 延迟/丢包代理，最终快照与房主对应发送记录的 hash 匹配、全部规定中间提示收到、36 帧回放收到：

| 最终包场景 | 客户端反向位移采样 / 行走采样 | 最大位置纠正 | >2m 硬纠正 |
|---|---:|---:|---:|
| domestic，2 人 | 0 / 2694 | 0 | 0 |
| cross-border，4 人、3 个客户端合计 | 0 / 8261 | 1.04 mm | 0 |
| relay-stress，2 人 | 2 / 2743（0.073%）| 188.28 mm | 0 |

旧同步方式的 2 人 cross-border 对照为 869 / 2730（31.83%），第一版修正的相同 2 人档为 0 / 2731；见后文。不能将不同人数/不同档位直接当作严格的同条件统计比较。

最终 4 人测试的实际回放载荷为 39,470 B，所有客户端均在进入回放阶段前收到；domestic / relay-stress 分别为 31,509 / 29,981 B。4 人档渲染客户端帧间隔 P95 / P99 为 9.09 / 14.72 ms；其余最终档位客户端为无界面运行，不能用于显卡性能结论。同机并行测试，亦不属于隔离硬件基准。

仍有明确限制：最终 4 人档个别运动包间隔最大约 917 ms，远端插值会短暂停住；relay-stress 的两次纠正没有消失。优化显著改善了本夹具中的持续回拉，但**不承诺丢包/中继链路完全无卡顿，也没有验证朋友电脑或真实中加线路的舒适度**。

最终网络证据目录：

- `artifacts/net-domestic-new-20260929-082531/`
- `artifacts/net-cross-border-new-20260929-082500/`
- `artifacts/net-relay-stress-new-20260929-082500/`
- `artifacts/package-check-20260929-082444/`：从 ZIP 解压到中文/空格路径，逐文件校验，隐藏全局 .NET 后验证实际加载捆绑运行库；菜单、18 个 B 引擎检查、双人完整游戏均通过。

重建 / 验证：

```powershell
./scripts/build.ps1
./scripts/run_b.ps1
./scripts/package_b.ps1 -BuildId 新的唯一标识
./scripts/verify_b_package.ps1 -Archive artifacts/packages/ProjectHairball-B-Windows-20260929-smooth.zip
./scripts/net_scenario.ps1 -Profile cross-border -Players 4 -Rendered -RequireReplay -Port 18110 -Executable artifacts/packages/ProjectHairball-B-Windows-20260929-smooth/ProjectHairball-B/ProjectHairball-B.exe
```

## 判断与实现

发现旧客户端每个物理帧都向 5 Hz 的旧房主位置回拉；转头相机也只在物理帧更新。整份头发/世界状态以可靠包发送，丢包后旧状态排队，会使这一问题在高延迟下更明显。不能把这些问题全部归咎于 ZeroTier。

修正继续使用已有 Godot、ENet 和房主权威系统，A/B 的玩法差异不变：

- 本地立即预测移动；60 Hz 输入编号，20 Hz 发送最近 12 个输入作为冗余；房主去重、有界追赶，确认已处理序号。客户端从确认状态重放剩余输入，平滑小幅视觉修正。追赶不能跨过按键边沿，不能凭客户端命令增加模拟时间。
- 玩家、顾客和道具的紧凑运动包 20 Hz、独立不可靠有序通道，不随整份头发状态排队。远端按单调时间缓冲插值，缓冲按抖动在 100–280 ms 内调整。短暂预警、扶头和降落状态也走快速通道。
- 完整世界事实仍可靠同步，每个接收者仅允许一份应用层未确认快照，避免旧快照无限积压。未改变的密度数组复用缓存。判定、金钱、工具效果和最终结果仍由房主决定。
- 远端插值只移动可见模型，预测碰撞体读取最新已知的权威运动状态。镜头平滑不能反过来修改碰撞世界。
- 世界 / 回放使用 .NET 内置 Brotli Fastest 和带格式标识的编码，保留旧 Deflate 解码及旧算法对照开关。较大的压缩窗口减少重复头发和回放数据，不需要额外依赖。
- 相机每个渲染帧读取鼠标方向，并在物理位置之间插值；旋翼、头发摆动和手持工具的装饰时钟独立于慢速世界快照。
- F3 显示 FPS / ENet RTT / 帧耗时；双方每秒写本机 network-*.jsonl，不上传、不录麦克风。

这不是全世界物理回滚：动态碰撞、突然外力及丢失超过冗余窗口的输入仍可能需要权威纠正。远端插值用少量显示延迟换连续性；跨国工具结果的往返时间仍存在。

## 选型结论

当前最合适的路线是 **保留 ENet，先修正预测、重放、插值和拥塞行为**。传输协议替换不会自动修复旧位置回拉。

| 方案 | 适用性 / 代价 | 当前决定 |
|---|---|---|
| ENet + 房主权威 + 本地预测 / 远端插值 | 重用全部现有 Godot 和游戏系统；需处理可达性 | 本次实施 |
| SteamNetworkingSockets + Steam Datagram Relay | 若上 Steam，适合好友邀请、NAT / 中继和隐藏 IP；路由可能改善，不能保证中加延迟 | Steam 发行阶段优先评估，未接入 |
| 开源 GameNetworkingSockets | 成熟可靠 / 不可靠消息、加密和拥塞控制；不自动获得 Valve 中继网络 | 暂不为当前朋友试玩迁移 |
| Photon Fusion Godot | 有现成同步能力，但官方 Godot 页面仍标记 development preview、非生产用途；付费服务 / 重接游戏系统的成本高 | 不采用本轮大迁移 |
| netfox / Netfox Sharp | Godot 原生社区方案，提供预测、回滚和插值；官方仍将 C# 支持标为 experimental，接入需改造既有 tick 和状态管理 | 可作后续对照，当前不替换已能验证的路径 |
| 确定性锁步 / 全世界回滚 | 当前 Godot 碰撞、可变头发与世界效果并非确定性模型；回滚范围和维护成本大 | 不适合当前原型 |

中国与海外也不能简单选同一云区：Photon 官方明确说明中国区和海外连通存在限制，不能据此承诺它能解决加拿大到中国的问题。ZeroTier 的 DIRECT / RELAY / TCP fallback 会影响路径；本机只读 CLI 检查因鉴权文件不可读而未得到实际路径，未读取密钥、修改网络或申请管理员权限。

主要资料（均为官方或原作者）：

- [Godot 相机与高级物理插值](https://docs.godotengine.org/en/stable/tutorials/physics/interpolation/advanced_physics_interpolation.html)
- [Snapshot Interpolation — Glenn Fiedler](https://gafferongames.com/post/snapshot_interpolation/)
- [Steam Datagram Relay](https://partner.steamgames.com/doc/features/multiplayer/steamdatagramrelay)
- [Valve GameNetworkingSockets](https://github.com/ValveSoftware/GameNetworkingSockets/blob/master/README.md)
- [netfox 功能与 C# 支持状态](https://github.com/foxssake/netfox)
- [Photon Fusion Godot 当前预览说明](https://doc.photonengine.com/fusion-godot/v3-shared-authority/getting-started/quick-start-guide)
- [Photon 区域与中国区限制](https://doc.photonengine.com/realtime/v5/connection-and-authentication/regions)
- [ZeroTier relaying](https://docs.zerotier.com/faq/relaying/)、[TCP relay](https://docs.zerotier.com/relay/)

## 可复现网络实验

`scripts/net_proxy.py` 对真实 ENet UDP 数据包双向加入延迟、抖动、随机丢包和带宽队列，无需系统驱动或管理员权限。固定随机种子 729；OS 调度和包抵达顺序不同仍会使每次结果略有差异。

以下是**合成压力档位，不是对某国网络的实测或保证**。国内同区、海外同区可用 domestic 档作低延迟参考；中加连接用 cross-border 档作高延迟参考。

| 参数档 | 每向基础延迟 / 抖动 | 双向基础 RTT | 每向丢包 | 每向带宽 |
|---|---:|---:|---:|---:|
| lan | 2 ± 1 ms | 4 ms | 0% | 100 Mbps |
| domestic | 25 ± 8 ms | 50 ms | 0.5% | 8 Mbps |
| cross-border | 120 ± 35 ms | 240 ms | 2% | 4 Mbps |
| relay-stress | 180 ± 60 ms | 360 ms | 3% + 每 15 秒断流 200 ms | 2 Mbps |

```powershell
./scripts/build.ps1
./scripts/net_scenario.ps1 -Profile domestic -Rendered
./scripts/net_scenario.ps1 -Profile cross-border -Rendered
./scripts/net_scenario.ps1 -Profile relay-stress -Rendered
./scripts/net_scenario.ps1 -Profile cross-border -Players 4 -Port 18110
# 保留旧移动 / 相机 / 快照发送方式以作对照，不改变 A/B 产品玩法。
./scripts/net_scenario.ps1 -Profile cross-border -Legacy -Rendered
./scripts/verify_b.ps1 -Network
# 可对导出的 exe 执行完全相同的网络模拟
./scripts/net_scenario.ps1 -Profile cross-border -Rendered -Executable '完整路径/ProjectHairball-B.exe'
```

每次运行保存 host/client 日志、世界最终 hash、network 指标、代理丢包统计和 summary.json。新实现还要求看到所有测试触发的飞行 / 注意 / 扶头状态，行走采样至少 1000 次、反向位移比例不超过 5%、无超过 2 米的硬纠正。这是回归门槛，不是人类舒适度标准。

## 测量结果

旧协议对照保留了本轮密度缓存复用，属于旧同步算法对照，不是旧 ZIP 的逐字节重测。开发机运行渲染客户端，完整 B 一局；固定横向往返路径，最后 20 秒继续原有扶头 smoke 操作。反向采样表示有方向输入时，单个物理 tick 实际反向移动超过 2 mm；包含碰撞/接近顾客阶段，不是对所有逆向位移的原因分类。

| 运行 | 反向采样 / 行走采样 | >2m 硬纠正 | 最大纠正 | 渲染帧间隔 P95 / P99 |
|---|---:|---:|---:|---:|
| 旧同步 cross-border | 869 / 2730（31.83%）| 0 | 155.4 mm | 6.25 / 9.45 ms |
| 新同步 domestic | 0 / 2705 | 0 | 1.02 mm | 7.09 / 11.11 ms |
| 新同步 cross-border | 0 / 2731 | 0 | 10.64 mm | 6.06 / 8.33 ms |
| 新同步 relay-stress | 3 / 2810（0.107%）| 0 | 152.78 mm | 6.06 / 8.33 ms |

以上新运行的最终权威 hash 一致，所有规定中间提示均收到。新 cross-border 的世界 + 运动有效载荷 6,738,043 字节 / 109.24 秒，旧世界载荷 20,303,875 字节 / 112.43 秒，平均下行有效载荷减少约 66%（不含 ENet / UDP 开销和重传；代理另存实际数据报统计）。新输入冗余增加了一些上行流量，属于换取丢包容忍的明确取舍。

证据：`artifacts/net-cross-border-legacy-20260929-074418/`、`net-domestic-new-20260929-080011/`、`net-cross-border-new-20260929-080206/`。另有较早开发版本对照 `net-cross-border-new-20260929-074641/`，不与最后候选混称。新运行期间同机还执行了无界面的原有回归，因此帧耗时不是隔离硬件基准；朋友电脑的 GPU / CPU 表现未测。

新增真实 Godot 胶囊测试覆盖恢复后同帧批量重放：跳跃落地、撞墙、走真实梯级。最初梯级重放差 3.4 cm，发现 CharacterBody3D 保存了上一位置的地面接触法线；恢复时刷新地面接触后，三个场景原轨迹与重放一致。未放宽原定误差阈值。

relay-stress 也通过最终 hash、所有中间事实和纠正门槛，证据 `artifacts/net-relay-stress-new-20260929-080402/`。最大运动包间隔 333 ms、插值耗尽 25 个渲染帧，说明恶劣链路仍有停顿；它不是“完全无抖动”的证明。

原有回归：`scripts/verify_v06.ps1 -Network` 通过，索引 `artifacts/verify-v06-20260929-080011/`，底层 `verify-v04-20260929-080011/`。零编译警告/错误，139/139 核心测试，123 个引擎断言（其中新增 3 个恢复碰撞测试），原有本地化、实际取工具 walkthrough 和主机丢失检查；A 1/4 人、B 1/4 人、B 2 人完整时长、4 人中途掉线、B 房主断线回菜单均通过。存活客户端最终状态一致。

随后补充了渲染检查视角保护、A 的旧验证阶段不覆盖、远端头发转向和长局遥测窗口；这些同样进入最终导出包。

### 最终候选追加修正

初版优化包的 4 人共享 4 Mbps 测试 `net-cross-border-new-20260929-080904/` 虽然通过原门槛，一名客户端仍有一次 0.781 m 纠正。进一步检查发现可见角色插值在修改碰撞体。将两者分开，增加引擎断言验证画面插值不会改变碰撞体；随后 `net-cross-border-new-20260929-081303/` 三名客户端分别为 0 / 2766、0 / 2745、0 / 2936 次反向采样，最大纠正分别 6.21、0.80、2.33 mm。由于包到达顺序不同，这不是统计意义上对所有拥挤场景的保证。

该轮还记录到大数据传输期间运动消息间隔超过 1 秒，不能仅凭“没有回拉”忽略它。压缩对照使用 4 人初始世界、36 个重复回放帧的真实序列化结构（非随机不可压缩数据），热身后 7 次均值：

| 结构 | Deflate Fastest 大小 / 压缩时间 | Brotli Fastest 大小 / 压缩时间 |
|---|---:|---:|
| 世界（JSON 1.15 MB） | 44,458 B / 1.914 ms | 16,794 B / 0.682 ms |
| 回放（JSON 41.32 MB） | 1,574,619 B / 62.949 ms | 527,663 B / 19.355 ms |

证据 `artifacts/net-compression-check.log`，脚本 `artifacts/compression-check/`；此微基准不包含 JSON 编解码，动态编辑数据的收益会变化。按这个结果使用 Brotli Fastest，增加两种格式的头发数据完整性与压缩体积测试。开发临时 C# 输出最初被主项目自动纳入编译，已经明确排除 artifacts/**/*.cs，修复构建后重跑，不删除用户文件。

包验证器的 SDK 隔离环境曾泄漏到后续同进程脚本，使 Python 查找失败；现已在 finally 恢复环境，实际重跑输出 PACKAGE_ENVIRONMENT_RESTORE_OK。该失败不是游戏联网失败。

多进程同毫秒启动还暴露了遥测文件名冲突，造成旧候选房间启动失败。文件名现加入进程 ID，日志初始化 I/O 失败不会阻止游戏启动。旧候选的完整回归重跑曾因此中断，随后使用最终构建分别补跑受影响的 4 人、掉线和主机丢失检查，不把中断的整轮标为通过。

最后对回放单独测试 Brotli Optimal：上述重复帧微基准缩到 10,783 B，压缩约 78.23 ms；因此仅回放在后台线程编码，常规世界仍用 Fastest。实际动态回放的最终网络大小见最前表格。旧录制帧包含独立克隆的游戏数据；冻结列表后交给后台，不从后台读取/修改 Godot 场景，完成后回主线程发送。

## 实际中加复测

双方均换新包，继续用现有 ZeroTier 房间地址。先空手移动 / 转头 / 跳跃，再上梯子、围着顾客移动和共同使用工具。F3 可显示 RTT 和 FPS。若仍抖动，提供双方同一局的 network-*.jsonl 与录屏，区分低 FPS、尖峰帧、网络断流和位置修正。真实路由、朋友硬件性能及眩晕改善必须由这一步确认；自动测试不能替代。
