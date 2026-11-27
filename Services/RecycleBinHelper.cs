using Microsoft.VisualBasic.FileIO;

namespace Zquick.Services;

/// <summary>
/// 严格隔离 Microsoft.VisualBasic.FileIO 命名空间，避免 SearchOption 与 System.IO 冲突。
/// </summary>
public static class RecycleBinHelper
{
    public static void DeleteDirectory(string directoryPath, bool moveToRecycleBin)
    {
        if (!System.IO.Directory.Exists(directoryPath))
        {
            return;
        }

        if (moveToRecycleBin)
        {
            FileSystem.DeleteDirectory(
                directoryPath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin);
        }
        else
        {
            System.IO.Directory.Delete(directoryPath, recursive: true);
        }
    }
}
