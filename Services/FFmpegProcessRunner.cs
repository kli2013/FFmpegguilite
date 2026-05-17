using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FFLiteGUI.Services
{
    public class FFmpegProcessRunner
    {
        public async Task<(bool Success, string Error)> RunAsync(string command, CancellationToken token, Action<string> outputCallback = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi })
            {
                var tcs = new TaskCompletionSource<int>();
                process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);

                process.OutputDataReceived += (s, e) => { if (e.Data != null) outputCallback?.Invoke(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) outputCallback?.Invoke(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (token.Register(() => { try { process.Kill(); } catch { } }))
                {
                    await tcs.Task;
                    return (process.ExitCode == 0, process.ExitCode != 0 ? $"返回码 {process.ExitCode}" : null);
                }
            }
        }
    }
}
