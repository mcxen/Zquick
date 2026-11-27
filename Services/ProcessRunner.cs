using System.Diagnostics;
using System.Text;

namespace Zquick.Services;

public static class ProcessRunner
{
    public static string? ResolveToolPath(string configuredPath, string exeName, params string[] commonPaths)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            string expanded = Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"'));
            if (File.Exists(expanded))
            {
                return Path.GetFullPath(expanded);
            }

            string? inPath = FindInPath(expanded);
            if (inPath != null)
            {
                return inPath;
            }
        }

        foreach (string candidate in commonPaths)
        {
            string expanded = Environment.ExpandEnvironmentVariables(candidate);
            if (File.Exists(expanded))
            {
                return Path.GetFullPath(expanded);
            }
        }

        return FindInPath(exeName);
    }

    private static string? FindInPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (Path.IsPathRooted(fileName))
        {
            return File.Exists(fileName) ? fileName : null;
        }

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        foreach (string dir in pathEnv.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                string candidate = Path.Combine(dir.Trim().Trim('"'), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                if (!fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    string withExe = candidate + ".exe";
                    if (File.Exists(withExe))
                    {
                        return withExe;
                    }
                }
            }
            catch
            {
                // 忽略格式异常的 PATH 条目
            }
        }

        return null;
    }

    public static async Task<int> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        Action<string> onOutput,
        CancellationToken cancellationToken,
        bool showWindow = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = showWindow,
            CreateNoWindow = !showWindow,
            RedirectStandardOutput = !showWindow,
            RedirectStandardError = !showWindow
        };

        if (!showWindow)
        {
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.Exited += (_, _) =>
        {
            try
            {
                if (!showWindow)
                {
                    // 确保异步 stdout/stderr 缓冲区在回调完成前刷新完毕
                    process.WaitForExit();
                }

                tcs.TrySetResult(process.ExitCode);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        if (!showWindow)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    onOutput(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    onOutput(e.Data);
                }
            };
        }

        if (!process.Start())
        {
            throw new InvalidOperationException($"无法启动进程：{fileName}");
        }

        if (!showWindow)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        if (process.HasExited)
        {
            if (!showWindow)
            {
                process.WaitForExit();
            }

            return process.ExitCode;
        }

        using CancellationTokenRegistration reg = cancellationToken.Register(() =>
        {
            KillProcessTreeSafe(process);
            tcs.TrySetCanceled(cancellationToken);
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    private static void KillProcessTreeSafe(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            int pid = process.Id;
            using (var killer = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/PID {pid} /T /F",
                CreateNoWindow = true,
                UseShellExecute = false
            }))
            {
                killer?.WaitForExit(3000);
            }

            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch
        {
            // 忽略进程已退出的异常
        }
    }
}
