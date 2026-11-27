namespace Zquick.Services;

public static class PathGuard
{
    private static readonly char[] InvalidFileChars = Path.GetInvalidFileNameChars();

    public static string NormalizeFolderPath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return "";
        }

        string cleaned = rawPath.Trim().Trim('"').Trim();
        if (cleaned.Length == 2 && char.IsLetter(cleaned[0]) && cleaned[1] == ':')
        {
            cleaned += @"\";
        }
        else if (cleaned.Length > 3)
        {
            cleaned = cleaned.TrimEnd('\\', '/');
        }

        try
        {
            return Path.GetFullPath(cleaned);
        }
        catch
        {
            return cleaned;
        }
    }

    public static bool IsDriveRoot(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        string normalized = NormalizeFolderPath(folderPath);
        string trimmed = normalized.TrimEnd('\\', '/');
        if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
        {
            return true;
        }

        return Path.GetDirectoryName(trimmed) == null;
    }

    public static bool ValidateFolder(string folderPath, bool allowDriveRoot, out string error)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            error = "未提供目标文件夹路径。";
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            error = $"目标文件夹不存在：{folderPath}";
            return false;
        }

        if (!allowDriveRoot && IsDriveRoot(folderPath))
        {
            error = $"安全保护：禁止直接在磁盘根目录（{folderPath}）执行批量重命名、压缩或删除操作。";
            return false;
        }

        error = "";
        return true;
    }

    public static bool ContainsInvalidFileNameChars(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        return name.IndexOfAny(InvalidFileChars) >= 0;
    }
}
