using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PromptMasterv5.Utils
{
    public class SelectedIdToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var selectedId = values.Length > 0 ? values[0] as string : null;
            var currentId = values.Length > 1 ? values[1] as string : null;

            var isSelected = !string.IsNullOrWhiteSpace(selectedId) && string.Equals(selectedId, currentId, StringComparison.Ordinal);

            var active = System.Windows.Application.Current.TryFindResource("MiniModeBtnActiveBrush") as System.Windows.Media.Brush;
            var inactive = System.Windows.Application.Current.TryFindResource("MiniModeBtnInactiveBrush") as System.Windows.Media.Brush;

            return isSelected ? (active ?? System.Windows.Media.Brushes.White) : (inactive ?? System.Windows.Media.Brushes.Gray);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
