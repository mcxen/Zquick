using Zquick.Models;

namespace Zquick.UI;

public class RenamePreviewForm : Form
{
    private readonly List<RenamePlanItem> _items;
    private readonly DataGridView _grid;
    private readonly Label _lblSummary;
    private readonly Button _btnConfirm;

    public RenamePreviewForm(string actionTitle, string targetFolder, List<RenamePlanItem> items)
    {
        _items = items;
        UiHelper.ApplyFormDefaults(
            this,
            $"重命名预览确认 - {actionTitle}",
            new Size(920, 580),
            new Size(680, 400));

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

        _lblSummary = new Label
        {
            AutoSize = true,
            Font = UiHelper.BoldFont,
            Margin = UiHelper.SPad(0, 0, 0, 8)
        };
        root.Controls.Add(_lblSummary, 0, 0);

        _grid = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.StyleGrid(_grid, readOnly: false);

        var colSelect = new DataGridViewCheckBoxColumn
        {
            HeaderText = "执行",
            Width = UiHelper.S(64),
            MinimumWidth = UiHelper.S(58),
            FlatStyle = FlatStyle.Standard
        };
        var colType = new DataGridViewTextBoxColumn
        {
            HeaderText = "类型",
            Width = UiHelper.S(72),
            ReadOnly = true
        };
        var colOld = new DataGridViewTextBoxColumn
        {
            HeaderText = "原名称",
            Width = UiHelper.S(230),
            ReadOnly = true
        };
        var colNew = new DataGridViewTextBoxColumn
        {
            HeaderText = "新名称",
            Width = UiHelper.S(230),
            ReadOnly = true
        };
        var colDir = new DataGridViewTextBoxColumn
        {
            HeaderText = "所在目录",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true
        };
        var colStatus = new DataGridViewTextBoxColumn
        {
            HeaderText = "状态",
            Width = UiHelper.S(130),
            ReadOnly = true
        };

        _grid.Columns.AddRange(colSelect, colType, colOld, colNew, colDir, colStatus);

        foreach (RenamePlanItem item in _items)
        {
            int rowIndex = _grid.Rows.Add(
                item.Selected,
                item.IsDirectory ? "文件夹" : "文件",
                item.OldName,
                item.Skipped ? "-" : item.NewName,
                item.ParentFolder,
                item.Skipped ? $"跳过：{item.SkipReason}" : "待重命名");

            if (item.Skipped)
            {
                DataGridViewRow row = _grid.Rows[rowIndex];
                row.DefaultCellStyle.ForeColor = Color.Gray;
                row.Cells[0].ReadOnly = true;
            }
        }

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                bool checkedVal = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value);
                _items[e.RowIndex].Selected = checkedVal && !_items[e.RowIndex].Skipped;
                UpdateSummary(targetFolder);
            }
        };

        root.Controls.Add(_grid, 0, 1);

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Padding = UiHelper.SPad(0, 8, 0, 0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var leftBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        Button btnSelectAll = UiHelper.CreateButton("全选可改项", 104, 30);
        btnSelectAll.Click += (_, _) => SetAllSelection(true, targetFolder);
        Button btnUnselectAll = UiHelper.CreateButton("全部取消", 92, 30);
        btnUnselectAll.Click += (_, _) => SetAllSelection(false, targetFolder);
        leftBtns.Controls.Add(btnSelectAll);
        leftBtns.Controls.Add(btnUnselectAll);

        var rightBtns = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        _btnConfirm = UiHelper.CreateButton("确认执行重命名", 136, 30, primary: true);
        _btnConfirm.DialogResult = DialogResult.OK;
        Button btnCancel = UiHelper.CreateButton("取消", 90, 30);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Margin = new Padding(0);
        rightBtns.Controls.Add(_btnConfirm);
        rightBtns.Controls.Add(btnCancel);

        bottom.Controls.Add(leftBtns, 0, 0);
        bottom.Controls.Add(rightBtns, 1, 0);
        root.Controls.Add(bottom, 0, 2);

        Controls.Add(root);
        AcceptButton = _btnConfirm;
        CancelButton = btnCancel;

        UpdateSummary(targetFolder);
    }

    private void SetAllSelection(bool selected, string targetFolder)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Skipped)
            {
                continue;
            }

            _items[i].Selected = selected;
            _grid.Rows[i].Cells[0].Value = selected;
        }

        UpdateSummary(targetFolder);
    }

    private void UpdateSummary(string targetFolder)
    {
        int selectedCount = _items.Count(i => i.Selected && !i.Skipped);
        int skippedCount = _items.Count(i => i.Skipped);
        _lblSummary.Text = $"目标：{targetFolder}   |   将重命名 {selectedCount} 项，跳过 {skippedCount} 项";
        _btnConfirm.Enabled = selectedCount > 0;
    }
}
