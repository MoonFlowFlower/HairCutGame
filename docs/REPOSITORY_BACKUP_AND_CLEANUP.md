# Git 备份与历史产物清理

2026-10-02 用户明确要求：把当前版本提交到 [MoonFlowFlower/HairCutGame](https://github.com/MoonFlowFlower/HairCutGame)，然后清理历史版本和不需要的版本文件以节省硬盘空间。

## 备份边界

当前项目原本没有 `.git` 历史，目标远程仓库没有任何 refs。这次从当前 B 主线建立版本记录，包含当前源码、shader、场景、项目配置、脚本、测试、文档、第三方许可与运行所需资源。`assets/b_mainline_fur` 的四个资源全部纳入 Git。另保留约 17.16 MiB 的[精简实拍与原始验证数据](evidence/current-mainline/README.md)。

Git 忽略 `.godot`、`bin/obj`、`artifacts`、本地环境/凭据文件和临时文件。当前可玩版本可以重新构建；尚未重新制作可分发免安装包。

本次不把所有旧实验源码、失败候选和历史安装包上传到 Git。用户当前清理请求取代此前文档中“在本地永久保留旧 ZIP / 全部中间候选”的要求。清理后的旧文档路径是历史出处，不保证文件仍存在；保留下来的文字报告和哈希不等于完整旧版本可以恢复。

## 执行顺序与状态

1. 已完成只读盘点：项目 116,509 个文件、53,891,854,502 字节（50.191 GiB）；没有发现 reparse point。详见 `evidence/current-mainline/backup/original-inventory.json`。
2. 当前主线与精简证据已推送 `main`：`5ad03a6b52798d9f00b5ddca30887e597ba09a87`，远程回读一致。784 个文件约 152.72 MiB，最大文件 37.59 MiB，无需 Git LFS；没有把构建缓存、凭据和历史产物提交到 Git。
3. 已从 GitHub 全新克隆该提交：784 个跟踪文件逐字节一致，101 个保留证据文件的 SHA256 全部匹配。`scripts/build.ps1` 编译/导入成功（0 错误、2 个既有可空警告），`scripts/test.ps1` 为 248/248。真实 Forward+ B 单人启动、首人称截图和正常退出通过，stderr 为空。截图 `evidence/current-mainline/backup/restored-mainline.png`。此项证明备份可恢复启动，不替代新的全玩法/联机或性能验收。
4. 仅清理本项目 `artifacts/` 中旧包、导出副本、重复源码快照、逐帧截图、实验下载/解压文件和临时工具。清理前验证每个绝对路径位于项目内且没有链接跳转，保留当前运行缓存和实际资源。
5. 清理后再次检查当前入口与文件完整性，提交并推送实际清理结果。

当前状态：**远程备份和恢复检查完成，历史产物清理待执行**。后续实际结果会在同一文档更新，不将计划记为已完成。 `.gitattributes` 保留文件字节，避免 Windows 换行转换破坏原始证据哈希；`docs/evidence/.gdignore` 避免验收图片被作为游戏资源导入。

## 保留与恢复

当前正式 B 资源和第三方许可证保留；Godot 引擎及已安装的 export templates 位于项目之外，不在清理范围。用户 Downloads 下的原始 ZIP、参考视频和其他项目也不在清理范围。

历史比较脚本 `compare_puppet_hair.py`、`compare_visual_target.py`、`compare_puppet.py` 依赖旧资料包解压目录；`prepare_target_assets.py` 依赖原始素材下载和 Blender。这些是可选历史工具，清理后要使用它们，需要按脚本/许可记录重新提供输入。当前主线加载 `assets/` 内的已处理资源，不依赖这些下载目录。

恢复当前主线需要 Git、.NET 9 SDK 和 Godot 4.7.2 Mono；可设置 `GODOT_BIN` 指向 Godot Mono 控制台程序，然后运行：

```powershell
git clone https://github.com/MoonFlowFlower/HairCutGame.git
Set-Location HairCutGame
./scripts/build.ps1
./scripts/run_b.ps1
```
