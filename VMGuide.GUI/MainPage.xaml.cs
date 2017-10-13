using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

namespace VMGuide
{
    /// <summary>
    /// Page2.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Page
    {
        private static VirtualMachine VM;

        //返回当前的虚拟机
        public static VirtualMachine CurrentVM
        {
            get { return VM; }
            set { VM = value; }
        }

        //返回当前的VMwareVM
        public static VMwareVM CurrentVMwareVM
        {
            get
            {
                if (VM is VMwareVM) { return (VMwareVM)VM; } else { return null; }
            }
            set
            {
                if (value is VMwareVM) { VM = value; } else { return; }
            }
        }

        //当前虚拟机的网卡列表
        public static ObservableCollection<string> CurrentVMwareNICs
        {
            get
            {
                if (VM is VMwareVM) { return new ObservableCollection<string>(((VMwareVM)VM).NICs); } else { return new ObservableCollection<string> (); }
            }
            set
            {
                if (VM is VMwareVM) { ((VMwareVM)VM).NICs = value.ToList(); } else { return; }
            }
        }
        public static Boolean IsVMware
        {
            get { return (VM is VMwareVM); }
        }
        public static Boolean IsVirtualPC
        {
            get { return (VM is VirtualPC_VM); }
        }
        
        public Main()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetWindow(this).Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetWindow(this).WindowState = WindowState.Minimized;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            VM = null;
            if (NavigationService.CanGoBack) NavigationService?.GoBack();
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MainWindow.GetWindow(this).DragMove();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBox在选中的要修改的那个ListBoxItem内
            
            int ComboBoxSel = ((ComboBox)sender).SelectedIndex;
            int ListBoxSel = list_nic.SelectedIndex;
            if (ComboBoxSel == -1 || ListBoxSel == -1) return;

            var list = CurrentVMwareNICs;
            list[ListBoxSel] = VMware.NICs[ComboBoxSel];
            CurrentVMwareNICs = list;

            list_nic.ItemsSource = CurrentVMwareNICs;
            list_nic.SelectedItem = null;
        }

        private void DeleteNIC_Button_Click(object sender, RoutedEventArgs e)
        {
            int ListBoxSel = list_nic.SelectedIndex;
            if (ListBoxSel == -1) return;

            var list = CurrentVMwareNICs;
            list.RemoveAt(ListBoxSel);
            CurrentVMwareNICs = list;

            list_nic.ItemsSource = CurrentVMwareNICs;
            list_nic.SelectedItem = null;
        }

        private void AddNIC_Button_Click(object sender, RoutedEventArgs e)
        {
            var list = CurrentVMwareNICs;
            list.Add("vlance");
            CurrentVMwareNICs = list;

            list_nic.ItemsSource = CurrentVMwareNICs;
            list_nic.SelectedItem = null;
        }

        /* 
         * VirtualBox的DateLock由TimeOffest(BIOSDate与当前时间的差值)决定
         * TimeOffest!=0 --> DateLock=true
         * TimeOffest==0 --> DateLock=false
         * 
         * DateLock只能set为false，即将TimeOffest设为0
         * true会被直接忽略
         * 
         * 所以直接写死数据绑定，会导致DateLock开关直接点不了
         * 
         * 所以DateLock只能用事件手动写值
         * 然后DateLock为true的时候再建立与TimeOffest(BIOSDate)的数据绑定
        */
        private void check_biosdate_Checked(object sender, RoutedEventArgs e)
        {
            if (datepicker == null) return;
            CurrentVM.DateLock = true;
            var binding = new Binding("CurrentVM.BIOSDate");
            binding.Source = MainPage;
            datepicker.SetBinding(DatePicker.SelectedDateProperty, binding);
        }

        private void check_biosdate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (datepicker == null) return;
            BindingOperations.ClearBinding(datepicker, DatePicker.SelectedDateProperty);
            CurrentVM.DateLock = false;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            check_biosdate.Checked -= check_biosdate_Checked;
            check_biosdate.Unchecked -= check_biosdate_Unchecked;

            check_biosdate.IsChecked = CurrentVM.DateLock;

            check_biosdate.Checked += check_biosdate_Checked;
            check_biosdate.Unchecked += check_biosdate_Unchecked;
        }
    }
}
