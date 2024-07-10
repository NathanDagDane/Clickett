using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;
using Forms = System.Windows.Forms;
using s = Clickett.Properties.Settings;

namespace Clickett
{
    public partial class MainWindow : Window
    {
        // Clicking Configuration
        public bool interType, doLocation, rocket, jitter, doubleClick;
        private int clickInterval, modeInt, burstCount, threads;
        private uint clickDo, clickUp, xPos, yPos;
        
        // Clickett / UI State
        public bool active, clicking, settOpen, exOp, inTuto;
        private int tutStep, hudCurrentPriority;
        private string hudCurrentToken, curTheme;

        // Event Listening
        private bool newTrigListen, newLocListen, awaitReset;

        // Settings
        private bool doAnimations, aot, startup, trayIcon, minToTray, hkShift, hkCtrl, hkAlt, countTotal;
        private int uiScale, clickCounter;
        private long totalClickCounter;
        private float nOpacity, cOpacity;
        private Key hotkey;

        // Misc ig
        private DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Send), tcResetTimer;
        private PeriodicTimer pTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));

        private Forms.NotifyIcon _tbi;

        private UpdateManager _um;
        private UpdateInfo _update;

        private IntPtr hwnd;
        private IntPtr _mainWindowHandle;
        private HwndSource _source;
        private readonly int _hotkeyID = 696938548;


        // STARTUP LOGIC
        public MainWindow()
        {
            InitializeComponent();
            InitializeThingies();
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Auto);
            this.DataContext = this;

            _um = new UpdateManager(new GithubSource("https://github.com/NathanDagDane/Clickett", null, false));

            UpdateBut.Visibility = Visibility.Hidden;
            CheckUpdate(false);
        }
        private void InitializeThingies()
        {
            try { hotkey = s.Default.hkAction; }
            catch { hotkey = s.Default.hkAction = Key.Z; }
            hkCtrl = s.Default.hkCtrl;
            hkShift = s.Default.hkShift;
            hkAlt = s.Default.hkAlt;
            triggerDis.Text = (hkCtrl ? "Ctrl + " : "") + (hkShift ? "Shift + " : "") + (hkAlt ? "Alt + " : "") + KeyChar(hotkey);
            curTheme = s.Default.Theme;
            doAnimations = !s.Default.doAnimations;
            ToggleAnim(this, null);
            doLocation = true;
            ToggleLoc(this, null);
            jitter = !s.Default.jitter;
            ToggleJit(this, null);
            doubleClick = !s.Default.doubleClick;
            ToggleDou(this, null);
            interType = true;
            millisInput.Text = "0";
            secondsInput.Text = "0";
            minutesInput.Text = "0";
            clickInterval = s.Default.clickInterval;
            InterTypeSwap(this, null);
            rocket = true;
            RocketSwap(this, null);
            burstCount = s.Default.burstCount;
            ModeChange(this, null);
            settOpen = true;
            SettingsToggle(this, null);
            countTotal = !s.Default.countTotal;
            ToggleCt(this, null);
            aot = !s.Default.aot;
            ToggleAot(this, null);
            startup = !s.Default.startup;
            ToggleStartup(this, null);
            trayIcon = !s.Default.trayIcon;
            ToggleTray(this, null);
            minToTray = !s.Default.minToTray;
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
            if (s.Default.welcomed)
            {
                fullGrid.Children.Remove(welcomeGrid);
            }
            else
            {
                inTuto = true;
                helpBut.Visibility = Visibility.Collapsed;
                FocusItem(0);
                welcomeGrid.Visibility = Visibility.Visible;
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _mainWindowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_mainWindowHandle);
            _source.AddHook(Hooks);
        }


        // CLICKING
        private void Activate(object sender, RoutedEventArgs? e)
        {
            if (inTuto && tutStep == 14) TutoNext(this, null);

            if (active)
            {
                UnregisterHotkey();
                activateButtText.Text = "Activate";
                activateButt.SetResourceReference(BackgroundProperty, "AcGrad");
                active = false;
                trigSet.IsEnabled = true;
                locSetButt.IsEnabled = doLocation;
                if (trayIcon)
                {
                    try
                    {
                        _tbi.Icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcircbw.ico");
                        _tbi.ContextMenuStrip.Items[0].Text = "Activate";
                    }
                    catch { }
                }
            }
            else
            {
                if (hotkey == Key.None) { MakeNotification("Slow down there!", "Set a hotkey first\nPretty please"); triggerDis.Text = "Choose a hotkey"; return; }
                try { RegisterHotkey(); }
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
                if (trayIcon)
                {
                    try
                    {
                        _tbi.Icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcirc.ico");
                        _tbi.ContextMenuStrip.Items[0].Text = "Deactivate";
                    }
                    catch { }
                }
            }
        }
        private void AutoClick() // Called when hotkey pressed
        {
            if (modeInt == 2)            //Hold
            {
                if (clicking) return;

                dispatcherTimer.Tick += new EventHandler(HoldCheck);
                EnterClickState();
            }
            else if (modeInt == 1)       //Toggle
            {
                if (clicking) ExitClickState();
                else EnterClickState();
            }
            else if (modeInt == 0)       //Burst
            {
                if (clicking)
                {
                    ExitClickState();
                    clickCounter = 0;
                }
                else
                {
                    clickCounter = 1;
                    EnterClickState();
                }
            }
        }
        private void EnterClickState()
        {
            clicking = true;
            Focusable = false;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(clickInterval * 0.8);
            hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
            fullCanvas.Opacity = cOpacity;

            fullGrid.RenderTransform = new ScaleTransform(0.94,0.94);
            var blur = new BlurEffect();
            blur.Radius = 10;
            fullGrid.Effect = blur;

            if(doLocation) SetCursorPos((int)xPos, (int)yPos);
            mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
            totalClickCounter++;

            Thread.Sleep(1);

            dispatcherTimer.Start();
            if (!rocket)
            {
                _ = Clicking(clickInterval);
            }
            else
            {
                for (int i = 0; i < threads; i++)
                {
                    Thread clickHandler = new Thread(Clicker);
                    clickHandler.Start();
                    Thread.Sleep(1);
                }
            }
        }
        private void ExitClickState()
        {
            dispatcherTimer.Stop();
            clicking = false;
            pTimer.Dispose();
            Focusable = true;
            SetWindowExDefault(hwnd);
            fullCanvas.Opacity = nOpacity;

            fullGrid.RenderTransform = new ScaleTransform(1, 1);
            fullGrid.Effect = null;
            if (countTotal)
            {
                s.Default.totalClicks += totalClickCounter - 1;
                s.Default.Save();
                totalText.Text = s.Default.totalClicks.ToString();
                totalClickCounter = 0;
            }
        }
        private async Task Clicking(int clickInterval) // Performs actual clicking
        {
            var cap = (modeInt == 0); // Whether limited to number of clicks before stopping
            pTimer = new(TimeSpan.FromMilliseconds(clickInterval));

            if (cap) IterateClick();
            if (doubleClick)
            {
                mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
                totalClickCounter++;
            }

            while (await pTimer.WaitForNextTickAsync()) // Clicking loop
            {
                if (jitter)
                {
                    Thread.Sleep((int)Math.Floor((float)clickInterval * (float)(new Random().Next(0, 6)) / 10f));
                }
                if (cap) IterateClick();
                mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
                totalClickCounter++;
                if (doubleClick)
                {
                    mouse_event(clickDo | clickUp, xPos, yPos, 0, 0);
                    totalClickCounter++;
                }
            }
        }
        void Clicker() // Performs Rocket Mode clicking (Thread instances)
        {
            var count = 0;
            uint CclickDo = 0;
            uint CclickUp = 0;
            var CdoLoc = false;
            var CdoCount = false;
            uint CxPos = 0;
            uint CyPos = 0;
            var CburstCount = 0;
            var doub = false;

            Dispatcher.Invoke((Action)(() =>
            {
                CclickDo = clickDo;
                CclickUp = clickUp;
                CdoLoc = doLocation;
                CdoCount = modeInt == 0;
                CxPos = xPos;
                CyPos = yPos;
                CburstCount = (int)Math.Ceiling((float)burstCount / (float)threads);
                doub = doubleClick;
            }));

            while (true)
            {
                bool doClick = true;
                Dispatcher.Invoke((Action)(() =>
                {
                    doClick = clicking;
                }));

                if (doClick)
                {
                    if (CdoCount)
                    {
                        if (count >= CburstCount)
                        {
                            Dispatcher.Invoke((Action)(() =>
                            {
                                ExitClickState();
                            }));
                            break;
                        }
                        count += 1;
                    }
                    mouse_event(CclickDo | CclickUp, CxPos, CyPos, 0, 0);
                    if (doub) mouse_event(CclickDo | CclickUp, CxPos, CyPos, 0, 0);

                    Thread.Sleep(1);
                }
                else break;
            }
        }
        // ADDITIONAL HOOKS
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
        private void IterateClick()
        {
            if (clickCounter >= burstCount)
            {
                ExitClickState();
                clickCounter = 0;
                return;
            }
            clickCounter++;
        }
        private void CountTotal(object sender, EventArgs? e)
        {
            totalClickCounter++;
        }


        // HOTKEY
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
                    trigSet.IsEnabled = true;
                    locText.Margin = new Thickness(8, 0, 0, 0);
                    locSetButt.Margin = new Thickness(0, 3, 153, 3);
                    locSetButt.Width = 40;
                    locSetButtText.Text = "Set";

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


        // BUTTON EVENTS
        private void Help(object sender, RoutedEventArgs? e)
        {
            FocusItem(0);
            helpMenu.Visibility = Visibility.Visible;
        }
        private void StartTuto(object sender, RoutedEventArgs? e)
        {

            helpMenu.Visibility = Visibility.Collapsed;
            Tutorial();
        }
        private void AcceptTuto(object sender, RoutedEventArgs? e)
        {
            s.Default.welcomed = true;
            helpBut.Visibility = Visibility.Visible;
            fullGrid.Children.Remove(welcomeGrid);
            StartTuto(this, null);
        }
        private void DenyTuto(object sender, RoutedEventArgs? e)
        {
            s.Default.welcomed = true;
            helpMenu.Visibility = Visibility.Collapsed;
            fullGrid.Children.Remove(welcomeGrid);
            tutOverlay.Visibility = Visibility.Visible;
            TutoExit(this, null);
        }
        private void HelpLink(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/NathanDagDane/Clickett/wiki/Getting-Started,-Help-and-FAQ") { UseShellExecute = true });
        }
        private void OpenGithubLink(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/NathanDagDane") { UseShellExecute = true });
        }
        private void HelpContact(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("mailto:nathandane.dev@gmail.com?subject=Clickett%20Support") { UseShellExecute = true });
        }
        private void HelpExit(object sender, RoutedEventArgs? e)
        {
            FocusItem(69);
            helpMenu.Visibility = Visibility.Collapsed;
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
        private void OnExit(object sender, EventArgs e)
        {
            clicking = false;
            _source.RemoveHook(Hooks);
            UnregisterHotkey();

            s.Default.hkAction = hotkey;
            s.Default.hkAlt = hkAlt;
            s.Default.hkCtrl = hkCtrl;
            s.Default.hkShift = hkShift;
            s.Default.doubleClick = doubleClick;
            s.Default.jitter = jitter;
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
        private void NewTrigger(object sender, RoutedEventArgs? e)
        {
            if (newTrigListen)
            {
                newTrigListen = false;
                trigBorder.Visibility = Visibility.Visible;
                trigText.Text = "Trigger";
                trigText.HorizontalAlignment = HorizontalAlignment.Left;
                trigText.Margin = new Thickness(8, 0, 0, 0);
                trigText.Opacity = 1;
                locSetButt.IsEnabled = true;
                trigSet.Width = 40;
                trigSetText.Text = "Set";
                activateButt.IsEnabled = true;
            }
            else
            {
                newTrigListen = true;
                trigBorder.Visibility = Visibility.Hidden;
                trigText.Text = "Press a key combo";
                trigText.HorizontalAlignment = HorizontalAlignment.Center;
                trigText.Opacity = 0.7;
                trigText.Margin = new Thickness(8, 0, 78, 0);
                locSetButt.IsEnabled = false;
                trigSet.Width = 69;
                trigSetText.Text = "Cancel";
                activateButt.IsEnabled = false;
            }
        }
        private void SettingsToggle(object sender, RoutedEventArgs? e)
        {
            settOpen = !settOpen;
            if (doAnimations)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                if (settOpen) SettView.BeginAnimation(OpacityProperty, fadeInAnimation);
                else homeStack.BeginAnimation(OpacityProperty, fadeInAnimation);
            }
            SettView.Visibility = settOpen ? Visibility.Visible : Visibility.Collapsed;
            homeStack.Visibility = settOpen ? Visibility.Hidden : Visibility.Visible;

            if (doAnimations)
            {
                var settAnim = new DoubleAnimation(0, 90 * (settOpen ? 1 : -1), TimeSpan.FromMilliseconds(300), FillBehavior.HoldEnd);
                settAnim.DecelerationRatio = 1;
                var settStoryboard = new Storyboard();
                settStoryboard.Children.Add(settAnim);
                Storyboard.SetTarget(settAnim, settIcon);
                Storyboard.SetTargetProperty(settAnim, new PropertyPath("RenderTransform.Children[0].Angle"));
                settStoryboard.Begin(this);
            }
        }
        private void SupportLink(object sender, RoutedEventArgs? e)
        {
            Process.Start(new ProcessStartInfo("https://nathandagdane.github.io/Clickett/Donate/") { UseShellExecute = true });
        }
        private void ToggleExOp(object sender, RoutedEventArgs? e)
        {
            exOp = !exOp;

            if (doAnimations)
            {
                DoubleAnimation sizeAnimation = new DoubleAnimation(exOp ? 0.0 : 30.0, exOp ? 30.0 : 0.0, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                sizeAnimation.DecelerationRatio = 1;
                if (!exOp) { sizeAnimation.Completed += CollapseExOp; }
                else { CollapseExOp(this, null); }
                exOptions.BeginAnimation(HeightProperty, sizeAnimation);
            }
            else { exOptions.Height = 30.0; CollapseExOp(this, null); }

            if (doAnimations)
            {
                var exAnim = new DoubleAnimation((exOp ? 0 : 180), (exOp ? 180 : 0), TimeSpan.FromMilliseconds(200), FillBehavior.HoldEnd);
                exAnim.DecelerationRatio = 1;
                var settStoryboard = new Storyboard();
                settStoryboard.Children.Add(exAnim);
                Storyboard.SetTarget(exAnim, OptionsArrow);
                Storyboard.SetTargetProperty(exAnim, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
                settStoryboard.Begin(this);
            }
            else OptionsArrow.RenderTransform = new RotateTransform(exOp ? 180.0 : 0.0, 0.5, 0.5);

            s.Default.openedExOp = true;
            s.Default.Save();

            if (inTuto && tutStep == 9)
            {
                TutoNext(this, null);
            }
        }
        private void CollapseExOp(object? sender, EventArgs? e)
        {
            exOptions.Visibility = exOp ? Visibility.Visible : Visibility.Collapsed;
            SettView.Height = exOp ? 183 : 153;
            fullCanvas.Height = exOp ? 262.1 : 232.1;
        }


        // TUTORIAL LOGIC
        private void Tutorial()
        {
            if (active) Activate(this, null);
            if (settOpen) SettingsToggle(this, null);
            if (exOp) ToggleExOp(this, null);
            if (rocket) RocketSwap(this, null);
            if (interType) InterTypeSwap(this, null);
            helpBut.IsEnabled = false;
            if (newLocListen)
            {
                UnregisterHotkey();
                newLocListen = false;
                locGrid.Visibility = Visibility.Visible;
                locText.Text = "Location";
                locText.HorizontalAlignment = HorizontalAlignment.Left;
                locText.Margin = new Thickness(8, 0, 0, 0);
                locText.Opacity = 1;
                trigSet.IsEnabled = true;
                locSetButt.Margin = new Thickness(0, 3, 153, 3);
                locSetButt.Width = 40;
                locSetButtText.Text = "Set";
            }
            if (newTrigListen)
            {
                newTrigListen = false;
                trigBorder.Visibility = Visibility.Visible;
                trigText.Text = "Trigger";
                trigText.HorizontalAlignment = HorizontalAlignment.Left;
                trigText.Margin = new Thickness(8, 0, 0, 0);
                trigText.Opacity = 1;
                locSetButt.IsEnabled = true;
                trigSet.Width = 40;
                trigSetText.Text = "Set";
                activateButt.IsEnabled = true;
            }
            tutNextBut.Visibility = Visibility.Visible;
            tutExitBut.Visibility = Visibility.Visible;
            tutArrow.Visibility = Visibility.Visible;
            tutTextOther.Visibility = Visibility.Collapsed;
            helpBut.Visibility = Visibility.Collapsed;
            tutHelpPage.Visibility = Visibility.Collapsed;
            tutContact.Visibility = Visibility.Collapsed;
            FocusItem(1);
            TutoArrange(340, 125, 100, 64, 300, false, -98, 44, 48, "You can use this slider to choose how fast you want to click");
            tutNextButText.Text = "Next";
            tutOverlay.Visibility = Visibility.Visible;
            tutStep = 0;
            inTuto = true;
        }
        private void TutoExit(object sender, RoutedEventArgs? e)
        {
            if (!inTuto || tutStep == 17)
            {
                tutStep = 0;
                FocusItem(69);
                tutExitBut.Visibility = Visibility.Collapsed;
                helpBut.Visibility = Visibility.Visible;
                helpBut.IsEnabled = true;
                tutOverlay.Visibility = Visibility.Collapsed;
                tutHelpPage.Visibility = Visibility.Collapsed;
                tutContact.Visibility = Visibility.Collapsed;
                tutArrow.Visibility = Visibility.Collapsed;
                tutTextOther.Visibility = Visibility.Collapsed;
            }
            else
            {
                tutStep = 16;
                TutoNext(this, null);
            }
            inTuto = false;
        }
        private void TutoNext(object sender, RoutedEventArgs? e)
        {
            switch (tutStep)
            {
                case 0:
                    FocusItem(3);
                    TutoArrange(340, 125, 30, 64, 400, true, 98, 430, 48, "Click this to choose between the clicks per second slider and typing the time between clicks");
                    break;
                case 1:
                    FocusItem(2);
                    TutoArrange(340, 125, 20, 72, 460, true, 98, 400, 48, "Click this for Rocket mode!\nThis lets you choose how many CPU threads are clicking");
                    break;
                case 2:
                    TutoArrange(340, 125, 10, 72, 400, true, 98, 400, 48, "This can click up to 20,000 times per second!\nBe careful!");
                    break;
                case 3:
                    FocusItem(6);
                    TutoArrange(361, 113, 25, 20, 250, false, 67, 274, 0, "Choose between the 3 different clicking modes with this\n\nBurst - Click a set amount of times every time you press the trigger");
                    modeSel.SelectedIndex = 0;
                    break;
                case 4:
                    TutoArrange(361, 113, 25, 20, 250, false, 67, 274, 0, "Choose between the 3 different clicking modes with this\n\nToggle - Switch between clicking and not clicking when you press the trigger");
                    modeSel.SelectedIndex = 1;
                    break;
                case 5:
                    TutoArrange(361, 113, 25, 20, 250, false, 67, 274, 0, "Choose between the 3 different clicking modes with this\n\nHold - Start clicking when you push the trigger down and stops when you let go");
                    modeSel.SelectedIndex = 2;
                    break;
                case 6:
                    FocusItem(11);
                    TutoArrange(361, 40, 36, 44, 300, true, 180, 220, 64, "Choose what type of click will happen");
                    break;
                case 7:
                    FocusItem(5);
                    TutoArrange(361, 68, 50, 10, 400, true, 100, 222, 95, "Lock the cursor to a specific place on the screen\nClick 'Set' to choose where");
                    tutTextOther.Visibility = Visibility.Visible;
                    break;
                case 8:
                    FocusItem(4);
                    TutoArrange(361, 82, 80, 106, 280, false, -84, 22, 72, "See extra options by clicking here");
                    tutNextBut.Visibility = Visibility.Collapsed;
                    tutTextOther.Visibility = Visibility.Collapsed;
                    break;
                case 9:
                    FocusItem(8);
                    TutoArrange(108, 149, 16, 24, 260, false, 85, 278, 24, "Add a random wait between each click\n\nThis makes it harder for games to detect the autockicler");
                    tutNextBut.Visibility = Visibility.Visible;
                    break;
                case 10:
                    FocusItem(9);
                    TutoArrange(150, 125, 27, 60, 320, true, 165, 295, 74, "This makes each click a double click\nDouble the speed!");
                    break;
                case 11:
                    //FocusItem(7);
                    tutStep++;
                    TutoNext(this, null);
                    return;
                case 12:
                    FocusItem(10);
                    TutoArrange(361, 90, 35, 20, 360, true, -90, 45, 55, "This shows you the keyboard shortcut you need to press to start clicking\n\nClick 'Set' to choose your own!");
                    break;
                case 13:
                    if (active) Activate();
                    FocusItem(13);
                    TutoArrange(375, 69, 27, 60, 320, false, 100, 250, 82, "Clickett is disabled by default\nClick this to enable it");
                    tutNextBut.Visibility = Visibility.Collapsed;
                    break;
                case 14:
                    TutoArrange(375, 69, 27, 40, 320, false, 100, 250, 82, "Clickett is now activated!\nIt will start clicking when you press the shortcut\n\nClickett cannot click when it isn't activated");
                    tutNextBut.Visibility = Visibility.Visible;
                    tutArrow.Visibility = Visibility.Collapsed;
                    break;
                case 15:
                    FocusItem(12);
                    TutoArrange(213, 145, 50, 40, 400, false, -180, 56, 120, "See settings by clicking here\n\nHere you can choose a theme and customise the experience!");
                    tutArrow.Visibility = Visibility.Visible;
                    break;
                case 16:
                    FocusItem(0);
                    tutExitBut.Visibility = Visibility.Collapsed;
                    tutTextOther.Visibility = Visibility.Collapsed;
                    tutHelpPage.Visibility = Visibility.Collapsed;
                    tutContact.Visibility = Visibility.Collapsed;
                    helpBut.Visibility = Visibility.Visible;
                    helpBut.IsEnabled = false;
                    tutNextButText.Text = "Okay!";
                    TutoArrange(213, 84, 100, 28, 300, true, 70, 390, -28, "You can always see the tutorial or get other help by clicking here!");
                    break;
                case 17:
                    if (!inTuto)
                    {
                        TutoExit(this, null);
                        return;
                    }
                    FocusItem(0);
                    TutoArrange(213, 145, 50, 28, 400, false, 0, 0, 0, "Thanks for taking the tour!\n\nIf you still need help:");
                    tutArrow.Visibility = Visibility.Collapsed;
                    tutHelpPage.Visibility = Visibility.Visible;
                    tutContact.Visibility = Visibility.Visible;
                    tutNextButText.Text = "Finish";
                    break;
                case 18:
                    inTuto = false;
                    TutoExit(this, null);
                    break;
            }
            tutStep++;
        }
        private void TutoArrange(float nextX, float nextY, float textX, float textY, float textWidth, bool arrowFlip, float arrowRot, float arrowX, float arrowY, string text)
        {
            tutNextBut.Margin = new Thickness(nextX, nextY, 0, 0);
            tutText.Margin = new Thickness(textX, textY, 0, 0);
            tutText.Width = textWidth;
            tutText.Text = text;
            TransformGroup bruh = new TransformGroup();
            bruh.Children.Add(new ScaleTransform((arrowFlip ? -1 : 1), 1));
            bruh.Children.Add(new RotateTransform(arrowRot));
            tutArrow.RenderTransform = bruh;
            tutArrow.Margin = new Thickness(arrowX, arrowY, 0, 0);
        }
        private void FocusItem(int i)
        {
            var blur = new BlurEffect();
            blur.Radius = 10;
            interBor.Effect = blur;

            interText.SetResourceReference(OpacityProperty, "OverlayOpacity");
            interText.Effect = blur;
            interBor.SetResourceReference(OpacityProperty, "OverlayOpacity");
            interBor.Effect = blur;
            rocketSwapGrid.SetResourceReference(OpacityProperty, "OverlayOpacity");
            rocketSwap.Effect = blur;
            rocketSwap.IsEnabled = false;
            swapButtGrid.SetResourceReference(OpacityProperty, "OverlayOpacity");
            swapButt.Effect = blur;
            swapButt.IsEnabled = false;
            optionsTitGrid.SetResourceReference(OpacityProperty, "OverlayOpacity");
            optionsTitGrid.Effect = blur;
            optionsTit.IsEnabled = false;
            locBorder.SetResourceReference(OpacityProperty, "OverlayOpacity");
            locBorder.Effect = blur;
            LocBut.IsEnabled = false;
            locSetButt.IsEnabled = false;
            modeBor.SetResourceReference(OpacityProperty, "OverlayOpacity");
            modeBor.Effect = blur;
            modeSel.IsEnabled = false;
            ProfBorder.SetResourceReference(OpacityProperty, "OverlayOpacity");
            ProfBorder.Effect = blur;
            jitBorder.SetResourceReference(OpacityProperty, "OverlayOpacity");
            jitBorder.Effect = blur;
            jitBut.IsEnabled = false;
            douBorder.SetResourceReference(OpacityProperty, "OverlayOpacity");
            douBorder.Effect = blur;
            douBut.IsEnabled = false;
            trigBor.SetResourceReference(OpacityProperty, "OverlayOpacity");
            trigBor.Effect = blur;
            trigSet.IsEnabled = false;
            clickBor.SetResourceReference(OpacityProperty, "OverlayOpacity");
            clickBor.Effect = blur;
            clickSel.IsEnabled = false;
            settButt.SetResourceReference(OpacityProperty, "OverlayOpacity");
            settButt.Effect = blur;
            settButt.IsEnabled = false;
            activateButt.SetResourceReference(OpacityProperty, "OverlayOpacity");
            activateButt.Effect = blur;
            activateButt.IsEnabled = false;
            settGrid.SetResourceReference(OpacityProperty, "OverlayOpacity");
            settGrid.Effect = blur;

            switch (i)
            {
                case 0:
                    break;
                case 1:
                    interText.Opacity = 1;
                    interText.Effect = null;
                    interBor.Opacity = 1;
                    interBor.Effect = null;
                    break;
                case 2:
                    rocketSwapGrid.Opacity = 1;
                    rocketSwap.Effect = null;
                    rocketSwap.IsEnabled = true;
                    interText.Opacity = 1;
                    interText.Effect = null;
                    interBor.Opacity = 1;
                    interBor.Effect = null;
                    break;
                case 3:
                    swapButtGrid.Opacity = 1;
                    swapButt.Effect = null;
                    swapButt.IsEnabled = true;
                    interText.Opacity = 1;
                    interText.Effect = null;
                    interBor.Opacity = 1;
                    interBor.Effect = null;
                    break;
                case 4:
                    optionsTitGrid.Opacity = 1;
                    optionsTitGrid.Effect = null;
                    optionsTit.IsEnabled = true;
                    break;
                case 5:
                    LocBut.IsEnabled = true;
                    locBorder.Effect = null;
                    locSetButt.IsEnabled = true;
                    ToggleLoc(this, null);
                    ToggleLoc(this, null);
                    break;
                case 6:
                    modeBor.Opacity = 1;
                    modeBor.Effect = null;
                    modeSel.IsEnabled = true;
                    break;
                case 7:
                    ProfBorder.Opacity = 1;
                    ProfBorder.Effect = null;
                    break;
                case 8:
                    jitBut.IsEnabled = true;
                    jitBorder.Effect = null;
                    ToggleJit(this, null);
                    ToggleJit(this, null);
                    break;
                case 9:
                    douBut.IsEnabled = true;
                    douBorder.Effect = null;
                    ToggleDou(this, null);
                    ToggleDou(this, null);
                    break;
                case 10:
                    trigBor.Opacity = 1;
                    trigBor.Effect = null;
                    trigSet.IsEnabled = true;
                    break;
                case 11:
                    clickBor.Opacity = 1;
                    clickBor.Effect = null;
                    clickSel.IsEnabled = true;
                    break;
                case 12:
                    settButt.Opacity = 1;
                    settButt.Effect = null;
                    settButt.IsEnabled = true;
                    break;
                case 13:
                    activateButt.Opacity = 1;
                    activateButt.Effect = null;
                    activateButt.IsEnabled = true;
                    break;
                default:
                    interText.Opacity = 1;
                    interText.Effect = null;
                    interBor.Opacity = 1;
                    interBor.Effect = null;
                    rocketSwapGrid.Opacity = 1;
                    rocketSwap.Effect = null;
                    rocketSwap.IsEnabled = true;
                    swapButtGrid.Opacity = 1;
                    swapButt.Effect = null;
                    swapButt.IsEnabled = true;
                    optionsTitGrid.Opacity = 1;
                    optionsTitGrid.Effect = null;
                    optionsTit.IsEnabled = true;
                    ToggleLoc(this, null);
                    ToggleLoc(this, null);
                    locBorder.Effect = null;
                    LocBut.IsEnabled = true;
                    modeBor.Opacity = 1;
                    modeBor.Effect = null;
                    modeSel.IsEnabled = true;
                    ProfBorder.Opacity = 1;
                    ProfBorder.Effect = null;
                    jitBorder.Effect = null;
                    ToggleJit(this, null);
                    ToggleJit(this, null);
                    jitBut.IsEnabled = true;
                    douBorder.Effect = null;
                    ToggleDou(this, null);
                    ToggleDou(this, null);
                    douBut.IsEnabled = true;
                    trigBor.Opacity = 1;
                    trigBor.Effect = null;
                    trigSet.IsEnabled = true;
                    clickBor.Opacity = 1;
                    clickBor.Effect = null;
                    clickSel.IsEnabled = true;
                    settButt.Opacity = 1;
                    settButt.Effect = null;
                    settButt.IsEnabled = true;
                    activateButt.Opacity = 1;
                    activateButt.Effect = null;
                    activateButt.IsEnabled = true;
                    settGrid.Opacity = 1;
                    settGrid.Effect = null;
                    break;
            }
        }

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, -20); //-20: Extended Style (int)
            SetWindowLong(hwnd, -20, extendedStyle | 0x00000020); //0x00000020: Transparent (int)
        }
        public static void SetWindowExDefault(IntPtr hwnd)
        {
            SetWindowLong(hwnd, (-20), 0x00000000); //I don't need to explain what 0 is
        }


        // UPDATING LOGIC
        private async void CheckUpdate(bool manual)
        {
            UpdateBut.Visibility = Visibility.Collapsed;
            try
            {
                // ConfigureAwait(true) so that UpdateStatus() is called on the UI thread
                _update = await _um.CheckForUpdatesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (manual) MakeNotification("Update Error", "Failed to check for updates");
                return;
            }

            if (_update == null)
            {
                if (manual) MakeNotification("Up to Date!", "You're on the latest version!");
                return;
            }

            MakeNotification("Update Available", null);

            try
            {
                await _um.DownloadUpdatesAsync(_update, UpdateDownloadProgress).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    await _um.DownloadUpdatesAsync(_update, UpdateDownloadProgress, ignoreDeltas: true).ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    if (manual) MakeNotification("Download Error", "Failed to download update");
                }
            }
        }
        private void UpdateDownloadProgress(int percent)
        {
            // progress can be sent from other threads
            this.Dispatcher.InvokeAsync(() =>
            {

                //CHT(2, "UpdatePercentage", percent.ToString());
                if (percent == 100)
                {
                    UpdateBut.Visibility = Visibility.Visible;
                    //CHT(2, "UpdatePercentage", "");
                }
            });

        }
        private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            clicking = false;
            _source.RemoveHook(Hooks);
            UnregisterHotkey();

            _um.ApplyUpdatesAndRestart(_update);
        }


        // CLICKING CONFIG CHANGE
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
        private void ToggleJit(object sender, RoutedEventArgs? e)
        {
            jitter = s.Default.jitter = !jitter;
            ColourToggle(jitBut, jitter);
            jitBorder.Opacity = jitter ? 1 : 0.4;
            s.Default.Save();
        }
        private void ToggleDou(object sender, RoutedEventArgs? e)
        {
            doubleClick = s.Default.doubleClick = !doubleClick;
            ColourToggle(douBut, doubleClick);
            douBorder.Opacity = doubleClick ? 1 : 0.4;
            s.Default.Save();
        }
        private void SetLoc(object sender, RoutedEventArgs? e)
        {
            if (newLocListen)
            {
                UnregisterHotkey();
                newLocListen = false;
                locGrid.Visibility = Visibility.Visible;
                locText.Text = "Location";
                locText.HorizontalAlignment = HorizontalAlignment.Left;
                locText.Margin = new Thickness(8, 0, 0, 0);
                locText.Opacity = 1;
                trigSet.IsEnabled = true;
                locSetButt.Margin = new Thickness(0, 3, 153, 3);
                locSetButt.Width = 40;
                locSetButtText.Text = "Set";
            }
            else
            {
                newLocListen = true;
                locGrid.Visibility = Visibility.Hidden;
                locText.Text = "Press Trigger Key";
                locText.HorizontalAlignment = HorizontalAlignment.Center;
                locText.Opacity = 0.7;
                locText.Margin = new Thickness(8, 0, 78, 0);
                trigSet.IsEnabled = false;
                RegisterHotkey();
                locSetButt.Margin = new Thickness(0, 3, 3, 3);
                locSetButt.Width = 69;
                locSetButtText.Text = "Cancel";
            }

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
        private void ModeChange(object sender, SelectionChangedEventArgs? e)
        {
            modeInt = modeSel.SelectedIndex;
            if (modeInt == 0)
            {
                burstCountInput.Text = burstCount.ToString();
                burstGrid.Visibility = Visibility.Visible;
            }
            else burstGrid.Visibility = Visibility.Hidden;
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
            if (cps > 50)
            {
                cpsWarn.Visibility = Visibility.Visible;
                var mar = cpsWarn.Margin;
                mar.Left = 30 + (355 * ((cps - 1) / 68));
                cpsWarn.Margin = mar;
                cpsWarnBack.Opacity = 0.2 + (0.6 * ((cps - 50) / 19));
            }
            else cpsWarn.Visibility = Visibility.Hidden;
        }
        private void ThreadsChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            threads = (int)threadsSlid.Value;
            var speed = "";
            if (threads < 5) speed = "Fast";
            else if (threads < 10) speed = "Very fast";
            else if (threads < 15) speed = "Stupidly fast";
            else if (threads < 20) speed = "Insanely fast";
            else speed = "Broken";
            interText.Text = "Threads - " + threads + "  " + speed;

            if (threads > 9)
            {
                threadsWarn.Visibility = Visibility.Visible;
                var mar = threadsWarn.Margin;
                mar.Left = 30 + (355 * (((float)threads - 1) / 19));
                threadsWarn.Margin = mar;
                threadsWarnBack.Opacity = 0.2 + (0.6 * (((float)threads - 10) / 10));

                if (threads > 14) threadsWarnBack.SetResourceReference(BackgroundProperty, "AcCol2");
                else threadsWarnBack.SetResourceReference(BackgroundProperty, "FgCol1");
            }
            else threadsWarn.Visibility = Visibility.Hidden;
        }
        private void RocketSwap(object sender, RoutedEventArgs? e)
        {
            rocket = !rocket;

            if (rocket)
            {
                rocketIconDis.ImageSource = (ImageSource)FindResource("Rocket2Icon");
                rocketBorder.SetResourceReference(BackgroundProperty, "AcCol2");
                swapBorder.SetResourceReference(BackgroundProperty, "FgCol2");
                swapButt.IsEnabled = false;
                threadsGrid.Visibility = Visibility.Visible;
                cpsGrid.Visibility = Visibility.Hidden;
                interGrid.Visibility = Visibility.Hidden;
                ThreadsChange(this, null);
            }
            else
            {
                rocketIconDis.ImageSource = (ImageSource)FindResource("Rocket1Icon");
                rocketBorder.SetResourceReference(BackgroundProperty, "FgCol2");
                swapBorder.SetResourceReference(BackgroundProperty, "AcCol2");
                swapButt.IsEnabled = true;
                threadsGrid.Visibility = Visibility.Hidden;
                interType = !interType;
                InterTypeSwap(this, null);
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
        private void HandleInterTextChange(object sender, TextChangedEventArgs? e)
        {
            if (millisInput.Text != "") try { int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; }
            if (secondsInput.Text != "") try { int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; }
            if (minutesInput.Text != "") try { int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; }
            if (xPosInput.Text != "") try { int.Parse(xPosInput.Text); } catch { xPosInput.Text = "0"; xPos = 0; }
            if (yPosInput.Text != "") try { int.Parse(yPosInput.Text); } catch { yPosInput.Text = "0"; yPos = 0; }
            if (burstCountInput.Text != "") try { _ = int.Parse(burstCountInput.Text); } catch { burstCountInput.Text = "0"; burstCount = 0; }
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


        // SETTINGS CHANGE
        private void ToggleAnim(object sender, RoutedEventArgs? e)
        {
            doAnimations = s.Default.doAnimations = !doAnimations;
            ColourToggle(animButt, doAnimations);
            animBorder.Opacity = doAnimations ? 1 : 0.4;

            UpdateMergedDictionaries();

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

            if (startup) key.SetValue("Clickett", System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Clickett.exe", RegistryValueKind.ExpandString);
            else key.DeleteValue("Clickett", false);

            s.Default.Save();
        }
        private void ToggleTray(object sender, RoutedEventArgs? e)
        {
            if (!trayIcon)
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
                    if (active) { _tbi.ContextMenuStrip.Items.Add("Deactivate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcirc.ico"); }
                    else { _tbi.ContextMenuStrip.Items.Add("Activate", null, (s, e) => { Activate(this, null); }); _tbi.Icon = new System.Drawing.Icon(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\res\iconcircbw.ico"); }
                    _tbi.ContextMenuStrip.Items.Add("Open", null, (s, e) => { WindowState = WindowState.Normal; Activate(); });
                    var temp = _tbi.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { Close(); });
                    _tbi.DoubleClick += (s, e) => { Activate(this, null); };
                    _tbi.Visible = true;
                }
                catch
                {
                    MakeNotification("Tray icon died", "There was a problem setting up the tray icon. The tray icon option has been disabled.");
                    ToggleTray(this, null);
                }
            }
            else
            {
                try
                {
                    _tbi.Dispose();
                    if (minToTray) ToggleMinTray(this, null);
                }
                catch {
                    MakeNotification("Error", "Failed to remove tray icon");
                    return;
                }
            }
            trayIcon = s.Default.trayIcon = !trayIcon;
            ColourToggle(trayButt, trayIcon);
            trayBorder.Opacity = trayIcon ? 1 : 0.4;
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
            b.SetResourceReference(BackgroundProperty, colour ? "AcGrad" : "InacBut");
        }
        private void ScaleSliderChange(object sender, RoutedPropertyChangedEventArgs<double>? e)
        {
            uiScaleText.Text = "UI Scale - " + uiScaleSlid.Value + "%";
        }
        private void ScaleChange(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs? e)
        {
            uiScale = s.Default.uiScale = (int)((Slider)sender).Value;
            uiScaleSlid.Value = uiScale;
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

        private void TcReset(object sender, RoutedEventArgs? e)
        {
            if (!awaitReset)
            {
                tcResetText.Text = "Click again to confirm         3";
                tcResetText.Opacity = 0.7;
                awaitReset = true;
                tcResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                var timerCount = 3;
                tcResetTimer.Tick += (sender, args) =>
                {
                    tcResetText.Text = "Click again to confirm         " + (timerCount - 1);
                    timerCount--;
                    if (timerCount == 0) { tcResetTimer.Stop(); awaitReset = false; tcResetText.Text = "Count total clicks"; tcResetText.Opacity = 1; }
                };
                tcResetTimer.Start();
            }
            else
            {
                tcResetTimer.Stop();
                s.Default.totalClicks = 0;
                s.Default.Save();
                totalText.Text = s.Default.totalClicks.ToString();
                tcResetText.Text = "Count total clicks";
                tcResetText.Opacity = 1;
                awaitReset = false;
            }
        }
        private void SetTheme(object sender, RoutedEventArgs? e)
        {
            curTheme = ((Button)sender).Tag.ToString();
            UpdateMergedDictionaries();

            s.Default.Theme = curTheme;
            s.Default.Save();
        }


        // MOUSE-OVER EFFECTS
        private void HoverHudDisplayTagEnter(object sender, MouseEventArgs? e)
        {
             CHT(2, ((FrameworkElement)sender).Name + "Hover", ((FrameworkElement)sender).Tag.ToString());
        }
        private void HoverHudDisplayTagLeave(object sender, MouseEventArgs? e)
        {
            CHT(5, ((FrameworkElement)sender).Name + "Hover", "");
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

        private void RandomBioText()
        {

        }


        // UTIL METHODS
        private void MakeNotification(string title, string meat)
        {
            ToastNotificationManagerCompat.History.Clear();
            new ToastContentBuilder()
                .AddText(title)
                .AddText(meat)
                .Show();
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
                if (inTuto && priority >= 2) return;
                hudText.Text = text;
                hudCurrentPriority = priority;
                hudCurrentToken = token;
            }
        }
        private void WindowDrag(object sender, MouseButtonEventArgs? e)
        {
            DragMove();
        }
        private void fullGridFocus(object sender, MouseButtonEventArgs? e)
        {
            fullGrid.Focus();
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

                activateButt.IsEnabled = true;

                newTrigListen = false;
                trigBorder.Visibility = Visibility.Visible;
                trigText.Text = "Trigger";
                trigText.HorizontalAlignment = HorizontalAlignment.Left;
                trigText.Margin = new Thickness(8, 0, 0, 0);
                trigText.Opacity = 1;
                locSetButt.IsEnabled = true;
                trigSet.Width = 40;
                trigSetText.Text = "Set";

                s.Default.hkAction = hotkey;
                s.Default.hkAlt = hkAlt;
                s.Default.hkCtrl = hkCtrl;
                s.Default.hkShift = hkShift;
                s.Default.Save();
            }
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || e.Key == Key.Tab) e.Handled = true;
        }
        private void HandleTextChange(object sender, RoutedEventArgs? e)
        {
            int millis = 0;
            int seconds = 0;
            int minutes = 0;
            try { millis = int.Parse(millisInput.Text); } catch { millisInput.Text = "0"; millis = 0; }
            try { seconds = int.Parse(secondsInput.Text); } catch { secondsInput.Text = "0"; seconds = 0; }
            try { minutes = int.Parse(minutesInput.Text); } catch { minutesInput.Text = "0"; minutes = 0; }
            if (millis == 0 && seconds == 0 && minutes == 0)
            {
                millis = 1;
                millisInput.Text = "1";
                seconds = 0;
                secondsInput.Text = "0";
                minutes = 0;
                minutesInput.Text = "0";
            }
            InterTextSet(millis, seconds, minutes);
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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
        public string AssemblyVersion
        {
            get
            {
                return "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " - Beta";
            }
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Try catches here just to ensure app does not crash and instead falls back to default WPF scrolling behaviour if an exception occurs here for whatever reason
            try
            {
                // Disables custom scroll handling when scroll by one screen at a time is enabled in mouse settings. Feel free to handle this yourself if you wish.
                if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines == -1) e.Handled = false;
                else
                    try
                    {
                        // Scrolls the scroll viewer according to the delta value and the user's scrolling settings
                        System.Windows.Controls.ScrollViewer SenderScrollViewer = (System.Windows.Controls.ScrollViewer)sender;
                        SenderScrollViewer.ScrollToVerticalOffset(SenderScrollViewer.VerticalOffset - e.Delta * 10 * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / (double)80);
                        e.Handled = true;
                    }
                    catch { }
            }
            catch { }
        }
        private void UpdateMergedDictionaries()
        {
            ResourceDictionary newRes = Application.Current.Resources.MergedDictionaries[1];
            newRes.MergedDictionaries.Clear();

            // Add theme dictionary
            try { newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Themes/" + curTheme + ".xaml", UriKind.Relative) }); }
            catch
            {
                try
                {
                    curTheme = s.Default.Theme;
                    newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Themes/" + curTheme + ".xaml", UriKind.Relative) });
                }
                catch
                {
                    s.Default.Theme = curTheme = "Default";
                    newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Themes/Default.xaml", UriKind.Relative) });
                }
            }

            // Add styles dictionary
            try { newRes.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("res/dic/Styles/" + "Styles" + (doAnimations ? "Animated" : "Static") + ".xaml", UriKind.Relative) }); }
            catch
            {
                doAnimations = !doAnimations;
                ColourToggle(animButt, doAnimations);
                animBorder.Opacity = doAnimations ? 1 : 0.4;
                return;
            }
        }


        // DLL IMPORTS
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }
}
