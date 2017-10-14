using System;
using System.Collections.Generic;


namespace VMGuide
{
    //虚拟机基类
    public class VirtualMachine
    {
        private string path;
        public string Path { get { return path; } }
        public string Name { get; set; }

        public VirtualMachine(string FilePath = null)
        {
            path = FilePath;
            if (FilePath != null) Name = System.IO.Path.GetFileNameWithoutExtension(FilePath);
        }
    
        //输出设置值
        public virtual void ShowSettings()
        {
            if (Path == null) return;

            Console.WriteLine();
            Console.WriteLine(Name);
            for (int i = 0; i < Name.Length; i++) Console.Write("=");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"Date Lock\t{DateLock}");
            Console.WriteLine($"BIOS Date\t{BIOSDate.ToShortDateString()}");
        }
        
        public virtual Boolean DateLock { get; set; }

        public virtual DateTime BIOSDate { get; set; }

        //返回当前虚拟机是否已锁定
        public virtual Boolean IsLocked { get { return false; } }

    }

    //支持修改ACPI的虚拟机基类
    public class VirtualMachineWithACPI : VirtualMachine
    {
        public VirtualMachineWithACPI(string FilePath) : base(FilePath) { }

        //输出设置值
        public override void ShowSettings()
        {
            if (Path == null) return;

            base.ShowSettings();

            Console.WriteLine($"ACPI Enabled\t{ACPI}");
        }

        public virtual Boolean ACPI { get; set; }
    }

    public class Core
    {

        public static string GetCoreVersion()
        {
            var CoreDLL = ((new Core()).GetType().Assembly.Location);
            var FileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(CoreDLL);

            return ($"VMGuide Core {FileVersion.FileMajorPart}.{FileVersion.FileMinorPart} ({FileVersion.Comments})");
        }

        //搜索虚拟机
        public static List<VirtualMachine> SearchVM()
        {
            var VMList = new List<VirtualMachine>();
            VMware.SearchVM(ref VMList);
            VirtualBox.SearchVM(ref VMList);
            VirtualPC.SearchVM(ref VMList);
            return VMList;
        }
    }

}
