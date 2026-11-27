namespace Zquick.UI;

public class InputDialog : Form
{
    private readonly Control _inputControl;
    private readonly CheckBox? _showPasswordCheck;

    public string ValueText { get; private set; } = "";

    private InputDialog(
        string title,
        string prompt,
        string defaultValue,
        IReadOnlyList<string>? historyItems,
        bool isPassword)
    {
        UiHelper.ApplyFormDefaults(this, title, new Size(480, isPassword ? 200 : 175));
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = UiHelper.SSize(440, 150);
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = UiHelper.SPad(14),
            ColumnCount = 1,
            RowCount = isPassword ? 4 : 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        if (isPassword)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblPrompt = new Label
        {
            Text = prompt,
            AutoSize = true,
            MaximumSize = new Size(UiHelper.S(460), 0),
            Margin = UiHelper.SPad(0, 0, 0, 8)
        };
        layout.Controls.Add(lblPrompt, 0, 0);

        if (!isPassword && historyItems != null && historyItems.Count > 0)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDown,
                MinimumSize = UiHelper.SSize(400, 0),
                Text = defaultValue
            };
            foreach (string item in historyItems)
            {
                combo.Items.Add(item);
            }

            _inputControl = combo;
        }
        else
        {
            var textBox = new TextBox
            {
                Dock = DockStyle.Top,
                MinimumSize = UiHelper.SSize(400, 0),
                Text = defaultValue,
                UseSystemPasswordChar = isPassword
            };
            _inputControl = textBox;
        }

        layout.Controls.Add(_inputControl, 0, 1);

        int btnRow = 2;
        if (isPassword && _inputControl is TextBox pwdBox)
        {
            _showPasswordCheck = new CheckBox
            {
                Text = "显示密码",
                AutoSize = true,
                Margin = UiHelper.SPad(0, 8, 0, 0)
            };
            _showPasswordCheck.CheckedChanged += (_, _) =>
            {
                pwdBox.UseSystemPasswordChar = !_showPasswordCheck.Checked;
            };
            layout.Controls.Add(_showPasswordCheck, 0, 2);
            btnRow = 3;
        }

        var btnPanel = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = UiHelper.SPad(0, 12, 0, 0)
        };

        Button btnOk = UiHelper.CreateButton("确定", 88, 30, primary: true);
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Click += (_, _) => { ValueText = _inputControl.Text; };

        Button btnCancel = UiHelper.CreateButton("取消", 88, 30);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Margin = new Padding(0);

        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnCancel);
        layout.Controls.Add(btnPanel, 0, btnRow);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    public static string? ShowTextPrompt(
        IWin32Window? owner,
        string title,
        string prompt,
        string defaultValue = "",
        IReadOnlyList<string>? historyItems = null)
    {
        using var dlg = new InputDialog(title, prompt, defaultValue, historyItems, isPassword: false);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.ValueText : null;
    }

    public static string? ShowPasswordPrompt(
        IWin32Window? owner,
        string title,
        string prompt)
    {
        using var dlg = new InputDialog(title, prompt, "", null, isPassword: true);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.ValueText : null;
    }
}
