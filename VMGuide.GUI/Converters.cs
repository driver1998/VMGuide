using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VMGuide
{
    //boolean转是否为collapse
    //true<->visible  false<->collapse
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

    //boolean取反
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

    //boolean转是否为collapse，与BooleanToCollapseConverter结果相反
    //true<->collapse  false<->visible
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

    //VMware中设定值与对应的描述文本互相转换
    public sealed class VMwareValueToDescriptionConverter : IValueConverter
    {

        public object Convert(object value, Type type, object para, CultureInfo culture)
        {
            if (value == null) { return ""; } else { return VMware.ValueToDescripton(value.ToString()); }
            
        }
        public object ConvertBack(object value, Type type, object para, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
