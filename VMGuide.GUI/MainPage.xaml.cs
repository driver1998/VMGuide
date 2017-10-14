using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Collections.Generic;

namespace VMGuide
{
    /// <summary>
    /// Page2.xaml 的交互逻辑
    /// </summary>
    public partial class MainPage : Page
    {
        public IEnumerable<String> VMwareFirmwareList => VMware.Firmwares.Keys;
        public IEnumerable<String> VMwareSoundCardList => VMware.SoundCards.Keys;
        public IEnumerable<String> VMwareNicList => VMware.NICs.Keys;

        //当前虚拟机
        public VirtualMachine CurrentVM { get; set; }
        public VirtualMachineWithACPI CurrentVirtualMachineWithACPI
        {
            get
            {
                if (CurrentVM is VirtualMachineWithACPI)
                    return (VirtualMachineWithACPI)CurrentVM;
                else
                    return null;
            }
        }
        public VMwareVM CurrentVMwareVM
        {
            get
            {
                if (CurrentVM is VMwareVM)
                    return (VMwareVM)CurrentVM;
                else
                    return null;
            }
        }

        //当前虚拟机的网卡列表
        public ObservableCollection<string> CurrentVMwareNICs
        {
            get
            {
                if (CurrentVM is VMwareVM)
                    return new ObservableCollection<string>(((VMwareVM)CurrentVM).NICs);
                else
                    return null;
            }
            set
            {
                if (CurrentVM is VMwareVM)
                    ((VMwareVM)CurrentVM).NICs = value.ToList();
                else
                    return;
            }
        }

        public Boolean IsVMware => (CurrentVM is VMwareVM);
        public Boolean IsVirtualMachineWithACPI => (CurrentVM is VirtualMachineWithACPI);
        
        public MainPage(VirtualMachine VM)
        {
            InitializeComponent();
            this.CurrentVM = VM;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            page_main.DataContext = this;
            check_biosdate.IsChecked = CurrentVM.DateLock;
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
            if (NavigationService.CanGoBack)
            {
                NavigationService?.GoBack();
            }
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
            list[ListBoxSel] = ((ComboBox)sender).SelectedItem as string ;
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
        private void CheckBox_biosdate_Checked(object sender, RoutedEventArgs e)
        {
            if (datepicker == null) return;
            CurrentVM.DateLock = true;
            var binding = new Binding("CurrentVM.BIOSDate")
            {
                Source = page_main
            };
            datepicker.SetBinding(DatePicker.SelectedDateProperty, binding);
        }

        private void CheckBox_biosdate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (datepicker == null) return;
            BindingOperations.ClearBinding(datepicker, DatePicker.SelectedDateProperty);
            CurrentVM.DateLock = false;
        }
    }
}
