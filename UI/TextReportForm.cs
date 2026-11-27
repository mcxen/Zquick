namespace Zquick.UI;

public class TextReportForm : Form
{
    public TextReportForm(string title, string headerSummary, string contentText)
    {
        UiHelper.ApplyFormDefaults(this, title, new Size(760, 560));
        MinimumSize = new Size(520, 380);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblHeader = new Label
        {
            Text = headerSummary,
            Font = UiHelper.BoldFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(lblHeader, 0, 0);

        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = UiHelper.MonoFont,
            BackColor = SystemColors.Window,
            Text = contentText
        };
        root.Controls.Add(textBox, 0, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnClose = new Button
        {
            Text = "关闭",
            Width = 90,
            Height = 30,
            DialogResult = DialogResult.OK
        };

        var btnCopy = new Button
        {
            Text = "复制全部",
            Width = 100,
            Height = 30
        };
        btnCopy.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                Clipboard.SetText(textBox.Text);
                MessageBox.Show(this, "目录结构已复制到剪贴板。", "已复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        footer.Controls.Add(btnClose);
        footer.Controls.Add(btnCopy);
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);
        AcceptButton = btnClose;
        CancelButton = btnClose;
    }
}
