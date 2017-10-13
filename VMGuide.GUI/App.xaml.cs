using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace VMGuide
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class App : Application
    {
        //无人值守模式对象
        public static UnattendMode unattendMode = null;
    }

    class Program
    {
        [System.STAThreadAttribute()]
        static void Main (string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args[0].StartsWith(@"vm-settings://"))
                    {
                        URLParser(args[0]);
                    }
                    else if (File.Exists(args[0]))
                    {
                        HomePage.PreLoadFile = args[0];
                    }
                }

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "VMGuide", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //从URL中分析值，为进入无人值守模式作准备
        //URL格式： vm-settings://type/parameter
        static void URLParser (string url) 
        {
            var regex = new Regex(@"vm-settings://(.*?)/(.*?)/");
            var match = regex.Match (url + @"/");
            
            var value = match.Groups[2].Value.ToLower();     //值
            var type = match.Groups[1].Value.ToLower();      //修改的项目名

            var u = new UnattendMode();
            try
            {
                u.Configure(type, value);
                App.unattendMode = u;
            }
            catch (FormatException)
            {
                throw;
            }
        }
    }
}
