当前 B 已接入 Puppet 体积毛发、剪发回弹、布偶人物/手和环境材质，并隐藏右上角自己的镜像 HUD。协议 25。当前可构建源码已备份到 [GitHub](https://github.com/MoonFlowFlower/HairCutGame)；旧试玩包和重复实验产物已按用户要求清理，共 49.915 GiB。[美术接入记录](docs/B_PUPPET_MAINLINE.md) · [精简实拍与验收证据](docs/evidence/current-mainline/README.md) · [Git 备份与清理记录](docs/REPOSITORY_BACKUP_AND_CLEANUP.md)。

# Project Hairball — B 开发主线

Godot 4.7.2 stable Mono / C#，1–4 人第一人称合作。**从 2026-09-29 起，后续开发全部基于 B；A 已收纳为历史对照。** 当前规则见 [B 开发基线](docs/42_B_DEVELOPMENT_BASELINE.md)，最新交付与验证见 [PROJECT_STATE](PROJECT_STATE.md)。

大家围着同一位能看、能听、会反应的顾客施工。用 E 把桌上的直升机手放到头发上，试放、滑落、修补；最终护送起身的顾客到门口检验。玩家可以分散注意力、挡镜子或暂时扶头。结果由实际支撑与稳定性决定，失败会指出原因。

## 构建和启动

在仓库根目录使用 PowerShell 7：

```powershell
./scripts/build.ps1
./scripts/run_b.ps1
# 房主等朋友到齐后按 Enter 开始：
./scripts/run_b.ps1 -Mode Host -Port 7777
./scripts/run_b.ps1 -Mode Join -Address 127.0.0.1 -Port 7777
./scripts/run_4p_local.ps1
```

另一台电脑用房主地址替换 `127.0.0.1`。当前朋友测试继续使用 ENet / ZeroTier 和 UDP 7777，没有接入 Steam。`GODOT_BIN` 可指定 Godot Mono 控制台程序。旧的 `run_solo` / `run_host` / `run_client` 启动脚本也默认 B。

编辑器 F5 / 默认菜单及 B 启动脚本使用当前 B 美术；自定义命令行启动显式传入 `--v06-variant-b`。主线渲染需要 Forward+；`./scripts/run_b.ps1 -LegacyArt` 可回看旧表现。底层旧 QA 与实验室的未指定变体语义仍保留。

## 操作与体验

- WASD / 鼠标移动和看向，Space 跳跃。
- E 拿取／交换工具、搬运／放置物件；靠近并对准顾客头部时按住 E 扶头。G 放下，LMB / RMB 使用工具。
- 一次持有一件主工具，工具有物理后果；复杂性来自协作、顾客与世界反应。
- B 使用结果海报，不以技术三视图、目标描摹或实时形状百分比为目标。四瓶生长剂各自计量；假发和回收碎发可用于补救。
- 当前朋友测试局有 150 秒施工。从桌上拿放委托物；试放不结束施工，缺支撑一秒会滑落。完工铃或倒计时触发顾客起身、照镜子、走到门口，物件仍有支撑才完成。
- 生长剂左键短按起步更快，持续命中会逐步加速，增长与喷流一起增强并消耗本瓶材料；松手重置，右键保留慢速细喷。试放提示实际缺支撑、软或不平的部位。
- 模具可由队友扶着喷，锚钉固定，打洞后可向外长；偏心作品会缓慢歪头，扶头能减轻晃动。
- 队友冻结 2 秒、粘手 3 秒、着火最多 4 秒；敲击、热风／火焰、喷水可分别救助。三种事故不强制挪动镜头。
- 秘密任务默认开、可在菜单关闭，只私发给本人，结算揭晓，不计分；基本工钱加小费，照片保存在本机并展示在店内墙上。
- Esc 显示光标和选项，可选简体中文 / English；F3 显示网络／性能信息。离开房间有确认。
- 短暂失联时提示并自动恢复；房主最多暂停全队 30 秒，也可以提前继续。施工计时和玩法随暂停冻结，网络／界面继续；同步完成后回到原局。

开发实验室保留：`./scripts/run_solo.ps1 -Lab`、`-Materials`、`-Laundry`。它们是工具或历史夹具，不是新的产品主线。

## 检查、打包与朋友试玩

```powershell
./scripts/test.ps1
./scripts/verify_b.ps1 -Network
./scripts/package_b.ps1 -BuildId 新的唯一标识
```

[网络恢复测试说明](docs/41_NETWORK_RECOVERY_AND_TESTS.md)包含故障注入、长测及导出包验证命令；[验证报告](NETWORK_RECOVERY_VERIFICATION.md)区分实际执行的测试与真人验收。

旧检查点包名、SHA256 和实际验证记录保留在历史文档中，当前源码不等于已重新导出的免安装包。需要分发时用上面的打包脚本生成新包，所有玩家使用同一 BUILD；当前协议 25，不能与旧协议包混用。

加拿大—中国真实线路的 3 局和约 5 秒短断网验收仍待真人完成；恢复成功不等于消除全部眩晕。后续试玩聚焦 B 的可读性、功能验证、协作与舒适度，不再默认组织 A/B 对照。

## 历史归档

[A 归档索引](docs/archive/variant-a/README.md)保留历史文档和旧校验记录。旧源码 ZIP 与历史包按本轮用户要求清理；当前代码仍保留明确的 A 入口和共享系统。旧 A/B 预注册阈值与文字结论仍可查阅，不把用户选择 B 误写成实验已经证明 B 胜出。后续可恢复版本使用 Git 提交和标签保存。
