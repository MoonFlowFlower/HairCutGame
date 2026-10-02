# B2 — UE 5.8 原生渲染对照（实验已结束）

2026-10-01。Owner 提出是否使用 UE 5.8 实现一次，判断换引擎是否更容易达到 Puppet 概念图。本轮建立独立 UE 工程并实际渲染、剪切，没有迁移正式 B。

**同日 Owner 查看后决定继续 Godot，并要求清理 UE 项目。** 工程、缓存、导出中间文件、辅助脚本及启动入口共475个文件、2.824GiB已移入回收站；截图、日志和本报告保留。以下渲染与剪切结果是历史实验记录，UE 小样已不能直接启动。清理记录：`artifacts/b2-ue58/cleanup-result.json`。

## 路线判断

**Owner 已决定不迁移正式 B，继续 Godot。** 这次 UE 候选没有达到概念图，也没有显示出足够的画面优势来承担重写玩法、网络和工具系统的成本。截图仍可复查；这是此次候选的结论，不是 UE 的能力上限。

这两套画面共同留下的主要差距是发束轮廓、真实纤维的组织方式、脸部结构和角色表演。下一轮最高价值工作是在 Godot B2 内提高一颗主角头的资产质量。正式 B 和独立 Godot B2 均保留。

UE 的现成 Cloth、Lumen、Virtual Shadow Maps 确实提供了完整的工具，但动态可剪几何需要单独设计。Epic 当前文档明确列出 DynamicMesh 的 Mesh Distance Field/Nanite 限制，不能把静态资产的 Lumen 展示直接算作可剪头发的能力。[Geometry Scripting](https://dev.epicgames.com/documentation/en-us/unreal-engine/geometry-scripting-users-guide-in-unreal-engine)、[Lumen 技术说明](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-technical-details-in-unreal-engine)、[材质模型](https://dev.epicgames.com/documentation/en-us/unreal-engine/shading-models-in-unreal-engine)。

## 实验内容（历史记录）

- 原工程路径：`artifacts/b2-ue58/PuppetUE58/PuppetUE58.uproject`，原地图 `/Game/Puppet/Maps/PuppetLookdev`，现已清理。实验使用 UE **5.8.2 / CL56702186**。
- 从 Godot R6 的运行时场景导出 **501 个原始网格、64 种材质**，按材质/用途合并为 **76 个网格**；共 **2,054,262 个三角形**。大型墙、地板、顶面保持分离，尺度经三轴标记校准为 100 cm/m。
- 保留同一顾客、同一发型、28,004 根原始细毛、椅子、布景和第一人称工具。没有用另一个漂亮静态 Groom 替代可剪几何。
- 固定 Beauty、FPS、近景三机位。Godot 的垂直 FOV 换算为 UE 的水平 FOV，画幅 1280×800。FPS 保持 73° 垂直 FOV、1.7 m 眼高；两边 FPS 均无景深。
- UE 使用软件 Lumen、Virtual Shadow Maps、面积灯、固定曝光；普通 PBR 与 UE Cloth/扫描法线及高度颜色变化可对照。没有改 UE 引擎源码，没有启用硬件光追或 Nanite。
- 原始帧位于 `artifacts/b2-ue58/selected-final`。`pbr-*` / `native-*` 分别为两套 UE 材质。Godot 对照保留在 `artifacts/b2-puppet/round6-selected`。

清理前，这是一份编辑器中的独立视觉场景，可以查看模型/灯光/材质、执行脚本网格切割；未实现 UE LMB 工具玩法、多人同步、density 真值移植或五状态演出。

## 真实截图的判断

当前 UE 布料已有表面变化，面积灯能建立体积；但毛发边缘黑点、接触区域颗粒、帽沿般的发束和简化的脸部结构仍明显。不能用“已接入 Lumen/Cloth”认定质感达标。

Godot Beauty 带轻微背景景深，UE 本轮 Beauty 没有景深，所以不能仅凭背景虚化比较渲染能力。优先比较同机位 FPS 和近景。材质是重建而非跨引擎像素等价移植，照明单位也不同；本轮不是严格的纯渲染器基准。

第一轮过曝、导出时取到眨眼中间帧的画面已否决。导出现在冻结场景过程，曝光固定为 -5。高分辨率截图和普通持续运行视口都出现过颗粒，因此“全是高分辨率截图造成”这一猜测不成立。额外关闭近距离 AO/屏幕追踪、增加软阴影采样、强制 TAA/AA 的小样也没有消除问题；根因尚未确定，相关目录保留。

旧 `selected` 没有启用精确相机视图，位置正确但实际视口 FOV 错误，不能用作同机位比较。`selected-exact` 又发现 Slate 回调重入，截取任务可能交叠，也作废。最终 `selected-final` 启用精确相机和重入保护；六帧逐帧读回视口 FOV，Beauty 为 64.4429°、FPS/近景为 99.6285°，与配置相符。旧图保留，不混入最终拼图。

最终普通视口帧由项目内小型 Editor 模块 `PuppetCapture` 读取，持续预热后直接保存 framebuffer。它仅设置视口尺寸、刷新和请求截图，不改照明/材质，也不做离线渲染或后期修图。对照拼图只缩放、排版和加标签。

## 实际剪切探针

`artifacts/b2-ue58/cut-probe-exact/before.png`、`after.png` 来自 UE 的 DynamicMesh。导入静态网格后，Geometry Scripting 在实际几何上执行 Z=221.85 cm 的平面剪切；主体封口，细毛剪去越过平面的部分。

| 部分 | 切前三角形 | 切后三角形 | 单次剪切调用 |
|---|---:|---:|---:|
| 密度生成的主体网格 | 19,424 | 12,048 | 4.63 ms |
| 细毛网格 | 784,112 | 373,843 | 122.84 ms |

这次顺序调用合计约 **127 ms**。初始化复制主体约 50.48 ms、细毛约 1,718.57 ms，属于初始化成本，不能混进每次剪切。以上是编辑器 Python 调用原生几何函数的单次 CPU 墙钟样本，不是成品帧率、GPU 延迟、重复压力测试，也不是和 Godot 不同切割范围的公平速度对比。

修复启动脚本重复读取旧图片的风险后，新目录复跑得到 4.86 + 143.35 ms，完整 before/after 图和三角形变化一致；但此进程在写完图片、完成日志关闭后退出码为 `-1073741819`（0xC0000005）。记录保留在 `cut-probe-20261001-130929-848-50712*`，不能算自动化通过。前一条正常退出和这条失败均保留，不能用一次退出 0 宣称稳定性修复。

本探针证明 UE 能修改这套真实网格。它**没有**重建切面短绒，没有更新原有 density field，没有后台调度/连续剪切，也没有验证动态几何完整的间接光照。因此当前直接使用 GeometryScript 剪全部细毛的做法不适合直接投入实时玩法。

## 清理后的运行入口

UE 工程与专用启动器已移入回收站。正式 B 仍使用 `scripts/run_b.ps1`；Godot B2 使用 Windows 自带 PowerShell 即可运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\Project\Game\Godot\project_hairball\scripts\puppet_lab.ps1" -Round 6 -Interactive
```

## 验证边界

Editor 模块实际编译成功。最新资产生成运行成功，保留 **33 条 FBX 切线/副切线警告**：原程序网格部分没有常规 UV，UE 定制材质采用世界空间法线；这些警告仍需在正式资产制作时清理。不要把这写成零警告。

最终六张实拍和 `cut-probe-exact` 输出已生成并审图；原生进程记录均为退出 0（`selected-final-process.json`、`cut-exact-process.json`），未出现 Python 异常。启动入口使用 `Start-Process -PassThru -Wait` 记录真正的原生退出码，并检查所有预期图片；每次使用毫秒时间/PID 的独立目录，避免旧图片伪通过。上述复跑仍暴露原生退出访问冲突，根因未确定，自动化稳定性尚未通过；不将早期 PowerShell 返回 1 简单归因于重定向。

尚无打包游戏、真实硬件帧率对照、WAN/语音/多人测试或 Owner 审美验收。本轮没有重新运行 Godot Core 测试；旧 R6 测试结果不算作本轮 UE 验证。

正式 B 的 187 个受保护原路径哈希保持不变，记录为 `artifacts/b2-ue58/production-invariance.json`。清理后另核对 Godot B2 启动器、DLL、UE 引擎执行文件/版本文件及125个保留证据文件，哈希均未变。未改 Godot C# 源码、默认渲染器、规则、网络、发布包或 UE 引擎安装。

`selected-evidence.json` 的原始资源哈希仍作为历史记录保留，其中列出的 UE 工程资源、导出文件和源代码快照现已移入回收站，不代表当前存在可运行工程。没有清空回收站，因此2.824GiB是从工作目录移走的文件量，不是已释放的磁盘空间。
