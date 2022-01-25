using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Forms = System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NHotkey.Wpf;
using NHotkey;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Reflection;
using s = Clickett.Properties.Settings;

namespace Clickett
{
    public partial class MainWindow : Window
    {
        public bool doLocation, active, clicking;
        private bool newTrigListen, newLocListen, interType, settOpen, doAnimations, aot, startup, trayIcon, minToTray;
        private int modeInt, clickCounter, burstCount;
        private uint clickDo, clickUp, xPos, yPos;
        private int clickInterval, uiScale;
        private Key hotkey;
        private System.Drawing.Icon icon, iconbw;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        IntPtr hwnd;
        private Forms.NotifyIcon _tbi;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        public MainWindow()
        {
            InitializeComponent();
            InitializeThingies();
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Auto);
        }

        private void InitializeThingies()
        {
            icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcirc.ico");
            iconbw = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcircbw.ico");
            hotkey = Key.F;
            doLocation = true;
            ToggleLoc(this, null);
            interType = true;
            millisInput.Text = "0";
            secondsInput.Text = "0";
            minutesInput.Text = "0";
            clickInterval = s.Default.clickInterval;
            InterTypeSwap(this, null);
            burstCount = s.Default.burstCount;
            ModeChange(this, null);
            settOpen = true;
            SettingsToggle(this, null);
            doAnimations = !s.Default.doAnimations;
            ToggleAnim(this, null);
            aot = !s.Default.aot;
            ToggleAot(this, null);
            startup = !s.Default.startup;
            ToggleStartup(this, null);
            trayIcon = !s.Default.trayIcon;
            ToggleTray(this, null);
            minToTray = !s.Default.trayIcon;
            ToggleMinTray(this, null);

            uiScaleSlid.Value = uiScale = s.Default.uiScale;
            ScaleChange(this, null);
            modeSel.SelectedIndex = modeInt = s.Default.modeInt;
            clickSel.SelectedIndex = 0;
            clickDo = 0x02; //LMB = 0x02  MMB = 0x20  RMB = 0x08
            clickUp = 0x04; //LMB = 0x04  MMB = 0x40  RMB = 0x10
        }

        private void Activate(object sender, RoutedEventArgs e)
        {
            if (active)
            {
                HotkeyManager.Current.Remove("0");
                activateButtText.Text = "Activate";
                LinearGradientBrush myHorizontalGradient = new LinearGradientBrush();
                myHorizontalGradient.StartPoint = new Point(0, 0.5);
                myHorizontalGradient.EndPoint = new Point(1, 0.5);
                myHorizontalGradient.GradientStops.Add(new GradientStop(Color.FromRgb(69, 180, 180), 0.0));
                myHorizontalGradient.GradientStops.Add(new GradientStop(Color.FromRgb(69, 170, 150), 1));
                activateButt.Background = myHorizontalGradient;
                active = false;
                clicking = false;
                trigSet.IsEnabled = locSetButt.IsEnabled = true;
                trigSet.Opacity = locSetButt.Opacity = 1;
                if (trayIcon)
                {
                    _tbi.Icon = iconbw;
                    _tbi.ContextMenuStrip.Items[0].Text = "Activate";
                }
            }
            else
            {
                activateButtText.Text = "Stop";
                activateButt.Background = new SolidColorBrush(Color.FromArgb(100, 69, 69, 69));
                active = true;
                clicking = false;
                trigSet.IsEnabled = locSetButt.IsEnabled = false;
                trigSet.Opacity = locSetButt.Opacity = 0.5;
                HotkeyManager.Current.AddOrReplace("0", hotkey, ModifierKeys.None, HotkeyPressed);
                if (trayIcon)
                {
                    _tbi.Icon = icon;
                    _tbi.ContextMenuStrip.Items[0].Text = "Deactivate";
                }
            }
        }

        private void ToggleLoc(object sender, RoutedEventArgs e)
        {
            doLocation = !doLocation;
            ColourToggle(LocBut, doLocation);
            locBorder.Opacity = doLocation ? 1 : 0.4;
            xPosInput.IsEnabled = yPosInput.IsEnabled = locSetButt.IsEnabled = doLocation ? true : false;
            if (doLocation) dispatcherTimer.Tick += new EventHandler(SetCurLoc);
            else dispatcherTimer.Tick -= new EventHandler(SetCurLoc);
        }

        private void ToggleAnim(object sender, RoutedEventArgs e)
        {
            doAnimations = !doAnimations;
            ColourToggle(animButt, doAnimations);
            animBorder.Opacity = doAnimations ? 1 : 0.4;
        }

        private void ToggleAot(object sender, RoutedEventArgs e)
        {
            aot = !aot;
            Topmost = aot;
            ColourToggle(aotButt, aot);
            aotBorder.Opacity = aot ? 1 : 0.4;
        }

        private void ToggleStartup(object sender, RoutedEventArgs e)
        {
            startup = !startup;
            ColourToggle(startupButt, startup);
            startupBorder.Opacity = startup ? 1 : 0.4;
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (startup)
            {
                key.SetValue("Clickett", System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Clickett.exe", RegistryValueKind.ExpandString);
            }
            else
            {
                key.DeleteValue("Clickett", false);
            }
        }

        private void ToggleTray(object sender, RoutedEventArgs e)
        {
            trayIcon = !trayIcon;
            ColourToggle(trayButt, trayIcon);
            trayBorder.Opacity = trayIcon ? 1 : 0.4;
            if (trayIcon)
            {
                _tbi = new Forms.NotifyIcon();
                _tbi.Text = "Clickett";
                _tbi.ContextMenuStrip = new Forms.ContextMenuStrip();
                _tbi.ContextMenuStrip.Font = new System.Drawing.Font("Verdana Pro", 10);
                _tbi.ContextMenuStrip.ImageScalingSize = System.Drawing.Size.Empty;
                _tbi.ContextMenuStrip.BackColor = System.Drawing.Color.FromArgb(255,100,117,117);
                _tbi.ContextMenuStrip.ForeColor = System.Drawing.Color.FromArgb(255,255,255,255);
                if (active) { _tbi.ContextMenuStrip.Items.Add("Deactivate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = icon; }
                else { _tbi.ContextMenuStrip.Items.Add("Activate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = iconbw; }
                _tbi.ContextMenuStrip.Items.Add("Open", null, (s,e) => { WindowState = WindowState.Normal; Activate(); });
                _tbi.ContextMenuStrip.Items.Add("Close", null, (s,e) => { Close(); });
                _tbi.DoubleClick += (s,e) =>
                {
                    Activate(this, null);
                };
                _tbi.Visible = true;
            }
            else
            {
                if(minToTray)ToggleMinTray(this, null);
                _tbi.Dispose();
            }
        }

        private void ToggleMinTray(object sender, RoutedEventArgs e)
        {
            minToTray = !minToTray;
            ColourToggle(minTrayButt, minToTray);
            minTrayBorder.Opacity = minToTray ? 1 : 0.4;
            if (minToTray && !trayIcon) ToggleTray(this, null);
        }

        private void ColourToggle(Button b, bool colour)
        {
            b.Background = new SolidColorBrush(colour ? Color.FromRgb(69, 180, 180) : Color.FromArgb(90, 178, 187, 175));
        }

        private void HotkeyPressed(object? sender, HotkeyEventArgs e)
        {
            if (active)
            {
                AutoClick();
            }
            else
            {
                if (newLocListen)
                {
                    HotkeyManager.Current.Remove("0");
                    Mouse.Capture(this);
                    Point pointToWindow = Mouse.GetPosition(this);
                    Point pointToScreen = PointToScreen(pointToWindow);
                    xPosInput.Text = pointToScreen.X.ToString();
                    xPos = (uint)pointToScreen.X;
                    yPosInput.Text = pointToScreen.Y.ToString();
                    yPos = (uint)pointToScreen.Y;
                    Mouse.Capture(null);

                    newLocListen = false;
                    locGrid.Visibility = Visibility.Visible;
                    locText.Text = "Location";
                    locText.HorizontalAlignment = HorizontalAlignment.Left;
                    locText.Opacity = 1;
                }
            }
        }

        private void AutoClick()
        {
            if (modeInt == 2)            //Hold
            {
                if (clicking)
                {
                    return;
                }
                clicking = true;
                dispatcherTimer.Tick += new EventHandler(Click);
                dispatcherTimer.Tick += new EventHandler(HoldCheck);
                EnterClickState();
            }
            else if (modeInt == 1)      //Toggle
            {
                if (clicking)
                {
                    ExitClickState();
                }
                else
                {
                    dispatcherTimer.Tick += new EventHandler(Click);
                    EnterClickState();
                }
            }
            else if (modeInt == 0)     //Burst
            {
                if (clicking)
                {
                    clickCounter = 0;
                }
                else
                {
                    dispatcherTimer.Tick += new EventHandler(Click);
                    dispatcherTimer.Tick += new EventHandler(IterateClick);
                    EnterClickState();
                }
            }
        }
        private void Click(object sender, EventArgs e)
        {
            mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
        }

        private void SetCurLoc(object sender, EventArgs e)
        {
            SetCursorPos((int)xPos, (int)yPos);
        }

        private void HoldCheck(object sender, EventArgs e)
        {
            if (!Keyboard.IsKeyDown(hotkey)){
                ExitClickState();
                dispatcherTimer.Tick -= HoldCheck;
            }
        }

        private void IterateClick(object sender, EventArgs e)
        {
            clickCounter++;
            if (clickCounter >= burstCount)
            {
                ExitClickState();
                dispatcherTimer.Tick -= IterateClick;
            }
        }

        private void EnterClickState()
        {
            clicking = true;
            Focusable = false;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(Math.Round(clickInterval*0.9f));
            hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
            fullCanvas.Opacity = 0.4;
            var blur = new System.Windows.Media.Effects.BlurEffect();
            blur.Radius = 7;
            fullGrid.Effect = blur;
            dispatcherTimer.Start();
        }

        private void ExitClickState()
        {
            dispatcherTimer.Stop();
            Focusable = true;
            SetWindowExDefault(hwnd);
            fullCanvas.Opacity = 1;
            fullGrid.Effect = null;
            dispatcherTimer.Tick -= Click;
            clicking = false;
        }

        private void WindowDrag(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void AppMin(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;
            ShowInTaskbar = !minToTray;
        }

        private void AppClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NewTrigger(object sender, RoutedEventArgs e)
        {
            newTrigListen = true;
            trigText.Text = "Press a key";
            trigText.HorizontalAlignment = HorizontalAlignment.Center;
            trigText.Opacity = 0.7;
            trigBorder.Opacity = trigSet.Opacity = 0;
        }

        private void ModeChange(object sender, SelectionChangedEventArgs e)
        {
            modeInt = modeSel.SelectedIndex;
            if(modeInt == 0)
            {
                burstCountInput.Text = burstCount.ToString();
                burstGrid.Visibility = Visibility.Visible;
            }
            else
            {
                burstGrid.Visibility = Visibility.Hidden;
            }
        }

        private void CliclChange(object sender, SelectionChangedEventArgs e)
        {
            switch (clickSel.SelectedIndex)
            {
                case 0:
                    clickDo = 0x02; //Left
                    clickUp = 0x04;
                    break;
                case 1:
                    clickDo = 0x20; //Middle
                    clickUp = 0x40;
                    break;
                case 2:
                    clickDo = 0x08; //Right
                    clickUp = 0x10;
                    break;
            }
        }

        private void cpsChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var cps = cpsSlid.Value;
            clickInterval = (int)Math.Round(1000 / cps);
            interText.Text = "Clicks Per Second - " + cps;
        }

        private void InterTypeSwap(object sender, RoutedEventArgs e)
        {
            interType = !interType;
            if (interType)
            {
                interText.Text = "Click Interval";
                InterTextSet(clickInterval, 0, 0);
                interGrid.Visibility = Visibility.Visible;
                cpsGrid.Visibility = Visibility.Hidden;
            }
            else
            {
                if(clickInterval < 1) clickInterval = 1; 
                var cps = 1000 / clickInterval;
                if (cps < 1)
                {
                    cps = 1;
                    clickInterval = 1000;
                }
                else if (cps > 69)
                {
                    cps = 69;
                    clickInterval = 14;
                }
                cpsSlid.Value = cps;
                interText.Text = "Clicks Per Second - " + cps;
                interGrid.Visibility = Visibility.Hidden;
                cpsGrid.Visibility = Visibility.Visible;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void HandleInterTextChange(object sender, TextChangedEventArgs e)
        {
            if (millisInput.Text != "") try { int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; }
            if (secondsInput.Text != "") try { int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; }
            if (minutesInput.Text != "") try { int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; }
            if (xPosInput.Text != "") try { int.Parse(xPosInput.Text); } catch { xPosInput.Text = "0"; xPos = 0; }
            if (yPosInput.Text != "") try { int.Parse(yPosInput.Text); } catch { yPosInput.Text = "0"; yPos = 0; }
            if (burstCountInput.Text != "") try { _ = int.Parse(burstCountInput.Text); } catch { burstCountInput.Text = "0"; burstCount = 0; }
        }

        private void HandleTextChange(object sender, RoutedEventArgs e)
        {
            int millis = 0;
            int seconds = 0;
            int minutes = 0;
            try { millis = int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; millis = 0; }
            try { seconds = int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; seconds = 0; }
            try { minutes = int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; minutes = 0; }
            InterTextSet(millis, seconds, minutes);
        }

        private void HandleLocTextChange(object sender, RoutedEventArgs e)
        {
            int x = 0;
            int y = 0;
            try { x = int.Parse(xPosInput.Text); } catch { xPosInput.Text = "0"; x = 0; }
            try { y = int.Parse(yPosInput.Text); } catch { yPosInput.Text = "0"; y = 0; }
            xPos = (uint)x;
            yPos = (uint)y;
        }

        private void ChangeBurstCount(object sender, RoutedEventArgs e)
        {
            int count = 0;
            try { count = int.Parse(burstCountInput.Text); } catch { burstCountInput.Text = "0"; count = 0; }
            burstCount = count;
        }

        private void InterTextSet(int millis, int seconds, int minutes)
        {
            if (millis >= 1000)
            {
                seconds += (int)Math.Truncate(millis / 1000f);
                millis = millis % 1000;
            }
            if (seconds >= 60)
            {
                var minMore = (int)Math.Floor(seconds / 60f);
                minutes += minMore;
                seconds -= minMore * 60;
            }
            if (minutes > 6969)
            {
                minutes = 6969;
            }
            millisInput.Text = millis.ToString();
            secondsInput.Text = seconds.ToString();
            minutesInput.Text = minutes.ToString();
            clickInterval = millis + (seconds * 1000) + (minutes * 60000);
        }

        private void ScaleSliderChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            uiScaleText.Text = "UI Scale - " + uiScaleSlid.Value + "%";
        }

        private void ScaleChange(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            uiScale = (int)uiScaleSlid.Value;
            if (doAnimations)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(Width, uiScale * 6, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                BeginAnimation(WidthProperty, fadeOutAnimation);
            }
            else
            {
                Width = uiScale * 6;
            }
        }

        private void SettingsToggle(object sender, RoutedEventArgs e)
        {
            if (settOpen)
            {
                settOpen = false;
                if (doAnimations)
                {
                    DoubleAnimation fadeInAnimation = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                    confStack.BeginAnimation(OpacityProperty, fadeInAnimation);
                }
                SettView.Visibility = Visibility.Hidden;
                confStack.Visibility = Visibility.Visible;
            }
            else
            {
                settOpen = true;
                if (doAnimations)
                {
                    DoubleAnimation fadeInAnimation = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                    SettView.BeginAnimation(OpacityProperty, fadeInAnimation);
                }
                SettView.Visibility = Visibility.Visible;
                confStack.Visibility = Visibility.Hidden;
            }
        }

        private void fullGridFocus(object sender, MouseButtonEventArgs e)
        {
            fullGrid.Focus();
        }

        private void SetLoc(object sender, RoutedEventArgs e)
        {
            newLocListen = true;
            locGrid.Visibility = Visibility.Hidden;
            locText.Text = "Press Trigger Key";
            locText.HorizontalAlignment = HorizontalAlignment.Center;
            locText.Opacity = 0.7;
            HotkeyManager.Current.AddOrReplace("0", hotkey, ModifierKeys.None, HotkeyPressed);
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            if (newTrigListen)
            {
                HotkeyManager.Current.AddOrReplace("0", e.Key, ModifierKeys.None, HotkeyPressed);
                trigText.Text = "Trigger";
                trigText.HorizontalAlignment = HorizontalAlignment.Left;
                trigText.Opacity = trigBorder.Opacity = trigSet.Opacity = 1;
                triggerDis.Text = e.Key.ToString();
                hotkey = e.Key;
                newTrigListen = false;
            }
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
            }
        }

        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
        public static void SetWindowExDefault(IntPtr hwnd)
        {
            SetWindowLong(hwnd, GWL_EXSTYLE, 0x00000000);
        }

        private void OnExit(object sender, EventArgs e)
        {
            s.Default.doAnimations = doAnimations;
            s.Default.aot = aot;
            s.Default.startup = startup;
            s.Default.trayIcon = trayIcon;
            s.Default.minToTray = minToTray;
            s.Default.modeInt = modeInt;
            s.Default.burstCount = burstCount;
            s.Default.clickInterval = clickInterval;
            s.Default.uiScale = uiScale;
            s.Default.Save();
            _tbi.Dispose();
        }
    }
}
