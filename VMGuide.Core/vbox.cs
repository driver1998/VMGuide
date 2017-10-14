using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Xml;

namespace VMGuide
{
    public class VBoxVM : VirtualMachineWithACPI
    {
        VBoxXML XML;

        public VBoxVM(string FilePath) : base(FilePath)
        {
            if (FilePath == null) return;
            XML = new VBoxXML(FilePath);
            Name = XML.ReadAttribute(@"VirtualBox/Machine", "name");
        }

        public override bool ACPI
        {
            get
            {
                if (Path == null) return false;

                string value;
                value = XML.ReadAttribute(@"VirtualBox/Machine/Hardware/BIOS/ACPI", "enabled");
                return (value.ToLower() == "false") ? false : true;
            }

            set
            {
                if (Path == null || IsLocked) return;
                XML.WriteAttribute(@"VirtualBox/Machine/Hardware/BIOS/ACPI", "enabled", value.ToString().ToLower() );
            }
        }

        public override DateTime BIOSDate
        {
            get
            {
                if (Path == null) return DateTime.Now;

                string value; DateTime ret = DateTime.Now;
                value = XML.ReadAttribute(@"VirtualBox/Machine/Hardware/BIOS/TimeOffset", "value");

                if (double.TryParse(value, out double TimeOffest))
                    ret = ret.AddMilliseconds(TimeOffest);

                return ret;
            }

            set
            {
                if (Path == null || IsLocked) return;
                var TimeOffset =(long)((value - DateTime.Now).TotalMilliseconds);
                XML.WriteAttribute(@"VirtualBox/Machine/Hardware/BIOS/TimeOffset", "value", TimeOffset.ToString());
            }
        } 

        public override bool DateLock
        {
            get
            {
                if (Path == null) return false;

                string value; bool ret = false;
                value = XML.ReadAttribute(@"VirtualBox/Machine/Hardware/BIOS/TimeOffset", "value");

                if (double.TryParse(value, out double TimeOffset))
                {
                    if (TimeOffset != 0) ret = true;
                }

                return ret;
            }
            set
            {
                if (Path == null || IsLocked) return;
                if (value == false)
                {
                    XML.WriteAttribute(@"VirtualBox/Machine/Hardware/BIOS/TimeOffset", "value", "0");
                }
            }
        }

        public override bool IsLocked
        {
            get
            {
                if (Path == null) return true;
                var process = System.Diagnostics.Process.GetProcessesByName("VboxSVC");
                return (process.Length > 0);
            }
        }
    }

    //读写VirtualBox相关XML配置文件的类
    public class VBoxXML
    {
        private string path;
        public string Path { get { return path; } }

        XmlDocument XML;
        XmlNamespaceManager NamespaceMgr;

        public VBoxXML (string FilePath)
        {
            path = FilePath;
            XML = new XmlDocument();
            try { XML.Load(FilePath); } catch { return; }            
            NamespaceMgr = new XmlNamespaceManager(XML.NameTable);
            NamespaceMgr.AddNamespace("v", "http://www.virtualbox.org/");            
        }

        //不带namespace的路转换为带namespace修饰的路径
        public string XPathConvert(string XPath)
        {
            if (XPath.Substring(0, 2) == "v:") return XPath;

            XPath = XPath.Replace("/", "/v:");
            XPath = "v:" + XPath;
            return XPath;
        }

        public string ReadAttribute(string XPath, string name)
        {
            XmlNode Node;
            string value = string.Empty;
            XPath = XPathConvert(XPath); //add prefix

            try
            {
                Node = XML.SelectSingleNode(XPath, NamespaceMgr);
                if (Node != null) value = Node.Attributes[name].Value;
            }
            catch { }

            return value;
        }
        public List<string> ReadAttributes(string XPath, string name)
        {
            XmlNodeList NodeList;
            var list = new List<string>();
            XPath = XPathConvert(XPath); //add prefix

            try
            {
                NodeList = XML.SelectNodes(XPath, NamespaceMgr);

                foreach (XmlNode node in NodeList)
                    list.Add(node.Attributes[name].Value);
            }
            catch { }

            return list;

        }
        public void WriteAttribute(string XPath, string Attribute, string Value) //no prefix in ElementName!
        {
            XPath = XPathConvert(XPath); //add prefix

            var Node = XML.SelectSingleNode(XPath, NamespaceMgr);
            if (Node == null)
            {
                CreateTree(XPath);
                Node = XML.SelectSingleNode(XPath, NamespaceMgr);
            }

            if (Node.Attributes[Attribute] == null)
            {
                Node.Attributes.Append(XML.CreateAttribute(Attribute));
            }

            Node.Attributes[Attribute].Value = Value;

            XML.Save(Path);
        }

        //根据一个路径，一路创建一棵树
        private void CreateTree (string XPath)
        {
            char[] s = { '/' };
            var Elements = XPathConvert(XPath).Split(s);

            XmlNode Parent = XML; //从XML的根开始操作
            foreach (string Element in Elements)
            {
                var Node = Parent.SelectSingleNode(Element, NamespaceMgr);
                if (Node == null)
                {
                    var name = Element.Replace("v:", ""); //CreateNode方法需要的是没有前缀修饰的名字
                    var Child = XML.CreateNode(XmlNodeType.Element, name, "http://www.virtualbox.org/");
                    Parent.AppendChild(Child);
                    Parent = Parent.SelectSingleNode(Element, NamespaceMgr);
                }
                else
                {
                    Parent = Node;
                }
                
            }
        }
    }


    public static class VirtualBox
    {
        public static void SearchVM (ref List<VirtualMachine> VMList)
        {
            string ConfigFile; VBoxXML XML;
            ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".VirtualBox\VirtualBox.xml");
            if (!File.Exists(ConfigFile)) return;

            XML = new VBoxXML(ConfigFile);

            var list = XML.ReadAttributes("VirtualBox/Global/MachineRegistry/MachineEntry", "src");
            foreach (string path in list)
            {
                if (File.Exists (path))
                {
                    var VBOX = new VBoxVM(path);
                    VMList.Add(VBOX);
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
