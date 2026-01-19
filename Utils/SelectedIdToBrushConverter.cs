using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PromptMasterv5.Utils;

public sealed class SelectedIdToBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var selectedId = values.Length > 0 ? values[0] as string : null;
        var itemId = values.Length > 1 ? values[1] as string : null;

        var isSelected = !string.IsNullOrEmpty(selectedId) &&
                         !string.IsNullOrEmpty(itemId) &&
                         string.Equals(selectedId, itemId, StringComparison.Ordinal);

        var key = isSelected ? "MiniModeBtnActiveBrush" : "MiniModeBtnInactiveBrush";
        var brush = System.Windows.Application.Current?.TryFindResource(key) as System.Windows.Media.Brush;
        return brush ?? System.Windows.Media.Brushes.White;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
