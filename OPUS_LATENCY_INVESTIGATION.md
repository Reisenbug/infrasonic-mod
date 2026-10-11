# 编码与延迟调查

2026-10-11 用户报告语音延迟较高。尚未测得实际录音到播放的延迟，也未确认是本地回放还是联机语音；未操作游戏或录制麦克风。

2.1.4 的独立编码测试发现：20 ms、960 样本的语音输入设置为 24000 bps，却输出 1276 字节包。之前只验证解码后的样本数和非零内容，未验证实际码率，不能代表完整的语音质量验收。

`Tests/MacNativeChecks/OpusProbe.c` 用原生 C 调用 Opus 的控制接口，读取 P-OpusCore 编码器的同一个句柄；随后用 C 设置正确码率进行比较。在 tModLoader 自带 .NET 8.0.0、macOS ARM64 下测得：

```text
Managed setter requested 24000 bps; native getter measured 510400 bps.
Native setter returned 0; native getter measured 24000 bps; next packet size=113 bytes.
```

仓库自带的 P-OpusCore 将 `opus_encoder_ctl` 声明为固定参数 P/Invoke；原生接口实际是可变参数。C 包装调用与托管调用的结果不一致，证明此路径有兼容问题，但没有证明它就是用户延迟的根因。

此测试读取的是独立编码器实例，不是运行中客户端的编码器；113 字节是切换设置后的单包，不是稳态平均码率。

对延迟的后续定位应分别测量录音设备积压、主线程排队、编码耗时和播放队列长度；本地回放不经过联机转发，因此不能用网络带宽解释它的全部延迟。此轮没有修改生产编码和播放逻辑。

复现，在仓库根目录执行：

```sh
clang -dynamiclib -arch arm64 -I/opt/homebrew/opt/opus/include/opus Tests/MacNativeChecks/OpusProbe.c /opt/homebrew/opt/opus/lib/libopus.dylib -o /tmp/infrasonic-opus-probe.dylib
dotnet build Tests/MacNativeChecks/MacNativeChecks.csproj
"/Users/lhy/Library/Application Support/Steam/steamapps/common/tModLoader/dotnet_arm64/dotnet" --fx-version 8.0.0 Tests/MacNativeChecks/bin/Debug/net8.0/MacNativeChecks.dll "$PWD" /tmp/infrasonic-opus-probe.dylib
```
