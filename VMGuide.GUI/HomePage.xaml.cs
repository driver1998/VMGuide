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
    public partial class HomePage : Page
    {

        private ObservableCollection<VirtualMachine> VMware_OC = new ObservableCollection<VirtualMachine>();
        private ObservableCollection<VirtualMachine> VirtualPC_OC = new ObservableCollection<VirtualMachine>();
        private ObservableCollection<VirtualMachine> VBox_OC = new ObservableCollection<VirtualMachine>();

        private static NotifyChanged<bool> unattend;
        public static NotifyChanged<Boolean> IsUnattendMode
        {
            get
            {
                if (unattend == null)
                    unattend = new NotifyChanged<bool>(App.unattendMode != null);

                return unattend;
            }
        }
        
        public static string PreLoadFile;

        public HomePage()
        {
            InitializeComponent();
        }


        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {



            Refresh();

            if (IsUnattendMode.Value)
            {
                ImportPrompt.Content = "Ready to Import: " + UnattendMode.Types[App.unattendMode.Type] + " " + App.unattendMode.Value;
            }

            if (PreLoadFile != null)
            {
                DetectAndOpen(PreLoadFile);
                PreLoadFile = null;
            }

            listbox_vbox.ItemsSource = VBox_OC;
            listbox_vmx.ItemsSource = VMware_OC;
            listbox_vpc.ItemsSource = VirtualPC_OC;
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

            ((ListBox)sender).SelectionChanged -= VMLists_SelectionChanged;
            ((ListBox)sender).SelectedItem = null;
            ((ListBox)sender).SelectionChanged += VMLists_SelectionChanged;
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

        //按照文件后缀判断格式并打开
        private void DetectAndOpen(string file)
        {
            VirtualMachine VM = null;

            switch (Path.GetExtension(file))
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
            //自动备份
            if (File.Exists(VM.Path + ".VMGuide")) File.Delete(VM.Path + ".VMGuide");
            File.Copy(VM.Path, VM.Path + ".VMGuide");

            //无人值守模式
            if (IsUnattendMode.Value)
            {
                try
                {
                    App.unattendMode.Execute(VM);
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show(e.Message, "VMGuide", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            MainPage.CurrentVM = VM;
            NavigationService?.Navigate(new Uri("mainpage.xaml", UriKind.Relative));

            if (IsUnattendMode.Value)
            {
                IsUnattendMode.Value = false;
                App.unattendMode = null;
                MessageBox.Show("Settings updated successfully.","",MessageBoxButton.OK ,MessageBoxImage.Information);
            }
        }

        private void ImportCancel_Click(object sender, RoutedEventArgs e)
        {
            IsUnattendMode.Value = false;
            App.unattendMode = null;
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
