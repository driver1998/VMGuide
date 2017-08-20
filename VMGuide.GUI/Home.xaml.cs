using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace VMGuide
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Page
    {

        private static ObservableCollection<VirtualMachine> VMware_OC = new ObservableCollection<VirtualMachine>();
        private static ObservableCollection<VirtualMachine> VirtualPC_OC = new ObservableCollection<VirtualMachine>();
        private static ObservableCollection<VirtualMachine> VBox_OC = new ObservableCollection<VirtualMachine>();
        private static Mode unattend = new Mode();

        public static ObservableCollection<VirtualMachine> VMware_Collection
        {
            get { return VMware_OC; }
            set { VMware_OC = value; }
        }
        public static ObservableCollection<VirtualMachine> VirtualPC_Collection
        {
            get { return VirtualPC_OC; }
            set { VirtualPC_OC = value; }
        }
        public static ObservableCollection<VirtualMachine> VBox_Collection
        {
            get { return VBox_OC; }
            set { VBox_OC = value; }
        }
        public static Mode UnattendedMode
        {
            get { return unattend; }
            set { unattend = value; }
        }

        //for UnattendMode
        public static ImportType typeU;
        public static bool boolU;
        public static DateTime dateU;

        public static string PreLoadFile;

        public Home()
        {
            InitializeComponent();

            Refresh();

            if (UnattendedMode.Value)
            {
                string FriendlyTypeU = "", FriendlyValue = "";

                switch (typeU)
                {
                    case ImportType.biosdate:
                        FriendlyTypeU = "BIOS Date";
                        FriendlyValue = dateU.ToShortDateString();
                        break;

                    case ImportType.acpi:
                        FriendlyTypeU = "ACPI";
                        FriendlyValue = boolU.ToString();
                        break;

                    case ImportType.datelock:
                        FriendlyTypeU = "BIOS Date Lock";
                        FriendlyValue = boolU.ToString();
                        break;
                }

                ImportPrompt.Content = "Ready to Import: " + FriendlyTypeU + " " + FriendlyValue;
            }

        }


        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PreLoadFile != null)
            {
                DetectAndOpen(PreLoadFile);
                PreLoadFile = null;
            }
            
        }

        private void Refresh()
        {
            VMware_OC.Clear();
            VBox_OC.Clear();
            VirtualPC_OC.Clear();
            VMware.SearchVM(ref VMware_OC);
            VirtualBox.SearchVM(ref VBox_OC);
            VirtualPC.SearchVM(ref VirtualPC_OC);
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MainWindow.GetWindow(this).DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetWindow(this).Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetWindow(this).WindowState = WindowState.Minimized ;
        }

        private void VMLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListBox)sender).SelectedItem == null) return;
            OpenVM ( (VirtualMachine)((ListBox)sender).SelectedItem );
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dlgFileOpen = new OpenFileDialog()
            {
                Filter = "Virtual Machines (*.vmx; *.vbox; *.vmc)|*.vmx;*.vbox;*.vmc",
                CheckFileExists = true,
                Multiselect = false
            };

            dlgFileOpen.ShowDialog();
            if (File.Exists (dlgFileOpen.FileName))
            {
                DetectAndOpen(dlgFileOpen.FileName);
            }
        }

        private void DetectAndOpen(string file)
        {
            VirtualMachine VM = null;

            switch (System.IO.Path.GetExtension(file))
            {
                case ".vmx":
                    VM = new VMwareVM(file);
                    break;

                case ".vbox":
                    VM = new VBoxVM(file);
                    break;

                case ".vmc":
                    VM = new VirtualPC_VM(file);
                    break;
            }

            if (VM != null) OpenVM(VM);
        }

        private void OpenVM(VirtualMachine VM)
        {
            if (File.Exists(VM.Path + ".VMGuide")) File.Delete(VM.Path + ".VMGuide");
            File.Copy(VM.Path, VM.Path + ".VMGuide");

            Main.CurrentVM = VM;
            if (UnattendedMode.Value)
            {
                if (VM.IsLocked)
                {
                    MessageBox.Show("This Virtual Machine is Locked by your hypervisor, exit your hypervisor and try again.","VMGuide",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                    return;
                }

                switch (typeU)
                {
                    case ImportType.datelock:
                        Main.CurrentVM.DateLock = boolU;
                        break;
                    case ImportType.biosdate:
                        Main.CurrentVM.BIOSDate = dateU;
                        break;
                    case ImportType.acpi:    
                        Main.CurrentVM.ACPI = boolU;
                        break;
                }
            }

            NavigationService?.Navigate(new Uri("mainpage.xaml", UriKind.Relative));

            if (UnattendedMode.Value)
            {
                UnattendedMode.Value = false;
                MessageBox.Show("Settings updated successfully.","",MessageBoxButton.OK ,MessageBoxImage.Information);
            }
        }

        private void ImportCancel_Click(object sender, RoutedEventArgs e)
        {
            UnattendedMode.Value = false;
        }
        

        private void RefreshMenuClicked(object sender, MouseButtonEventArgs e)
        {
            settingsToggle.IsChecked = false;
            Refresh();
        }

        private void AboutMenuClicked(object sender, MouseButtonEventArgs e)
        {
            var FileVersion = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo;


            MessageBox.Show($"VMGuide\t{FileVersion.FileMajorPart}.{FileVersion.FileMinorPart} ({FileVersion.Comments})" +
                $"\n\n{FileVersion.LegalCopyright}","VMGuide",MessageBoxButton.OK ,MessageBoxImage.Information);
        }
    }


}
