using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DarkReignBootstrap;

namespace DarkReignLauncher
{
    /// <summary>
    /// Interaction logic for ModListPanel.xaml
    /// </summary>
    public partial class ModListPanel : Border
    {
        public ModListPanel(System.Collections.ObjectModel.ObservableCollection<LauncherMenuItem> CurrentMenuItems)
        {
            InitializeComponent();

            Binding binding1 = new Binding();
            binding1.Source = CurrentMenuItems;
            MenuItems.SetBinding(ItemsControl.ItemsSourceProperty, binding1);
        }

        public delegate void MenuItem_MouseEvent(LauncherMenuItem item);

        public MenuItem_MouseEvent MenuItem_MouseEnter_Event;
        public MenuItem_MouseEvent MenuItem_MouseLeave_Event;
        public MenuItem_MouseEvent MenuItem_MouseDown_Event;

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseEnter_Event.Invoke(item);
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseLeave_Event.Invoke(item);
        }

        private void MenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseDown_Event.Invoke(item);
        }
    }
}
