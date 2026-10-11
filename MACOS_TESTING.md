# macOS ARM64 测试版 2.1.3

## 编译

仓库在 `ModSources/infrasonic-mod`；`ModSources/TerraVoice` 是指向仓库 `src/TerraVoice` 的源码入口。内部模组名保持 TerraVoice，显示名保持 InfraSonic。

在终端执行：

```sh
dotnet build "/Users/lhy/Library/Application Support/Terraria/tModLoader/ModSources/TerraVoice/TerraVoice.csproj"
```

这会编译并打包到默认 Mods 目录。使用终端 MSBuild，不使用游戏内普通 Build 按钮：原项目包含 unsafe 代码及游戏私有成员访问，普通 Build 不会执行 csproj 中的 unsafe/publicizer 设置。该限制已实测，并非已验证所有编译入口均可用。

## 修改

- 新增 `lib/Native/osx-arm64` 的 Opus、RNNoise、OpenAL Soft 动态库及许可证文件。
- 平台选择器明确识别 macOS ARM64，只扫描该目录，修复原来混读 Windows/Linux 库引发的重复键异常。
- 缓存改为 `TerraVoice/Native/<平台>/<模组版本>`，避免旧平台或旧版本同名库被复用；无需手动删除旧缓存。
- DllImport 解析器只处理自己对应的库名。
- OpenAL 在加载失败后的卸载中允许句柄尚未初始化，避免额外空引用异常掩盖加载错误。
- 项目从 .NET 6 / Catalyst 构建链更新到当前 tModLoader 的 .NET 8 / Krafs.Publicizer 构建链。
- macOS 打包命令显式传入游戏原生库搜索路径，解决 MSBuild 子进程找不到 FNA3D 的问题。
- 版本更新为 2.1.3；Windows/Linux 自带原生库不变。

当前只支持 ARM64 原生进程，不支持 Intel Mac 或 Rosetta。OpenAL 二进制的最低系统版本为 macOS 27.0，此包针对本机环境验证；没有声称兼容老 macOS。

## 已验证

- 完整 mod 源码 MSBuild 编译成功。
- 预编译 DLL 的隔离 tModLoader 打包成功。
- 上述终端 MSBuild 入口在隔离保存目录完成编译和打包，0 错误；没有把检查产物装入用户 Mods。
- .NET 8.0.0 下执行实际 `NativeFeatureSystem.Load()`：从源码资源模拟的 tmod 文件接口提取并缓存三份 dylib，没有混入 Windows/Linux 文件。
- 原 Opus 包装器编解码、原 RNNoise 包装器降噪、原 Silk.NET 包装器设备枚举及原生库卸载通过。

独立检查的 ModSystem、tmod 资源接口和加载进度对象是测试替身；原生加载器及音频包装代码是生产源码。测试没有启动游戏、打开麦克风、录音或播放声音，因此不等于游戏内语音验收。

## 游戏内验收

1. 加载 InfraSonic 2.1.3，确认没有加载失败；设置面板快捷键并打开面板。
2. 选择麦克风，允许系统麦克风访问；使用面板的语音测试模式检查录音和回放。
3. 同一服务器的两个客户端使用相同语音频道，测试互相听见、按键说话及距离音量变化。服务器也需要启用兼容的 TerraVoice 模组以转发语音数据；本次没有部署到远程服务器。
4. 退出世界、再次进入，检查麦克风与面板；最后检查模组卸载重载。

## 原生库来源

见 `MACOS_INVESTIGATION.md` 和 `src/TerraVoice/NativeLicenses`。RNNoise 使用 0.1.1 源码本机编译；Opus 1.6.1 来自本机已有安装，OpenAL Soft 1.25.2 来自官方 Homebrew bottle。Opus/OpenAL 安装名改为 `@rpath` 并重新进行本机 ad-hoc 签名，依赖仅为系统库和系统框架，无 Homebrew 运行路径依赖。
