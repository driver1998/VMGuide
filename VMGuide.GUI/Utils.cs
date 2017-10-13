using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace VMGuide
{

    //一个实现了INotifyPropertyChanged的包装类
    public class NotifyChanged<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private T v;

        public NotifyChanged() { }
        public NotifyChanged(T value) { v = value; }

        public T Value
        {
            get { return v; }
            set
            {
                if (!v.Equals(value))
                {
                    v = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                }
            }
        }
    }

    //保存无人值守模式的参数
    public class UnattendMode
    {
        private static Dictionary<string, string> types;

        //URL的type部分，也是项目字典的key
        const string TYPE_BIOSDATE = "biosdate";
        const string TYPE_ACPI = "acpi";
        const string TYPE_DATELOCK = "datelock";

        //项目字典
        public static Dictionary<string, string> Types
        {
            get
            {
                if (types == null)
                {
                    types = new Dictionary<string, string>
                    {
                        { TYPE_BIOSDATE, "BIOS Date" },
                        { TYPE_ACPI, "ACPI" },
                        { TYPE_DATELOCK, "BIOS DateLock" }
                    };
                }

                return types;
            }
        }

        //修改的项目名
        public string Type { get; private set; }

        //值
        public string Value
        {
            get
            {
                switch (Type)
                {
                    case null:
                        return "";
                    case TYPE_BIOSDATE:
                        return dateTimeValue.ToShortDateString();
                    default:
                        return booleanValue.ToString();
                }
            }
        }          //外部获取用

        //内部使用的值
        private DateTime dateTimeValue; //TYPE_DATELOCK的值
        private bool booleanValue;      //其他TYPE的值

        //无人值守模式的配置方法
        public void Configure(string type, string value)
        {
            if (!Types.ContainsKey(type)) throw new FormatException("Invalid parameter.");

            bool isValid;

            if (type == TYPE_BIOSDATE)
                isValid = DateTime.TryParseExact(value, "yyyyMMdd", null,
                        DateTimeStyles.None, out dateTimeValue);
            else
                isValid = Boolean.TryParse(value, out booleanValue);

            if (isValid)
                Type = type;
            else
                throw new FormatException("Invalid parameter.");
        }

        //执行无人值守操作 修改对应的虚拟机
        public void Execute(VirtualMachine VM)
        {
            if (VM.IsLocked) throw new UnauthorizedAccessException("Virtual Machine is Locked.");

            switch (Type)
            {
                case TYPE_DATELOCK:
                    VM.DateLock = booleanValue;
                    break;
                case TYPE_ACPI:
                    VM.ACPI = booleanValue;
                    break;
                case TYPE_BIOSDATE:
                    VM.BIOSDate = dateTimeValue;
                    break;
            }
        }
    }
}
