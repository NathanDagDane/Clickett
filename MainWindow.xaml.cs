using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using s = Clickett.Properties.Settings;

namespace Clickett
{
    public partial class MainWindow : Window
    {
        public bool doLocation, active, clicking, awaitReset;
        private bool newTrigListen, newLocListen, interType, settOpen, doAnimations, aot, startup, trayIcon, minToTray, hkShift, hkCtrl, hkAlt, countTotal;
        private int modeInt, clickCounter, burstCount;
        private long totalClickCounter;
        private uint clickDo, clickUp, xPos, yPos;
        private int clickInterval, uiScale, hudCurrentPriority;
        private string hudCurrentToken, versionNum = "0.6.2", curTheme;
        private float nOpacity, cOpacity;
        private Key hotkey;
        private System.Drawing.Icon icon, iconbw;
        private Storyboard settStoryboard;
        DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Send), tcResetTimer;
        IntPtr hwnd;
        private Forms.NotifyIcon _tbi;

        private IntPtr _mainWindowHandle;
        private HwndSource _source;
        private readonly int _hotkeyID = 696914248;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
    byte[] keyboardState,
    [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
    StringBuilder receivingBuffer,
    int bufferSize, uint flags);

        public MainWindow()
        {
            InitializeComponent();
            InitializeThingies();
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Auto);
            this.DataContext = this;
        }

        private void InitializeThingies()
        {
            icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcirc.ico");
            iconbw = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcircbw.ico");
            try { hotkey = s.Default.hkAction; }
            catch { hotkey = Key.F; }
            hkCtrl = s.Default.hkCtrl;
            hkShift = s.Default.hkShift;
            hkAlt = s.Default.hkAlt;
            triggerDis.Text = (hkCtrl ? "Ctrl + " : "") + (hkShift ? "Shift + " : "") + (hkAlt ? "Alt + " : "") + KeyChar(hotkey);
            curTheme = s.Default.Theme;
            ResourceDictionary newRes = Application.Current.Resources.MergedDictionaries[1];
            newRes.MergedDictionaries.Clear();
            newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Themes/" + s.Default.Theme + ".xaml", UriKind.Relative) });
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
            countTotal = !s.Default.countTotal;
            ToggleCt(this, null);
            aot = !s.Default.aot;
            ToggleAot(this, null);
            startup = !s.Default.startup;
            ToggleStartup(this, null);
            trayIcon = !s.Default.trayIcon;
            ToggleTray(this, null);
            minToTray = !s.Default.trayIcon;
            ToggleMinTray(this, null);
            ToolTipService.SetInitialShowDelay(trigBorder, 69);

            fullCanvas.Opacity = nOpacity = s.Default.normalOpacity;
            nOpSlid.Value = nOpacity * 100;
            cOpacity = s.Default.clickingOpacity;
            cOpSlid.Value = cOpacity * 100;
            uiScaleSlid.Value = uiScale = s.Default.uiScale;
            Width = uiScale * 6;
            modeSel.SelectedIndex = modeInt = s.Default.modeInt;
            clickSel.SelectedIndex = 0;
            totalText.Text = s.Default.totalClicks.ToString();
            hudCurrentPriority = 5;
            clickDo = 0x02; //LMB = 0x02  MMB = 0x20  RMB = 0x08
            clickUp = 0x04; //LMB = 0x04  MMB = 0x40  RMB = 0x10
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            _mainWindowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_mainWindowHandle);
            _source.AddHook(Hooks);
        }

        private void Activate(object sender, RoutedEventArgs e)
        {
            if (active)
            {
                UnregisterHotkey();
                activateButtText.Text = "Activate";
                activateButt.SetResourceReference(BackgroundProperty, "AcGrad"); //This makes my balls go YES
                active = false;
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
                if (hotkey == Key.None) { MakeNotification("Slow down there!", "Set a hotkey first\nPretty please"); triggerDis.Text = "Choose a hotkey"; return; }
                try
                {
                    RegisterHotkey();
                }
                catch
                {
                    MakeNotification("Ah dang! There was an error!", "Could not register this hotkey.\nTry another or restart the system.");
                    hotkey = Key.None;
                    triggerDis.Text = "oops Fail";
                    trigBorder.ToolTip = "Try something else";
                    return;
                }
                activateButtText.Text = "Stop";
                activateButt.SetResourceReference(BackgroundProperty, "InacBut");
                active = true;
                clicking = false;
                trigSet.IsEnabled = locSetButt.IsEnabled = false;
                trigSet.Opacity = locSetButt.Opacity = 0.5;
                if (trayIcon)
                {
                    _tbi.Icon = icon;
                    _tbi.ContextMenuStrip.Items[0].Text = "Deactivate";
                }
            }
        }

        private void ToggleLoc(object sender, RoutedEventArgs? e)
        {
            doLocation = !doLocation;
            ColourToggle(LocBut, doLocation);
            locBorder.Opacity = doLocation ? 1 : 0.4;
            xPosInput.IsEnabled = yPosInput.IsEnabled = locSetButt.IsEnabled = doLocation ? true : false;
            if (active) locSetButt.IsEnabled = false;
            if (doLocation) dispatcherTimer.Tick += new EventHandler(SetCurLoc);
            else dispatcherTimer.Tick -= new EventHandler(SetCurLoc);
        }

        private void ToggleAnim(object sender, RoutedEventArgs? e)
        {
            doAnimations = s.Default.doAnimations = !doAnimations;
            ColourToggle(animButt, doAnimations);
            animBorder.Opacity = doAnimations ? 1 : 0.4;
            s.Default.Save();
        }

        private void ToggleCt(object sender, RoutedEventArgs? e)
        {
            countTotal = s.Default.countTotal = !countTotal;
            ColourToggle(ctButt, countTotal);
            ctBorder.Opacity = countTotal ? 1 : 0.4;
            s.Default.Save();
        }

        private void ToggleAot(object sender, RoutedEventArgs? e)
        {
            aot = s.Default.aot = !aot;
            Topmost = aot;
            ColourToggle(aotButt, aot);
            aotBorder.Opacity = aot ? 1 : 0.4;
            s.Default.Save();
        }

        private void ToggleStartup(object sender, RoutedEventArgs? e)
        {
            startup = s.Default.startup = !startup;
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
            s.Default.Save();
        }

        private void ToggleTray(object sender, RoutedEventArgs? e)
        {
            trayIcon = s.Default.trayIcon = !trayIcon;
            ColourToggle(trayButt, trayIcon);
            trayBorder.Opacity = trayIcon ? 1 : 0.4;
            if (trayIcon)
            {
                try
                {
                    _tbi = new Forms.NotifyIcon();
                    _tbi.Text = "Clickett";
                    _tbi.ContextMenuStrip = new Forms.ContextMenuStrip();
                    _tbi.ContextMenuStrip.Font = new System.Drawing.Font("LEMON MILK Pro FTR", 10);
                    _tbi.ContextMenuStrip.ImageScalingSize = System.Drawing.Size.Empty;
                    _tbi.ContextMenuStrip.BackColor = System.Drawing.Color.FromArgb(255, 69, 170, 150);
                    _tbi.ContextMenuStrip.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255, 255);
                    if (active) { _tbi.ContextMenuStrip.Items.Add("Deactivate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = icon; }
                    else { _tbi.ContextMenuStrip.Items.Add("Activate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = iconbw; }
                    _tbi.ContextMenuStrip.Items.Add("Open", null, (s, e) => { WindowState = WindowState.Normal; Activate(); });
                    var temp = _tbi.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { Close(); });
                    _tbi.DoubleClick += (s, e) => { Activate(this, null); };
                    _tbi.Visible = true;
                }
                catch
                {
                    MakeNotification("Tray icon died", "There was a problem setting up the tray icon. The tray icon option had been disabled. (Sorry)");
                    ToggleTray(this, null);
                }
            }
            else
            {
                if (minToTray) ToggleMinTray(this, null);
                _tbi.Dispose();
            }
            s.Default.Save();
        }

        private void ToggleMinTray(object sender, RoutedEventArgs? e)
        {
            minToTray = s.Default.minToTray = !minToTray;
            ColourToggle(minTrayButt, minToTray);
            minTrayBorder.Opacity = minToTray ? 1 : 0.4;
            if (minToTray && !trayIcon) ToggleTray(this, null);
            s.Default.Save();
        }

        private void ColourToggle(Button b, bool colour)
        {
            var thingy = (LinearGradientBrush)Application.Current.Resources["AcGrad"];
            b.Background = colour ? thingy : new SolidColorBrush(Color.FromArgb(90, 178, 187, 175));
        }

        private void SetTheme(object sender, RoutedEventArgs? e)
        {
            var themeName = curTheme = s.Default.Theme = ((Button)sender).Name.Substring(0, ((Button)sender).Name.Length - 8);
            s.Default.Save();
            ResourceDictionary newRes = Application.Current.Resources.MergedDictionaries[1];
            newRes.MergedDictionaries.Clear();
            newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Themes/" + themeName + ".xaml", UriKind.Relative) });

            var thingy = (LinearGradientBrush)Application.Current.Resources["AcGrad"];
            if (doLocation) LocBut.Background = thingy;
            if (doAnimations) animButt.Background = thingy;
            if (countTotal) ctButt.Background = thingy;
            if (aot) aotButt.Background = thingy;
            if (startup) startupButt.Background = thingy;
            if (trayIcon) trayButt.Background = thingy;
            if (minToTray) minTrayButt.Background = thingy;

        }

        private IntPtr Hooks(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            var hotkeyID = wParam.ToInt64();
            if (hotkeyID != _hotkeyID) { return IntPtr.Zero; }
            if (active)
            {
                AutoClick();
                handled = true;
            }
            else
            {
                if (newLocListen)
                {
                    UnregisterHotkey();
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

                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterHotkey()
        {
            var mod = 0;
            if (hkAlt) mod |= 0x0001;
            if (hkCtrl) mod |= 0x0002;
            if (hkShift) mod |= 0x0004;
            mod |= 0x4000;
            RegisterHotKey(_mainWindowHandle, _hotkeyID, mod, KeyInterop.VirtualKeyFromKey(hotkey));

        }

        private void UnregisterHotkey()
        {
            UnregisterHotKey(_mainWindowHandle, _hotkeyID);
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
                if (countTotal) dispatcherTimer.Tick += new EventHandler(CountTotal);
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
                    if (countTotal) dispatcherTimer.Tick += new EventHandler(CountTotal);
                    EnterClickState();
                }
            }
            else if (modeInt == 0)     //Burst
            {
                if (clicking)
                {
                    ExitClickState();
                    dispatcherTimer.Tick -= IterateClick;
                    clickCounter = 0;
                }
                else
                {
                    clickCounter = 0;
                    dispatcherTimer.Tick += new EventHandler(IterateClick);
                    if (countTotal) dispatcherTimer.Tick += new EventHandler(CountTotal);
                    EnterClickState();
                }
            }
        }

        private void Click(object sender, EventArgs? e)
        {
            mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
        }

        private void SetCurLoc(object sender, EventArgs? e)
        {
            SetCursorPos((int)xPos, (int)yPos);
        }

        private void HoldCheck(object sender, EventArgs? e)
        {
            if (Keyboard.IsKeyUp(hotkey))
            {
                dispatcherTimer.Tick -= HoldCheck;
                ExitClickState();
            }
        }

        private void IterateClick(object sender, EventArgs? e)
        {
            if (clickCounter >= burstCount)
            {
                ExitClickState();
                dispatcherTimer.Tick -= IterateClick;
                clickCounter = 0;
                return;
            }
            clickCounter++;
            Click(sender, e);
        }

        private void CountTotal(object sender, EventArgs? e)
        {
            totalClickCounter++;
        }

        private void EnterClickState()
        {
            clicking = true;
            Focusable = false;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(clickInterval * 0.8);
            hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
            fullCanvas.Opacity = cOpacity;
            var blur = new System.Windows.Media.Effects.BlurEffect();
            blur.Radius = 5;
            fullGrid.Effect = blur;
            dispatcherTimer.Start();
        }

        private void ExitClickState()
        {
            dispatcherTimer.Stop();
            Focusable = true;
            SetWindowExDefault(hwnd);
            fullCanvas.Opacity = nOpacity;
            fullGrid.Effect = null;
            dispatcherTimer.Tick -= Click;
            dispatcherTimer.Tick -= CountTotal;
            if (countTotal)
            {
                s.Default.totalClicks += totalClickCounter - 1;
                totalClickCounter = 0;
                s.Default.Save();
                totalText.Text = s.Default.totalClicks.ToString();
            }
            clicking = false;
        }

        private void CHT(int priority, string token, string text) // Change Hud Text
        {
            if (token == hudCurrentToken)
            {
                hudText.Text = text;
                hudCurrentPriority = priority;
                if (priority == 5) hudCurrentToken = string.Empty;
            }
            else if (priority <= hudCurrentPriority)      // Compare priorities: 5:Cleanup    4:Barely matters    3:Should see    2:Current situe    1:Important    0:Do not override
            {
                hudText.Text = text;
                hudCurrentPriority = priority;
                hudCurrentToken = token;
            }
        }

        private void WindowDrag(object sender, MouseButtonEventArgs? e)
        {
            DragMove();
        }

        private void HelpLink(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/NathanDagDane/Clickett/wiki/Getting-Started,-Help-and-FAQ") { UseShellExecute = true });
        }

        private void WarnLink(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/NathanDagDane/Clickett/wiki/Getting-Started,-Help-and-FAQ#only-69-clicks-per-second") { UseShellExecute = true });
        }

        private void AppMin(object sender, RoutedEventArgs? e)
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;
            ShowInTaskbar = !minToTray;
        }

        private void AppClose(object sender, RoutedEventArgs? e)
        {
            Close();
        }

        private void NewTrigger(object sender, RoutedEventArgs? e)
        {
            newTrigListen = true;
            trigText.Text = "Press a key combo";
            trigText.HorizontalAlignment = HorizontalAlignment.Center;
            trigText.Opacity = 0.6;
            trigBorder.Visibility = trigSet.Visibility = Visibility.Hidden;
            activateButt.IsEnabled = false;
        }

        private void ModeChange(object sender, SelectionChangedEventArgs? e)
        {
            modeInt = modeSel.SelectedIndex;
            if (modeInt == 0)
            {
                burstCountInput.Text = burstCount.ToString();
                burstGrid.Visibility = Visibility.Visible;
            }
            else
            {
                burstGrid.Visibility = Visibility.Hidden;
            }
        }

        private void CliclChange(object sender, SelectionChangedEventArgs? e)
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

        private void cpsChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            var cps = cpsSlid.Value;
            clickInterval = (int)Math.Round(1000 / cps);
            interText.Text = "Clicks Per Second - " + cps;
            if (cps > 55)
            {
                cpsWarn.Visibility = Visibility.Visible;
                var mar = cpsWarn.Margin;
                mar.Left = 30+(385*((cps-1)/68));
                cpsWarn.Margin = mar;
                cpsWarnBack.Opacity = 0.2 + (0.6 * ((cps - 55) / 14));
            }
            else
            {
                cpsWarn.Visibility = Visibility.Hidden;
            }
            
        }

        private void InterTypeSwap(object sender, RoutedEventArgs? e)
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
                if (clickInterval < 1) clickInterval = 1;
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

        private void LogoMouseEnter(object sender, MouseEventArgs? e)
        {
            if (countTotal) CHT(2, "ShowScore", "Total Clicks\n" + s.Default.totalClicks.ToString());
        }

        private void LogoMouseLeave(object sender, MouseEventArgs? e)
        {
            CHT(5, "ShowScore", "");
        }

        private void HotMouseEnter(object sender, MouseEventArgs? e)
        {
            CHT(2, "ShowHotkey", triggerDis.Text);
        }

        private void HotMouseLeave(object sender, MouseEventArgs? e)
        {
            CHT(5, "ShowHotkey", "");
        }
        private void WarnMouseEnter(object sender, MouseEventArgs? e)
        {
            CHT(2, "ShowWarning", "Some games may struggle to process this many clicks!");
        }

        private void WarnMouseLeave(object sender, MouseEventArgs? e)
        {
            CHT(5, "ShowWarning", "");
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void HandleInterTextChange(object sender, TextChangedEventArgs? e)
        {
            if (millisInput.Text != "") try { int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; }
            if (secondsInput.Text != "") try { int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; }
            if (minutesInput.Text != "") try { int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; }
            if (xPosInput.Text != "") try { int.Parse(xPosInput.Text); } catch { xPosInput.Text = "0"; xPos = 0; }
            if (yPosInput.Text != "") try { int.Parse(yPosInput.Text); } catch { yPosInput.Text = "0"; yPos = 0; }
            if (burstCountInput.Text != "") try { _ = int.Parse(burstCountInput.Text); } catch { burstCountInput.Text = "0"; burstCount = 0; }
        }

        private void HandleTextChange(object sender, RoutedEventArgs? e)
        {
            int millis = 0;
            int seconds = 0;
            int minutes = 0;
            try { millis = int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; millis = 0; }
            try { seconds = int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; seconds = 0; }
            try { minutes = int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; minutes = 0; }
            InterTextSet(millis, seconds, minutes);
        }

        private void HandleLocTextChange(object sender, RoutedEventArgs? e)
        {
            int x = 0;
            int y = 0;
            try { x = int.Parse(xPosInput.Text); } catch { xPosInput.Text = "0"; x = 0; }
            try { y = int.Parse(yPosInput.Text); } catch { yPosInput.Text = "0"; y = 0; }
            xPos = (uint)x;
            yPos = (uint)y;
        }

        private void ChangeBurstCount(object sender, RoutedEventArgs? e)
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

        private void ScaleSliderChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            uiScaleText.Text = "UI Scale - " + uiScaleSlid.Value + "%";
        }

        private void ScaleChange(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs? e)
        {
            uiScale = s.Default.uiScale = (int)uiScaleSlid.Value;
            s.Default.Save();
            if (doAnimations)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(Width, uiScale * 6, TimeSpan.FromMilliseconds(200), FillBehavior.HoldEnd);
                fadeOutAnimation.AccelerationRatio = 0.2;
                fadeOutAnimation.DecelerationRatio = 0.6;
                BeginAnimation(WidthProperty, fadeOutAnimation);
            }
            else
            {
                Width = uiScale * 6;
            }
            s.Default.Save();
        }

        private void NOpSliderChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            nOpText.Text = nOpSlid.Value.ToString() + "%";
        }

        private void NOpChange(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs? e)
        {
            nOpacity = s.Default.normalOpacity = (float)nOpSlid.Value / 100;
            if (doAnimations)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(fullCanvas.Opacity, nOpacity, TimeSpan.FromMilliseconds(200), FillBehavior.HoldEnd);
                fadeOutAnimation.AccelerationRatio = 0.2;
                fadeOutAnimation.DecelerationRatio = 0.6;
                fullCanvas.BeginAnimation(OpacityProperty, fadeOutAnimation);
            }
            else
            {
                fullCanvas.Opacity = nOpacity;
            }
            s.Default.Save();
        }

        private void COpSliderChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            cOpText.Text = cOpSlid.Value + "%";
        }

        private void COpChange(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs? e)
        {
            cOpacity = s.Default.clickingOpacity = (float)cOpSlid.Value / 100;
            s.Default.Save();
        }

        private void SettingsToggle(object sender, RoutedEventArgs? e)
        {

            settOpen = !settOpen;
            if (doAnimations)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                if (settOpen) SettView.BeginAnimation(OpacityProperty, fadeInAnimation);
                else confStack.BeginAnimation(OpacityProperty, fadeInAnimation);
            }
            SettView.Visibility = settOpen ? Visibility.Visible : Visibility.Hidden;
            confStack.Visibility = settOpen ? Visibility.Hidden : Visibility.Visible;

            if (doAnimations)
            {
                var settAnim = new DoubleAnimation(0, 90 * (settOpen ? 1 : -1), TimeSpan.FromMilliseconds(300), FillBehavior.HoldEnd);
                settAnim.DecelerationRatio = 1;
                settStoryboard = new Storyboard();
                settStoryboard.Children.Add(settAnim);
                Storyboard.SetTarget(settAnim, settIcon);
                Storyboard.SetTargetProperty(settAnim, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
                settStoryboard.Begin(this);
            }
        }

        private void fullGridFocus(object sender, MouseButtonEventArgs? e)
        {
            fullGrid.Focus();
        }

        private void SetLoc(object sender, RoutedEventArgs? e)
        {
            newLocListen = true;
            locGrid.Visibility = Visibility.Hidden;
            locText.Text = "Press Trigger Key";
            locText.HorizontalAlignment = HorizontalAlignment.Center;
            locText.Opacity = 0.7;
            RegisterHotkey();
        }

        private void TcReset(object sender, RoutedEventArgs? e)
        {
            if (!awaitReset)
            {
                CHT(1, "TcReset", "Sure? Click again to reset\n3");
                awaitReset = true;
                tcResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                var timerCount = 3;
                tcResetTimer.Tick += (sender, args) =>
                {
                    CHT(0, "TcReset", "Sure? Click again to reset\n" + (timerCount - 1));
                    timerCount--;
                    if (timerCount == 0) { tcResetTimer.Stop(); awaitReset = false; CHT(5, "TcReset", ""); }
                };
                tcResetTimer.Start();
            }
            else
            {
                tcResetTimer.Stop();
                s.Default.totalClicks = 0;
                s.Default.Save();
                totalText.Text = s.Default.totalClicks.ToString();
                CHT(5, "TcReset", "");
                awaitReset = false;
            }
        }

        private void MakeNotification(string title, string meat)
        {
            ToastNotificationManagerCompat.History.Clear();
            new ToastContentBuilder()
                .AddText(title)
                .AddText(meat)
                .Show();
        }

        private new void KeyDown(object sender, KeyEventArgs e)
        {
            if (newTrigListen && !new[] { Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.LWin, Key.RWin, Key.System }.Contains(e.Key))
            {
                hkShift = s.Default.hkShift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                hkCtrl = s.Default.hkCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                hkAlt = s.Default.hkAlt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
                try
                {
                    var mod = 0;
                    if (hkAlt) mod |= 1;
                    if (hkCtrl) mod |= 2;
                    if (hkShift) mod |= 4;
                    RegisterHotKey(_mainWindowHandle, _hotkeyID, mod, KeyInterop.VirtualKeyFromKey(e.Key));
                    UnregisterHotkey();
                    triggerDis.Text = (hkCtrl ? "Ctrl + " : "") + (hkShift ? "Shift + " : "") + (hkAlt ? "Alt + " : "") + KeyChar(e.Key);
                    hotkey = s.Default.hkAction = e.Key;
                }
                catch
                {
                    triggerDis.Text = "oops Fail";
                    hotkey = Key.None;
                }

                trigText.Text = "Trigger";
                trigText.HorizontalAlignment = HorizontalAlignment.Left;
                trigBorder.Visibility = trigSet.Visibility = Visibility.Hidden;
                trigText.Opacity = 1;
                trigBorder.Visibility = trigSet.Visibility = Visibility.Visible;

                activateButt.IsEnabled = true;

                newTrigListen = false;

                s.Default.hkAction = hotkey;
                s.Default.hkAlt = hkAlt;
                s.Default.hkCtrl = hkCtrl;
                s.Default.hkShift = hkShift;
                s.Default.Save();
            }
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || e.Key == Key.Tab) e.Handled = true;

        }

        static string KeyChar(Key key)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            ToUnicode((uint)KeyInterop.VirtualKeyFromKey(key), 0, keyboardState, buf, 256, 0);

            var charStr = buf.ToString();
            if (charStr == "") charStr = key.ToString();

            charStr = charStr[0].ToString().ToUpper() + charStr.Substring(1).ToLower();

            return charStr;
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


        public string AssemblyVersion
        {
            get
            {
                return "v" + versionNum + " - Beta";
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            _source.RemoveHook(Hooks);
            UnregisterHotkey();

            s.Default.hkAction = hotkey;
            s.Default.hkAlt = hkAlt;
            s.Default.hkCtrl = hkCtrl;
            s.Default.hkShift = hkShift;
            s.Default.doAnimations = doAnimations;
            s.Default.countTotal = countTotal;
            s.Default.aot = aot;
            s.Default.startup = startup;
            s.Default.trayIcon = trayIcon;
            s.Default.minToTray = minToTray;
            s.Default.modeInt = modeInt;
            s.Default.burstCount = burstCount;
            s.Default.clickInterval = clickInterval;
            s.Default.uiScale = uiScale;
            s.Default.Theme = curTheme;
            s.Default.Save();
            _tbi.Dispose();
        }
    }
}
