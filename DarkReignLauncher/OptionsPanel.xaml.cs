using DarkReignBootstrap;
using IniParser.Model;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.IO;
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
    public partial class OptionsPanel : Border
    {
        private IniDataParser parser;

        bool loading = true;

        public OptionsPanel(System.Collections.ObjectModel.ObservableCollection<LauncherOptionItem> OptionsList)
        {
            InitializeComponent();
            List<string> ShaderOption = new List<string>() { string.Empty }
                .Union(Directory.GetFiles("Shaders", "*.glsl", SearchOption.AllDirectories)).ToList();
            ShaderOption.ForEach(dr =>
            {
                Ddraw_ddraw_shader.Items.Add(dr);
            });

            parser = new IniDataParser();
            ReadIniTactics();
            ReadIniDdraw();

            Binding binding1 = new Binding();
            binding1.Source = OptionsList;
            ModItems.SetBinding(ItemsControl.ItemsSourceProperty, binding1);

            loading = false;
        }

        private void ReadIniTactics()
        {
            IniData config = parser.Parse(File.ReadAllText("TACTICS.CFG"));

            Tactics_Logging_ClearLog.IsChecked = config["Logging"]["ClearLog"] == "1";
            Tactics_Logging_LogToFile.IsChecked = config["Logging"]["LogToFile"] == "1";
            Tactics_Logging_EnableErrors.IsChecked = config["Logging"]["EnableErrors"] == "1";
            Tactics_Logging_EnableWarnings.IsChecked = config["Logging"]["EnableWarnings"] == "1";
            Tactics_Logging_EnableNetErrors.IsChecked = config["Logging"]["EnableNetErrors"] == "1";
            Tactics_Logging_EnableDiagnostics.IsChecked = config["Logging"]["EnableDiagnostics"] == "1";
            Tactics_Logging_EnableNetWarnings.IsChecked = config["Logging"]["EnableNetWarnings"] == "1";
            Tactics_Logging_EnableNetDiagnostics.IsChecked = config["Logging"]["EnableNetDiagnostics"] == "1";
            Tactics_Logging_EnableNetStatistics.IsChecked = config["Logging"]["EnableNetStatistics"] == "1";
            Tactics_Logging_EnableAFI.IsChecked = config["Logging"]["EnableAFI"] == "1";
            Tactics_Logging_EnableAJP.IsChecked = config["Logging"]["EnableAJP"] == "1";
            Tactics_Logging_EnableBCA.IsChecked = config["Logging"]["EnableBCA"] == "1";
            Tactics_Logging_EnableCBC.IsChecked = config["Logging"]["EnableCBC"] == "1";
            Tactics_Logging_EnableCRA.IsChecked = config["Logging"]["EnableCRA"] == "1";
            Tactics_Logging_EnableGDM.IsChecked = config["Logging"]["EnableGDM"] == "1";
            Tactics_Logging_EnableILD.IsChecked = config["Logging"]["EnableILD"] == "1";
            Tactics_Logging_EnableMBJ.IsChecked = config["Logging"]["EnableMBJ"] == "1";
            Tactics_Logging_EnableMDV.IsChecked = config["Logging"]["EnableMDV"] == "1";
        }

        private void WriteIniTactics()
        {
            if (loading) return;

            IniData config = parser.Parse(File.ReadAllText("TACTICS.CFG"));

            config["Logging"]["ClearLog"] = Tactics_Logging_ClearLog.IsChecked ?? false ? "1" : "0";
            config["Logging"]["LogToFile"] = Tactics_Logging_LogToFile.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableErrors"] = Tactics_Logging_EnableErrors.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableWarnings"] = Tactics_Logging_EnableWarnings.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableNetErrors"] = Tactics_Logging_EnableNetErrors.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableDiagnostics"] = Tactics_Logging_EnableDiagnostics.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableNetWarnings"] = Tactics_Logging_EnableNetWarnings.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableNetDiagnostics"] = Tactics_Logging_EnableNetDiagnostics.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableNetStatistics"] = Tactics_Logging_EnableNetStatistics.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableAFI"] = Tactics_Logging_EnableAFI.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableAJP"] = Tactics_Logging_EnableAJP.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableBCA"] = Tactics_Logging_EnableBCA.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableCBC"] = Tactics_Logging_EnableCBC.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableCRA"] = Tactics_Logging_EnableCRA.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableGDM"] = Tactics_Logging_EnableGDM.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableILD"] = Tactics_Logging_EnableILD.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableMBJ"] = Tactics_Logging_EnableMBJ.IsChecked ?? false ? "1" : "0";
            config["Logging"]["EnableMDV"] = Tactics_Logging_EnableMDV.IsChecked ?? false ? "1" : "0";

            if (File.Exists("TACTICS.CFG") && !File.Exists("TACTICS.CFG.BAK")) File.Copy("TACTICS.CFG", "TACTICS.CFG.BAK");
            File.WriteAllText("TACTICS.CFG", config.ToString());
        }

        private void ReadIniDdraw()
        {
            IniData config = parser.Parse(File.ReadAllText("ddraw.ini"));

            //Ddraw_ddraw_curres.IsChecked = config["ddraw"]["width"] == "true";
            //Ddraw_ddraw_curres.IsChecked = config["ddraw"]["height"] == "true";
            Ddraw_ddraw_fullscreen.IsChecked = config["ddraw"]["fullscreen"] == "true";
            Ddraw_ddraw_windowed.IsChecked = config["ddraw"]["windowed"] == "true";
            Ddraw_ddraw_maintas.IsChecked = config["ddraw"]["maintas"] == "true";
            Ddraw_ddraw_boxing.IsChecked = config["ddraw"]["boxing"] == "true";
            Ddraw_ddraw_border.IsChecked = config["ddraw"]["border"] == "true";
            Ddraw_ddraw_adjmouse.IsChecked = config["ddraw"]["adjmouse"] == "true";
            Ddraw_ddraw_renderer.SelectedValue = config["ddraw"]["renderer"];
            Ddraw_ddraw_shader.SelectedValue = config["ddraw"]["shader"];
        }

        private void WriteIniDdraw()
        {
            if (loading) return;

            IniData config = parser.Parse(File.ReadAllText("ddraw.ini"));

            //config["ddraw"]["width"] = Ddraw_ddraw_curres.IsChecked.HasValue ? Ddraw_ddraw_curres.IsChecked.Value ? System.Windows.SystemParameters.WorkArea.Width.ToString() : "0" : string.Empty;
            //config["ddraw"]["height"] = Ddraw_ddraw_curres.IsChecked.HasValue ? Ddraw_ddraw_curres.IsChecked.Value ? System.Windows.SystemParameters.WorkArea.Height.ToString() : "0" : string.Empty;
            config["ddraw"]["fullscreen"] = Ddraw_ddraw_fullscreen.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["windowed"] = Ddraw_ddraw_windowed.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["maintas"] = Ddraw_ddraw_maintas.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["boxing"] = Ddraw_ddraw_boxing.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["border"] = Ddraw_ddraw_border.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["adjmouse"] = Ddraw_ddraw_adjmouse.IsChecked ?? false ? "true" : "false";
            config["ddraw"]["renderer"] = (string)Ddraw_ddraw_renderer.SelectedValue;
            config["ddraw"]["shader"] = (string)Ddraw_ddraw_shader.SelectedValue;

            if (File.Exists("ddraw.ini") && !File.Exists("ddraw.ini.bak")) File.Copy("ddraw.ini", "ddraw.ini.bak");
            File.WriteAllText("ddraw.ini", config.ToString());
        }

        private void CheckBoxOpt_Checked(object sender, RoutedEventArgs e)
        {
            switch (((CheckBox)sender).Name.Split('_')[0])
            {
                case "Tactics":
                    WriteIniTactics();
                    break;
                case "Ddraw":
                    WriteIniDdraw();
                    break;
            }
        }

        private void CheckBoxOpt_Unchecked(object sender, RoutedEventArgs e)
        {
            switch (((CheckBox)sender).Name.Split('_')[0])
            {
                case "Tactics":
                    WriteIniTactics();
                    break;
                case "Ddraw":
                    WriteIniDdraw();
                    break;
            }
        }

        private void Renderer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteIniDdraw();
        }

        private void Shader_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteIniDdraw();
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
