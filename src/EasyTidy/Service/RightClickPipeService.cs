using System;
using System.Diagnostics;
using System.IO.Pipes;

namespace EasyTidy.Service;

public class RightClickPipeService
{
    private const string PipeName = "EasyTidyPipe";
    private bool _listening;

    /// <summary>
    /// 启动主实例管道监听器（在已确认为主实例后调用）
    /// </summary>
    public void StartListening(Func<string, Task> onPathReceived)
    {
        if (_listening) return;
        _listening = true;

        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    string? path = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        Debug.WriteLine($"[Pipe] 收到路径: {path}");
                        await onPathReceived(path);
                    }

                    server.Disconnect();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Pipe] 异常: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// 向主实例发送参数（在确认是副实例时调用）
    /// </summary>
    public async Task SendToMainInstanceAsync(string path)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(500);

            using var writer = new StreamWriter(client) { AutoFlush = true };
            await writer.WriteLineAsync(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Pipe] 发送失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 是否为右键菜单参数（判断路径有效性）
    /// </summary>
    public static bool IsRightClickArgument(string? arg)
    {
        return !string.IsNullOrWhiteSpace(arg) &&
               (File.Exists(arg) || Directory.Exists(arg));
    }
}
