namespace Zquick.UI;

public static class UiHelper
{
    public static Font DefaultFont { get; } = new("Microsoft YaHei UI", 9f, FontStyle.Regular);
    public static Font BoldFont { get; } = new("Microsoft YaHei UI", 9f, FontStyle.Bold);
    public static Font TitleFont { get; } = new("Microsoft YaHei UI", 11f, FontStyle.Bold);
    public static Font MonoFont { get; } = new("Consolas", 9.5f, FontStyle.Regular);

    private static Icon? _appIcon;

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

    public static void ApplyFormDefaults(Form form, string title, Size size)
    {
        form.Text = title;
        form.Icon = AppIcon;
        form.Font = DefaultFont;
        form.ClientSize = size;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.AutoScaleMode = AutoScaleMode.Dpi;
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
        grid.ColumnHeadersHeight = 30;
        grid.RowTemplate.Height = 28;
        grid.ReadOnly = readOnly;
    }
}
