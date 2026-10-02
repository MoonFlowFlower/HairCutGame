PROJECT HAIRBALL — B 版朋友试玩包
Windows x64 · 1–4 人 · v0.6

开始游戏
1. 将 ZIP 完整解压到一个普通文件夹。
2. 双击 ProjectHairball-B.exe，选择“单人游戏”，或创建/加入房间。
不需要安装 Godot 或 .NET。请保留同目录的 .pck 和 data_ 文件夹，不要只发送 exe。
菜单可切换简体中文 / English。

游戏内语音 / In-game voice
默认自动语音激活，F8 打开设置；可选择输入设备、增益、噪声门、输出及单人音量/静音。
开启“按键说话”后按住 V 说话。建议戴耳机，避免回声。
没有声音时，检查 Windows 设置 → 隐私和安全性 → 麦克风 → 允许桌面应用访问。
游戏不更改系统权限，不保存任何音频。没有麦克风仍然可玩。
Voice activation is default. F8: device, gain, noise gate, output and per-player mute/volume.
Enable push-to-talk and hold V. Use headphones. Windows Settings → Privacy & security → Microphone → Allow desktop apps.
No audio is recorded. The game still works without a microphone.
本包附 B_THREE_ROUND_PROTOCOL.txt 和空白记录表，供真人三局观察使用。
自动多人脚本通过只证明机械流程；本包尚未取得真人合作乐趣验收。

这局要做什么
所有人围绕同一个顾客工作，用 E 从桌上拿起直升机，瞄准头发表面再按 E 放下。
150 秒施工。支撑跟随你选的位置和朝向，缺支撑一秒会滑落，可以重新捡起来。
试放不会自动通关。完工铃可提前起身；否则倒计时结束后顾客起身、照镜子、走向门口。
到门口时直升机仍在头上、有合格支撑才算完成。全程可跟着扶头或使用工具补救。
火灾忍耐始终可见，满了顾客会逃走；喷水可以救回来。普通事故只影响结算。
顾客会注意突然的声音和镜中的危险，并预告转头。
落脚区域需要足够的支撑、平整度和硬度；胶水或冷冻可以加固头发。
接地时标出实际有问题的区域；失败界面保留原因。结算与回放后，按 Enter 可再玩一局。
生发剂短按微调，持续按住逐渐加速并消耗本瓶材料；松手恢复，右键保持细调。

材料与协作
四瓶生长剂各自最多 1.0，容量可从瓶身和 HUD 看。空瓶不影响其他瓶。
椅旁两顶假发可用 E 当补丁。转移炮左键回收碎发（60%，焦发 30%），右键喷出储存材料。
模具放在后桌：扶在头发上由队友喷；锚钉可固定并释放持有者的主手。未扶／未钉会掉落。
剪刀、锚钉、修剪机、火焰可打洞，先填内部，再从洞向外长。偏心作品会歪头，扶头或配重可应对。
冻结队友 2 秒（敲一下解冻）、粘手 3 秒（热风右键或火焰解开）、头发着火最多 4 秒（喷水熄灭）。
始终可以转视角，时间到自动恢复；短暂免疫避免连续锁住。没有生命值，也不强制挪动镜头。
秘密任务默认开启，菜单由房主设置。只在本人 HUD 显示，Esc 后可收起；结算揭晓，不计分、不影响收益。
结算显示基本工钱、小费和最多六条事实记录。镜前照片保存在本机 gallery，店内照片墙保留最近 24 张。

操作
WASD 移动；鼠标看向；Space 跳跃。
E 拿起/放下工具或道具；G 短按丢下，按住 0.2–1 秒松开投掷；鼠标左键使用；右键使用副功能。
靠近并看向顾客头部，按住 E 暂时扶稳；扶头期间不能用工具。
身体或拿起的杂志可以挡住顾客与镜子之间的视线。
空手看向椅子控制处，左 / 右键旋转椅子；R / F 调节高度；搬动梯子后可走上梯级。
Esc 释放鼠标，显示说明及菜单。工具使用说明在左下角。
退出游戏可点击窗口关闭按钮或按 Alt+F4。

与朋友联机（最多 4 人，所有人使用这份相同的包）
这是 B 派对循环检查点（协议以 BUILD_INFO.json 为准）；房主和加入者都必须换成这一份包，不能与旧包混用。
核对本说明首行 BUILD 和 BUILD_INFO.json 的 build：协议号相同也可能是较旧的包。
房主：选择“创建房间”，默认端口 7777；等朋友都加入后按 Enter 开始。
朋友：在菜单地址栏填入房主的网络地址，端口保持一致，然后选择“加入”。
127.0.0.1 只用于同一台电脑测试，不是另一台电脑的房主地址。
支持同一局域网或已连通的虚拟局域网直连。异地公网直连需要房主的 UDP
端口可达（例如路由器转发 UDP 7777）；本版本没有自动匹配或联机中继。
若 Windows 询问网络权限，房主需允许游戏在用于联机的网络上通信。
连接失败先核对 IP、端口、网络及防火墙。不要关闭整个防火墙。

短暂断网与恢复
网络不稳会显示提示，随后自动重连；全队最多暂停等待 30 秒。
暂停会停住施工倒计时、直升机和顾客反应。房主可选择提前继续。
恢复后回到原房间和席位；请松开鼠标／E，再重新开始操作。
等待到期会显示重试入口，不会直接跳回主菜单。房主关闭游戏无法自动接管。
“离开房间”会请求确认；Esc 本身只释放鼠标，不会断开连接。
如果版本不匹配，请双方重新解压同一新包。此包仍需要可达的房主 IP。

反馈
请记下：哪里看不懂、觉得不公平的地方、最有趣的一次事故，以及愿不愿再玩。
卡住或失败难以理解时，录屏很有帮助。游戏本身不会录制麦克风或上传日志。
双击 Open_Test_Logs.cmd 可打开本机日志文件夹。
如果卡顿或抖动，按 F3 显示 FPS、RTT 和帧耗时，截取当时画面。
playtests 下的 network-join-*.jsonl / network-host-*.jsonl 记录网络和性能，
请双方都提供对应本局的文件；不用手动安装诊断工具。
跨国高延迟仍会影响工具结果反馈。上一份 playtest-r1 包做过延迟、抖动和丢包模拟；
本候选的检查范围以随包的三局说明及本机验证记录为准，不能当作实际路线验收。
实际加拿大—中国 ZeroTier 路由及不同电脑的表现还需双方复测。
ZeroTier 可能直连也可能中继；其客户端/CLI 的 peers 中 DIRECT/RELAY
可帮助排查路径。更换网络技术不能保证消除跨太平洋传播延迟。
playtests 子目录保存房主/单人局的判定时间线；logs 子目录是引擎日志。
请把相应录屏和最新的 v06-B-*.jsonl 一起发回来。
日志默认位于：%APPDATA%\Godot\app_userdata\Project Hairball

这是 B 派对循环原型，包含失败原因说明、移动预测、镜头及远端运动平滑修正，尚不是正式发布版本。
本包仅适用于 Windows x64；显卡需支持 OpenGL 3.3。

ENGLISH QUICK START
Extract the entire ZIP, then run ProjectHairball-B.exe. No Godot or .NET install
is needed. Keep the .pck and data_ folder beside the executable. Select English
in the menu if needed. Solo starts immediately; a host waits for friends and
presses Enter. Join using the host's reachable IP address and matching UDP port
(default 7777). LAN/VPN/direct IP only; there is no matchmaking or relay.
Pick up the table helicopter with E, aim at a hair surface and press E to place.
Try any position and orientation. Bad support slides off after one second; pick it up again.
Build for 150 seconds, or ring the completion bell to leave early. Trials never
end construction. Escort the customer past the mirror to the door with tools
or a close E brace. The helicopter must remain on supported hair at the door.
Visible fire tolerance can be rescued with water; at 100 the customer flees.
WASD/mouse: move/look. E: pick/place; hold near head to brace. G: drop.
Left/right mouse: tool actions. Space: jump. Esc: release mouse and show options.
Support must be broad, level and firm; glue or freezing reinforces soft hair.
Hold primary growth spray to accelerate and spend this bottle faster;
release to reset the pressure. Secondary growth remains fine control.
Read the live landing/failure explanation. Enter restarts after the round/replay.
Open_Test_Logs.cmd opens logs for sharing with the developer. No automatic uploads.
All players must use this updated build. Press F3 for FPS, RTT and frame timing.
Match the BUILD identifier in this README, even when both packages have the same protocol.
The included three-round notes and blank records are for human observation;
automated multiplayer checks do not establish cooperative fun.
Share both peers' network-*.jsonl from playtests when reporting stutter.

Each of four growth bottles has its own 1.0 supply. Wigs can patch the head;
vacuum recycles loose hair at 60% (charred 30%), RMB transfers stored hair.
Hold a mold against hair while a teammate sprays; nail it to free the holder.
Unsupported molds fall. Holes allow growth outside the filled cavity. Uneven mass leans the head.
Freeze lasts 2s: a teammate tap rescues. Sticky hands last 3s: hot blower RMB or flame rescues.
Hair fire lasts at most 4s: water rescues. Look remains free; short immunity prevents chaining.
Private secret tasks are optional, foldable and revealed at results, without score or pay.
Base pay + tips, six factual actions, local photos and the 24-photo wall complete the round.

Automatic recovery: the team waits up to 30 seconds with gameplay paused.
The host can continue early. On reconnection, the same seat and current scene
are restored. Release held action buttons before continuing. A failed recovery
shows Retry / Leave; it does not silently return to the menu. There is no host
migration. All peers must match BUILD_INFO.json. Include both logs/ and playtests/ when
sharing diagnostics; no credentials are recorded and nothing is uploaded.

Runtime licenses and third-party notices are in the licenses folder.

检查点 D 新操作 / Checkpoint D controls
空手时，飞过手旁的物件会自动接住。假发先 E 拿起，再 E 戴上。
倒地功能在设置中默认关闭。开启后被重物砸中可倒地，队友 E 拍醒；最长 4 秒，起身免疫 3 秒。
3–4 人局桌上有双人大剪刀、管线喷枪和大平台：剪刀两人同时按左键；罐子左键泵液、右键切换；喷头持有者瞄准，超过 2.5 米两件脱开。大平台单人慢拖、双人合抬。
梯上工具会轻微晃动，队友在梯旁按住 E 扶稳。施工数值辅助默认关闭，可在设置开启。
照片墙有改造前后、合影和各人头像；合影倒数时可以摆姿势。
Tap G to drop; hold 0.2–1 s then release to throw. Empty hands catch objects passing nearby.
Pick up a wig with E, then E to attach it. Empty-handed LMB/RMB at the chair control rotates it.
Downed bodies are opt-in in settings; E revives, maximum 4 s with 3 s immunity. FPS camera stays stable.
In 3–4P: two-handle scissors need synchronized LMB; tank LMB pumps, RMB cycles liquid; nozzle aims. Hose disconnects beyond 2.5 m. Lift the large platform together.
Hold E beside an occupied ladder to stabilize the worker's tool point. Numeric assistance is optional.
Before/after photos, group countdown and player portraits are saved locally.

阶段 11：2–4 人轮换一人做顾客，只有他能看目标卡（Tab）。左/右键点头/摇头，R 转头，E 拍开工具/拿道具。W 连按起身；理发师在扶头范围连按 E 按住。站立后正常走动，E 在椅子旁坐下；离店沿路线自己走，20 秒后就地判定。顾客语音自动变物种声音。揭晓默认 5 秒回赠一刀，接着 8 秒扔派（左键）；顾客右键狗屎、E 点赞，纯外观。
Phase 11: 2–4 players rotate one customer with a private target (Tab). LMB/RMB nod/shake, R turns head, E slaps tools/picks props. Tap W to stand; barbers tap E in brace range to oppose. Walk normally, E near chair to sit; follow exit route yourself, judged locally after 20s. Customer speech automatically uses species sounds. Reveal: 5s return cut by default, then 8s pies (LMB); customer RMB poop/E like, cosmetic only.


PARTY EXPANSION / 派对扩展 V–13 (checkpoints D/E/F)
Ordinary sessions have 3 rounds. Round 1 has no twist; one eligible card is drawn from round 2. Host can disable twists and the shop cat. Local flipped/covered/dark views and glued dragging default OFF; opt in from the menu.
普通场次 3 局。第 1 局没有变局卡，第 2 局起每局抽一张适用卡。房主可关闭变局卡和店猫。镜像/布罩/暗场视图以及粘住拖行默认关闭，可在菜单自行开启。
Reverse card: customer presses 1–4 to guess before reveal. Family card: the assigned relative can speak but cannot edit hair. Fog canvas: blow/spray the customer mirror, customer holds LMB to draw. Mini demonstration lasts 10 seconds with the assigned tool.
反转：顾客在揭晓前按 1–4 猜目标。家属可以说话，但不能做头发。雾镜作画：吹风或喷水到顾客镜子，顾客按住左键画线。迷你示范只能用指定工具，持续 10 秒。
Cat: 35% chance, at most 2 actions; 1.5 second warning. E carry it, blower shoo it, thrown light prop lure it. Recover stolen hair/props by carrying/shooing the cat. Hot blower (RMB) or flame frees glued hands; drag speed is capped at 0.8 m/s and never rotates the view.
店猫：35% 出现、最多出手 2 次、提前 1.5 秒示警。E 抱走、吹风机吹开、扔轻道具引走；猫会归还碎发和道具。热风（右键）或火焰解开粘手；拖行限速 0.8 米/秒，不旋转镜头。
Returning session option: same head over 3 or 4 rounds, small natural growth between rounds, new target and rotating human customer. Final view cycles through revealed targets and local photos in round order.
回头客选项：同一个头连续 3 或 4 局，局间少量自然生长、换目标卡和真人顾客。结束后按局序回放已揭晓的目标和本机照片。
Voice is never recorded. Render/gameplay/network checks do not establish real microphone listening quality, comfort, fun or the actual Canada–China route.
语音不会录制。自动检查不代替真人麦克风听感、舒适度、趣味性和真实加拿大—中国线路验收。
