# 第三方来源与包内容

## P5136_Rust

- 项目：[ILoveKartrider/P5136_Rust](https://github.com/ILoveKartrider/P5136_Rust)
- 服务端发行版：[v1.0.0](https://github.com/ILoveKartrider/P5136_Rust/releases/tag/v1.0.0)
- 源码快照提交：`87de9460205cfd6818e1db56673b8ff13c92878e`
- 上游 `Cargo.toml` 和 README 声明许可证为 `AFL-3.0`。
- Release 补丁中保留未修改的 `app/vendor/p5136-windows-x64.zip`，以及本地固定的完整上游源码 `app/vendor/P5136_Rust-v1.0.0-source.zip`。发行文档保留在官方 ZIP 内，安装后位于 `app/server`。

## 游戏与赛道资源

原版 KartRider 客户端不在本仓库或 Release 补丁中。指南列出的 MediaFire 页面为第三方存档来源，不是 Nexon 官方页面。

地图补丁包含由 Assetto Corsa 纽北赛道素材转换得到的资源，不是纯代码或原创美术素材包。相关第三方游戏与素材权利不因本仓库发布而改变；本仓库没有将这些资源重新许可为开源美术素材。

转换阶段使用了 KartSim 的格式处理代码进行研究与打包。接收者运行已发布补丁无需安装 KartSim、Assetto Corsa、Python、Node 或 Blender；发布包没有包含开发工作区。

`package/` 是分享包安装与启动脚本的源码镜像。大体积资源和第三方发行包仅作为 GitHub Release 附件中的内容提供，不能从源码树直接运行安装器。
