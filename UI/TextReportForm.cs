namespace Zquick.UI;

public class TextReportForm : Form
{
    public TextReportForm(string title, string headerSummary, string contentText)
    {
        UiHelper.ApplyFormDefaults(this, title, new Size(780, 580), new Size(520, 380));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = UiHelper.SPad(12),
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
            Margin = UiHelper.SPad(0, 0, 0, 8)
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
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = UiHelper.SPad(0, 8, 0, 0)
        };

        Button btnCopy = UiHelper.CreateButton("复制全部", 100, 30);
        btnCopy.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                Clipboard.SetText(textBox.Text);
                MessageBox.Show(this, "目录结构已复制到剪贴板。", "已复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        Button btnClose = UiHelper.CreateButton("关闭", 90, 30);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Margin = new Padding(0);

        footer.Controls.Add(btnCopy);
        footer.Controls.Add(btnClose);
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);
        AcceptButton = btnClose;
        CancelButton = btnClose;
    }
}
