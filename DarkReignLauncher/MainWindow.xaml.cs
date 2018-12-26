using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        System.Windows.Threading.DispatcherTimer dispatcherTimerTest;

        public MainWindow()
        {
            InitializeComponent();

            Image ParalaxBG = new Image();
            //this.Background = new ImageBrush(new BitmapImage(new Uri(@"pack://siteoforigin:,,,/ldata/bg-norm.png")));
            ParalaxBG.Source = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/ldata/bg-norm.png"));
            ParalaxBackground.Content = ParalaxBG;

            Image ParalaxLogo_ = new Image();
            ParalaxLogo_.Source = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/ldata/logo-dkreign.png"));
            ParalaxLogo.Content = ParalaxLogo_;

            this.Cursor = new Cursor(Application.GetResourceStream(new Uri("pack://application:,,,/dkreign.cur")).Stream);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = ParalaxTimespan;
            dispatcherTimer.Start();


            dispatcherTimerTest = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimerTest.Tick += dispatcherTimerTest_Tick;
            dispatcherTimerTest.Interval = TimeSpan.FromSeconds(5);
            dispatcherTimerTest.Start();
        }

        static Random rnd = new Random();
        private void dispatcherTimerTest_Tick(object sender, EventArgs e)
        {
            string[] BGs = Directory.GetFiles("ldata", "bg-*.png", SearchOption.TopDirectoryOnly);
            int r = rnd.Next(BGs.Length);
            Image ParalaxBG = new Image();
            ParalaxBG.Source = new BitmapImage(new Uri($@"pack://siteoforigin:,,,/ldata/{System.IO.Path.GetFileName(BGs[r])}"));
            ParalaxBackground.Content = ParalaxBG;

            string[] LOGOs = Directory.GetFiles("ldata", "logo-*.png", SearchOption.TopDirectoryOnly);
            r = rnd.Next(LOGOs.Length);
            Image ParalaxLogo_ = new Image();
            ParalaxLogo_.Source = new BitmapImage(new Uri($@"pack://siteoforigin:,,,/ldata/{System.IO.Path.GetFileName(LOGOs[r])}"));
            ParalaxLogo.Content = ParalaxLogo_;
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

                    var t = (ParalaxLogo.RenderTransform as TransformGroup);
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
    }
}
