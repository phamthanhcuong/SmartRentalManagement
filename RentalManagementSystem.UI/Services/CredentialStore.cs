using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RentalManagementSystem.UI.Services;

/// <summary>
/// Lưu/đọc thông tin đăng nhập "ghi nhớ" — mật khẩu được mã hóa bằng DPAPI
/// (chỉ giải mã được trên cùng tài khoản Windows của máy này).
/// </summary>
public static class CredentialStore
{
    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuanLyNhaTro");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "login.dat");
        }
    }

    public static void Save(string username, string password)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            var line = username + "\n" + Convert.ToBase64String(enc);
            File.WriteAllText(FilePath, line);
        }
        catch { /* không chặn đăng nhập nếu lưu lỗi */ }
    }

    public static (string Username, string Password)? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var parts = File.ReadAllText(FilePath).Split('\n');
            if (parts.Length < 2) return null;
            var enc = Convert.FromBase64String(parts[1]);
            var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
            return (parts[0], Encoding.UTF8.GetString(dec));
        }
        catch { return null; }
    }

    public static void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
    }
}
