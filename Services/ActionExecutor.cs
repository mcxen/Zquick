using System.Text;
using System.Text.RegularExpressions;
using Zquick.Models;

namespace Zquick.Services;

public class FolderOverviewResult
{
    public int DirectFolderCount { get; set; }
    public int DirectFileCount { get; set; }
    public long DirectFilesTotalBytes { get; set; }
    public string ReportText { get; set; } = "";
}

public static class ActionExecutor
{
    private const int MaxTreeLines = 2000;

    private static int ClampInt(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    public static FolderOverviewResult BuildFolderOverview(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        DirectoryInfo[] directDirs = SafeGetDirectories(dirInfo, skipHiddenSystem: false);
        FileInfo[] directFiles = SafeGetFiles(dirInfo, skipHiddenSystem: false);
        long totalBytes = directFiles.Sum(SafeFileLength);

        var sb = new StringBuilder();
        sb.AppendLine($"目标路径：{dirInfo.FullName}");
        sb.AppendLine($"直接子文件夹：{directDirs.Length} 个");
        sb.AppendLine($"直接子文件：{directFiles.Length} 个（合计 {FormatBytes(totalBytes)}）");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine(dirInfo.Name);

        int lineCount = 0;
        bool truncated = false;
        AppendTreeRecursive(dirInfo, "", sb, ref lineCount, ref truncated);

        if (truncated)
        {
            sb.AppendLine();
            sb.AppendLine($"... 目录条目超过 {MaxTreeLines} 行，已截断显示。");
        }

        return new FolderOverviewResult
        {
            DirectFolderCount = directDirs.Length,
            DirectFileCount = directFiles.Length,
            DirectFilesTotalBytes = totalBytes,
            ReportText = sb.ToString()
        };
    }

    private static void AppendTreeRecursive(
        DirectoryInfo current,
        string indent,
        StringBuilder sb,
        ref int lineCount,
        ref bool truncated)
    {
        if (truncated)
        {
            return;
        }

        DirectoryInfo[] dirs = SafeGetDirectories(current, skipHiddenSystem: false)
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        FileInfo[] files = SafeGetFiles(current, skipHiddenSystem: false)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int totalEntries = dirs.Length + files.Length;
        int idx = 0;

        foreach (DirectoryInfo sub in dirs)
        {
            if (lineCount >= MaxTreeLines)
            {
                truncated = true;
                return;
            }

            idx++;
            bool isLast = idx == totalEntries;
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.AppendLine($"[{sub.Name}]");
            lineCount++;

            string childIndent = indent + (isLast ? "    " : "│   ");
            AppendTreeRecursive(sub, childIndent, sb, ref lineCount, ref truncated);
        }

        foreach (FileInfo file in files)
        {
            if (lineCount >= MaxTreeLines)
            {
                truncated = true;
                return;
            }

            idx++;
            bool isLast = idx == totalEntries;
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.AppendLine(file.Name);
            lineCount++;
        }
    }

    public static List<RenamePlanItem> BuildRenamePlan(MenuAction action, string folderPath, string? runtimePrefix)
    {
        return action.Kind switch
        {
            ActionKind.AddFolderPrefix => BuildAddPrefixPlan(action, folderPath, runtimePrefix),
            ActionKind.RenameFilesByFolder => BuildRenameFilesByFolderPlan(action, folderPath),
            ActionKind.RemovePrefix => BuildRemovePrefixPlan(action, folderPath),
            _ => new List<RenamePlanItem>()
        };
    }

    private static List<RenamePlanItem> BuildAddPrefixPlan(MenuAction action, string folderPath, string? runtimePrefix)
    {
        string prefix = runtimePrefix ?? action.DefaultPrefix ?? "";
        var plan = new List<RenamePlanItem>();
        if (string.IsNullOrEmpty(prefix))
        {
            return plan;
        }

        var dirInfo = new DirectoryInfo(folderPath);
        DirectoryInfo[] subDirs = SafeGetDirectories(dirInfo, action.SkipHiddenAndSystem)
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DirectoryInfo sub in subDirs)
        {
            reserved.Add(sub.FullName);
        }

        foreach (DirectoryInfo sub in subDirs)
        {
            string newName = prefix + sub.Name;
            string newPath = Path.Combine(folderPath, newName);

            if (sub.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                plan.Add(new RenamePlanItem
                {
                    OldPath = sub.FullName,
                    NewPath = sub.FullName,
                    IsDirectory = true,
                    Selected = false,
                    Skipped = true,
                    SkipReason = "已包含该前缀"
                });
                continue;
            }

            if (PathGuard.ContainsInvalidFileNameChars(newName))
            {
                plan.Add(new RenamePlanItem
                {
                    OldPath = sub.FullName,
                    NewPath = newPath,
                    IsDirectory = true,
                    Selected = false,
                    Skipped = true,
                    SkipReason = "新名称含非法字符"
                });
                continue;
            }

            if (reserved.Contains(newPath) || Directory.Exists(newPath) || File.Exists(newPath))
            {
                plan.Add(new RenamePlanItem
                {
                    OldPath = sub.FullName,
                    NewPath = newPath,
                    IsDirectory = true,
                    Selected = false,
                    Skipped = true,
                    SkipReason = "目标已存在"
                });
                continue;
            }

            reserved.Add(newPath);
            plan.Add(new RenamePlanItem
            {
                OldPath = sub.FullName,
                NewPath = newPath,
                IsDirectory = true,
                Selected = true
            });
        }

        return plan;
    }

    private static List<RenamePlanItem> BuildRenameFilesByFolderPlan(MenuAction action, string folderPath)
    {
        var plan = new List<RenamePlanItem>();
        var targetDirs = new List<DirectoryInfo>();

        if (action.RenameScope == RenameScope.CurrentFolder)
        {
            targetDirs.Add(new DirectoryInfo(folderPath));
        }
        else
        {
            targetDirs.AddRange(
                SafeGetDirectories(new DirectoryInfo(folderPath), action.SkipHiddenAndSystem)
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
        }

        string sep = action.NumberSeparator ?? "_";
        int minPad = ClampInt(action.PadDigits, 1, 6);

        foreach (DirectoryInfo targetDir in targetDirs)
        {
            string baseName = targetDir.Name;
            FileInfo[] files = SafeGetFiles(targetDir, action.SkipHiddenAndSystem)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
            {
                continue;
            }

            // 先占用目录内全部现有路径，防止编号撞车
            var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FileInfo f in files)
            {
                occupied.Add(f.FullName);
            }

            foreach (DirectoryInfo d in SafeGetDirectories(targetDir, skipHiddenSystem: false))
            {
                occupied.Add(d.FullName);
            }

            IEnumerable<IGrouping<string, FileInfo>> groups = action.GroupByExtension
                ? files.GroupBy(f => f.Extension.ToLowerInvariant())
                : new[] { files.GroupBy(_ => "*").First() };

            string numberedRegexPattern = $"^{Regex.Escape(baseName)}{Regex.Escape(sep)}\\d+$";

            foreach (IGrouping<string, FileInfo> group in groups)
            {
                List<FileInfo> items = group.ToList();
                if (action.SingleFileNoNumber && items.Count == 1)
                {
                    FileInfo file = items[0];
                    string targetName = baseName + file.Extension;
                    string targetPath = Path.Combine(targetDir.FullName, targetName);

                    if (string.Equals(file.Name, targetName, StringComparison.Ordinal))
                    {
                        plan.Add(new RenamePlanItem
                        {
                            OldPath = file.FullName,
                            NewPath = targetPath,
                            IsDirectory = false,
                            Selected = false,
                            Skipped = true,
                            SkipReason = "已是目标名称"
                        });
                        continue;
                    }

                    bool isCaseOnly = string.Equals(file.FullName, targetPath, StringComparison.OrdinalIgnoreCase);
                    if (!isCaseOnly && (occupied.Contains(targetPath) || File.Exists(targetPath) || Directory.Exists(targetPath)))
                    {
                        plan.Add(new RenamePlanItem
                        {
                            OldPath = file.FullName,
                            NewPath = targetPath,
                            IsDirectory = false,
                            Selected = false,
                            Skipped = true,
                            SkipReason = "目标已存在"
                        });
                        continue;
                    }

                    occupied.Add(targetPath);
                    plan.Add(new RenamePlanItem
                    {
                        OldPath = file.FullName,
                        NewPath = targetPath,
                        IsDirectory = false,
                        Selected = true
                    });
                    continue;
                }

                int padWidth = Math.Max(minPad, items.Count.ToString().Length);
                int index = 1;

                foreach (FileInfo file in items)
                {
                    string fileStem = Path.GetFileNameWithoutExtension(file.Name);
                    if (Regex.IsMatch(fileStem, numberedRegexPattern, RegexOptions.IgnoreCase))
                    {
                        plan.Add(new RenamePlanItem
                        {
                            OldPath = file.FullName,
                            NewPath = file.FullName,
                            IsDirectory = false,
                            Selected = false,
                            Skipped = true,
                            SkipReason = "已符合编号规则"
                        });
                        continue;
                    }

                    string targetPath;
                    do
                    {
                        string num = index.ToString($"D{padWidth}");
                        string targetName = $"{baseName}{sep}{num}{file.Extension}";
                        targetPath = Path.Combine(targetDir.FullName, targetName);
                        index++;
                    } while (occupied.Contains(targetPath) || File.Exists(targetPath) || Directory.Exists(targetPath));

                    occupied.Add(targetPath);
                    plan.Add(new RenamePlanItem
                    {
                        OldPath = file.FullName,
                        NewPath = targetPath,
                        IsDirectory = false,
                        Selected = true
                    });
                }
            }
        }

        return plan;
    }

    private static List<RenamePlanItem> BuildRemovePrefixPlan(MenuAction action, string folderPath)
    {
        var plan = new List<RenamePlanItem>();
        List<string> prefixes = action.RemovePrefixes
            .Where(p => !string.IsNullOrEmpty(p))
            .OrderByDescending(p => p.Length)
            .ToList();

        if (prefixes.Count == 0)
        {
            return plan;
        }

        var rootDir = new DirectoryInfo(folderPath);
        List<FileSystemInfo> entries = WalkEntriesSafe(rootDir, action.Recursive, action.SkipHiddenAndSystem)
            .OrderByDescending(e => e.FullName.Length)
            .ToList();

        var plannedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (FileSystemInfo entry in entries)
        {
            string? matchedPrefix = prefixes.FirstOrDefault(
                p => entry.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (matchedPrefix == null)
            {
                continue;
            }

            string newName = entry.Name.Substring(matchedPrefix.Length);
            bool isDir = entry is DirectoryInfo;
            string parentDir = Path.GetDirectoryName(entry.FullName) ?? folderPath;
            string targetPath = Path.Combine(parentDir, newName);

            if (string.IsNullOrWhiteSpace(newName) || PathGuard.ContainsInvalidFileNameChars(newName))
            {
                plan.Add(new RenamePlanItem
                {
                    OldPath = entry.FullName,
                    NewPath = targetPath,
                    IsDirectory = isDir,
                    Selected = false,
                    Skipped = true,
                    SkipReason = "移除后名称为空或非法"
                });
                continue;
            }

            bool isCaseOnly = string.Equals(entry.FullName, targetPath, StringComparison.OrdinalIgnoreCase);
            if (!isCaseOnly && (plannedTargets.Contains(targetPath) || File.Exists(targetPath) || Directory.Exists(targetPath)))
            {
                plan.Add(new RenamePlanItem
                {
                    OldPath = entry.FullName,
                    NewPath = targetPath,
                    IsDirectory = isDir,
                    Selected = false,
                    Skipped = true,
                    SkipReason = "目标已存在"
                });
                continue;
            }

            plannedTargets.Add(targetPath);
            plan.Add(new RenamePlanItem
            {
                OldPath = entry.FullName,
                NewPath = targetPath,
                IsDirectory = isDir,
                Selected = true
            });
        }

        return plan;
    }

    private static List<FileSystemInfo> WalkEntriesSafe(DirectoryInfo root, bool recursive, bool skipHiddenSystem)
    {
        var results = new List<FileSystemInfo>();
        var stack = new Stack<DirectoryInfo>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            DirectoryInfo current = stack.Pop();

            foreach (FileInfo file in SafeGetFiles(current, skipHiddenSystem))
            {
                results.Add(file);
            }

            foreach (DirectoryInfo sub in SafeGetDirectories(current, skipHiddenSystem))
            {
                results.Add(sub);
                if (recursive)
                {
                    stack.Push(sub);
                }
            }
        }

        return results;
    }

    public static RenameBatch? ExecuteRenamePlan(
        IReadOnlyList<RenamePlanItem> plan,
        string actionTitle,
        string targetDirectory,
        HistoryService historyService,
        Action<string> log,
        Action<int, int> progress,
        CancellationToken cancellationToken)
    {
        List<RenamePlanItem> runnable = plan.Where(p => p.Selected && !p.Skipped).ToList();
        int total = runnable.Count;
        int current = 0;

        foreach (RenamePlanItem skipped in plan.Where(p => p.Skipped))
        {
            log($"[跳过] {skipped.OldName} ({skipped.SkipReason})");
        }

        if (total == 0)
        {
            log("没有需要执行重命名的项目。");
            progress(1, 1);
            return null;
        }

        var batch = new RenameBatch
        {
            ActionTitle = actionTitle,
            TargetDirectory = targetDirectory
        };

        foreach (RenamePlanItem item in runnable)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;

            try
            {
                bool caseOnly = string.Equals(item.OldPath, item.NewPath, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(item.OldPath, item.NewPath, StringComparison.Ordinal);

                if (item.IsDirectory)
                {
                    if (!Directory.Exists(item.OldPath))
                    {
                        log($"[跳过] 目录已不存在：{item.OldPath}");
                        progress(current, total);
                        continue;
                    }

                    if (!caseOnly && (Directory.Exists(item.NewPath) || File.Exists(item.NewPath)))
                    {
                        log($"[跳过] 目标已存在：{item.NewPath}");
                        progress(current, total);
                        continue;
                    }

                    HistoryService.MoveDirectorySafe(item.OldPath, item.NewPath, caseOnly);
                }
                else
                {
                    if (!File.Exists(item.OldPath))
                    {
                        log($"[跳过] 文件已不存在：{item.OldPath}");
                        progress(current, total);
                        continue;
                    }

                    if (!caseOnly && (File.Exists(item.NewPath) || Directory.Exists(item.NewPath)))
                    {
                        log($"[跳过] 目标已存在：{item.NewPath}");
                        progress(current, total);
                        continue;
                    }

                    HistoryService.MoveFileSafe(item.OldPath, item.NewPath, caseOnly);
                }

                batch.Records.Add(new RenameRecord
                {
                    OldPath = item.OldPath,
                    NewPath = item.NewPath,
                    IsDirectory = item.IsDirectory
                });

                log($"[OK] {item.OldName} -> {item.NewName}");
            }
            catch (Exception ex)
            {
                log($"[失败] {item.OldName} -> {item.NewName} : {ex.Message}");
            }

            progress(current, total);
        }

        if (batch.Records.Count > 0)
        {
            historyService.AddBatch(batch);
            log($"完成：共重命名 {batch.Records.Count} 项（已记录撤销历史）。");
            return batch;
        }

        log("完成：未修改任何文件或目录。");
        return null;
    }

    public static async Task ExecuteCompressSubFoldersAsync(
        AppConfig config,
        MenuAction action,
        string folderPath,
        string? runtimePassword,
        Action<string> log,
        Action<int, int> progress,
        CancellationToken cancellationToken)
    {
        string? sevenZip = ProcessRunner.ResolveToolPath(
            config.SevenZipPath,
            "7z.exe",
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe");

        if (sevenZip == null)
        {
            throw new FileNotFoundException(
                "未找到 7z.exe，请在【全局设置】中配置正确的 7-Zip 路径。");
        }

        DirectoryInfo[] subDirs = SafeGetDirectories(new DirectoryInfo(folderPath), skipHiddenSystem: true)
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (subDirs.Length == 0)
        {
            log("当前目录下没有可压缩的子文件夹。");
            progress(1, 1);
            return;
        }

        string password = "";
        if (action.UsePassword)
        {
            if (!string.IsNullOrEmpty(runtimePassword))
            {
                password = runtimePassword!;
            }
            else if (!string.IsNullOrEmpty(action.EncryptedPassword))
            {
                password = DpapiHelper.UnprotectString(action.EncryptedPassword);
            }
        }

        string format = string.Equals(action.ArchiveFormat, "7z", StringComparison.OrdinalIgnoreCase) ? "7z" : "zip";
        int total = subDirs.Length;
        int current = 0;
        int successCount = 0;
        int failCount = 0;

        log($"使用 7-Zip：{sevenZip}");
        log($"压缩模式：.{format} | {(action.IncludeRootFolder ? "包含外层文件夹" : "平铺内容")} | " +
            $"{(action.UsePassword ? "AES256 加密" : "不加密")} | " +
            $"{(action.DeleteSourceAfterSuccess ? (action.MoveDeletedToRecycleBin ? "成功后移入回收站" : "成功后永久删除") : "保留源目录")}");

        foreach (DirectoryInfo sub in subDirs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;

            string archiveName = $"{sub.Name}.{format}";
            string archivePath = Path.Combine(folderPath, archiveName);
            string sourcePattern = action.IncludeRootFolder ? $"{sub.Name}\\" : $"{sub.Name}\\*";

            var argsList = new List<string>
            {
                "a",
                $"-t{format}",
                "-sccUTF-8",
                "-y"
            };

            var displayArgsList = new List<string>(argsList);

            if (action.UsePassword && !string.IsNullOrEmpty(password))
            {
                string escapedPwd = EscapeWindowsQuotedContent(password);
                argsList.Add($"-p\"{escapedPwd}\"");
                displayArgsList.Add("-p\"***\"");

                if (format == "zip")
                {
                    argsList.Add("-mem=AES256");
                    displayArgsList.Add("-mem=AES256");
                }
                else
                {
                    argsList.Add("-mhe=on");
                    displayArgsList.Add("-mhe=on");
                }
            }

            argsList.Add($"\"{archiveName}\"");
            argsList.Add($"\"{sourcePattern}\"");
            displayArgsList.Add($"\"{archiveName}\"");
            displayArgsList.Add($"\"{sourcePattern}\"");

            log($"[{current}/{total}] 正在压缩：{sub.Name} -> {archiveName}");
            log($"    命令：7z.exe {string.Join(" ", displayArgsList)}");

            int exitCode = await ProcessRunner.RunAsync(
                sevenZip,
                string.Join(" ", argsList),
                folderPath,
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        log("    " + line);
                    }
                },
                cancellationToken);

            if (exitCode == 0 && File.Exists(archivePath))
            {
                successCount++;
                log($"[OK] 压缩成功：{archiveName}");

                if (action.DeleteSourceAfterSuccess)
                {
                    try
                    {
                        RecycleBinHelper.DeleteDirectory(sub.FullName, action.MoveDeletedToRecycleBin);
                        log($"    已{(action.MoveDeletedToRecycleBin ? "移入回收站" : "永久删除")}源目录：{sub.Name}");
                    }
                    catch (Exception ex)
                    {
                        log($"    [警告] 压缩成功但清理源目录失败：{ex.Message}");
                    }
                }
            }
            else
            {
                failCount++;
                log($"[失败] {sub.Name} 压缩失败（退出码 {exitCode}），已保留源文件夹防止数据丢失。");
            }

            progress(current, total);
        }

        log($"压缩全部结束：成功 {successCount} 个，失败 {failCount} 个。");
    }

    public static async Task ExecuteRotateVideosAsync(
        AppConfig config,
        MenuAction action,
        string folderPath,
        Action<string> log,
        Action<int, int> progress,
        CancellationToken cancellationToken)
    {
        string? ffmpeg = ProcessRunner.ResolveToolPath(
            config.FfmpegPath,
            "ffmpeg.exe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe");

        if (ffmpeg == null)
        {
            throw new FileNotFoundException(
                "未找到 ffmpeg.exe，请在【全局设置】中配置 FFmpeg 路径或将其加入系统 PATH。");
        }

        var extSet = new HashSet<string>(
            (action.VideoExtensions ?? ".mp4")
                .Split(new[] { ';', ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        if (extSet.Count == 0)
        {
            extSet.Add(".mp4");
        }

        FileInfo[] videoFiles = SafeGetFiles(new DirectoryInfo(folderPath), skipHiddenSystem: true)
            .Where(f => extSet.Contains(f.Extension.ToLowerInvariant()))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (videoFiles.Length == 0)
        {
            log($"当前目录下未找到匹配的视频文件（扩展名：{string.Join(", ", extSet)}）。");
            progress(1, 1);
            return;
        }

        string outputSub = string.IsNullOrWhiteSpace(action.OutputSubFolder)
            ? "newfiles"
            : action.OutputSubFolder.Trim().Trim('\\', '/');
        if (PathGuard.ContainsInvalidFileNameChars(outputSub))
        {
            outputSub = "newfiles";
        }

        string outputDir = Path.Combine(folderPath, outputSub);
        Directory.CreateDirectory(outputDir);

        int angle = action.RotationAngle is 90 or 180 or 270 ? action.RotationAngle : 90;
        int total = videoFiles.Length;
        int current = 0;
        int successCount = 0;
        int failCount = 0;

        log($"使用 FFmpeg：{ffmpeg}");
        log($"输出目录：{outputDir} | 旋转角度：{angle}° | 模式：{action.RotationMethod}");

        foreach (FileInfo video in videoFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;

            string outputPath = Path.Combine(outputDir, video.Name);
            string args = action.RotationMethod == VideoRotationMethod.DisplayRotation
                ? $"-y -display_rotation:v:0 {-angle} -i \"{video.FullName}\" -c copy \"{outputPath}\""
                : $"-y -i \"{video.FullName}\" -c copy -metadata:s:v:0 rotate={angle} \"{outputPath}\"";

            log($"[{current}/{total}] 处理视频：{video.Name}");

            int exitCode = await ProcessRunner.RunAsync(
                ffmpeg,
                args,
                folderPath,
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line) &&
                        (line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("Output #0", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        log("    " + line);
                    }
                },
                cancellationToken);

            if (exitCode == 0 && File.Exists(outputPath))
            {
                successCount++;
                log($"[OK] 已输出：{outputSub}\\{video.Name}");
            }
            else
            {
                failCount++;
                log($"[失败] {video.Name} 退出码 {exitCode}");
            }

            progress(current, total);
        }

        log($"视频旋转完成：成功 {successCount} 个，失败 {failCount} 个。");
    }

    public static async Task ExecuteCustomScriptAsync(
        MenuAction action,
        string folderPath,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.ScriptPath) || !File.Exists(action.ScriptPath))
        {
            throw new FileNotFoundException($"自定义脚本不存在：{action.ScriptPath}");
        }

        string scriptPath = Path.GetFullPath(action.ScriptPath);
        string ext = Path.GetExtension(scriptPath).ToLowerInvariant();
        string extraArgs = (action.ScriptArguments ?? "").Replace("{dir}", $"\"{folderPath}\"");

        string fileName;
        string arguments;

        if (ext is ".bat" or ".cmd")
        {
            fileName = "cmd.exe";
            string callPart = string.IsNullOrWhiteSpace(extraArgs)
                ? $"call \"{scriptPath}\""
                : $"call \"{scriptPath}\" {extraArgs}";
            arguments = action.ShowConsoleWindow
                ? $"/c \"{callPart}\""
                : $"/c \"chcp 65001>nul & {callPart}\"";
        }
        else if (ext == ".ps1")
        {
            fileName = "powershell.exe";
            string psEscaped = scriptPath.Replace("'", "''");
            string cmdBody = $"[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; & '{psEscaped}' {extraArgs}".Trim();
            arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -Command \"{cmdBody.Replace("\"", "\\\"")}\"";
        }
        else
        {
            fileName = scriptPath;
            arguments = extraArgs;
        }

        log($"执行脚本：{scriptPath}");
        if (!string.IsNullOrWhiteSpace(extraArgs))
        {
            log($"附加参数：{extraArgs}");
        }

        int exitCode = await ProcessRunner.RunAsync(
            fileName,
            arguments,
            folderPath,
            log,
            cancellationToken,
            showWindow: action.ShowConsoleWindow);

        log(exitCode == 0
            ? "脚本执行完成（退出码 0）。"
            : $"[警告] 脚本已结束，退出码：{exitCode}");
    }

    private static DirectoryInfo[] SafeGetDirectories(DirectoryInfo dir, bool skipHiddenSystem)
    {
        try
        {
            return dir.GetDirectories()
                .Where(d => !skipHiddenSystem || !IsHiddenOrSystem(d.Attributes))
                .ToArray();
        }
        catch
        {
            return Array.Empty<DirectoryInfo>();
        }
    }

    private static FileInfo[] SafeGetFiles(DirectoryInfo dir, bool skipHiddenSystem)
    {
        try
        {
            return dir.GetFiles()
                .Where(f => !skipHiddenSystem || !IsHiddenOrSystem(f.Attributes))
                .ToArray();
        }
        catch
        {
            return Array.Empty<FileInfo>();
        }
    }

    private static bool IsHiddenOrSystem(FileAttributes attributes)
    {
        return (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
    }

    private static long SafeFileLength(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unitIdx = 0;
        while (value >= 1024 && unitIdx < units.Length - 1)
        {
            value /= 1024;
            unitIdx++;
        }

        return $"{value:0.##} {units[unitIdx]}";
    }

    private static string EscapeWindowsQuotedContent(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        int backslashCount = 0;

        foreach (char ch in value)
        {
            if (ch == '\\')
            {
                backslashCount++;
            }
            else if (ch == '"')
            {
                sb.Append('\\', backslashCount * 2 + 1);
                sb.Append('"');
                backslashCount = 0;
            }
            else
            {
                if (backslashCount > 0)
                {
                    sb.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                sb.Append(ch);
            }
        }

        if (backslashCount > 0)
        {
            sb.Append('\\', backslashCount * 2);
        }

        return sb.ToString();
    }
}
