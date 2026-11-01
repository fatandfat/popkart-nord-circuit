# 跑跑卡丁车纽北全环单机补丁

基于**韩服原版 P5136 客户端**，将纽北布局移植到跑跑卡丁车的离线环境。本仓库发布已由使用者确认采用的 **V10 最终版，整体比例 1.5 倍**，每圈约 31.25 公里。

## 下载与文档

- **[下载补丁 ZIP（约 156 MB）](https://github.com/fatandfat/popkart-nord-circuit/releases/download/v1.0.0/Nordschleife-P5136-Final-1.5x-v1.zip)**
- [首次安装、客户端地址、每次启动和故障处理](docs/安装与使用指南.md)
- **[给 AI agent 的完整配置步骤](docs/AI-AGENT-SETUP.md)**
- [SHA-256 与版本信息](checksums.json)
- [发布记录与验证范围](docs/验证范围.md)
- [第三方来源](THIRD_PARTY_NOTICES.md)

补丁 ZIP 不包含原版跑跑客户端。请自行准备与本项目校验值匹配的韩服 P5136 客户端；其他地区、其他版本、已修改的资源包不能直接使用。原版客户端的第三方存档页面是 [MediaFire / KartRider_5136.zip](https://www.mediafire.com/file/4vz8t1k0sra1pb3/KartRider_5136.zip/file)。下载后必须按指南核对 SHA-256。

## 最短使用步骤

1. 准备原版客户端，解压到能看到 `KartRider.exe` 和 `Data` 的目录。
2. 将补丁 ZIP 完整解压到另一个目录，双击 **`1-Install.cmd`**，输入原版客户端目录。会额外创建约 4 GB 的独立副本，原客户端不变。
3. 以后双击 **`2-Play.cmd`**，同意 UAC，等待大厅显示。
4. **先不要开始比赛。Alt+Tab 回启动窗口，按一次回车，看到“准备完成”再切回游戏。**
5. 选择 **빌리지 고가의 질주（城镇高速槽位）**、正向，开始驾驶。昵称自动使用 `NordPlayer`。

每次新开游戏都需要第 4 步；只需安装一次。启动窗口可最小化，正常退出游戏后，本次本地服务自动停止。无需 Python、Node、Blender 或 Codex。

## 交给 AI agent

复制下面这段话给能访问本机文件、浏览器和终端的 agent：

> 请阅读 https://github.com/fatandfat/popkart-nord-circuit/blob/main/docs/AI-AGENT-SETUP.md ，按文档在我的 Windows 电脑上配置跑跑卡丁车纽北补丁。优先复用已有的、校验匹配的 P5136 客户端。安装完成后运行 VerifyOnly 和 ServerCheckOnly，不自动开始比赛；告诉我启动入口、UAC 和大厅回车操作。下载被验证码拦住或需要管理员确认时交给我完成，不跳过版本校验。

## 仓库与安装包的区别

此 Git 仓库保存说明、校验信息、启动/安装脚本源码。**GitHub 的绿色 Code → Download ZIP 是源码，不是可运行安装包**；请使用上面的 Release 补丁 ZIP。`package/` 下未放入大体积地图资源和第三方可执行文件，它用于审阅脚本，不能单独启动游戏。

发布标签 `v1.0.0` 是本仓库的发行版本；地图内部版本仍叫 V10。当前分享版仅配置本机离线使用，没有配置局域网或公网联机。
