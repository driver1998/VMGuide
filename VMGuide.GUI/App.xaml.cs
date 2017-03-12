using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;

namespace VMGuide
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    /// 
    public enum ImportType { biosdate, acpi, datelock }

    public partial class App : Application
    {
    }

    class Program
    {
        [System.STAThreadAttribute()]
        static void Main (string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].StartsWith(@"vm-settings://"))
                {
                    URLParser(args[0]);
                }
                else if (File.Exists(args[0]))
                {
                    Home.PreLoadFile = args[0];
                }
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        static void URLParser (string url) 
        {
            var regex = new Regex(@"vm-settings://(.*?)/(.*?)/");
            var match = regex.Match (url + @"/");
            var str = match.Groups[2].Value.ToLower();
            var type = match.Groups[1].Value.ToLower();

            
            if (Enum.TryParse(type, out Home.typeU))
            {
                Home.UnattendedMode.Value = true;
                bool.TryParse(str, out Home.boolU);
                DateTime.TryParseExact(str, "yyyyMMdd" ,null, DateTimeStyles.None, out Home.dateU);
                if (Home.typeU == ImportType.datelock && Home.boolU == true) Home.UnattendedMode.Value = false;
            }
        }
    }
}
