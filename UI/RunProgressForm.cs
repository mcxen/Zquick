using Zquick.Models;
using Zquick.Services;

namespace Zquick.UI;

public class RunProgressForm : Form
{
    private readonly ConfigService _configService;
    private readonly HistoryService _historyService;
    private readonly AppConfig _config;
    private readonly MenuAction _action;
    private readonly string _folderPath;
    private readonly CancellationTokenSource _cts = new();

    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;
    private readonly TextBox _txtLog;
    private readonly Button _btnCancel;
    private readonly Button _btnUndo;
    private readonly Button _btnClose;

    private readonly List<RenameBatch> _executedBatches = new();
    private bool _isRunning;

    public RunProgressForm(
        ConfigService configService,
        HistoryService historyService,
        AppConfig config,
        MenuAction action,
        string folderPath)
    {
        _configService = configService;
        _historyService = historyService;
        _config = config;
        _action = action;
        _folderPath = PathGuard.NormalizeFolderPath(folderPath);

        UiHelper.ApplyFormDefaults(
            this,
            $"Zquick - {action.Title}",
            new Size(780, 540),
            new Size(560, 380));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = UiHelper.SPad(12),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblHeader = new Label
        {
            Text = $"动作：{action.Title}\n目录：{_folderPath}",
            Font = UiHelper.BoldFont,
            AutoSize = true,
            Margin = UiHelper.SPad(0, 0, 0, 6)
        };
        root.Controls.Add(lblHeader, 0, 0);

        var progressPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true
        };
        _lblStatus = new Label
        {
            Text = "准备就绪...",
            AutoSize = true,
            Margin = UiHelper.SPad(0, 0, 0, 4)
        };
        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = UiHelper.S(22),
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        progressPanel.Controls.Add(_lblStatus, 0, 0);
        progressPanel.Controls.Add(_progressBar, 0, 1);
        root.Controls.Add(progressPanel, 0, 1);

        _txtLog = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = UiHelper.MonoFont,
            BackColor = Color.FromArgb(250, 250, 250),
            Margin = UiHelper.SPad(0, 8, 0, 8)
        };
        root.Controls.Add(_txtLog, 0, 2);

        var footer = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        _btnUndo = UiHelper.CreateButton("撤销本次重命名", 136, 30);
        _btnUndo.Enabled = false;
        _btnUndo.Click += OnUndoExecutedClick;

        _btnCancel = UiHelper.CreateButton("停止取消", 96, 30);
        _btnCancel.Click += (_, _) =>
        {
            _btnCancel.Enabled = false;
            _lblStatus.Text = "正在取消...";
            _cts.Cancel();
        };

        _btnClose = UiHelper.CreateButton("关闭", 90, 30);
        _btnClose.Enabled = false;
        _btnClose.Margin = new Padding(0);
        _btnClose.Click += (_, _) => Close();

        footer.Controls.Add(_btnUndo);
        footer.Controls.Add(_btnCancel);
        footer.Controls.Add(_btnClose);
        root.Controls.Add(footer, 0, 3);

        Controls.Add(root);

        Shown += OnFormShownAsync;
        FormClosing += OnFormClosing;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isRunning)
        {
            _cts.Cancel();
        }
    }

    private async void OnFormShownAsync(object? sender, EventArgs e)
    {
        bool allowRoot = _action.Kind == ActionKind.ListFolder;
        if (!PathGuard.ValidateFolder(_folderPath, allowRoot, out string error))
        {
            MessageBox.Show(this, error, "路径校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        // 单独运行的目录概览直接弹出更直观的目录树报告窗口
        if (_action.Kind == ActionKind.ListFolder)
        {
            try
            {
                FolderOverviewResult overview = ActionExecutor.BuildFolderOverview(_folderPath);
                string summary = $"子文件夹：{overview.DirectFolderCount} 个  |  直接文件：{overview.DirectFileCount} 个";
                Hide();
                using var report = new TextReportForm($"目录概览 - {Path.GetFileName(_folderPath)}", summary, overview.ReportText);
                report.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"读取目录失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Close();
            }

            return;
        }

        _isRunning = true;
        try
        {
            bool executed = await RunActionAsync(_action, isPipelineStep: false);
            if (!executed && _executedBatches.Count == 0 && _txtLog.TextLength == 0)
            {
                _isRunning = false;
                Close();
                return;
            }

            _lblStatus.Text = _cts.IsCancellationRequested ? "已取消" : "全部任务已结束";
            if (!_cts.IsCancellationRequested)
            {
                _progressBar.Value = _progressBar.Maximum;
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("[已取消] 操作已被用户中止。");
            _lblStatus.Text = "操作已取消";
        }
        catch (Exception ex)
        {
            AppendLog($"[错误] {ex.Message}");
            _lblStatus.Text = "执行出错";
            MessageBox.Show(this, ex.Message, "执行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isRunning = false;
            _btnCancel.Enabled = false;
            _btnClose.Enabled = true;
            _btnUndo.Enabled = _executedBatches.Any(b => !b.Undone);
            AcceptButton = _btnClose;
        }
    }

    private async Task<bool> RunActionAsync(MenuAction action, bool isPipelineStep)
    {
        _cts.Token.ThrowIfCancellationRequested();

        if (!isPipelineStep && !PathGuard.ValidateFolder(_folderPath, action.Kind == ActionKind.ListFolder, out string err))
        {
            throw new InvalidOperationException(err);
        }

        switch (action.Kind)
        {
            case ActionKind.ListFolder:
                FolderOverviewResult overview = await Task.Run(() => ActionExecutor.BuildFolderOverview(_folderPath), _cts.Token);
                AppendLog(overview.ReportText);
                UpdateProgress(1, 1);
                return true;

            case ActionKind.AddFolderPrefix:
            case ActionKind.RenameFilesByFolder:
            case ActionKind.RemovePrefix:
                return await RunRenameActionAsync(action);

            case ActionKind.CompressSubFolders:
                string? runtimePwd = null;
                if (action.UsePassword)
                {
                    bool hasStored = false;
                    if (!string.IsNullOrEmpty(action.EncryptedPassword))
                    {
                        try
                        {
                            hasStored = !string.IsNullOrEmpty(DpapiHelper.UnprotectString(action.EncryptedPassword));
                        }
                        catch
                        {
                            hasStored = false;
                        }
                    }

                    if (!hasStored && action.PromptPasswordIfEmpty)
                    {
                        runtimePwd = InputDialog.ShowPasswordPrompt(
                            this,
                            action.Title,
                            $"请输入用于加密压缩（{Path.GetFileName(_folderPath)}）的密码（留空则不加密）：");
                        if (runtimePwd == null)
                        {
                            AppendLog("[取消] 用户取消了密码输入。");
                            return false;
                        }
                    }
                }

                _lblStatus.Text = $"正在执行：{action.Title}...";
                await ActionExecutor.ExecuteCompressSubFoldersAsync(
                    _config,
                    action,
                    _folderPath,
                    runtimePwd,
                    AppendLogSafe,
                    UpdateProgressSafe,
                    _cts.Token);
                return true;

            case ActionKind.RotateVideos:
                _lblStatus.Text = $"正在执行：{action.Title}...";
                await ActionExecutor.ExecuteRotateVideosAsync(
                    _config,
                    action,
                    _folderPath,
                    AppendLogSafe,
                    UpdateProgressSafe,
                    _cts.Token);
                return true;

            case ActionKind.CustomScript:
                _lblStatus.Text = $"正在运行脚本：{action.Title}...";
                _progressBar.Style = ProgressBarStyle.Marquee;
                try
                {
                    await ActionExecutor.ExecuteCustomScriptAsync(
                        action,
                        _folderPath,
                        AppendLogSafe,
                        _cts.Token);
                }
                finally
                {
                    _progressBar.Style = ProgressBarStyle.Continuous;
                }

                UpdateProgress(1, 1);
                return true;

            case ActionKind.Pipeline:
                AppendLog($"=== 开始执行组合流水线：{action.Title}（共 {action.StepActionIds.Count} 步）===");
                int stepNo = 0;
                foreach (string stepId in action.StepActionIds)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    stepNo++;
                    MenuAction? stepAction = _config.Actions.FirstOrDefault(
                        a => string.Equals(a.Id, stepId, StringComparison.OrdinalIgnoreCase));

                    if (stepAction == null)
                    {
                        AppendLog($"[警告] 第 {stepNo} 步找不到动作 ID：{stepId}，已跳过。");
                        continue;
                    }

                    if (stepAction.Kind == ActionKind.Pipeline)
                    {
                        AppendLog($"[警告] 跳过嵌套流水线：{stepAction.Title}");
                        continue;
                    }

                    AppendLog(string.Empty);
                    AppendLog($"--- 步骤 {stepNo}/{action.StepActionIds.Count}：{stepAction.Title} ---");
                    bool stepOk = await RunActionAsync(stepAction, isPipelineStep: true);
                    if (!stepOk)
                    {
                        AppendLog($"[中止] 步骤 {stepNo} 已取消，组合流水线停止后续步骤。");
                        return false;
                    }
                }

                AppendLog("=== 组合流水线全部执行完毕 ===");
                return true;

            default:
                AppendLog($"未识别的动作类型：{action.Kind}");
                return false;
        }
    }

    private async Task<bool> RunRenameActionAsync(MenuAction action)
    {
        string? runtimePrefix = null;
        if (action.Kind == ActionKind.AddFolderPrefix && action.PromptForInput)
        {
            runtimePrefix = InputDialog.ShowTextPrompt(
                this,
                action.Title,
                "请输入要添加到所有子文件夹名称前面的前缀：",
                action.DefaultPrefix,
                _config.RecentPrefixes);

            if (string.IsNullOrEmpty(runtimePrefix))
            {
                AppendLog("[取消] 未输入前缀，已取消操作。");
                return false;
            }

            if (PathGuard.ContainsInvalidFileNameChars(runtimePrefix!))
            {
                MessageBox.Show(this, "前缀包含 Windows 非法文件名字符（如 \\ / : * ? \" < > |）。", "无效前缀", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _configService.RecordRecentPrefix(_config, runtimePrefix!);
        }

        _lblStatus.Text = "正在扫描重命名目标...";
        List<RenamePlanItem> plan = await Task.Run(
            () => ActionExecutor.BuildRenamePlan(action, _folderPath, runtimePrefix),
            _cts.Token);

        int runnableCount = plan.Count(p => !p.Skipped);
        if (plan.Count == 0 || runnableCount == 0)
        {
            foreach (RenamePlanItem skipped in plan.Where(p => p.Skipped))
            {
                AppendLog($"[跳过] {skipped.OldName} ({skipped.SkipReason})");
            }

            AppendLog("没有需要重命名的文件或文件夹。");
            UpdateProgress(1, 1);
            return true;
        }

        if (action.ConfirmBeforeRun)
        {
            using var preview = new RenamePreviewForm(action.Title, _folderPath, plan);
            if (preview.ShowDialog(this) != DialogResult.OK)
            {
                AppendLog($"[取消] 用户取消了「{action.Title}」的重命名确认。");
                return false;
            }
        }

        _lblStatus.Text = $"正在执行重命名：{action.Title}...";
        RenameBatch? batch = await Task.Run(
            () => ActionExecutor.ExecuteRenamePlan(
                plan,
                action.Title,
                _folderPath,
                _historyService,
                AppendLogSafe,
                UpdateProgressSafe,
                _cts.Token),
            _cts.Token);

        if (batch != null)
        {
            _executedBatches.Add(batch);
        }

        return true;
    }

    private void OnUndoExecutedClick(object? sender, EventArgs e)
    {
        List<RenameBatch> undoable = _executedBatches.Where(b => !b.Undone).ToList();
        if (undoable.Count == 0)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                $"确定要撤销本次执行产生的 {undoable.Sum(b => b.Records.Count)} 条重命名记录吗？",
                "确认撤销",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        AppendLog(string.Empty);
        AppendLog("--- 开始撤销本次重命名 ---");
        int totalRestored = 0;
        int totalSkipped = 0;

        for (int i = undoable.Count - 1; i >= 0; i--)
        {
            (int restored, int skipped) = _historyService.UndoBatch(undoable[i].Id, AppendLog);
            undoable[i].Undone = true;
            totalRestored += restored;
            totalSkipped += skipped;
        }

        AppendLog($"撤销结束：已还原 {totalRestored} 项，跳过 {totalSkipped} 项。");
        _btnUndo.Enabled = false;
    }

    private void AppendLogSafe(string line)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke((Action)(() => AppendLog(line)));
        }
        catch
        {
            // 忽略窗口关闭期间的回调
        }
    }

    private void AppendLog(string line)
    {
        if (IsDisposed)
        {
            return;
        }

        _txtLog.AppendText(line + Environment.NewLine);
    }

    private void UpdateProgressSafe(int current, int total)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke((Action)(() => UpdateProgress(current, total)));
        }
        catch
        {
            // 忽略窗口关闭期间的回调
        }
    }

    private void UpdateProgress(int current, int total)
    {
        if (total <= 0)
        {
            _progressBar.Value = 0;
            return;
        }

        int percent = Math.Max(0, Math.Min(100, (int)Math.Round(current * 100.0 / total)));
        _progressBar.Value = percent;
        _lblStatus.Text = $"进度：{current} / {total} ({percent}%)";
    }
}
