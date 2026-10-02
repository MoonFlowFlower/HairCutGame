# 当前主线的精简验收证据

2026-10-02，按用户“当前版本提交 Git 备份，再清理历史版本以节省硬盘空间”的要求，从原始 `artifacts/` 中保留这一组原文件。`manifest.json` 记录每个文件的原路径、大小和 SHA256；原始目录清理后，使用本目录查阅。未保存每一轮旧候选的源码副本、旧安装包及完整 PNG 动画序列。

- [最新第一人称 HUD 实拍](hud/mainline-hud.png)：已隐藏右上角自己的镜像与状态块。对应 DLL `45D354D569870D6DEFE05D2C0D95B966A50539FDDFAC4FA8D9E722869A46BF80`，编译及启动日志在 `hud/`。
- [Normal 近景](visual/normal-close.png)、[Trimmed](visual/trimmed.png)、[Carved](visual/carved.png)、[实际剪切](visual/native-cut.png)；[三态对照](visual/review/three-states.png)与[美术接入前后](visual/review/before-after-close.png)。十个镜头的 core/fur 原始 mask、GPU 样本和双向 silhouette 数据也在 `visual/`。
- [真实剪发回弹短视频](visual/cut-response.mp4)、[动态遥测](visual/motion.json)、[分析](visual/motion-analysis.json)。保留已按实际时间戳编码的 72 帧视频，删除重复的逐帧 PNG；这不等于仍可从无损原帧重算所有图像指标。
- `integration/` 保留 Core、引擎、世界与旧画面启动日志及源码差异审计。`network/` 和 `four-player/` 保留受控网络与四人完整局的实际数据。
- `backup/` 记录这次 Git 恢复检查、清理前后统计与删除清单；详见 [备份与清理记录](../../REPOSITORY_BACKUP_AND_CLEANUP.md)。

这里包含不同构建的证据，不能合并成同一个 DLL 的全套通过：美术视觉为 `07D254C0D4DE72616F80BCE8FDC46B94E434929D84B7FC2A6C87B58FE43D9E9E`；联网/世界候选为 `DFE35CDBA2D15073B57EA437AD2988BBF0A390F9139DF9083A7CC00CEAE1742B`；最后 HUD 调整为上面的 `45D354…`。细节及未通过项见 [美术接入报告](../../B_PUPPET_MAINLINE.md)。

GPU 样本存在其他桌面工作负载；四人测试只有一个渲染实例。旧 Compatibility 强制结束的启动检查有两条 ObjectDB 泄漏警告。首次加载、剪切尖峰、低角度硬尖/薄边和真实跨国线路验收仍有余项。备份与磁盘清理不改变这些原始结论。
