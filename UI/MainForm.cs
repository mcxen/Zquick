using System.Diagnostics;
using Zquick.Models;
using Zquick.Services;

namespace Zquick.UI;

public class MainForm : Form
{
    private readonly ConfigService _configService;
    private readonly HistoryService _historyService;
    private AppConfig _config;
    private List<RenameBatch> _historyBatches = new();

    // 顶部状态栏
    private readonly Label _lblMenuStatus;
    private readonly Button _btnSyncMenu;
    private readonly Button _btnUninstallMenu;

    // Tab 1: 右键菜单管理
    private readonly DataGridView _gridActions;
    private readonly Label _lblActionLimitHint;
    private bool _suppressGridChange;

    // Tab 2: 全局设置
    private readonly TextBox _txtRootTitle;
    private readonly CheckBox _chkRegFolder;
    private readonly CheckBox _chkRegBackground;
    private readonly TextBox _txtSevenZip;
    private readonly TextBox _txtFfmpeg;
    private readonly CheckBox _chkConfirmByDefault;
    private readonly TextBox _txtDataDir;

    // Tab 3: 撤销历史
    private readonly DataGridView _gridHistoryBatches;
    private readonly DataGridView _gridHistoryDetails;

    public MainForm(ConfigService configService, HistoryService historyService)
    {
        _configService = configService;
        _historyService = historyService;
        _config = _configService.Load();

        UiHelper.ApplyFormDefaults(
            this,
            $"Zquick v{Program.GetDisplayVersion()} - 资源管理器右键快捷工具箱",
            new Size(980, 660));
        MinimumSize = new Size(820, 540);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // 顶部状态栏
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(247, 249, 252),
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 10)
        };

        var topGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblAppTitle = new Label
        {
            Text = "Zquick 文件夹快捷整理与右键菜单管理",
            Font = UiHelper.TitleFont,
            AutoSize = true
        };
        _lblMenuStatus = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 3, 0, 0)
        };
        titleStack.Controls.Add(lblAppTitle, 0, 0);
        titleStack.Controls.Add(_lblMenuStatus, 0, 1);

        var topBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        };
        _btnUninstallMenu = new Button
        {
            Text = "卸载右键菜单",
            Width = 115,
            Height = 32
        };
        _btnUninstallMenu.Click += OnUninstallContextMenuClick;

        _btnSyncMenu = new Button
        {
            Text = "同步到右键菜单",
            Width = 135,
            Height = 32,
            BackColor = Color.FromArgb(0, 103, 192),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSyncMenu.FlatAppearance.BorderSize = 0;
        _btnSyncMenu.Click += OnSyncContextMenuClick;

        topBtns.Controls.Add(_btnUninstallMenu);
        topBtns.Controls.Add(_btnSyncMenu);

        topGrid.Controls.Add(titleStack, 0, 0);
        topGrid.Controls.Add(topBtns, 1, 0);
        topPanel.Controls.Add(topGrid);
        root.Controls.Add(topPanel, 0, 0);

        // 主选项卡
        var tabs = new TabControl { Dock = DockStyle.Fill };

        // ---------- Tab 1: 右键菜单管理 ----------
        var tabActions = new TabPage("右键菜单管理") { Padding = new Padding(10) };
        var tabActionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1
        };
        tabActionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabActionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tabActionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var actionToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        var btnAdd = new Button { Text = "新增动作", Width = 88, Height = 29 };
        var btnEdit = new Button { Text = "编辑", Width = 72, Height = 29 };
        var btnDelete = new Button { Text = "删除", Width = 72, Height = 29 };
        var btnMoveUp = new Button { Text = "上移", Width = 70, Height = 29 };
        var btnMoveDown = new Button { Text = "下移", Width = 70, Height = 29 };
        var btnTestRun = new Button { Text = "测试运行...", Width = 96, Height = 29 };
        var btnRestoreDefault = new Button { Text = "恢复默认列表", Width = 108, Height = 29 };

        btnAdd.Click += OnAddActionClick;
        btnEdit.Click += OnEditActionClick;
        btnDelete.Click += OnDeleteActionClick;
        btnMoveUp.Click += (_, _) => MoveSelectedAction(-1);
        btnMoveDown.Click += (_, _) => MoveSelectedAction(1);
        btnTestRun.Click += OnTestRunClick;
        btnRestoreDefault.Click += OnRestoreDefaultClick;

        actionToolbar.Controls.AddRange(new Control[]
        {
            btnAdd, btnEdit, btnDelete, btnMoveUp, btnMoveDown, btnTestRun, btnRestoreDefault
        });
        tabActionsLayout.Controls.Add(actionToolbar, 0, 0);

        _gridActions = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleGrid(_gridActions, readOnly: false);
        _gridActions.Columns.AddRange(
            new DataGridViewCheckBoxColumn { HeaderText = "启用", Width = 48 },
            new DataGridViewCheckBoxColumn { HeaderText = "分割线", Width = 58 },
            new DataGridViewTextBoxColumn { HeaderText = "菜单名称", Width = 250, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "动作类型", Width = 115, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "参数摘要", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 135, ReadOnly = true });

        _gridActions.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_gridActions.IsCurrentCellDirty)
            {
                _gridActions.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _gridActions.CellValueChanged += OnActionGridCellValueChanged;
        _gridActions.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                OnEditActionClick(null, EventArgs.Empty);
            }
        };
        tabActionsLayout.Controls.Add(_gridActions, 0, 1);

        _lblActionLimitHint = new Label
        {
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 6, 0, 0)
        };
        tabActionsLayout.Controls.Add(_lblActionLimitHint, 0, 2);
        tabActions.Controls.Add(tabActionsLayout);

        // ---------- Tab 2: 全局设置 ----------
        var tabSettings = new TabPage("全局设置") { Padding = new Padding(14) };
        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true
        };
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));

        _txtRootTitle = new TextBox { Dock = DockStyle.Fill };
        _chkRegFolder = new CheckBox { Text = "右键点击【文件夹】时显示", AutoSize = true };
        _chkRegBackground = new CheckBox { Text = "右键点击【文件夹内空白处】时显示", AutoSize = true };
        _txtSevenZip = new TextBox { Dock = DockStyle.Fill };
        _txtFfmpeg = new TextBox { Dock = DockStyle.Fill };
        _chkConfirmByDefault = new CheckBox { Text = "新建重命名动作默认开启「执行前预览确认」", AutoSize = true };
        _txtDataDir = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = SystemColors.ControlLight };

        AddSettingRow(settingsLayout, 0, "一级菜单名称：", _txtRootTitle, null);

        var regPosPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        regPosPanel.Controls.AddRange(new Control[] { _chkRegFolder, _chkRegBackground });
        AddSettingRow(settingsLayout, 1, "右键注册位置：", regPosPanel, null);

        var sevenZipBtns = CreatePathButtons(
            onBrowse: () => BrowseExe(_txtSevenZip, "选择 7z.exe", "7-Zip (7z.exe)|7z.exe|所有文件 (*.*)|*.*"),
            onDetect: () => AutoDetectTool(_txtSevenZip, "7z.exe", @"C:\Program Files\7-Zip\7z.exe", @"C:\Program Files (x86)\7-Zip\7z.exe"));
        AddSettingRow(settingsLayout, 2, "7-Zip (7z.exe) 路径：", _txtSevenZip, sevenZipBtns);

        var ffmpegBtns = CreatePathButtons(
            onBrowse: () => BrowseExe(_txtFfmpeg, "选择 ffmpeg.exe", "FFmpeg (ffmpeg.exe)|ffmpeg.exe|所有文件 (*.*)|*.*"),
            onDetect: () => AutoDetectTool(_txtFfmpeg, "ffmpeg.exe", Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"), @"C:\ffmpeg\bin\ffmpeg.exe"));
        AddSettingRow(settingsLayout, 3, "FFmpeg 路径：", _txtFfmpeg, ffmpegBtns);

        AddSettingRow(settingsLayout, 4, "重命名确认策略：", _chkConfirmByDefault, null);

        var btnOpenDataDir = new Button { Text = "打开配置目录", Width = 110, Height = 27 };
        btnOpenDataDir.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _configService.DataDirectory,
                UseShellExecute = true
            });
        };
        AddSettingRow(settingsLayout, 5, "配置与历史目录：", _txtDataDir, btnOpenDataDir);

        var settingsBtnBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(155, 16, 0, 0)
        };
        var btnSaveSettings = new Button { Text = "保存设置", Width = 100, Height = 32 };
        var btnSaveAndSync = new Button { Text = "保存并同步右键菜单", Width = 155, Height = 32 };
        btnSaveSettings.Click += (_, _) =>
        {
            ApplySettingsFromUi();
            _configService.Save(_config);
            MessageBox.Show(this, "全局设置已保存。", "已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        btnSaveAndSync.Click += (_, _) =>
        {
            ApplySettingsFromUi();
            _configService.Save(_config);
            SyncContextMenuWithFeedback();
        };
        settingsBtnBar.Controls.Add(btnSaveSettings);
        settingsBtnBar.Controls.Add(btnSaveAndSync);

        var win11Hint = new Label
        {
            Text = "说明：本工具通过 HKCU 注册表实现零管理员权限二级级联菜单。\n" +
                   "在 Windows 10 中直接显示在一级右键；在 Windows 11 中位于「显示更多选项」（Shift+右键）内。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Padding = new Padding(155, 12, 0, 0)
        };

        var settingsContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        settingsContainer.Controls.Add(win11Hint);
        settingsContainer.Controls.Add(settingsBtnBar);
        settingsContainer.Controls.Add(settingsLayout);
        // 调整 Dock = Top 的堆叠顺序（后加的在上方）
        settingsContainer.Controls.SetChildIndex(win11Hint, 0);
        settingsContainer.Controls.SetChildIndex(settingsBtnBar, 1);
        settingsContainer.Controls.SetChildIndex(settingsLayout, 2);
        tabSettings.Controls.Add(settingsContainer);

        // ---------- Tab 3: 撤销历史 ----------
        var tabHistory = new TabPage("撤销历史") { Padding = new Padding(10) };
        var historyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        historyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var historyToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        var btnRefreshHist = new Button { Text = "刷新", Width = 75, Height = 29 };
        var btnUndoBatch = new Button { Text = "撤销选中批次", Width = 115, Height = 29 };
        var btnClearHist = new Button { Text = "清空历史", Width = 90, Height = 29 };
        btnRefreshHist.Click += (_, _) => BindHistoryBatches();
        btnUndoBatch.Click += OnUndoSelectedBatchClick;
        btnClearHist.Click += OnClearHistoryClick;
        historyToolbar.Controls.AddRange(new Control[] { btnRefreshHist, btnUndoBatch, btnClearHist });
        historyLayout.Controls.Add(historyToolbar, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 210
        };

        _gridHistoryBatches = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleGrid(_gridHistoryBatches, readOnly: true);
        _gridHistoryBatches.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "时间", Width = 145 },
            new DataGridViewTextBoxColumn { HeaderText = "动作", Width = 220 },
            new DataGridViewTextBoxColumn { HeaderText = "目标文件夹", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "条目数", Width = 70 },
            new DataGridViewTextBoxColumn { HeaderText = "状态", Width = 85 });
        _gridHistoryBatches.SelectionChanged += (_, _) => BindSelectedBatchDetails();
        split.Panel1.Controls.Add(_gridHistoryBatches);

        _gridHistoryDetails = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleGrid(_gridHistoryDetails, readOnly: true);
        _gridHistoryDetails.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "类型", Width = 65 },
            new DataGridViewTextBoxColumn { HeaderText = "原路径", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { HeaderText = "新路径", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        split.Panel2.Controls.Add(_gridHistoryDetails);

        historyLayout.Controls.Add(split, 0, 1);
        tabHistory.Controls.Add(historyLayout);

        tabs.TabPages.AddRange(new[] { tabActions, tabSettings, tabHistory });
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedTab == tabHistory)
            {
                BindHistoryBatches();
            }
        };

        root.Controls.Add(tabs, 0, 1);
        Controls.Add(root);

        BindActionsGrid();
        BindSettingsToUi();
        RefreshContextMenuStatus();
    }

    private void BindActionsGrid()
    {
        _suppressGridChange = true;
        try
        {
            int previousRow = _gridActions.CurrentRow?.Index ?? -1;
            _gridActions.Rows.Clear();

            foreach (MenuAction act in _config.Actions)
            {
                _gridActions.Rows.Add(
                    act.Enabled,
                    act.SeparatorBefore,
                    act.Title,
                    MenuAction.GetKindDisplayName(act.Kind),
                    act.GetSummary(),
                    act.Id);
            }

            if (previousRow >= 0 && previousRow < _gridActions.Rows.Count)
            {
                _gridActions.Rows[previousRow].Selected = true;
                _gridActions.CurrentCell = _gridActions.Rows[previousRow].Cells[2];
            }

            int enabledCount = _config.Actions.Count(a => a.Enabled);
            string limitWarn = enabledCount > ContextMenuService.MaxActionItems
                ? $"（警告：当前已启用 {enabledCount} 项，超出 Windows 静态级联菜单 {ContextMenuService.MaxActionItems} 项上限，超出部分将被忽略）"
                : "";
            _lblActionLimitHint.Text =
                $"共 {_config.Actions.Count} 个动作，已启用 {enabledCount}/{ContextMenuService.MaxActionItems} 项。" +
                $"双击某行可编辑详细参数；修改后点击右上角「同步到右键菜单」生效。{limitWarn}";
            _lblActionLimitHint.ForeColor = enabledCount > ContextMenuService.MaxActionItems ? Color.Firebrick : Color.DimGray;
        }
        finally
        {
            _suppressGridChange = false;
        }
    }

    private void OnActionGridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressGridChange || e.RowIndex < 0 || e.RowIndex >= _config.Actions.Count)
        {
            return;
        }

        if (e.ColumnIndex == 0)
        {
            _config.Actions[e.RowIndex].Enabled = Convert.ToBoolean(_gridActions.Rows[e.RowIndex].Cells[0].Value);
            _configService.Save(_config);
            BindActionsGrid();
        }
        else if (e.ColumnIndex == 1)
        {
            _config.Actions[e.RowIndex].SeparatorBefore = Convert.ToBoolean(_gridActions.Rows[e.RowIndex].Cells[1].Value);
            _configService.Save(_config);
        }
    }

    private int GetSelectedActionIndex()
    {
        if (_gridActions.SelectedRows.Count > 0)
        {
            return _gridActions.SelectedRows[0].Index;
        }

        return _gridActions.CurrentRow?.Index ?? -1;
    }

    private void OnAddActionClick(object? sender, EventArgs e)
    {
        using var dlg = new ActionEditForm(_config, null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Actions.Add(dlg.ResultAction);
            _configService.Save(_config);
            BindActionsGrid();
            SelectActionRow(_config.Actions.Count - 1);
        }
    }

    private void OnEditActionClick(object? sender, EventArgs e)
    {
        int idx = GetSelectedActionIndex();
        if (idx < 0 || idx >= _config.Actions.Count)
        {
            MessageBox.Show(this, "请先选择要编辑的动作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new ActionEditForm(_config, _config.Actions[idx]);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.Actions[idx] = dlg.ResultAction;
            _configService.Save(_config);
            BindActionsGrid();
            SelectActionRow(idx);
        }
    }

    private void OnDeleteActionClick(object? sender, EventArgs e)
    {
        int idx = GetSelectedActionIndex();
        if (idx < 0 || idx >= _config.Actions.Count)
        {
            return;
        }

        MenuAction target = _config.Actions[idx];
        if (MessageBox.Show(
                this,
                $"确定删除动作「{target.Title}」吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _config.Actions.RemoveAt(idx);
        _configService.Save(_config);
        BindActionsGrid();
    }

    private void MoveSelectedAction(int delta)
    {
        int idx = GetSelectedActionIndex();
        if (idx < 0 || idx >= _config.Actions.Count)
        {
            return;
        }

        int newIdx = idx + delta;
        if (newIdx < 0 || newIdx >= _config.Actions.Count)
        {
            return;
        }

        MenuAction item = _config.Actions[idx];
        _config.Actions.RemoveAt(idx);
        _config.Actions.Insert(newIdx, item);
        _configService.Save(_config);
        BindActionsGrid();
        SelectActionRow(newIdx);
    }

    private void SelectActionRow(int idx)
    {
        if (idx >= 0 && idx < _gridActions.Rows.Count)
        {
            _gridActions.ClearSelection();
            _gridActions.Rows[idx].Selected = true;
            _gridActions.CurrentCell = _gridActions.Rows[idx].Cells[2];
        }
    }

    private void OnTestRunClick(object? sender, EventArgs e)
    {
        int idx = GetSelectedActionIndex();
        if (idx < 0 || idx >= _config.Actions.Count)
        {
            MessageBox.Show(this, "请先在列表中选中一个要测试运行的动作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var fbd = new FolderBrowserDialog
        {
            Description = $"选择要运行「{_config.Actions[idx].Title}」的目标文件夹",
            UseDescriptionForTitle = true
        };
        if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            return;
        }

        using var runForm = new RunProgressForm(
            _configService,
            _historyService,
            _config,
            _config.Actions[idx],
            fbd.SelectedPath);
        runForm.ShowDialog(this);
        BindHistoryBatches();
    }

    private void OnRestoreDefaultClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
                this,
                "确定将动作列表恢复为初始默认配置吗？自定义的动作将会丢失（全局工具路径保留）。",
                "恢复默认列表",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        AppConfig defaults = DefaultConfigFactory.CreateDefault();
        _config.Actions = defaults.Actions;
        _configService.Save(_config);
        BindActionsGrid();
    }

    private void BindSettingsToUi()
    {
        _txtRootTitle.Text = _config.RootMenuTitle;
        _chkRegFolder.Checked = _config.RegisterOnFolder;
        _chkRegBackground.Checked = _config.RegisterOnBackground;
        _txtSevenZip.Text = _config.SevenZipPath;
        _txtFfmpeg.Text = _config.FfmpegPath;
        _chkConfirmByDefault.Checked = _config.ConfirmRenameByDefault;
        _txtDataDir.Text = _configService.DataDirectory;
    }

    private void ApplySettingsFromUi()
    {
        _config.RootMenuTitle = string.IsNullOrWhiteSpace(_txtRootTitle.Text)
            ? "Zquick"
            : _txtRootTitle.Text.Trim();
        _config.RegisterOnFolder = _chkRegFolder.Checked;
        _config.RegisterOnBackground = _chkRegBackground.Checked;
        _config.SevenZipPath = _txtSevenZip.Text.Trim();
        _config.FfmpegPath = _txtFfmpeg.Text.Trim();
        _config.ConfirmRenameByDefault = _chkConfirmByDefault.Checked;
    }

    private void OnSyncContextMenuClick(object? sender, EventArgs e)
    {
        ApplySettingsFromUi();
        _configService.Save(_config);
        SyncContextMenuWithFeedback();
    }

    private void SyncContextMenuWithFeedback()
    {
        try
        {
            string exePath = Program.GetCurrentExePath();
            if (!_config.RegisterOnFolder && !_config.RegisterOnBackground)
            {
                ContextMenuService.UninstallContextMenu();
                RefreshContextMenuStatus();
                MessageBox.Show(this, "由于未勾选任何注册位置，已从右键菜单移除。", "同步完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            (int count, bool truncated) = ContextMenuService.SyncContextMenu(_config, exePath);
            RefreshContextMenuStatus();

            string msg = $"已成功同步到当前用户右键菜单（HKCU），共注册 {count} 项功能（立即生效，无需重启 Explorer）。";
            if (truncated)
            {
                msg += $"\n\n注意：Windows 静态级联菜单最多显示 {ContextMenuService.MaxActionItems} 项，超出部分未写入。";
            }

            MessageBox.Show(this, msg, "同步成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"同步右键菜单失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnUninstallContextMenuClick(object? sender, EventArgs e)
    {
        try
        {
            ContextMenuService.UninstallContextMenu();
            RefreshContextMenuStatus();
            MessageBox.Show(this, "已从 Explorer 右键菜单彻底清理。", "卸载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"卸载右键菜单失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshContextMenuStatus()
    {
        try
        {
            ContextMenuStatus status = ContextMenuService.GetStatus(Program.GetCurrentExePath());
            if (!status.IsRegistered)
            {
                _lblMenuStatus.Text = "右键菜单状态：未注册（点击右侧「同步到右键菜单」即可启用）";
                _lblMenuStatus.ForeColor = Color.DarkOrange;
                return;
            }

            var places = new List<string>();
            if (status.OnFolder)
            {
                places.Add("文件夹");
            }

            if (status.OnBackground)
            {
                places.Add("目录空白处");
            }

            string pathHint = status.PathMatchesCurrent
                ? "路径匹配当前程序"
                : $"警告：当前注册指向其他路径 ({status.RegisteredExePath})，建议重新同步";

            _lblMenuStatus.Text = $"右键菜单状态：已注册 [{string.Join(" + ", places)}]，共 {status.RegisteredItemCount} 项 | {pathHint}";
            _lblMenuStatus.ForeColor = status.PathMatchesCurrent ? Color.ForestGreen : Color.Firebrick;
        }
        catch (Exception ex)
        {
            _lblMenuStatus.Text = $"无法读取注册表状态：{ex.Message}";
            _lblMenuStatus.ForeColor = Color.Firebrick;
        }
    }

    private void BindHistoryBatches()
    {
        _historyBatches = _historyService.LoadAll();
        _gridHistoryBatches.Rows.Clear();
        _gridHistoryDetails.Rows.Clear();

        foreach (RenameBatch batch in _historyBatches)
        {
            _gridHistoryBatches.Rows.Add(
                batch.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                batch.ActionTitle,
                batch.TargetDirectory,
                batch.Records.Count,
                batch.Undone ? "已撤销" : "可撤销");
        }

        if (_gridHistoryBatches.Rows.Count > 0)
        {
            _gridHistoryBatches.Rows[0].Selected = true;
            BindSelectedBatchDetails();
        }
    }

    private void BindSelectedBatchDetails()
    {
        _gridHistoryDetails.Rows.Clear();
        int idx = _gridHistoryBatches.SelectedRows.Count > 0
            ? _gridHistoryBatches.SelectedRows[0].Index
            : -1;

        if (idx < 0 || idx >= _historyBatches.Count)
        {
            return;
        }

        foreach (RenameRecord rec in _historyBatches[idx].Records)
        {
            _gridHistoryDetails.Rows.Add(
                rec.IsDirectory ? "文件夹" : "文件",
                rec.OldPath,
                rec.NewPath);
        }
    }

    private void OnUndoSelectedBatchClick(object? sender, EventArgs e)
    {
        int idx = _gridHistoryBatches.SelectedRows.Count > 0
            ? _gridHistoryBatches.SelectedRows[0].Index
            : -1;
        if (idx < 0 || idx >= _historyBatches.Count)
        {
            MessageBox.Show(this, "请先选择要撤销的批次。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        RenameBatch batch = _historyBatches[idx];
        if (batch.Undone)
        {
            MessageBox.Show(this, "该批次已经撤销过。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(
                this,
                $"确定撤销「{batch.ActionTitle}」产生的 {batch.Records.Count} 条重命名吗？",
                "确认撤销",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        var logs = new List<string>();
        (int restored, int skipped) = _historyService.UndoBatch(batch.Id, line => logs.Add(line));
        BindHistoryBatches();

        MessageBox.Show(
            this,
            $"撤销完成：还原 {restored} 项，跳过 {skipped} 项。",
            "撤销结果",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnClearHistoryClick(object? sender, EventArgs e)
    {
        if (_historyBatches.Count == 0)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                "确定清空全部撤销历史记录吗？清空后将无法一键还原历史重命名。",
                "清空历史",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        _historyService.ClearHistory();
        BindHistoryBatches();
    }

    private Control CreatePathButtons(Action onBrowse, Action onDetect)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(4, 0, 0, 0) };
        var btnBrowse = new Button { Text = "浏览...", Width = 78, Height = 27 };
        var btnDetect = new Button { Text = "自动检测", Width = 86, Height = 27 };
        btnBrowse.Click += (_, _) => onBrowse();
        btnDetect.Click += (_, _) => onDetect();
        panel.Controls.Add(btnBrowse);
        panel.Controls.Add(btnDetect);
        return panel;
    }

    private void BrowseExe(TextBox targetBox, string title, string filter)
    {
        using var ofd = new OpenFileDialog { Title = title, Filter = filter };
        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            targetBox.Text = ofd.FileName;
        }
    }

    private void AutoDetectTool(TextBox targetBox, string exeName, params string[] commonPaths)
    {
        string? found = ProcessRunner.ResolveToolPath(targetBox.Text, exeName, commonPaths);
        if (found != null)
        {
            targetBox.Text = found;
            MessageBox.Show(this, $"已检测到：{found}", "检测成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(this, $"未在常见路径和环境变量 PATH 中找到 {exeName}。", "未找到", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void AddSettingRow(TableLayoutPanel grid, int row, string labelText, Control input, Control? sideButton)
    {
        while (grid.RowCount <= row)
        {
            grid.RowCount++;
        }

        var lbl = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 6, 8)
        };
        input.Margin = new Padding(0, 6, 4, 6);
        grid.Controls.Add(lbl, 0, row);
        grid.Controls.Add(input, 1, row);
        if (sideButton != null)
        {
            sideButton.Margin = new Padding(4, 4, 0, 4);
            grid.Controls.Add(sideButton, 2, row);
        }
    }
}
