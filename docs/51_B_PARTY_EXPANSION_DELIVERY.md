# B 派对扩展交付 — 2026-09-30

阶段 V、8–13 已实现，D/E/F 已打包；阶段 7 未实施。最新试玩包是 **checkpoint-f-c5，协议 25**。F-c5 只更新试玩说明和记录表，195 个游戏/运行时文件与通过检查的 F-c4 逐字节一致，详见 ZIP 旁的 `.repack.json`。最终 F-c5 ZIP 另做了完整独立解压验证。

## 现在能玩到什么

1–4 人围绕一颗共享头；单人使用 AI，2–4 人轮换一人做真人顾客。物理委托决定成功，风格只影响小费和反应。

- V：内置语音，自动激活或按住 V，F8 选设备、音量、静音；顾客走物种声音管线，不保存音频。
- 8：材料接触反馈、委托物下陷、放置提示、改造前后照片、合影和个人头像。
- 9：G 蓄力投掷与空手接、可选倒地和 E 拍醒、转椅子、二人喷枪、大剪刀、平台合抬与扶梯子。
- 10：可测量风格、30 张双语目标卡（含情绪和程度卡）、20 件比划道具、中文双关及独立英文表达；AI 用提示参与。
- 11：真人顾客私看目标、表达、拍开工具、起身对抗和自己离店；揭晓、回赠一刀、扔派和点赞。
- 12：16 张按人数、角色和设置筛选的变局卡。私密目标、反转选项和制作提示定向发送，揭晓前不进入广播快照。
- 13：店猫、胶水拖行、AI 困倦与惊醒，以及同一颗头连续 3–4 局的回头客场次和目标/照片顺序回放。

普通场次自动 3 局，第一局无变局卡，第二局起抽卡。视角类卡、倒地和胶水拖行默认关闭；房主可关闭猫和变局卡。程度卡按 owner 澄清保留单项区间，允许一道具覆盖它，其余道具有干扰读法；已写回 49 号需求。

实现补充初值：猫重量 2.5、拖行上限 0.8 m/s/最长 3 s、回头客局间自然生长 0.025 m。猫每局由房主独立按 35% 抽取，最多两次行动、提前 1.5 s 示警。

## 构建与运行

完整解压 ZIP，双击 `ProjectHairball-B/ProjectHairball-B.exe`。不用安装 Godot 或 .NET；保留 exe、pck 和 data 文件夹。所有朋友用同一份 F-c5。源码在仓库根目录用 PowerShell 7：

```powershell
& scripts/build.ps1
& scripts/run_b.ps1
& scripts/run_b.ps1 -Mode Host -Port 7777
& scripts/run_b.ps1 -Mode Join -Address 房主地址 -Port 7777
& scripts/run_4p_local.ps1
& scripts/verify_b.ps1
```

顾客 Tab 私看目标，左/右键点头/摇头，R 转头，E 拍开/拿道具，W 连按起身；理发师在扶头范围连按 E 对抗。站起后正常移动，离店超时 20 s 就地判定。具体操作及真人观察方案在包内 READ_ME_FIRST、B_THREE_ROUND_PROTOCOL 和空白 CSV。

## 试玩包与 SHA256

位于 `artifacts/packages/`；D/E 是开发检查点，朋友试玩使用 F。

| 检查点 | ZIP | SHA256 |
|---|---|---|
| D | ProjectHairball-B-Windows-20260930-checkpoint-d-c2.zip | D356B29C9BF9F47303507AC2F2326776DED489835ED3E11261D5A6FD76AE2FBA |
| E | ProjectHairball-B-Windows-20260930-checkpoint-e-c5.zip | 248E85C23D3173A8C7978C65DBC12ED6D108D479C115334A54CBF80B6F851ECD |
| F | ProjectHairball-B-Windows-20260930-checkpoint-f-c5.zip | B329F4CD11B503487EF74E7C031DD5800D27C8AE77ADCA52EED90BC5FD360A96 |

## 实际验证

D：Core 203/203、verify_b、原生真实输入、双语截图、2P/带种子劣化 4P 协作事故与隐私/最终哈希、独立解压运行通过。E：Core 221/221、verify_b、原生真人操作/稳定视角、真实 WASAPI 播放消费的 2P 和劣化 4P、独立解压运行通过。分阶段详情和保留的失败见 PROJECT_STATE。

最终可执行产物的检查：

| 检查 | 实际结果与证据 |
|---|---|
| 编译、Core、完整主线回归 | 243/243，`verify-v06-20260930-193515` 通过；包含共享系统、访问/跳跃/物理楼梯、工具、双语、正常走动、房主失联 |
| 冻结包原生 | `f-c4-native-0`：16 卡双语渲染及权威/镜头不变；`f-c4-native-1..3`：真实 E 抱猫、重量、物理帧拖行/关闭设置、AI 惊醒、三局实际照片与目标回放；三次退出 0、无错误；截图已检查 |
| 反转卡 2P + WASAPI | `package-smoke-2-v06-B-20260930-193515-574`，38.607 s，私密发牌、声音播放消费、最终哈希通过 |
| 回头客+猫+WASAPI 2P/4P | `package-smoke-2-v06-B-20260930-193636-063`（107.390 s）和 `package-smoke-4-v06-B-20260930-193636-063`（111.656 s），三局角色/私密目标/历史、猫行动、真实生发、最终哈希通过；跨局材质保留/生长另由 Core 定量检查 |
| F-c5 家属卡+猫+WASAPI 跨境劣化 4P | `net-cross-border-new-20260930-194328-257-20010-85928`，所有进程退出 0；家属零修改、有资格理发师实际生发、私密卡发给正确玩家、最终哈希一致。三客户端校正 P95=0、强制校正=0、反向步数=0；头部最大速度 2.445–3.117 m/s；运动间隔 P95=100.02 ms |
| 语音劣化数据 | 上述 12 条流平均估计延迟 220.3–341.8 ms，最大播放丢弃 0.030%；生成输入走真实 WASAPI 混音。UDP：种子 729、延迟 120 ms、抖动 35 ms、丢包 2%、4 Mbps |
| 最终 F-c5 独立解压 | `package-check-20260930-194028-198/verification.json`：逐文件 SHA256、中文/空格路径、隐藏系统 .NET、实际包内 CLR、菜单截图/默认 B 引擎检查、150 s 施工完整 2P 局与最终哈希通过 |

上述远端检查保留默认关闭拖行；拖行的速度/热风解开有 Core 与原生物理帧证据，网络字段有编码检查，不宣称远端拖行舒适度通过。

复跑主要命令：

```powershell
$exe = 'artifacts/packages/ProjectHairball-B-Windows-20260930-checkpoint-f-c5/ProjectHairball-B/ProjectHairball-B.exe'
& scripts/smoke.ps1 -Executable $exe -Players 2 -HumanCustomer -Twist Reverse -Voice -RealAudio -Port 19940
& scripts/net_scenario.ps1 -Executable $exe -Players 4 -HumanCustomer -Twist Family -Cat -Voice -RealAudio -Profile cross-border -Port 20010
& scripts/smoke.ps1 -Executable $exe -Players 2 -HumanCustomer -ReturningRounds 3 -Cat -Voice -RealAudio -Port 19980
& scripts/smoke.ps1 -Executable $exe -Players 4 -HumanCustomer -ReturningRounds 3 -Cat -Voice -RealAudio -Port 19960
& scripts/verify_b_package.ps1 -Archive artifacts/packages/ProjectHairball-B-Windows-20260930-checkpoint-f-c5.zip -Port 19990
```

## 保留的失败与验收缺口

- 早期阶段 12 自动开局绕过变局入口，修正为统一入口并强制检查实际卡种。家属生发测试现在由有资格理发师执行，保持原生发门槛。猫的检查改为行动次数，结算后离场不等于没出现。
- UDP 握手会改变进程标签对应的席位顺序；检查脚本已按实际 actor/customer/family ID 判断私密收件人，不再把第一个客户端当成顾客。曾按错身份失败的原始报告保留。
- F-c3 一次原生检查完成所有玩法断言后在 GodotObject.Finalize 退出异常，同包重试正常。退出前增加引擎仍存活时的回收、等待终结器与另一段延迟；源码连续 5 次、冻结产物连续 3 次正常退出，其他完整联机退出也通过。历史[同类引擎报告](https://github.com/godotengine/godot/issues/83247)仅供类比，本次根因未获得符号化证明，不能称为彻底消除。
- F-c4 一次头部最大速度 5.84 m/s 未通过原运动门槛；三端观察到同一权威峰值，原因未定位。控制器的 0.28m 物理台阶上移可能有关，这是推测。保留原始报告，没有放宽门槛；最终 F-c5 独立劣化运行满足原门槛，不证明先前峰值已解决。
- 真人麦克风、物种声可辨识且词句不可理解的听感、真实加拿大—中国线路、舒适度和合作趣味性均未验收。自动截图、生成语音和脚本成功不能代替这些。

## 下次真人试玩

先做 2P 和 4P 各一个完整三局场次：唯一理发师是否忙得过来、是否有人主动要当顾客、双人剪刀在延迟下能否配合、误解揭晓是否觉得冤。再观察变局是否改变做法、猫让人笑还是烦、回头客回放是否愿意看。特别留意起身、走台阶和退出时的异常。视角效果和拖行逐项开启后检查舒适度；用包内空白表记录实际事件。

源码快照：`artifacts/b-party-expansion-final-20260930-fc5-source.zip`，SHA256 `54D73C246C7799E39798330078F2E35AABE3BB79B3C98C46D55F99EC58A84D96`（451 文件，39,405,812 字节，生成缓存和测试产物另存）。
