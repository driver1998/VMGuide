using System;
using System.Collections.Generic;


namespace VMGuide
{
    public class VirtualMachine
    {
        private string path;
        public string Path { get { return path; } }
        public string Name { get; set; }

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
            if (!(this is VirtualPC_VM)) Console.WriteLine($"ACPI Enabled\t{ACPI}");
        }

        public virtual Boolean ACPI { get; set; }

        public virtual Boolean DateLock { get; set; }

        public virtual DateTime BIOSDate { get; set; }

        public virtual Boolean IsLocked { get { return false; } }

        public VirtualMachine(string FilePath = null)
        {
            path = FilePath;
            if (FilePath != null) Name = System.IO.Path.GetFileNameWithoutExtension(FilePath);
        }
    }

    public class Core
    {

        public static string GetCoreVersion()
        {
            var CoreDLL = ((new Core()).GetType().Assembly.Location);
            var FileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(CoreDLL);

            return ($"VMGuide Core {FileVersion.FileMajorPart}.{FileVersion.FileMinorPart} ({FileVersion.Comments})");
        }

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
