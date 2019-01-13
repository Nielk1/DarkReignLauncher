using DarkReignBootstrap;
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

namespace DarkReignLauncher
{
    /// <summary>
    /// Interaction logic for OptionsPanel.xaml
    /// </summary>
    public partial class OptionsPanel : UserControl
    {
        public OptionsPanel(System.Collections.ObjectModel.ObservableCollection<LauncherOptionItem> OptionsList)
        {
            InitializeComponent();

            Binding binding1 = new Binding();
            binding1.Source = OptionsList;
            ModItems.SetBinding(ItemsControl.ItemsSourceProperty, binding1);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ModOption opt = (ModOption)(((CheckBox)sender).DataContext);
            opt.Set(((CheckBox)sender).IsChecked ?? false);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModOption opt = (ModOption)(((CheckBox)sender).DataContext);
            opt.Set(((CheckBox)sender).IsChecked ?? false);
        }
    }
}
