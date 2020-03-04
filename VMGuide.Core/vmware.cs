using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace VMGuide
{
    //读写VMX配置文件的类
    //VMX本身是key=value形式的键值对
    public class VMXFile
    {
        private string file;
        private Encoding encoding;
        public string Path { get; private set; }
        
        public VMXFile(string filePath)
        {
            Path = filePath;
            encoding = DetectEncoding(filePath);
            System.Diagnostics.Debug.WriteLine(Path, encoding);
            file = File.ReadAllText(filePath, encoding);
        }

        private Encoding DetectEncoding(string filePath)
        {
            var stream = File.OpenRead(filePath);
            var streamReader = new StreamReader(stream);
            file = streamReader.ReadLine();
            stream.Close();

            var encodingName = ReadValue(".encoding");
            Console.WriteLine(encodingName);

            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException)
            {
                encoding = Encoding.UTF8;
            }

            return encoding;

        }
        public string ReadValue(string key)
        {
            Regex regex = new Regex(key + "\\s+?=\\s+?\"(.*?)\"", RegexOptions.IgnoreCase);
            return (regex.Match(file)?.Groups[1]?.Value);
        }
        public List<string> ReadValues(string key)
        {
            Regex regex = new Regex(key + "\\s+?=\\s+?\"(.*?)\"", RegexOptions.IgnoreCase);
            List<string> ret = new List<string>();
            MatchCollection matches = regex.Matches(file);

            foreach (Match match in matches)
                ret.Add(match.Groups[1]?.Value);

            return (ret);
        }
        public void WriteValue(string key, string value)
        {
            Regex regex = new Regex(".*?(" + key + ".*?=\\s*)\"+(.*?)\\s*\"+\\r\\n", RegexOptions.IgnoreCase);
            if (regex.IsMatch(file) == false) //不匹配就加一个能匹配的
            {
                file += (key + " = \"value\"" + Environment.NewLine);
            }
            if (value == "") //value为空就删除整行
            {
                file = regex.Replace(file, "");
            }
            else //对匹配项进行替换
            {
                file = regex.Replace(file, "$1\"" + value.ToLower() + "\"" + Environment.NewLine);
            }

            File.WriteAllText(Path, file, encoding);
        }

    }

    public class VMwareVM : VirtualMachineWithACPI //设置会直接储存
    {
        VMXFile VMX;

        public VMwareVM(string FilePath) : base(FilePath)
        {
            if (FilePath == null) return;
            VMX = new VMXFile(FilePath);
            Name = VMX.ReadValue("displayName");
        }

        public override void ShowSettings()
        {
            if (Path == null) return;

            base.ShowSettings();

            Console.WriteLine("Firmware\t{0}", Firmware.ToUpper());
            Console.WriteLine("Sound Card\t{0}\t{1}", SoundCard, VMware.ValueToDescripton(SoundCard));
            Console.WriteLine();

            List<string> NIC = NICs;
            if (NIC.Count >0) Console.Write("NICs");

            for (int i = 0; i < NIC.Count; i++)
            {
                Console.WriteLine("\t[{0}]\t{1}\t{2}", i, NIC[i], VMware.ValueToDescripton(NIC[i]));
            }
        }

        public override bool IsLocked
        {
            get
            {
                if (Path == null) return true;
                return Directory.Exists(Path + ".lck");
            }
        }

        public string SoundCard
        {
            get
            {
                if (Path == null) return "";

                string ret;

                if (VMX.ReadValue("sound.present").ToLower() == "false")
                {
                    ret = "none";
                }
                else
                {
                    ret = VMX.ReadValue("sound.virtualDev");
                }

                if (ret == "") ret = "es1371";
                return ret;
            }
            set
            {
                if (Path == null || IsLocked) return;

                if (value == "none")
                {
                    VMX.WriteValue("sound.present", "false");
                }
                else
                {
                    VMX.WriteValue("sound.virtualdev", value);
                    VMX.WriteValue("sound.present", "true");
                }

            }
        }

        public List<string> NICs
        {
            get
            {
                if (Path == null) return new List<string>();

                List<string> str = VMX.ReadValues("ethernet\\d+.present");
                List<string> _NIC = new List<string>();

                for (int i = 0; i < str.Count; i++)
                {
                    //对每一个符合条件的进行测试 NIC确实存在就加入到List中
                    if (str[i].ToLower() != "true") continue;

                    var value = VMX.ReadValue("ethernet" + Convert.ToString(i) + ".virtualdev");
                    if (value == "") value = "vmxnet";

                    _NIC.Add(value);
                }

                return _NIC;
            }
            set
            {
                if (Path == null || IsLocked) return;

                List<string> str = VMX.ReadValues("ethernet\\d+.present");
                for (int i = 0; i < str.Count; i++)
                {
                    //先将之前的网卡全部禁用
                    if (str[i].ToLower() == "true") VMX.WriteValue("ethernet" + Convert.ToString(i) + ".present", "false");
                }
                for (int i = 0; i < value.Count; i++)
                {
                    VMX.WriteValue("ethernet" + Convert.ToString(i) + ".present", "true");
                    VMX.WriteValue("ethernet" + Convert.ToString(i) + ".virtualdev", value[i]);
                }
            }
        }

        public string Firmware
        {
            get
            {
                if (Path == null) return "";

                string ret;
                ret = VMX.ReadValue("firmware");
                if (ret == "") ret = "bios";
                return ret;
            }
            set
            {
                if (Path == null || IsLocked) return;

                VMX.WriteValue("firmware", value);
            }
        }

        public override bool ACPI
        {
            get
            {
                if (Path == null) return false;

                bool ret;
                switch (VMX.ReadValue("acpi.present").ToLower())
                {
                    case "false":
                        ret = false;
                        break;
                    default:
                        ret = true;
                        break;
                }
                return ret;
            }
            set
            {
                if (Path == null || IsLocked) return;

                VMX.WriteValue("acpi.present", value.ToString ().ToLower ());
            }
        }

        public override bool DateLock
        {
            get
            {
                if (Path == null) return false;

                bool ret;
                switch (VMX.ReadValue("time.synchronize.continue").ToLower())
                {
                    case "false":
                        ret = true;
                        break;
                    default:
                        ret = false;
                        break;
                }
                return ret;
            }
            set
            {
                if (Path == null || IsLocked) return;

                VMX.WriteValue("tools.syncTime", value ? "false" : "true");
                VMX.WriteValue("time.synchronize.continue", value ? "false" : "true");
                VMX.WriteValue("time.synchronize.restore", value ? "false" : "true");
                VMX.WriteValue("time.synchronize.resume.disk", value ? "false" : "true");
                VMX.WriteValue("time.synchronize.shrink", value ? "false" : "true");

                if (value == false) VMX.WriteValue("rtc.starttime", "");
            }
        }

        //vmware的BIOSDate保存为unix时间戳
        public override DateTime BIOSDate
        {
            get
            {
                if (Path == null) return DateTime.Now;

                DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0);
                var str = VMX.ReadValue("rtc.starttime");

                if (int.TryParse(str, out int UnixTime) == true)
                {
                    ret = ret.AddSeconds(UnixTime);
                }
                else
                {
                    ret = DateTime.Now;
                }

                if (DateLock == false) ret = DateTime.Now;

                return ret;
            }

            set
            {
                if (Path == null || IsLocked) return;

                DateLock = true;
                DateTime UnixStart = new DateTime(1970, 1, 1, 0, 0, 0);
                int val = (int)(value - UnixStart).TotalSeconds;
                VMX.WriteValue("rtc.starttime", Convert.ToString(val));
            }
        }


    }

    public class VMware
    {
        private static Dictionary<string, string> firmwares;
        public static Dictionary<string, string> Firmwares
        {
            get
            {
                if (firmwares == null)
                {
                    firmwares = new Dictionary<string, string>()
                    {
                        {"bios" ,"BIOS"},
                        {"efi"  ,"UEFI"},
                    };
                }
                return firmwares;
            }
        }

        private static Dictionary<string, string> soundcards;
        public static Dictionary<string, string> SoundCards
        {
            get
            {
                if (soundcards == null)
                {
                    soundcards = new Dictionary<string, string>()
                    {
                        {"sb16"     ,"Sound Blaster 16"},
                        {"es1371"   ,"Sound Blaster PCI ES1371"},
                        {"hdaudio"  ,"HD Audio"},
                        {"none"     ,"None"},
                    };
                }
                return soundcards;
            }
        }

        private static Dictionary<string, string> nics;
        public static Dictionary<string, string> NICs
        {
            get
            {
                if (nics == null)
                {
                    nics = new Dictionary<string, string>()
                    {
                        {"vlance"   ,"AMD PCnet-PCI II Am79C970A"},
                        {"e1000"    ,"Intel E1000"},
                        {"e1000e"   ,"Intel E1000e"},
                        {"vmxnet"   ,"VMware VMXNet"},
                        {"vmxnet3"  ,"VMware VMXNet3"},
                    };
                }
                return nics;
            }
        }

        
        public static string ValueToDescripton(string value)
        {
            if (Firmwares.ContainsKey(value))  return Firmwares[value];
            if (SoundCards.ContainsKey(value)) return SoundCards[value];
            if (NICs.ContainsKey(value)) return NICs[value];
            return "";
        }

        public static void SearchVM(ref List<VirtualMachine> VMList)
        {
            /*
             * type=0 VMware Workstation
             * type=1 VMware Player
             */

            int type = 0;
            for (type = 0; type < 2; type++)
            {
                string ConfigDir, ConfigFile, name;
                VMXFile config; List<string> values;
                ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VMware");

                if (type == 0)
                {
                    ConfigFile = Path.Combine(ConfigDir, "inventory.vmls");
                    name = "index\\d+.id";
                }
                else
                {
                    ConfigFile = Path.Combine(ConfigDir, "preferences.ini");
                    name = "pref.mruVM\\d+.filename";
                }

                if (File.Exists(ConfigFile) == false) continue;
                config = new VMXFile(ConfigFile);
                values = config.ReadValues(name);


                foreach (string path in values)
                {
                    bool AlreadyExist = false;
                    foreach (VirtualMachine vm in VMList)
                    {
                        if (vm.Path == path) { AlreadyExist = true; break; }
                    }
                    if (AlreadyExist || !File.Exists(path)) continue;
                    var VMX = new VMwareVM(path);
                    VMList.Add(VMX);
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
