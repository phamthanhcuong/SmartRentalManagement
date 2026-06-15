using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace RentalManagementSystem.UI.Localization;

/// <summary>
/// Dịch vụ đa ngôn ngữ, chuyển ngôn ngữ tức thời (runtime).
/// Dữ liệu dịch nằm trong các file JSON (Localization/Languages/*.json) → dễ thêm/sửa, mở rộng ngôn ngữ mới về sau.
/// Cách dùng trong XAML:  Text="{loc:Tr Menu.Dashboard}"
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    public static Localizer Instance { get; } = new();

    private readonly Dictionary<string, Dictionary<string, string>> _all = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _current = new();
    private string _lang = "vi";

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Danh sách ngôn ngữ hỗ trợ (mã, tên hiển thị, cờ).</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } = new[]
    {
        new LanguageOption("vi", "Tiếng Việt", "🇻🇳"),
        new LanguageOption("en", "English", "🇬🇧"),
        new LanguageOption("ja", "日本語", "🇯🇵"),
        new LanguageOption("zh", "中文", "🇨🇳"),
    };

    public string CurrentLanguage => _lang;

    private Localizer()
    {
        LoadAll();
        SetLanguage(LanguageStore.Load() ?? "vi", persist: false);
    }

    /// <summary>Tra cứu chuỗi theo khóa; thiếu thì lùi về tiếng Việt, cuối cùng trả về chính khóa.</summary>
    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_current.TryGetValue(key, out var v)) return v;
            if (_all.TryGetValue("vi", out var vi) && vi.TryGetValue(key, out var fallback)) return fallback;
            return key;
        }
    }

    public void SetLanguage(string lang, bool persist = true)
    {
        if (!_all.ContainsKey(lang)) lang = "vi";
        _lang = lang;
        _current = _all[lang];
        if (persist) LanguageStore.Save(lang);
        // Cập nhật toàn bộ binding chỉ mục [key] một lần
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        LanguageChanged?.Invoke();
    }

    /// <summary>Sự kiện báo ngôn ngữ đổi (cho ViewModel cập nhật chuỗi sinh từ C#).</summary>
    public event Action? LanguageChanged;

    private void LoadAll()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "Languages");
        foreach (var code in new[] { "vi", "en", "ja", "zh" })
        {
            var path = Path.Combine(dir, code + ".json");
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    _all[code] = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
                }
                else _all[code] = new();
            }
            catch { _all[code] = new(); }
        }
        if (!_all.ContainsKey("vi")) _all["vi"] = new();
    }
}

public record LanguageOption(string Code, string Name, string Flag)
{
    public string Display => $"{Flag} {Name}";
}
