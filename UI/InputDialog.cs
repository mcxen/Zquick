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
        UiHelper.ApplyFormDefaults(this, title, new Size(460, isPassword ? 185 : 165));
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = isPassword ? 4 : 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        if (isPassword)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblPrompt = new Label
        {
            Text = prompt,
            AutoSize = true,
            MaximumSize = new Size(425, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(lblPrompt, 0, 0);

        if (!isPassword && historyItems != null && historyItems.Count > 0)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDown,
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
                Margin = new Padding(0, 6, 0, 0)
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
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Width = 85,
            Height = 28,
            DialogResult = DialogResult.Cancel
        };

        var btnOk = new Button
        {
            Text = "确定",
            Width = 85,
            Height = 28,
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (_, _) => { ValueText = _inputControl.Text; };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
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
