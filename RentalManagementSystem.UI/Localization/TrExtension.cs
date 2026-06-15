using System.Windows.Data;
using System.Windows.Markup;

namespace RentalManagementSystem.UI.Localization;

/// <summary>
/// Markup extension dịch chuỗi trong XAML: <c>Text="{loc:Tr Menu.Dashboard}"</c>.
/// Tự cập nhật khi đổi ngôn ngữ (bind tới Localizer.Instance[key]).
/// </summary>
public class TrExtension : MarkupExtension
{
    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public TrExtension() { }
    public TrExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
