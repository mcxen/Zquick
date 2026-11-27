namespace Zquick.UI;

public static class UiHelper
{
    public static Font DefaultFont { get; } = new("Microsoft YaHei UI", 9f, FontStyle.Regular);
    public static Font BoldFont { get; } = new("Microsoft YaHei UI", 9f, FontStyle.Bold);
    public static Font TitleFont { get; } = new("Microsoft YaHei UI", 11f, FontStyle.Bold);
    public static Font MonoFont { get; } = new("Consolas", 9.5f, FontStyle.Regular);

    private static float? _scaleFactor;
    private static Icon? _appIcon;

    public static float ScaleFactor
    {
        get
        {
            if (_scaleFactor.HasValue)
            {
                return _scaleFactor.Value;
            }

            try
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                _scaleFactor = Math.Max(1.0f, g.DpiX / 96f);
            }
            catch
            {
                _scaleFactor = 1.0f;
            }

            return _scaleFactor.Value;
        }
    }

    public static int S(int logicalPx)
    {
        return (int)Math.Ceiling(logicalPx * ScaleFactor);
    }

    public static Size SSize(int logicalWidth, int logicalHeight)
    {
        return new Size(S(logicalWidth), S(logicalHeight));
    }

    public static Padding SPad(int all)
    {
        int p = S(all);
        return new Padding(p);
    }

    public static Padding SPad(int left, int top, int right, int bottom)
    {
        return new Padding(S(left), S(top), S(right), S(bottom));
    }

    public static Icon AppIcon
    {
        get
        {
            if (_appIcon != null)
            {
                return _appIcon;
            }

            try
            {
                string exePath = Program.GetCurrentExePath();
                if (File.Exists(exePath))
                {
                    _appIcon = Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch
            {
                // 忽略提取图标异常，使用系统默认应用图标
            }

            _appIcon ??= SystemIcons.Application;
            return _appIcon;
        }
    }

    public static void ApplyFormDefaults(
        Form form,
        string title,
        Size logicalClientSize,
        Size? logicalMinSize = null)
    {
        form.Text = title;
        form.Icon = AppIcon;
        form.Font = DefaultFont;
        form.AutoScaleMode = AutoScaleMode.None;
        form.StartPosition = FormStartPosition.CenterScreen;

        Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int maxW = Math.Max(480, (int)(workArea.Width * 0.92));
        int maxH = Math.Max(360, (int)(workArea.Height * 0.90));

        int targetW = Math.Min(S(logicalClientSize.Width), maxW);
        int targetH = Math.Min(S(logicalClientSize.Height), maxH);
        form.ClientSize = new Size(targetW, targetH);

        if (logicalMinSize.HasValue)
        {
            int minW = Math.Min(S(logicalMinSize.Value.Width), targetW);
            int minH = Math.Min(S(logicalMinSize.Value.Height), targetH);
            form.MinimumSize = new Size(minW, minH);
        }
    }

    public static Button CreateButton(
        string text,
        int minLogicalWidth = 82,
        int minLogicalHeight = 30,
        bool primary = false)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = SSize(minLogicalWidth, minLogicalHeight),
            Padding = SPad(10, 3, 10, 3),
            Margin = SPad(0, 0, 6, 4),
            UseVisualStyleBackColor = !primary
        };

        if (primary)
        {
            btn.BackColor = Color.FromArgb(0, 103, 192);
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        return btn;
    }

    public static void StyleGrid(DataGridView grid, bool readOnly = false)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 248);
        grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = SPad(4, 6, 4, 6);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Font = DefaultFont;
        grid.DefaultCellStyle.Font = DefaultFont;
        grid.DefaultCellStyle.Padding = SPad(4, 4, 4, 4);
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.RowTemplate.Height = S(30);
        grid.ReadOnly = readOnly;
    }
}
