using DarkReignBootstrap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DarkReignLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        TimeSpan ParalaxTimespan = new TimeSpan(0, 0, 0, 0, 50);
        TimeSpan ParalaxTimespan2 = new TimeSpan(0, 0, 0, 0, 500);

        const int ParalaxLim = 10;

        enum Mode
        {
            Default,
            Mods,
            Options,
            Update,
            About
        }

        Mode mode = Mode.Default;

        ObservableCollection<LauncherMenuItem> CurrentMenuItems = new ObservableCollection<LauncherMenuItem>();
        ObservableCollection<LauncherMenuItem> ModMenuItems = new ObservableCollection<LauncherMenuItem>();
        ObservableCollection<LauncherOptionItem> OptionList = new ObservableCollection<LauncherOptionItem>();

        ModListPanel MoreModsPanel;
        OptionsPanel OptionsPanel;

        public MainWindow()
        {
            Console.WriteLine("Initalizing");

            InitializeComponent();

            Console.WriteLine("Initalized");

            {
                VersionInfo.Text = "PATCH: " + (File.Exists(@"ldata\patch.ver") ? File.ReadAllText(@"ldata\patch.ver").Trim() : "???");
                VersionInfo.Text += "    LAUNCHER: " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                VersionInfo.Text += "    BOOTSTRAP: " + (File.Exists("bootstrap.exe") ? FileVersionInfo.GetVersionInfo("bootstrap.exe").FileVersion : "???");
                VersionInfo.Text += "    DARKHOOK: " + (File.Exists("DarkHook.dll") ? FileVersionInfo.GetVersionInfo("DarkHook.dll").FileVersion : "???");
            }

            SetLogo(null);
            SetBackground(null);

            this.Cursor = new Cursor(Application.GetResourceStream(new Uri("pack://application:,,,/dkreign.cur")).Stream);
            
            Binding binding1 = new Binding();
            binding1.Source = CurrentMenuItems;
            MenuItems.SetBinding(ItemsControl.ItemsSourceProperty, binding1);

            string[] ModFiles = Directory.GetFiles("ldata", "*.launchprofile", SearchOption.TopDirectoryOnly);
            List<ModInstructions> Mods = ModFiles.Select(dr => new ModInstructions(dr)).ToList();

            Mods.Where(dr => dr.TopMenu)
                .OrderBy(dr => string.IsNullOrWhiteSpace(dr.Sort) ? 1 : 0)
                .ThenBy(dr => dr.Sort ?? string.Empty)
                .ThenBy(dr => dr.Title).ToList().ForEach(dr =>
                {
                    string Title = dr.Title;
                    //if (Title.StartsWith("Dark Reign:")) Title = Title.Substring(11).TrimStart();
                    CurrentMenuItems.Add(new LauncherMenuItem()
                    {
                        ItemType = LauncherMenuItemType.Mod,
                        Filename = dr.Filename,
                        Text = Title,
                        Note = dr.Note,
                        Logo = dr.Logo,
                        Background = dr.Background
                    });
                });

            CurrentMenuItems.Add(new LauncherMenuItem() {
                ItemType = LauncherMenuItemType.Mods,
                Text = "Mods",
                Note = $"({Mods.Where(dr => !dr.TopMenu).Count()})",
            });
            CurrentMenuItems.Add(new LauncherMenuItem() { ItemType = LauncherMenuItemType.None });
            CurrentMenuItems.Add(new LauncherMenuItem() { ItemType = LauncherMenuItemType.Options, Text = "Options" });
            //CurrentMenuItems.Add(new LauncherMenuItem() { ItemType = LauncherMenuItemType.Update, Text = "Update" });
            CurrentMenuItems.Add(new LauncherMenuItem() { ItemType = LauncherMenuItemType.About, Text = "About" });
            CurrentMenuItems.Add(new LauncherMenuItem() { ItemType = LauncherMenuItemType.Exit, Text = "Exit" });

            Mods.Where(dr => !dr.TopMenu)
                .OrderBy(dr => string.IsNullOrWhiteSpace(dr.Sort) ? 1 : 0)
                .ThenBy(dr => dr.Sort ?? string.Empty)
                .ThenBy(dr => dr.Title).ToList().ForEach(dr =>
                {
                    string Title = dr.Title;
                    //if (Title.StartsWith("Dark Reign:")) Title = Title.Substring(11).TrimStart();
                    ModMenuItems.Add(new LauncherMenuItem()
                    {
                        ItemType = LauncherMenuItemType.Mod,
                        Filename = dr.Filename,
                        Text = Title,
                        Note = dr.Note,
                        Logo = dr.Logo,
                        Background = dr.Background
                    });
                });

            MoreModsPanel = new ModListPanel(ModMenuItems);

            MoreModsPanel.MenuItem_MouseDown_Event += MenuItem_MouseDown_HandleItem;
            MoreModsPanel.MenuItem_MouseEnter_Event += MenuItem_MouseEnter_HandleItem;
            MoreModsPanel.MenuItem_MouseLeave_Event += MenuItem_MouseLeave_HandleItem;

            Mods.OrderBy(dr => dr.TopMenu ? 0 : 1)
                .ThenBy(dr => string.IsNullOrWhiteSpace(dr.Sort) ? 1 : 0)
                .ThenBy(dr => dr.Sort ?? string.Empty)
                .ThenBy(dr => dr.Title).ToList().ForEach(dr =>
                {
                    if(dr.Options.Count > 0)
                    {
                        OptionList.Add(new LauncherOptionItem()
                        {
                            ModInstructions = dr,
                        });
                    }
                });

            OptionsPanel = new OptionsPanel(OptionList);


            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = ParalaxTimespan;
            dispatcherTimer.Start();
        }

        private string currentLG = null;
        private void SetLogo(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) filename = "logo-patch.png";
            if (currentLG == filename) return;
            currentLG = filename;

            Uri path = new Uri($@"pack://siteoforigin:,,,/ldata/{filename}");
            Image ParalaxLOGO = new Image();
            ParalaxLOGO.Source = new BitmapImage(path);
            ParalaxLOGO.SnapsToDevicePixels = true;
            ParalaxLOGO.HorizontalAlignment = HorizontalAlignment.Right;
            ParalaxLOGO.VerticalAlignment = VerticalAlignment.Bottom;
            GameLogo.Content = ParalaxLOGO;
        }

        private string currentBG = null;
        private void SetBackground(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) filename = "bg-norm.png";
            if (currentBG == filename) return;
            currentBG = filename;

            Uri path = new Uri($@"pack://siteoforigin:,,,/ldata/{filename}");
            Image ParalaxBG = new Image();
            ParalaxBG.Source = new BitmapImage(path);
            ParalaxBackground.Content = ParalaxBG;
        }

        private void SetInfo(FrameworkElement elem)
        {
            if(elem != null)
            {
                Border tmp = (elem.Parent as Border);
                if (tmp != null) tmp.Child = null;

                Border tmpBorder = new Border();
                tmpBorder.Child = elem;
                InfoBox.Content = tmpBorder;
                return;
            }
            FrameworkElement tmpElem = new FrameworkElement();
            InfoBox.Content = tmpElem;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            WindowAspectRatio.Register((Window)sender);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        Win32Point curPoint = new Win32Point();
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (GetCursorPos(ref curPoint))
            {
                Point offset = (Point)(this.PointFromScreen(new Point(curPoint.X, curPoint.Y)) - new Point(this.Width / 2, this.Height / 2));

                double offsetX = -Math.Min(Math.Max(offset.X / 100, -ParalaxLim), ParalaxLim);
                double offsetY = -Math.Min(Math.Max(offset.Y / 100, -ParalaxLim), ParalaxLim);

                double halfV = new Vector(offsetX, offsetY).Length;
                double scale = 1.01f - ((1- halfV * halfV) / 5000.0f);

                double ang = offsetX / 40 *  Math.Sign(offsetY);

                {
                    var t = (ParalaxBackground.RenderTransform as TransformGroup);
                    var t1 = (t.Children[0] as TranslateTransform);
                    var t2 = (t.Children[1] as ScaleTransform);
                    var t3 = (t.Children[2] as RotateTransform);

                    t2.CenterX = this.Width / 2;
                    t2.CenterY = this.Height / 2;
                    t3.CenterX = this.Width / 2;
                    t3.CenterY = this.Height / 2;

                    var oldTranX = (double)t1.GetAnimationBaseValue(TranslateTransform.XProperty);
                    var oldTranY = (double)t1.GetAnimationBaseValue(TranslateTransform.YProperty);
                    var oldScaleX = (double)t2.GetAnimationBaseValue(ScaleTransform.ScaleXProperty);
                    var oldScaleY = (double)t2.GetAnimationBaseValue(ScaleTransform.ScaleYProperty);
                    var oldAngle = (double)t3.GetAnimationBaseValue(RotateTransform.AngleProperty);


                    var animationX = new DoubleAnimation { To = offsetX, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationY = new DoubleAnimation { To = offsetY, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationSX = new DoubleAnimation { To = scale, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationSY = new DoubleAnimation { To = scale, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationAng = new DoubleAnimation { To = ang, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };

                    t1.X = offsetX;
                    t1.Y = offsetY;
                    t2.ScaleX = scale;
                    t2.ScaleY = scale;
                    t3.Angle = ang;

                    t1.BeginAnimation(TranslateTransform.XProperty, animationX, HandoffBehavior.Compose);
                    t1.BeginAnimation(TranslateTransform.YProperty, animationY, HandoffBehavior.Compose);
                    t2.BeginAnimation(ScaleTransform.ScaleXProperty, animationSX, HandoffBehavior.Compose);
                    t2.BeginAnimation(ScaleTransform.ScaleYProperty, animationSY, HandoffBehavior.Compose);
                    t3.BeginAnimation(RotateTransform.AngleProperty, animationAng, HandoffBehavior.Compose);
                }
                {
                    offsetX /= -4;
                    offsetY /= -4;
                    ang /= -2;

                    var t = (ParalaxContent.RenderTransform as TransformGroup);
                    var t1 = (t.Children[0] as TranslateTransform);
                    var t3 = (t.Children[1] as RotateTransform);

                    t3.CenterX = this.Width / 2;
                    t3.CenterY = this.Height / 2;

                    var oldTranX = (double)t1.GetAnimationBaseValue(TranslateTransform.XProperty);
                    var oldTranY = (double)t1.GetAnimationBaseValue(TranslateTransform.YProperty);
                    var oldAngle = (double)t3.GetAnimationBaseValue(RotateTransform.AngleProperty);


                    var animationX = new DoubleAnimation { To = offsetX, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationY = new DoubleAnimation { To = offsetY, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationSX = new DoubleAnimation { To = scale, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationSY = new DoubleAnimation { To = scale, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };
                    var animationAng = new DoubleAnimation { To = ang, Duration = ParalaxTimespan2, DecelerationRatio = 0.5d, FillBehavior = FillBehavior.Stop };

                    t1.X = offsetX;
                    t1.Y = offsetY;
                    t3.Angle = ang;

                    t1.BeginAnimation(TranslateTransform.XProperty, animationX, HandoffBehavior.Compose);
                    t1.BeginAnimation(TranslateTransform.YProperty, animationY, HandoffBehavior.Compose);
                    t3.BeginAnimation(RotateTransform.AngleProperty, animationAng, HandoffBehavior.Compose);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseEnter_HandleItem(item);
        }
        private void MenuItem_MouseEnter_HandleItem(LauncherMenuItem item)
        {
            switch (item.ItemType)
            {
                case LauncherMenuItemType.Mod:
                    SetLogo(item.Logo);
                    SetBackground(item.Background);
                    break;
                //case LauncherMenuItemType.About:
                //    SetLogo(null);
                //    SetBackground(null);
                //    break;
                default:
                    SetLogo(null);
                    SetBackground(null);
                    break;
            }
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseLeave_HandleItem(item);
        }
        private void MenuItem_MouseLeave_HandleItem(LauncherMenuItem item)
        {
            /*switch (item.ItemType)
            {
                case LauncherMenuItemType.Mod:
                    break;
            }*/
        }

        private void MenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LauncherMenuItem item = (sender as FrameworkElement)?.DataContext as LauncherMenuItem;
            if (item == null) return;
            MenuItem_MouseDown_HandleItem(item);
        }
        private void MenuItem_MouseDown_HandleItem(LauncherMenuItem item)
        {
            switch (item.ItemType)
            {
                case LauncherMenuItemType.Exit:
                    Close();
                    break;
                case LauncherMenuItemType.Mod:
                    LaunchMod(item.Filename);
                    break;
                case LauncherMenuItemType.Mods:
                    if (mode == Mode.Mods)
                    {
                        SetInfo(null);
                        mode = Mode.Default;
                    }
                    else
                    {
                        SetInfo(MoreModsPanel);
                        mode = Mode.Mods;
                    }
                    break;
                case LauncherMenuItemType.Options:
                    if (mode == Mode.Options)
                    {
                        SetInfo(null);
                        mode = Mode.Default;
                    }
                    else
                    {
                        SetInfo(OptionsPanel);
                        mode = Mode.Options;
                    }
                    break;
                case LauncherMenuItemType.Update:
                    if (mode == Mode.Update)
                    {
                        SetInfo(null);
                        mode = Mode.Default;
                    }
                    else
                    {
                        SetInfo(new FrameworkElement());
                        mode = Mode.Update;
                    }
                    break;
                case LauncherMenuItemType.About:
                    if (mode == Mode.About)
                    {
                        SetInfo(null);
                        mode = Mode.Default;
                    }
                    else
                    {
                        TextBlock block = new TextBlock();
                        block.Padding = new Thickness(5);
                        block.Text = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
                        block.TextWrapping = TextWrapping.Wrap;
                        SetInfo(block);
                        mode = Mode.About;
                    }
                    break;

            }
        }

        private void LaunchMod(string modname)
        {
            string exe = Environment.GetCommandLineArgs()[0]; // Command invocation part
            string rawCmd = Environment.CommandLine;          // Complete command
            //string argsOnly = rawCmd.Remove(rawCmd.IndexOf(exe), exe.Length).TrimStart('"').Substring(1);

            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "bootstrap.exe",
                Arguments = modname,
                UseShellExecute = false,
            };

            Process proc = Process.Start(info);

            this.Hide();
            
            // let's wait for the process to exit by polling instead of using WaitForExit()
            // this seems to work better if the process is already closed or does something strange
            while (proc != null && !proc.HasExited)
            {
                Thread.Sleep(1000);
            }

            this.Close();
        }
    }

    public class LauncherOptionItem
    {
        public ModInstructions ModInstructions { get; set; }
    }

    public class LauncherMenuItem
    {
        public LauncherMenuItemType ItemType { get; set; }
        public string Filename { get; set; }

        public string Text { get { return _Text ?? " "; } set { _Text = value; } }
        private string _Text;
        public string Note { get { return _Note ?? string.Empty; } set { _Note = value; } }
        private string _Note;
        public bool ShowNote { get { return !string.IsNullOrWhiteSpace(Note); } }

        public string Logo { get; set; }
        public string Background { get; set; }
    }

    public enum LauncherMenuItemType
    {
        None,
        Exit,
        Mod,
        Mods,
        Options,
        Update,
        About
    }
}
