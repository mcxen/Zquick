using Zquick.Models;
using Zquick.Services;

namespace Zquick.UI;

public class ActionEditForm : Form
{
    private readonly AppConfig _config;
    private readonly bool _isNew;
    private readonly MenuAction _workingAction;

    // 公共字段控件
    private readonly TextBox _txtTitle;
    private readonly TextBox _txtId;
    private readonly ComboBox _cmbKind;
    private readonly CheckBox _chkEnabled;
    private readonly CheckBox _chkSeparator;
    private readonly CheckBox _chkConfirmBeforeRun;
    private readonly Panel _dynamicPanel;

    // AddFolderPrefix 控件
    private CheckBox _chkPromptPrefix = null!;
    private TextBox _txtDefaultPrefix = null!;
    private CheckBox _chkSkipHiddenPrefix = null!;

    // RenameFilesByFolder 控件
    private ComboBox _cmbRenameScope = null!;
    private CheckBox _chkGroupByExt = null!;
    private CheckBox _chkSingleNoNum = null!;
    private TextBox _txtNumberSep = null!;
    private NumericUpDown _numPadDigits = null!;
    private CheckBox _chkSkipHiddenRename = null!;

    // RemovePrefix 控件
    private TextBox _txtRemovePrefixes = null!;
    private CheckBox _chkRecursiveRemove = null!;
    private CheckBox _chkSkipHiddenRemove = null!;

    // CompressSubFolders 控件
    private ComboBox _cmbArchiveFormat = null!;
    private CheckBox _chkIncludeRoot = null!;
    private CheckBox _chkDeleteSource = null!;
    private CheckBox _chkRecycleBin = null!;
    private CheckBox _chkUsePassword = null!;
    private TextBox _txtPassword = null!;
    private CheckBox _chkShowPwd = null!;
    private CheckBox _chkPromptPwdIfEmpty = null!;

    // RotateVideos 控件
    private ComboBox _cmbAngle = null!;
    private TextBox _txtVideoExts = null!;
    private TextBox _txtOutputSubFolder = null!;
    private ComboBox _cmbRotationMethod = null!;

    // CustomScript 控件
    private TextBox _txtScriptPath = null!;
    private TextBox _txtScriptArgs = null!;
    private CheckBox _chkShowConsole = null!;

    // Pipeline 控件
    private CheckedListBox _clbPipelineSteps = null!;

    public MenuAction ResultAction => _workingAction;

    private sealed class ComboItem<T>
    {
        public string Label { get; }
        public T Value { get; }

        public ComboItem(string label, T value)
        {
            Label = label;
            Value = value;
        }

        public override string ToString() => Label;
    }

    public ActionEditForm(AppConfig config, MenuAction? sourceAction)
    {
        _config = config;
        _isNew = sourceAction == null;
        _workingAction = sourceAction?.Clone() ?? new MenuAction
        {
            Id = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Title = "新菜单动作",
            Enabled = true,
            Kind = ActionKind.RenameFilesByFolder,
            ConfirmBeforeRun = config.ConfirmRenameByDefault
        };

        UiHelper.ApplyFormDefaults(
            this,
            _isNew ? "新增右键动作" : $"编辑右键动作 - {_workingAction.Title}",
            new Size(680, 650),
            new Size(560, 520));
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = UiHelper.SPad(14),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 公共信息区
        var commonGroup = new GroupBox
        {
            Text = "基本菜单信息",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = UiHelper.SPad(10)
        };

        var commonGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true
        };
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        commonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtTitle = new TextBox { Dock = DockStyle.Fill, Text = _workingAction.Title };
        _txtId = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _workingAction.Id,
            ReadOnly = !_isNew
        };

        _cmbKind = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (ActionKind kind in (ActionKind[])Enum.GetValues(typeof(ActionKind)))
        {
            _cmbKind.Items.Add(new ComboItem<ActionKind>(MenuAction.GetKindDisplayName(kind), kind));
        }
        SelectComboValue(_cmbKind, _workingAction.Kind);

        _chkEnabled = new CheckBox
        {
            Text = "启用（同步到右键菜单）",
            AutoSize = true,
            Checked = _workingAction.Enabled,
            Margin = UiHelper.SPad(0, 0, 12, 4)
        };
        _chkSeparator = new CheckBox
        {
            Text = "在此菜单项上方显示分割线",
            AutoSize = true,
            Checked = _workingAction.SeparatorBefore,
            Margin = UiHelper.SPad(0, 0, 12, 4)
        };
        _chkConfirmBeforeRun = new CheckBox
        {
            Text = "重命名前弹窗预览并确认",
            AutoSize = true,
            Checked = _workingAction.ConfirmBeforeRun,
            Margin = UiHelper.SPad(0, 0, 0, 4)
        };

        AddLabeledRow(commonGrid, 0, "菜单显示名称：", _txtTitle);
        AddLabeledRow(commonGrid, 1, "动作唯一 ID：", _txtId);
        AddLabeledRow(commonGrid, 2, "动作类型：", _cmbKind);

        var flagsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = UiHelper.SPad(0, 4, 0, 0)
        };
        flagsPanel.Controls.AddRange(new Control[] { _chkEnabled, _chkSeparator, _chkConfirmBeforeRun });
        commonGrid.Controls.Add(new Label
        {
            Text = "菜单选项：",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = UiHelper.SPad(0, 6, 8, 6)
        }, 0, 3);
        commonGrid.Controls.Add(flagsPanel, 1, 3);

        commonGroup.Controls.Add(commonGrid);
        root.Controls.Add(commonGroup, 0, 0);

        // 动态参数区
        var paramGroup = new GroupBox
        {
            Text = "动作专属参数",
            Dock = DockStyle.Fill,
            Padding = UiHelper.SPad(10)
        };
        _dynamicPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        paramGroup.Controls.Add(_dynamicPanel);
        root.Controls.Add(paramGroup, 0, 1);

        // 底部按钮
        var btnBar = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = UiHelper.SPad(0, 8, 0, 0)
        };
        Button btnSave = UiHelper.CreateButton("确定保存", 104, 30, primary: true);
        btnSave.Click += OnSaveClick;
        Button btnCancel = UiHelper.CreateButton("取消", 90, 30);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Margin = new Padding(0);

        btnBar.Controls.Add(btnSave);
        btnBar.Controls.Add(btnCancel);
        root.Controls.Add(btnBar, 0, 2);

        Controls.Add(root);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        _cmbKind.SelectedIndexChanged += (_, _) => RebuildDynamicPanel();
        RebuildDynamicPanel();
    }

    private ActionKind SelectedKind =>
        _cmbKind.SelectedItem is ComboItem<ActionKind> item ? item.Value : ActionKind.RenameFilesByFolder;

    private void RebuildDynamicPanel()
    {
        _dynamicPanel.Controls.Clear();
        ActionKind kind = SelectedKind;
        _chkConfirmBeforeRun.Enabled = kind is ActionKind.AddFolderPrefix
            or ActionKind.RenameFilesByFolder
            or ActionKind.RemovePrefix;

        Control content = kind switch
        {
            ActionKind.ListFolder => BuildListFolderPanel(),
            ActionKind.AddFolderPrefix => BuildAddPrefixPanel(),
            ActionKind.RenameFilesByFolder => BuildRenameFilesPanel(),
            ActionKind.RemovePrefix => BuildRemovePrefixPanel(),
            ActionKind.CompressSubFolders => BuildCompressPanel(),
            ActionKind.RotateVideos => BuildRotateVideoPanel(),
            ActionKind.CustomScript => BuildCustomScriptPanel(),
            ActionKind.Pipeline => BuildPipelinePanel(),
            _ => new Label { Text = "暂不支持的动作类型。", AutoSize = true }
        };

        content.Dock = DockStyle.Top;
        _dynamicPanel.Controls.Add(content);
    }

    private Control BuildListFolderPanel()
    {
        return new Label
        {
            Text = "统计当前文件夹内的直接子文件夹数量、文件数量与总大小，并在弹窗中生成完整目录树，支持一键复制到剪贴板。",
            AutoSize = true,
            MaximumSize = new Size(UiHelper.S(580), 0),
            Padding = UiHelper.SPad(4)
        };
    }

    private Control BuildAddPrefixPanel()
    {
        var panel = CreateTwoColumnGrid();

        _chkPromptPrefix = new CheckBox
        {
            Text = "执行前弹窗输入前缀（支持从历史前缀下拉选择）",
            AutoSize = true,
            Checked = _workingAction.PromptForInput
        };
        _txtDefaultPrefix = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _workingAction.DefaultPrefix
        };
        _chkSkipHiddenPrefix = new CheckBox
        {
            Text = "跳过隐藏和系统属性文件夹",
            AutoSize = true,
            Checked = _workingAction.SkipHiddenAndSystem
        };

        AddFullSpanRow(panel, 0, _chkPromptPrefix);
        AddLabeledRow(panel, 1, "默认/固定前缀：", _txtDefaultPrefix);
        AddFullSpanRow(panel, 2, _chkSkipHiddenPrefix);
        return panel;
    }

    private Control BuildRenameFilesPanel()
    {
        var panel = CreateTwoColumnGrid();

        _cmbRenameScope = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbRenameScope.Items.Add(new ComboItem<RenameScope>("当前文件夹的直接子文件（用当前文件夹名命名）", RenameScope.CurrentFolder));
        _cmbRenameScope.Items.Add(new ComboItem<RenameScope>("所有子文件夹内的文件（用各自所属子文件夹名命名）", RenameScope.SubFolders));
        SelectComboValue(_cmbRenameScope, _workingAction.RenameScope);

        _chkGroupByExt = new CheckBox
        {
            Text = "按扩展名分别分组编号（例如同目录只有1个视频和1个字幕时，不互相抢序号）",
            AutoSize = true,
            Checked = _workingAction.GroupByExtension
        };
        _chkSingleNoNum = new CheckBox
        {
            Text = "某分组仅含 1 个文件时不加序号（直接命名为 文件夹名.扩展名）",
            AutoSize = true,
            Checked = _workingAction.SingleFileNoNumber
        };
        _txtNumberSep = new TextBox
        {
            Width = UiHelper.S(110),
            Text = _workingAction.NumberSeparator
        };
        _numPadDigits = new NumericUpDown
        {
            Width = UiHelper.S(110),
            Minimum = 1,
            Maximum = 6,
            Value = Math.Max(1, Math.Min(6, _workingAction.PadDigits))
        };
        _chkSkipHiddenRename = new CheckBox
        {
            Text = "跳过隐藏和系统文件（如 desktop.ini / Thumbs.db）",
            AutoSize = true,
            Checked = _workingAction.SkipHiddenAndSystem
        };

        AddLabeledRow(panel, 0, "重命名范围：", _cmbRenameScope);
        AddLabeledRow(panel, 1, "序号分隔符：", _txtNumberSep);
        AddLabeledRow(panel, 2, "序号最小位数：", _numPadDigits);
        AddFullSpanRow(panel, 3, _chkGroupByExt);
        AddFullSpanRow(panel, 4, _chkSingleNoNum);
        AddFullSpanRow(panel, 5, _chkSkipHiddenRename);
        return panel;
    }

    private Control BuildRemovePrefixPanel()
    {
        var panel = CreateTwoColumnGrid();

        var lblTip = new Label
        {
            Text = "要移除的前缀列表（每行一个，匹配时不区分大小写）：",
            AutoSize = true,
            Margin = UiHelper.SPad(0, 0, 0, 4)
        };
        _txtRemovePrefixes = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = UiHelper.S(120),
            Dock = DockStyle.Fill,
            Text = string.Join(Environment.NewLine, _workingAction.RemovePrefixes)
        };
        _chkRecursiveRemove = new CheckBox
        {
            Text = "递归处理所有子文件夹和深层文件（由深到浅安全重命名）",
            AutoSize = true,
            Checked = _workingAction.Recursive
        };
        _chkSkipHiddenRemove = new CheckBox
        {
            Text = "跳过隐藏和系统文件/目录",
            AutoSize = true,
            Checked = _workingAction.SkipHiddenAndSystem
        };

        AddFullSpanRow(panel, 0, lblTip);
        AddFullSpanRow(panel, 1, _txtRemovePrefixes);
        AddFullSpanRow(panel, 2, _chkRecursiveRemove);
        AddFullSpanRow(panel, 3, _chkSkipHiddenRemove);
        return panel;
    }

    private Control BuildCompressPanel()
    {
        var panel = CreateTwoColumnGrid();

        _cmbArchiveFormat = new ComboBox { Width = UiHelper.S(130), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbArchiveFormat.Items.AddRange(new object[] { "zip", "7z" });
        _cmbArchiveFormat.SelectedItem = string.Equals(_workingAction.ArchiveFormat, "7z", StringComparison.OrdinalIgnoreCase)
            ? "7z"
            : "zip";

        _chkIncludeRoot = new CheckBox
        {
            Text = "压缩包内保留子文件夹自身外层目录（对应 7z 的 dir\\；不选则为 dir\\* 平铺）",
            AutoSize = true,
            Checked = _workingAction.IncludeRootFolder
        };
        _chkDeleteSource = new CheckBox
        {
            Text = "严格验证 7z.exe 退出码为 0 后删除源文件夹",
            AutoSize = true,
            Checked = _workingAction.DeleteSourceAfterSuccess
        };
        _chkRecycleBin = new CheckBox
        {
            Text = "删除源文件夹时优先移入回收站（推荐，更安全）",
            AutoSize = true,
            Checked = _workingAction.MoveDeletedToRecycleBin,
            Enabled = _workingAction.DeleteSourceAfterSuccess
        };
        _chkDeleteSource.CheckedChanged += (_, _) => _chkRecycleBin.Enabled = _chkDeleteSource.Checked;

        _chkUsePassword = new CheckBox
        {
            Text = "启用密码加密（ZIP 使用 AES-256，7z 同时加密文件名）",
            AutoSize = true,
            Checked = _workingAction.UsePassword
        };

        string existingPwd = "";
        if (!string.IsNullOrEmpty(_workingAction.EncryptedPassword))
        {
            try
            {
                existingPwd = DpapiHelper.UnprotectString(_workingAction.EncryptedPassword);
            }
            catch
            {
                existingPwd = "";
            }
        }

        _txtPassword = new TextBox
        {
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true,
            Text = existingPwd,
            Enabled = _workingAction.UsePassword
        };
        _chkShowPwd = new CheckBox
        {
            Text = "显示密码",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = UiHelper.SPad(6, 2, 0, 2),
            Enabled = _workingAction.UsePassword
        };
        _chkShowPwd.CheckedChanged += (_, _) => _txtPassword.UseSystemPasswordChar = !_chkShowPwd.Checked;

        _chkPromptPwdIfEmpty = new CheckBox
        {
            Text = "若预设密码留空，则在执行前弹窗输入密码（不把密码保存在磁盘）",
            AutoSize = true,
            Checked = _workingAction.PromptPasswordIfEmpty,
            Enabled = _workingAction.UsePassword
        };

        _chkUsePassword.CheckedChanged += (_, _) =>
        {
            bool enabled = _chkUsePassword.Checked;
            _txtPassword.Enabled = enabled;
            _chkShowPwd.Enabled = enabled;
            _chkPromptPwdIfEmpty.Enabled = enabled;
        };

        var pwdPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true
        };
        pwdPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pwdPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pwdPanel.Controls.Add(_txtPassword, 0, 0);
        pwdPanel.Controls.Add(_chkShowPwd, 1, 0);

        AddLabeledRow(panel, 0, "压缩格式：", _cmbArchiveFormat);
        AddFullSpanRow(panel, 1, _chkIncludeRoot);
        AddFullSpanRow(panel, 2, _chkDeleteSource);
        AddFullSpanRow(panel, 3, _chkRecycleBin);
        AddFullSpanRow(panel, 4, _chkUsePassword);
        AddLabeledRow(panel, 5, "预设密码(DPAPI加密)：", pwdPanel);
        AddFullSpanRow(panel, 6, _chkPromptPwdIfEmpty);
        return panel;
    }

    private Control BuildRotateVideoPanel()
    {
        var panel = CreateTwoColumnGrid();

        _cmbAngle = new ComboBox { Width = UiHelper.S(150), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbAngle.Items.Add(new ComboItem<int>("顺时针 90°", 90));
        _cmbAngle.Items.Add(new ComboItem<int>("旋转 180°", 180));
        _cmbAngle.Items.Add(new ComboItem<int>("顺时针 270°", 270));
        int targetAngle = _workingAction.RotationAngle is 90 or 180 or 270 ? _workingAction.RotationAngle : 90;
        SelectComboValue(_cmbAngle, targetAngle);

        _txtVideoExts = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _workingAction.VideoExtensions
        };
        _txtOutputSubFolder = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _workingAction.OutputSubFolder
        };
        _cmbRotationMethod = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbRotationMethod.Items.Add(new ComboItem<VideoRotationMethod>("Metadata 元数据模式（兼容旧版 FFmpeg 与原脚本）", VideoRotationMethod.Metadata));
        _cmbRotationMethod.Items.Add(new ComboItem<VideoRotationMethod>("DisplayRotation 模式（推荐 FFmpeg 5+/7+）", VideoRotationMethod.DisplayRotation));
        SelectComboValue(_cmbRotationMethod, _workingAction.RotationMethod);

        AddLabeledRow(panel, 0, "旋转角度：", _cmbAngle);
        AddLabeledRow(panel, 1, "匹配扩展名：", _txtVideoExts);
        AddLabeledRow(panel, 2, "输出子文件夹：", _txtOutputSubFolder);
        AddLabeledRow(panel, 3, "FFmpeg 参数模式：", _cmbRotationMethod);
        return panel;
    }

    private Control BuildCustomScriptPanel()
    {
        var panel = CreateTwoColumnGrid();

        _txtScriptPath = new TextBox { Dock = DockStyle.Fill, Text = _workingAction.ScriptPath };
        Button btnBrowse = UiHelper.CreateButton("浏览...", 84, 28);
        btnBrowse.Margin = UiHelper.SPad(6, 0, 0, 0);
        btnBrowse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog
            {
                Title = "选择自定义脚本或可执行文件",
                Filter = "脚本与程序 (*.bat;*.cmd;*.ps1;*.exe)|*.bat;*.cmd;*.ps1;*.exe|所有文件 (*.*)|*.*"
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _txtScriptPath.Text = ofd.FileName;
            }
        };

        var pathBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        pathBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathBox.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathBox.Controls.Add(_txtScriptPath, 0, 0);
        pathBox.Controls.Add(btnBrowse, 1, 0);

        _txtScriptArgs = new TextBox { Dock = DockStyle.Fill, Text = _workingAction.ScriptArguments };
        var lblArgsHint = new Label
        {
            Text = "提示：参数中可使用 {dir} 占位符表示当前右键目录；脚本默认以该目录作为工作目录启动。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(UiHelper.S(580), 0)
        };
        _chkShowConsole = new CheckBox
        {
            Text = "显示外部控制台窗口（如果脚本内含 set /p 交互输入或 pause 请勾选）",
            AutoSize = true,
            Checked = _workingAction.ShowConsoleWindow
        };

        AddLabeledRow(panel, 0, "脚本文件路径：", pathBox);
        AddLabeledRow(panel, 1, "附加启动参数：", _txtScriptArgs);
        AddFullSpanRow(panel, 2, lblArgsHint);
        AddFullSpanRow(panel, 3, _chkShowConsole);
        return panel;
    }

    private Control BuildPipelinePanel()
    {
        var container = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Height = UiHelper.S(260),
            Dock = DockStyle.Top
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblTip = new Label
        {
            Text = "勾选并按顺序排列要串联执行的动作（自上而下依次执行）：",
            AutoSize = true,
            Margin = UiHelper.SPad(0, 0, 0, 6)
        };
        container.Controls.Add(lblTip, 0, 0);
        container.SetColumnSpan(lblTip, 2);

        _clbPipelineSteps = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false
        };

        var available = _config.Actions
            .Where(a => a.Kind != ActionKind.Pipeline && !string.Equals(a.Id, _workingAction.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ordered = new List<MenuAction>();
        foreach (string stepId in _workingAction.StepActionIds)
        {
            MenuAction? match = available.FirstOrDefault(a => string.Equals(a.Id, stepId, StringComparison.OrdinalIgnoreCase));
            if (match != null && !ordered.Contains(match))
            {
                ordered.Add(match);
            }
        }

        foreach (MenuAction act in available)
        {
            if (!ordered.Contains(act))
            {
                ordered.Add(act);
            }
        }

        foreach (MenuAction act in ordered)
        {
            bool isChecked = _workingAction.StepActionIds.Any(id => string.Equals(id, act.Id, StringComparison.OrdinalIgnoreCase));
            _clbPipelineSteps.Items.Add(new ComboItem<string>($"{act.Title}  [{act.Id}]", act.Id), isChecked);
        }

        container.Controls.Add(_clbPipelineSteps, 0, 1);

        var sidePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = UiHelper.SPad(8, 0, 0, 0)
        };
        Button btnUp = UiHelper.CreateButton("上移", 84, 30);
        Button btnDown = UiHelper.CreateButton("下移", 84, 30);
        btnUp.Click += (_, _) => MoveCheckedListItem(-1);
        btnDown.Click += (_, _) => MoveCheckedListItem(1);
        sidePanel.Controls.Add(btnUp);
        sidePanel.Controls.Add(btnDown);
        container.Controls.Add(sidePanel, 1, 1);

        return container;
    }

    private void MoveCheckedListItem(int delta)
    {
        int idx = _clbPipelineSteps.SelectedIndex;
        if (idx < 0)
        {
            return;
        }

        int newIdx = idx + delta;
        if (newIdx < 0 || newIdx >= _clbPipelineSteps.Items.Count)
        {
            return;
        }

        object item = _clbPipelineSteps.Items[idx];
        bool isChecked = _clbPipelineSteps.GetItemChecked(idx);
        _clbPipelineSteps.Items.RemoveAt(idx);
        _clbPipelineSteps.Items.Insert(newIdx, item);
        _clbPipelineSteps.SetItemChecked(newIdx, isChecked);
        _clbPipelineSteps.SelectedIndex = newIdx;
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        string title = _txtTitle.Text.Trim();
        string id = _txtId.Text.Trim();

        if (string.IsNullOrEmpty(title))
        {
            MessageBox.Show(this, "请填写菜单显示名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTitle.Focus();
            return;
        }

        if (string.IsNullOrEmpty(id))
        {
            MessageBox.Show(this, "请填写动作唯一 ID。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtId.Focus();
            return;
        }

        if (_isNew && _config.Actions.Any(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, $"动作 ID \"{id}\" 已存在，请更换一个唯一 ID。", "ID 冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtId.Focus();
            return;
        }

        ActionKind kind = SelectedKind;
        _workingAction.Title = title;
        _workingAction.Id = id;
        _workingAction.Kind = kind;
        _workingAction.Enabled = _chkEnabled.Checked;
        _workingAction.SeparatorBefore = _chkSeparator.Checked;
        _workingAction.ConfirmBeforeRun = _chkConfirmBeforeRun.Checked;

        switch (kind)
        {
            case ActionKind.AddFolderPrefix:
                _workingAction.PromptForInput = _chkPromptPrefix.Checked;
                _workingAction.DefaultPrefix = _txtDefaultPrefix.Text;
                _workingAction.SkipHiddenAndSystem = _chkSkipHiddenPrefix.Checked;
                break;

            case ActionKind.RenameFilesByFolder:
                _workingAction.RenameScope = _cmbRenameScope.SelectedItem is ComboItem<RenameScope> rs
                    ? rs.Value
                    : RenameScope.CurrentFolder;
                _workingAction.GroupByExtension = _chkGroupByExt.Checked;
                _workingAction.SingleFileNoNumber = _chkSingleNoNum.Checked;
                _workingAction.NumberSeparator = _txtNumberSep.Text;
                _workingAction.PadDigits = (int)_numPadDigits.Value;
                _workingAction.SkipHiddenAndSystem = _chkSkipHiddenRename.Checked;
                break;

            case ActionKind.RemovePrefix:
                _workingAction.RemovePrefixes = _txtRemovePrefixes.Text
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.TrimEnd('\r'))
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _workingAction.Recursive = _chkRecursiveRemove.Checked;
                _workingAction.SkipHiddenAndSystem = _chkSkipHiddenRemove.Checked;
                break;

            case ActionKind.CompressSubFolders:
                _workingAction.ArchiveFormat = _cmbArchiveFormat.SelectedItem?.ToString() ?? "zip";
                _workingAction.IncludeRootFolder = _chkIncludeRoot.Checked;
                _workingAction.DeleteSourceAfterSuccess = _chkDeleteSource.Checked;
                _workingAction.MoveDeletedToRecycleBin = _chkRecycleBin.Checked;
                _workingAction.UsePassword = _chkUsePassword.Checked;
                _workingAction.PromptPasswordIfEmpty = _chkPromptPwdIfEmpty.Checked;
                if (!_workingAction.UsePassword || string.IsNullOrEmpty(_txtPassword.Text))
                {
                    _workingAction.EncryptedPassword = "";
                }
                else
                {
                    _workingAction.EncryptedPassword = DpapiHelper.ProtectString(_txtPassword.Text);
                }

                break;

            case ActionKind.RotateVideos:
                _workingAction.RotationAngle = _cmbAngle.SelectedItem is ComboItem<int> ang ? ang.Value : 90;
                _workingAction.VideoExtensions = string.IsNullOrWhiteSpace(_txtVideoExts.Text)
                    ? ".mp4"
                    : _txtVideoExts.Text.Trim();
                _workingAction.OutputSubFolder = string.IsNullOrWhiteSpace(_txtOutputSubFolder.Text)
                    ? "newfiles"
                    : _txtOutputSubFolder.Text.Trim();
                _workingAction.RotationMethod = _cmbRotationMethod.SelectedItem is ComboItem<VideoRotationMethod> rm
                    ? rm.Value
                    : VideoRotationMethod.Metadata;
                break;

            case ActionKind.CustomScript:
                if (string.IsNullOrWhiteSpace(_txtScriptPath.Text))
                {
                    MessageBox.Show(this, "请选择自定义脚本路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _workingAction.ScriptPath = _txtScriptPath.Text.Trim();
                _workingAction.ScriptArguments = _txtScriptArgs.Text.Trim();
                _workingAction.ShowConsoleWindow = _chkShowConsole.Checked;
                break;

            case ActionKind.Pipeline:
                var stepIds = new List<string>();
                for (int i = 0; i < _clbPipelineSteps.Items.Count; i++)
                {
                    if (_clbPipelineSteps.GetItemChecked(i) && _clbPipelineSteps.Items[i] is ComboItem<string> step)
                    {
                        stepIds.Add(step.Value);
                    }
                }

                if (stepIds.Count == 0)
                {
                    MessageBox.Show(this, "请至少勾选一个流水线步骤。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _workingAction.StepActionIds = stepIds;
                break;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static TableLayoutPanel CreateTwoColumnGrid()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return panel;
    }

    private static void AddLabeledRow(TableLayoutPanel panel, int row, string labelText, Control control)
    {
        while (panel.RowCount <= row)
        {
            panel.RowCount++;
        }

        var lbl = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = UiHelper.SPad(0, 6, 8, 6)
        };
        control.Margin = UiHelper.SPad(0, 4, 0, 4);
        panel.Controls.Add(lbl, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private static void AddFullSpanRow(TableLayoutPanel panel, int row, Control control)
    {
        while (panel.RowCount <= row)
        {
            panel.RowCount++;
        }

        control.Margin = UiHelper.SPad(0, 5, 0, 5);
        panel.Controls.Add(control, 0, row);
        panel.SetColumnSpan(control, 2);
    }

    private static void SelectComboValue<T>(ComboBox combo, T value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboItem<T> item && EqualityComparer<T>.Default.Equals(item.Value, value))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }
}
