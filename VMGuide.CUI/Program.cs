using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace VMGuide
{
    class Program
    {
        
        enum Errors
        {
            Unknown,
            Complete,
            Exit,
            WrongCommand,
            WrongSet,
            FileNotFound,
            InvalidFile,
            NoCurrentVM,
            FileLocked
        }

        static VirtualMachine CurrentVM;
        static List<VirtualMachine> VMList;
        static bool UnattendMode = false;

        static void Main(string[] args)
        {
            Stream InStream = null ; StreamReader In = null;

            if (args.GetLength (0)>0)
            {
                if (File.Exists(args[0]))
                {
                    In = File.OpenText(args[0]);
                }
                else
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(args[0].Replace(";", "\n"));
                                       
                    InStream = new MemoryStream(bytes);
                    In = new StreamReader(InStream);
                }

                UnattendMode = true;
                Console.SetIn(In);
            }

            if (!UnattendMode) { PrintVersion(); Console.Write( Environment.NewLine ); }
            Search();

            var ret = Errors.Complete;
            while (ret != Errors.Exit) ret = CommandPrompt();

        }

        static Errors CommandPrompt() //Process sommands
        {
            var ret = Errors.Complete; //Return value to the OS, EXIT or COMPLETE
            var val = Errors.Unknown; //Return value from commands

            if (!UnattendMode) Console.Write( Environment.NewLine + "VMGuide> ");

            var command = Console.ReadLine();
            if (command == null) command = "exit";
            command = command.ToLower();

            char[] s = { ' ' }; string[] split = command.Split(s);
            if (split[0] != "")
            {
                switch (split[0])
                {
                    case "exit":
                    case "quit":
                        ret = Errors.Exit;
                        val = Errors.Complete;
                        break;

                    case "cls":
                    case "clear":
                        Console.Clear();
                        val = Errors.Complete;
                        break;

                    case "ver":
                    case "version":
                        val = PrintVersion();
                        break;

                    case "list":
                    case "lis":
                    case "ls":
                        val = List();
                        break;

                    case "select":
                    case "sel":
                        val = Select(command);
                        break;

                    case "set":
                        val = Set(command);
                        break;

                    case "show":
                    case "sho":
                        val = ShowSettings();
                        break;

                    case "search":
                    case "scan":
                    case "sea":
                    case "sca":
                        val = Search();
                        break;

                    default:
                        val = Errors.WrongCommand;
                        break;
                }
            }

            switch (val)
            {
                case Errors.FileNotFound:
                    Console.WriteLine("Virtual Machine not Found.");
                    break;
                case Errors.InvalidFile:
                    Console.WriteLine("Invalid file. Currently supported virtualizer: VMware, VirtualBox, Virtual PC.");
                    break;
                case Errors.NoCurrentVM:
                    Console.WriteLine("Please select a virtual machine first.");
                    break;
                case Errors.FileLocked:
                    Console.WriteLine("This Virtual Machine is Locked by your hypervisor, exit your hypervisor and try again.");
                    break;
                case Errors.WrongCommand:
                    Help(command);
                    break;
                case Errors.WrongSet:
                    Help_Set();
                    break;
            }

            return ret;
        }

        static Errors ShowSettings()
        {
            var ret = Errors.Unknown;

            if (CurrentVM == null)
            {
                ret = Errors.NoCurrentVM;
            }
            else
            {
                CurrentVM.ShowSettings();
                ret = Errors.Complete;
            }

            return ret;
        }
        static Errors Search()
        {
            VMList = Core.SearchVM();
            if (!UnattendMode) Console.WriteLine("Found {0} virtual machines, type LIST to see the full list.", VMList.Count);
            return Errors.Complete;
        }

        static Errors List() //Show VM LIST
        {
            for (int i=0;i<VMList.Count;i++)
            {
                string x = i.ToString().PadLeft ( VMList.Count.ToString().Length );
                string type= "Unknown";

                switch (VMList[i].GetType().ToString ())
                {
                    case "VMGuide.VMwareVM":
                        type = "VMware";
                        break;

                    case "VMGuide.VBoxVM":
                        type = "VirtualBox";
                        break;

                    case "VMGuide.VirtualPC_VM":
                        type = "Virtual PC";
                        break;
                }

                Console.WriteLine("{0}    {1} {2}", x, type.PadRight(12), VMList[i].Name);
            }
            return Errors.Complete;
        }

        static Errors Set(string command)
        {
            var ret = Errors.WrongSet;
            if (CurrentVM == null) return Errors.NoCurrentVM; 
            if (CurrentVM.IsLocked) return Errors.FileLocked; 
               
            string RegexString = "set (datelock|biosdate|acpi|firmware|sound|nic)=(.*?)/";
            var regex = new Regex(RegexString, RegexOptions.IgnoreCase);

            if (regex.IsMatch(command + "/"))
            {
                var match = regex.Match(command + "/");
                var name = match.Groups[1].Value;
                var value = match.Groups[2].Value;

                switch (name.ToLower ())
                {
                    case "datelock":
                        if (bool.TryParse(value, out bool DateLockVal))
                        {
                            CurrentVM.DateLock = DateLockVal;
                            ret = Errors.Complete;
                        }
                        break;

                    case "biosdate":
                        if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.None, out DateTime date))
                        {
                            CurrentVM.BIOSDate = date;
                            ret = Errors.Complete;
                        }
                        break;

                    case "acpi":
                        if (bool.TryParse(value, out bool ACPI_Val))
                        {
                            CurrentVM.ACPI = ACPI_Val;
                            ret = Errors.Complete;
                        }
                        break;

                    case "firmware":
                        if (CurrentVM is VMwareVM && Array.IndexOf (VMware.Firmwares, value) != -1)
                        {
                            ((VMwareVM)CurrentVM).Firmware = value;
                            ret = Errors.Complete;
                        }
                        break;

                    case "sound":
                        if (CurrentVM is VMwareVM && Array.IndexOf(VMware.SoundCards, value) != -1)
                        {
                            ((VMwareVM)CurrentVM).SoundCard = value;
                            ret = Errors.Complete;
                        }
                        break;

                    case "nic":
                        if (CurrentVM is VMwareVM)
                        {
                            char[] c = { ';' };
                            var split = value.Split(c);
                            var nic = new List<string>();
                            foreach (string s in split)
                            {
                                if (s == "none" || Array.IndexOf(VMware.NICs, s) == -1) continue;
                                nic.Add(s);
                            }
                            ((VMwareVM)CurrentVM).NICs = nic;
                            ret = Errors.Complete;
                        }
                        break;
                }

            }
            return ret;
        }
       

        static Errors Select(string command)
        {
            Regex regex = new Regex("(?:select|sel) (?:id=(\\d+)|file=\"(.*?)\")");
            var ret = Errors.WrongCommand;

            if (regex.IsMatch(command) == true)
            {
                Match match = regex.Match(command); 
                
                //Group[1].value ID
                //Group[2].value filename
                if ( match.Groups[1].Value != "" ) 
                {
                    var id = Convert.ToInt32(match.Groups[1].Value);
                    if (id>=0 && id <VMList .Count)
                    {
                        CurrentVM = VMList[id];
                        ret = Errors.Complete;
                    }
                    else
                    {
                        ret = Errors.FileNotFound;
                    }
                }
                else
                {
                    ret = DetectAndOpen(match.Groups[2].Value);
                }
            }

            if (ret == Errors.Complete) ShowSettings();
            return (ret);     
        }

        static Errors  DetectAndOpen(string file) //Detect file type, and open
        {
            var ret = Errors.FileNotFound;

            if (File.Exists(file))
            {
                switch (Path.GetExtension(file))
                {
                    case ".vmx":
                        CurrentVM = new VMwareVM(file);
                        ret = Errors.Complete;
                        break;

                    case ".vbox":
                        CurrentVM = new VBoxVM(file);
                        ret = Errors.Complete;
                        break;

                    case ".vmc":
                        CurrentVM = new VirtualPC_VM(file);
                        ret = Errors.Complete;
                        break;

                    default:
                        ret = Errors.InvalidFile;
                        break;
                }

                if (ret != Errors.InvalidFile )
                {
                    if (File.Exists(file + ".VMGuide")) File.Delete(file + ".VMGuide");
                    File.Copy(file, file + ".VMGuide");
                }
            }
            return ret;
        }

        static Errors PrintVersion()
        {
            var FileVersion = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo;
            Console.WriteLine();
            Console.WriteLine($"VMGuide {FileVersion.FileMajorPart}.{FileVersion.FileMinorPart} ({FileVersion.Comments})");
            Console.WriteLine( FileVersion.LegalCopyright);
            return Errors.Complete;
        }

        static void Help(string command = "help")
        {
            if (command == "help set")
            {
                Help_Set();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("CLEAR\t\t\tClear the screen.");
                Console.WriteLine();
                Console.WriteLine("EXIT\t\t\tExit VMGuide.");
                Console.WriteLine();
                Console.WriteLine("LIST\t\t\tList virtual machines.");
                Console.WriteLine();
                Console.WriteLine("SEARCH\t\t\tSearch for virtual machines ");
                Console.WriteLine("\t\t\tand refresh virtual machines list.");
                Console.WriteLine();
                Console.WriteLine("SELECT ID=x\t\tSelect a virtual machine with ID x.");
                Console.WriteLine();
                Console.WriteLine("SELECT FILE=\"file1\"\tSelect a virtual machine from file1.");
                Console.WriteLine();
                Console.WriteLine("SET config1=value\tSet config1 to value.");
                Console.WriteLine("\t\t\tFor full SET command list, type HELP SET.");
                Console.WriteLine();
                Console.WriteLine("SHOW\t\t\tShow the current virtual machine and its configuration.");
                Console.WriteLine();
                Console.WriteLine("VERSION\t\t\tDisplay the version of VMGuide.");
            }
        }

        static void Help_Set()
        {
            Console.WriteLine();
            Console.WriteLine("Generic");
            Console.WriteLine("========");
            Console.WriteLine("SET ACPI=boolean\tTurn ACPI on or off.");
            Console.WriteLine("\t\t\tPossible values: true, false.");
            Console.WriteLine();
            Console.WriteLine("SET DATELOCK=boolean\tTurn the BIOS Date Lock on or off.");
            Console.WriteLine("\t\t\tPossible values: true, false.");
            Console.WriteLine();
            Console.WriteLine("SET BIOSDATE=YYYYMMDD\tEdit the BIOS Date, and set DATELOCK to true.");

            Console.WriteLine();
            Console.WriteLine("VMware");
            Console.WriteLine("========");
            Console.WriteLine("SET FIRMWARE=value\tChange the firmware emulated.");
            Console.WriteLine("\t\t\tPossible values: bios, efi.");
            Console.WriteLine();
            Console.WriteLine("SET SOUND=soundcard\tChange the sound card emulated.");
            Console.WriteLine("\t\t\tPossible values: sb16, es1371, hdaudio, none.");
            Console.WriteLine();
            Console.WriteLine("SET NIC=n1;n2;n3;...\tEdit the network adapter list.");
            Console.WriteLine("\t\t\tPossible values: vlance, e1000, e1000e,");
            Console.WriteLine("\t\t\t\t\t vmxnet, vmxnet3, none.");
        }
}

}

