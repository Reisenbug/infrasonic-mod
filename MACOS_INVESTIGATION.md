# InfraSonic macOS 调查

检查日期：2026-10-11。源码基线：`9d6551e`，InfraSonic 2.1.2。测试机器：macOS Darwin 27.0.0、ARM64。

后续已完成 2.1.4 的 ARM64 修复及游戏内 Build 对应源码编译、打包检查；当前编译方式、修改和游戏内验收步骤见 `MACOS_TESTING.md`。以下保留首次调查时的结果。

## 结论

有明确的修复方向，基础原生依赖已验证可用；尚未完成游戏内修复和验收。

上游 issue #2 的维护者解释：没有提供 macOS 原生库，且没有 Mac 可用来编译，并非认定这些库在 macOS 上无法运行。

- https://github.com/terraria-catalyst/infrasonic-mod/issues/2#issuecomment-2618068803
- https://github.com/terraria-catalyst/infrasonic-mod/issues/2#issuecomment-2620930637

## 当前源码的确定问题

`src/TerraVoice/Native/NativeFeatureSystem.cs` 的 `ExtractPlatformBinaries()` 只识别 Windows 和 Linux。macOS 保留 `lib/Native/` 前缀，因而同时扫描两个平台的库。

这两个目录均包含 libopus、librnnoise、libopenal；以不带扩展名的文件名加入字典时发生重复键异常。独立程序使用仓库实际文件列表和同样的字典插入方式，测得：

```text
Original macOS extraction reproduced: An item with the same key has already been added. Key: librnnoise
```

这是源码路径的独立复现，没有启动 tModLoader 客户端，因此不能声称已经取得游戏内相同异常的堆栈。

## 实测结果

`Tests/MacNativeChecks` 直接链接模组原来的 `rnnoise.cs`、`ALMonoMicrophone.cs`、`NativeString.cs`，引用仓库自带 P-OpusCore 和 Silk.NET DLL。仅模组日志入口使用测试替身。

分别在系统 .NET 8.0.25 和 tModLoader 自带 .NET 8.0.0 运行，两次均通过：

- 原 macOS 提取逻辑重复键异常复现。
- 原 P-OpusCore 包装器编码 960 个 48 kHz 单声道样本，再解码得到 960 个非全零样本。
- 原 RNNoise 包装器处理两个 480 样本帧。
- 原 Silk.NET OpenAL 包装器成功加载并列出 AirPods Pro、iPhone、BlackHole 2ch、MacBook Air 麦克风。

没有打开麦克风，没有启动录音，也没有修改正在运行的游戏或用户 Mods。

验证所用依赖：

| 库 | 来源 | 架构 |
| --- | --- | --- |
| Opus 1.6.1 | 本机现有 Homebrew 安装 | ARM64 |
| RNNoise 0.1.1 | xiph/rnnoise `6cbfd53eb348a8d394e0757b4025c6ded34eb2b6`，本机 clang 编译 | ARM64 |
| OpenAL Soft 1.25.2 | Homebrew 官方 ARM64 bottle，下载后仅解压 | ARM64 |

RNNoise 构建命令，在对应源码目录执行：

```sh
clang -O2 -arch arm64 -dynamiclib -DRNNOISE_BUILD -Iinclude -Isrc src/denoise.c src/rnn.c src/rnn_data.c src/rnn_reader.c src/pitch.c src/kiss_fft.c src/celt_lpc.c -o /tmp/infrasonic-native-libs/librnnoise.dylib -install_name @rpath/librnnoise.dylib
```

测试命令，在仓库根目录执行：

```sh
dotnet run --project Tests/MacNativeChecks -- "$PWD" /tmp/infrasonic-native-libs
```

游戏自带运行时测试：

```sh
"/Users/lhy/Library/Application Support/Steam/steamapps/common/tModLoader/dotnet_arm64/dotnet" --fx-version 8.0.0 Tests/MacNativeChecks/bin/Debug/net8.0/MacNativeChecks.dll "$PWD" /tmp/infrasonic-native-libs
```

## 最小修复范围与尚待验证

1. 提供 macOS 原生库，按进程架构选择 ARM64 或 x64；不能仅判断操作系统，也不能把 Windows/Linux 库改名成 dylib。
2. `ExtractPlatformBinaries()` 增加严格的 macOS 平台目录前缀；缓存按平台和架构区分，避免以后切换 Rosetta 时使用错误的同名库。
3. 生成可分发的 dylib，检查安装名、外部依赖、最低 macOS 版本，保留各库许可证。本次 Homebrew bottle 仅证明当前机器能运行，不能代表老系统或 Intel Mac。
4. 编译当前 tModLoader 可加载的完整模组；独立测试尚未覆盖模组音频 IL 钩子的兼容性。
5. 经授权后，游戏内验证加载、麦克风权限、录音、播放、设备切换、卸载重载以及两客户端联机语音。

此轮只增加调查文档和独立检查程序，未修改生产模组代码，未启用或部署 InfraSonic。
