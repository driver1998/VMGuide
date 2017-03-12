using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VMGuide
{
    public sealed class BooleanToCollapseConverter : IValueConverter
    {
        public object Convert(object value, Type type, object para, CultureInfo culture)
        {
            return (value is bool) ? ((bool)value ? Visibility.Visible : Visibility.Collapsed) : Visibility.Visible;
        }
        public object ConvertBack(object value, Type type, object para, CultureInfo culture)
        {
            return (value is Visibility) ? (value.Equals(Visibility.Visible) ? true : false) : false;
        }
    }

    public sealed class BooleanReverseConverter : IValueConverter
    {
        public object Convert(object value, Type type, object para, CultureInfo culture)
        {
            return (value is bool) ? !((bool)value) : false;
        }
        public object ConvertBack(object value, Type type, object para, CultureInfo culture)
        {
            return (value is bool) ? !((bool)value) : false;
        }
    }

    public sealed class BooleanReverseToCollapseConverter : IValueConverter
    {
        public object Convert(object value, Type type, object para, CultureInfo culture)
        {
            var val = (value is bool) ? !((bool)value) : false;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type type, object para, CultureInfo culture)
        {
            return (value is Visibility) ? (value.Equals(Visibility.Visible) ? false : true) : false;
        }
    }

    public sealed class VMwareValueToDescriptionConverter : IValueConverter
    {

        public object Convert(object value, Type type, object para, CultureInfo culture)
        {
            if (value == null) { return ""; } else { return VMware.ValueToDescripton(value.ToString()); }
            
        }
        public object ConvertBack(object value, Type type, object para, CultureInfo culture)
        {
            if (value == null) { return ""; } else { return VMware.DescriptionToValue (value.ToString()); }
        }
    }

    public class Mode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool val = false;
        public bool Value
        {
            get { return val; }
            set
            {
                val = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("value"));
            }
        }
    }
}
