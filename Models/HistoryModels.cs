namespace Zquick.Models;

public class RenameRecord
{
    public string OldPath { get; set; } = "";
    public string NewPath { get; set; } = "";
    public bool IsDirectory { get; set; }
}

public class RenameBatch
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string ActionTitle { get; set; } = "";
    public string TargetDirectory { get; set; } = "";
    public bool Undone { get; set; }
    public string UndoneAt { get; set; } = "";
    public List<RenameRecord> Records { get; set; } = new();
}

public class RenamePlanItem
{
    public string OldPath { get; set; } = "";
    public string NewPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool Selected { get; set; } = true;
    public bool Skipped { get; set; }
    public string SkipReason { get; set; } = "";

    public string OldName => Path.GetFileName(OldPath);
    public string NewName => Path.GetFileName(NewPath);
    public string ParentFolder => Path.GetDirectoryName(OldPath) ?? "";
}
