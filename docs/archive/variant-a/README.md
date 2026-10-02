# Variant A — 历史归档

2026-10-02 清理说明：用户要求将当前版本备份到 GitHub 后清理历史版本。下述旧 ZIP、旧包及恢复命令仅为历史记录，不再表示本地文件仍保留；当前 B 由 Git 备份，当前代码中的显式 A 入口仍在。此要求取代此前长期保留本地旧快照的安排，详见 `docs/REPOSITORY_BACKUP_AND_CLEANUP.md`。

2026-09-29 用户选择 B 为后续开发基线。A 停止作为并行开发对象；此处用于历史查阅和恢复，不是当前待办。

## 冻结快照

- 源码 ZIP：`artifacts/archive/ProjectHairball-VariantA-source-20260929.zip`（仓库根目录下）。
- SHA256：`067562375a10d720fddeae5e20aa49a73bcf8db9822babe8d08c0453a130afc3`。
- 286 个原文件，39,119,528 字节；附 `ARCHIVE_SOURCE_SHA256.json`。已检查 ZIP CRC 和逐文件 SHA256。
- 快照在本次整理之前生成，包含共享 C# 源码、场景、资源、构建配置、脚本、测试和原文档；排除构建缓存、运行产物与日志。它是源码归档，不是免安装可执行包。恢复仍需匹配的 Godot Mono/.NET 开发环境。
- A 与 B 共用底层系统，因此快照保留完整可构建项目，而非抽出一个无法单独运行的 A 目录。最新 B 交付 ZIP 和以前的包也保留在 `artifacts/packages/`。

## 回看与恢复

当前仓库可显式回看：`./scripts/run_ab.ps1 -Variant A`。A 专属网络回归是可选项：`./scripts/verify_v06.ps1 -Network -ArchivedA`。

若要求冻结版本的精确源码：将 ZIP 解压到一个新的目录，校验归档 SHA256 和内部文件清单；从该目录运行 `./scripts/build.ps1`，然后 `./scripts/run_ab.ps1 -Variant A`。不要把归档覆盖到当前 B 工作目录。快照使用 `GODOT_BIN` / 原 `env.ps1` 发现引擎。

## 历史资料

原实验定义、配对试玩协议与预注册阈值保存在快照内的 docs 31、34、35、36、37 及 `V06_VERIFICATION.md`。当前同名文档保留历史正文并标注状态。原 README 和 kickoff 也在快照根目录保留。

本次归档不是一次新的 A/B 实验，不声称 B 已通过原预注册胜出标准。当前开发规则见 [B 开发基线](../../42_B_DEVELOPMENT_BASELINE.md)。
