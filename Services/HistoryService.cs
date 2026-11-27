using System.Text;
using Zquick.Models;

namespace Zquick.Services;

public class HistoryService
{
    private const int MaxBatches = 50;

    public string HistoryFilePath { get; }

    public HistoryService(string dataDirectory)
    {
        HistoryFilePath = Path.Combine(dataDirectory, "history.json");
    }

    public List<RenameBatch> LoadAll()
    {
        if (!File.Exists(HistoryFilePath))
        {
            return new List<RenameBatch>();
        }

        try
        {
            string json = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
            return JsonHelper.Deserialize<List<RenameBatch>>(json) ?? new List<RenameBatch>();
        }
        catch
        {
            return new List<RenameBatch>();
        }
    }

    public void SaveAll(List<RenameBatch> batches)
    {
        string? dir = Path.GetDirectoryName(HistoryFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonHelper.Serialize(batches);
        string tempPath = HistoryFilePath + ".tmp";
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        if (File.Exists(HistoryFilePath))
        {
            File.Delete(HistoryFilePath);
        }

        File.Move(tempPath, HistoryFilePath);
    }

    public void AddBatch(RenameBatch batch)
    {
        if (batch.Records.Count == 0)
        {
            return;
        }

        List<RenameBatch> batches = LoadAll();
        batches.Insert(0, batch);
        if (batches.Count > MaxBatches)
        {
            batches.RemoveRange(MaxBatches, batches.Count - MaxBatches);
        }

        SaveAll(batches);
    }

    public void ClearHistory()
    {
        SaveAll(new List<RenameBatch>());
    }

    public (int RestoredCount, int SkippedCount) UndoBatch(string batchId, Action<string> log)
    {
        List<RenameBatch> batches = LoadAll();
        RenameBatch? batch = batches.FirstOrDefault(b => b.Id == batchId);
        if (batch == null)
        {
            log($"未找到批次 {batchId}。");
            return (0, 0);
        }

        if (batch.Undone)
        {
            log("该批次已经被撤销过。");
            return (0, 0);
        }

        int restored = 0;
        int skipped = 0;

        for (int i = batch.Records.Count - 1; i >= 0; i--)
        {
            RenameRecord record = batch.Records[i];
            try
            {
                bool caseOnly = string.Equals(record.NewPath, record.OldPath, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(record.NewPath, record.OldPath, StringComparison.Ordinal);

                if (record.IsDirectory)
                {
                    if (!Directory.Exists(record.NewPath))
                    {
                        log($"[跳过] 目录已不存在：{record.NewPath}");
                        skipped++;
                        continue;
                    }

                    if (!caseOnly && (Directory.Exists(record.OldPath) || File.Exists(record.OldPath)))
                    {
                        log($"[跳过] 原路径已被占用：{record.OldPath}");
                        skipped++;
                        continue;
                    }

                    MoveDirectorySafe(record.NewPath, record.OldPath, caseOnly);
                }
                else
                {
                    if (!File.Exists(record.NewPath))
                    {
                        log($"[跳过] 文件已不存在：{record.NewPath}");
                        skipped++;
                        continue;
                    }

                    if (!caseOnly && (File.Exists(record.OldPath) || Directory.Exists(record.OldPath)))
                    {
                        log($"[跳过] 原路径已被占用：{record.OldPath}");
                        skipped++;
                        continue;
                    }

                    MoveFileSafe(record.NewPath, record.OldPath, caseOnly);
                }

                log($"[撤销] {Path.GetFileName(record.NewPath)} -> {Path.GetFileName(record.OldPath)}");
                restored++;
            }
            catch (Exception ex)
            {
                log($"[失败] {record.NewPath} : {ex.Message}");
                skipped++;
            }
        }

        if (restored > 0 || skipped == 0)
        {
            batch.Undone = true;
            batch.UndoneAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveAll(batches);
        }

        return (restored, skipped);
    }

    public static void MoveFileSafe(string sourcePath, string destPath, bool caseOnly)
    {
        if (!caseOnly)
        {
            File.Move(sourcePath, destPath);
            return;
        }

        string parent = Path.GetDirectoryName(sourcePath) ?? "";
        string tempPath = Path.Combine(parent, $".zquick_tmp_{Guid.NewGuid():N}");
        File.Move(sourcePath, tempPath);
        File.Move(tempPath, destPath);
    }

    public static void MoveDirectorySafe(string sourcePath, string destPath, bool caseOnly)
    {
        if (!caseOnly)
        {
            Directory.Move(sourcePath, destPath);
            return;
        }

        string parent = Path.GetDirectoryName(sourcePath) ?? "";
        string tempPath = Path.Combine(parent, $".zquick_tmp_{Guid.NewGuid():N}");
        Directory.Move(sourcePath, tempPath);
        Directory.Move(tempPath, destPath);
    }
}
