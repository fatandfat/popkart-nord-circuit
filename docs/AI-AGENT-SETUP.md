# AI agent 配置规程

目标：在用户的 Windows 电脑上安装并准备使用本仓库发布的 P5136 纽北 V10、1.5 倍离线补丁。按以下顺序执行；不要把源码 ZIP 当作补丁，也不要跳过版本校验。

## 固定输入

- 仓库：`https://github.com/fatandfat/popkart-nord-circuit`
- 原版客户端页面：`https://www.mediafire.com/file/4vz8t1k0sra1pb3/KartRider_5136.zip/file`
- 原版 ZIP：`KartRider_5136.zip`，`3157308012` 字节。
- 原版 ZIP SHA-256：`3c532cab31fe4226ade999c52331c364144c0e09003098150ab0c49e4de3da6e`。
- 补丁直链：`https://github.com/fatandfat/popkart-nord-circuit/releases/download/v1.0.0/Nordschleife-P5136-Final-1.5x-v1.zip`
- 补丁 ZIP：`155875922` 字节；SHA-256：`a0f182a058ce663e1b34ede6ad28b1f7c60c56ce4c100b7d41d7f17ae9a4fb07`。
- 机器可读的其他摘要：[checksums.json](../checksums.json)。人类操作说明：[安装与使用指南](安装与使用指南.md)。

## 阶段 A：准备环境与下载

1. 确认是 Windows 10/11 64 位，存在 `%WINDIR%\SysWOW64\WindowsPowerShell\v1.0\powershell.exe`。不需要安装 Python、Node 或新版本 PowerShell。
2. 优先使用用户已提供的客户端 ZIP 或根目录；没有时再下载。安装目录必须在原客户端目录外，使用普通可写目录，不覆盖现有文件。已有完整客户端需额外约 4.5 GB；从零开始并保留全部副本建议 12 GB 空间。
3. 若要下载原版客户端，通过浏览器/联网工具打开稳定 MediaFire 页面，从实际 `KartRider_5136.zip` 下载按钮取得本次链接。不要硬编码临时下载 CDN URL，也不要将页面 HTML 保存成 ZIP。命令行可能收到 403，遇到验证应交给用户完成或正常使用可访问的浏览器下载，不绕过验证。
4. 取得实际下载链接后，可用系统 `curl.exe --fail --location --retry 3 --output <路径> <本次真实ZIP链接>` 下载。URL 和文件路径应分别作为参数，不拼接不可信 shell 文本。下载结束后必须校验字节数和 SHA-256。
5. 原版 ZIP 解压后，定位直接含 `KartRider.exe` 的目录。确认 EXE SHA-256 为 `629f084e2a12c6fa1ff0ea603b90f8768454d13a1bc2df6a8504f8aa06fd6194`。完整 ZIP 的摘要是本项目已验证样本的本地指纹，不是发布方签名。

PowerShell 校验模板：

```powershell
$clientZip = 'D:\Downloads\KartRider_5136.zip'
if ((Get-Item -LiteralPath $clientZip).Length -ne 3157308012) { throw 'Client ZIP size mismatch' }
if ((Get-FileHash -LiteralPath $clientZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '3c532cab31fe4226ade999c52331c364144c0e09003098150ab0c49e4de3da6e') { throw 'Client ZIP hash mismatch' }
Expand-Archive -LiteralPath $clientZip -DestinationPath 'D:\Games\OriginalP5136'
```

解压目的地必须是新目录。不要读取、复制或修改任何 `.zcode` 内容。

## 阶段 B：取得补丁与无人值守安装

从固定 Release URL 下载补丁 ZIP，按上面的字节数和 SHA-256 校验，然后解压到新目录。下面假设实际路径为：

```powershell
$package = 'D:\Games\NordPatch\Nordschleife-P5136-Final-1.5x-v1'
$client = 'D:\Games\OriginalP5136\KartRider_5136'
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $package 'launcher\Install.ps1') `
    -ClientPath $client -NonInteractive
if ($LASTEXITCODE -ne 0) { throw 'Patch installation failed; inspect the complete message' }
```

也可以使用仓库的 [`scripts/Setup.ps1`](../scripts/Setup.ps1) 自动下载、校验和解压补丁后调用同一个安装器：

```powershell
# 运行前阅读脚本；该脚本不下载或代替选择原版客户端。
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass `
    -File 'D:\Source\popkart-nord-circuit\scripts\Setup.ps1' `
    -ClientDirectory $client -Destination 'D:\Games\NordPatch'
```

安装器会复制本项目已验证 ZIP 文件清单中的所需文件，使用干净默认配置，不复制用户的 `Profile`，并生成 `app/installed.json`。成功前不要启动游戏。已安装目录会做身份检查并直接返回；半安装目录不会被自动清空或覆盖。

## 阶段 C：安装后验证，不启动游戏

使用**32 位 Windows PowerShell**：

```powershell
$ps32 = Join-Path $env:WINDIR 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'
& $ps32 -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'launcher\Play.ps1') -VerifyOnly
if ($LASTEXITCODE -ne 0) { throw 'Package/client validation failed' }
& $ps32 -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'launcher\Play.ps1') -ServerCheckOnly
if ($LASTEXITCODE -ne 0) { throw 'Local server/dry-run validation failed' }
```

`VerifyOnly` 校验文件并编译补丁代码，不读写游戏进程。`ServerCheckOnly` 启动这个包自己的仅回环服务、验证连接可达并生成连接器 dry-run 计划，然后关闭该测试服务；不启动游戏、不安装进程补丁。若已有相关端口占用，报告并交由用户处理，不能结束未知进程。

分享包使用基准端口 `49311`：UDP 49311/49312，TCP 49312/49313/49314；地址为 `127.0.0.1`。无需开放外网端口，不要自动更改防火墙、路由器或系统策略。

## 阶段 D：真实启动与用户操作

默认把安装成功后的入口和以下步骤交给用户；不要替用户连续启动游戏或自动驾驶。若用户明确要求启动，可打开 `2-Play.cmd`，仍需用户完成 UAC 和大厅时机确认。

1. 正常退出已经打开的跑跑卡丁车。
2. 双击 `<package>\2-Play.cmd` 并同意 UAC。
3. 等大厅或“我的房间”显示，先不要开始比赛。
4. Alt+Tab 到启动窗口按回车；看到“准备完成”才可以开始比赛。
5. 选择 **`빌리지 고가의 질주`**，正向。昵称 `NordPlayer` 自动设置，无需输入韩文。
6. 保持启动窗口运行（可以最小化），正常退出游戏后服务自动关闭。

这一步每次启动都必须做，不能用直接双击内部 EXE 代替。启动器只给自己启动、路径和 EXE 摘要都匹配的进程启用范围补丁；不修改磁盘 EXE。

## 失败处理与最终交付

- 摘要不匹配：停止，报告实际文件/大小/摘要和期望值，不修改校验清单。
- 下载验证或 UAC：明确告诉用户要做哪一步，再等待；不能把等待时间当作授权。
- 半安装目录：保留失败内容，选择新的空目录重试；不要递归删除用户目录。
- 启动错误：保留 `app/user/logs` 最新目录。不得用猜测填补加载、驾驶、计圈或视觉验收。
- 向用户报告：安装位置、`2-Play.cmd` 完整路径、两个检查是否通过，以及尚未验证的真实游戏步骤。仅 dry-run 通过时不能说“游戏已能正常跑圈”。

分享给下一个人时，使用原始 Release ZIP。不要上传 `app/runtime`、`app/user`、个人存档、机器绝对路径、访问凭据或本地测试日志。
