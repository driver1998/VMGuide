using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Collections.ObjectModel;

namespace VMGuide
{
    public class VirtualPC_VM : VirtualMachine 
    {
        VirtualPC_XML XML;
        const string DefaultCMOS = "000040500025378002FFFF000000000000000000000000000030304C070707070434FFFF2085807F00000000700801800D8800000000000000000000000000901A32E252580050E999E62401002784004A2080244000000000085AACFE1032547698BAE400000000000003000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        public VirtualPC_VM(string FilePath) : base(FilePath)
        {
            if (FilePath == null) return;
            XML = new VirtualPC_XML(FilePath);
        }
        
        public override bool DateLock
        {
            get
            {
                string XPath;
                if (IsVersion7)
                {
                    XPath = @"preferences/hardware/bios/time_sync_at_boot";
                }
                else
                {
                    XPath = @"preferences/integration/microsoft/components/host_time_sync/enabled";
                }

                var str = XML.ReadValue(XPath);
                //var value = false;

                if (bool.TryParse(str, out bool value) == false) value = true;
                value = !value; //value is "time sync enabled", but we need "date lock enabled"
               
                return value;
            }
            set
            {
                string XPath;
                if (IsVersion7)
                {
                    XPath = @"preferences/hardware/bios/time_sync_at_boot";
                }
                else
                {
                    XPath = @"preferences/integration/microsoft/components/host_time_sync/enabled";
                }

                //value is "date lock enabled", but we need "time sync enabled"
                XML.WriteValue(XPath, "boolean", (!value).ToString().ToLower());

                if (value == false) { BIOSDate = DateTime.MaxValue; };
            }
            
        }

        public override DateTime BIOSDate
        {
            get
            {
                var CMOS = XML.ReadValue(@"preferences/hardware/bios/cmos");
                var TimeBytes = XML.ReadValue(@"preferences/hardware/bios/time_bytes");

                var YearString = CMOS.Substring(72, 2) + TimeBytes.Substring (18,2);
                var MonthString = TimeBytes.Substring(16, 2);
                var DayString = TimeBytes.Substring(14, 2);

                var validY = int.TryParse(YearString, out int year);
                var validM = int.TryParse(MonthString, out int month);
                var validD = int.TryParse(DayString, out int day);

                var Date = DateTime.Now;

                if (validY && validM && validD && DateLock)
                {
                    Date = new DateTime(year, month, day, Date.Hour, Date.Minute, Date.Second);
                }
                return Date;
            }
            set
            {
                var val = value;
                
                if (val == DateTime.MaxValue)
                {
                    val = DateTime.Now;
                }
                else
                {
                    if (!DateLock) DateLock = true;
                }
                    
                var CMOS = XML.ReadValue(@"preferences/hardware/bios/cmos");
                if (CMOS == "") CMOS = DefaultCMOS;

                var YearString    = val.Year  .ToString().PadLeft(2);
                var MonthString   = val.Month .ToString().PadLeft(2);
                var DayString     = val.Day   .ToString().PadLeft(2);
                var HourString    = val.Hour  .ToString().PadLeft(2);
                var MinuteString  = val.Minute.ToString().PadLeft(2);
                var SecondString  = val.Second.ToString().PadLeft(2);

                string TimeBytes =
                    SecondString + "00"   +
                    MinuteString + "00"   +
                    HourString   + "0000" +
                    DayString    +
                    MonthString  +
                    YearString.Substring(2, 2);

                TimeBytes = TimeBytes.Replace(' ', '0');

                CMOS = CMOS.Substring(0, 72) + YearString.Substring(0, 2) + CMOS.Substring(74);

                XML.WriteValue(@"preferences/hardware/bios/cmos", "bytes", CMOS);
                XML.WriteValue(@"preferences/hardware/bios/time_bytes", "bytes", TimeBytes);
            }
        }

        public bool IsVersion7
        {
            get
            {
                var creator = XML.ReadValue(@"preferences/properties/creator/build");
                return creator.StartsWith("6.1");
            }
        }
    }

    public class VirtualPC_XML
    {
        private string path;
        public string Path { get { return path; } }

        XmlDocument XML;

        public VirtualPC_XML(string FilePath)
        {
            path = FilePath;
            XML = new XmlDocument();
            XML.Load(FilePath);    
        }

        public string ReadValue(string XPath)
        {
            XmlNode Node;
            string value = "";
            try
            {
                Node = XML.SelectSingleNode(XPath);
                value = Node.InnerText;
            }
            catch { }

            return value;
        }

        public void WriteValue(string XPath, string type, string value)
        {
            var Node = XML.SelectSingleNode(XPath);
            if (Node == null)
            {
                CreateTree(XPath);
                Node = XML.SelectSingleNode(XPath);
            }

            if (Node.Attributes["type"] == null)
            {
                Node.Attributes.Append(XML.CreateAttribute("type"));
            }

            Node.Attributes["type"].Value = type;
            Node.InnerText = value;

            XML.Save(Path);
        }

        private void CreateTree(string XPath)
        {
            char[] s = { '/' };
            var Elements = (XPath).Split(s);

            XmlNode Parent = XML;
            
            foreach (string Element in Elements)
            {
                var Node = Parent.SelectSingleNode(Element);
                if (Node == null)
                {
                    var Child = XML.CreateNode(XmlNodeType.Element, Element, "");
                    Parent.AppendChild(Child);
                    Parent = Parent.SelectSingleNode(Element);
                }
                else
                {
                    Parent = Node;
                }   
            }
        }

    }

    public class VirtualPC
    {
        public static void SearchVM(ref List <VirtualMachine> VMList)
        {
            var RoamingAppdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var Version7Folder = Path.Combine(UserProfile, "Virtual Machines");
            var VMFolder = Path.Combine(RoamingAppdata, @"Microsoft\Virtual PC\Virtual Machines");
            string[] Files;

            //Virtual 2004 2007
            if (Directory.Exists(VMFolder))
            {
                Files = Directory.GetFiles(VMFolder);
                foreach (string lnkFile in Files)
                {
                    if (Path.GetExtension(lnkFile) == ".lnk")
                    {
                        var Shell = new IWshRuntimeLibrary.WshShell();
                        IWshRuntimeLibrary.WshShortcut Shortcut = Shell.CreateShortcut(lnkFile);
                        var Target = Shortcut.TargetPath;

                        if (File.Exists(Target) && (Path.GetExtension(Target) == ".vmc"))
                        {
                            var VM = new VirtualPC_VM(Target);
                            VMList.Add(VM);
                        }
                    }
                }
            }

            //Windows Virtual PC (Version7)
            if (Directory.Exists (Version7Folder))
            { 
                Files = Directory.GetFiles(Version7Folder);
                foreach (string vmcx in Files)
                {
                    if (Path.GetExtension(vmcx) == ".vmcx")
                    {
                        var XML = new VirtualPC_XML(vmcx);
                        var Target = XML.ReadValue(@"vm_description/vmc_path");

                        if (File.Exists(Target) && (Path.GetExtension(Target) == ".vmc"))
                        {
                            var VM = new VirtualPC_VM(Target);
                            VMList.Add(VM);
                        }
                    }
                }
            }
        }
        public static void SearchVM(ref ObservableCollection<VirtualMachine> VMCollection)
        {
            var tList = new List<VirtualMachine>();
            SearchVM(ref tList);
            foreach (VirtualMachine vm in tList) VMCollection.Add(vm);
        }
    }


}
