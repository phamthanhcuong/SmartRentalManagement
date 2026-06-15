using System.IO;

namespace RentalManagementSystem.UI.Localization;

/// <summary>Lưu/đọc lựa chọn ngôn ngữ vào AppData (sẵn sàng ngay khi khởi động, không cần CSDL).</summary>
public static class LanguageStore
{
    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuanLyNhaTro");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "lang.txt");
        }
    }

    public static string? Load()
    {
        try { return File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : null; }
        catch { return null; }
    }

    public static void Save(string lang)
    {
        try { File.WriteAllText(FilePath, lang); } catch { }
    }
}
