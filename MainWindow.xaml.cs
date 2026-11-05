using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Batoulapps.Adhan;
using Batoulapps.Adhan.Internal;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AzanApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer = null!;
        private AppSettings settings = AppSettings.Load();

        private int currentPrayerIndex = 0;
        private List<PrayerItem> prayersList = new List<PrayerItem>();
        private readonly HashSet<string> firedToday = new HashSet<string>();
        private DateTime currentDate = DateTime.Today;

        private DateTime tomorrowFajr;
        private DateTime yesterdayIsha;
        private bool showYesterdayIsha = false;

        private bool promptedThisSession = false;
        private bool awaitingPermission = false;

        private System.Windows.Forms.NotifyIcon? trayIcon;
        private bool reallyExit = false;

        public MainWindow()
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);

            bool startMinimized = Array.Exists(Environment.GetCommandLineArgs(), a => a == "--minimized");
            if (startMinimized)
            {
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
                Loaded += (s, e) => Hide();
            }

            UpdatePrayerTimes();
            StartTimer();
            SetupTrayIcon();

            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            Activated += async (s, e) =>
            {
                if (awaitingPermission)
                {
                    awaitingPermission = false;
                    await RefreshLocationAsync();
                }
            };

            _ = RefreshLocationAsync();
        }

        // ===================== الموقع =====================

        private async Task RefreshLocationAsync()
        {
            if (!settings.AutoLocation) return;

            var result = await LocationService.GetLocationAsync();

            if (result.PermissionDenied && !promptedThisSession && IsVisible)
            {
                promptedThisSession = true;
                AskForLocationPermission();
            }

            if (result.Coordinates == null) return;

            settings.LastLat = result.Coordinates.Value.Lat;
            settings.LastLon = result.Coordinates.Value.Lon;
            settings.Save();
            UpdatePrayerTimes();
        }

        private void AskForLocationPermission()
        {
            var answer = MessageBox.Show(
                this,
                "لتحديد مواقيت الصلاة بدقة حسب مكانك، يحتاج التطبيق إلى تفعيل خدمة الموقع في ويندوز.\n\n" +
                "هل تريد فتح إعدادات الموقع الآن؟\n\n" +
                "(فعّل \"خدمات الموقع\" و\"السماح لتطبيقات سطح المكتب بالوصول إلى موقعك\")",
                "تفعيل الموقع الجغرافي",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (answer == MessageBoxResult.Yes)
            {
                try
                {
                    awaitingPermission = true;
                    Process.Start(new ProcessStartInfo("ms-settings:privacy-location")
                    {
                        UseShellExecute = true
                    });
                }
                catch { awaitingPermission = false; }
            }
        }

        // ===================== المواقيت =====================

        private List<PrayerItem> BuildPrayers(DateTime day)
        {
            var loc = settings.GetCoordinates();
            var coordinates = new Coordinates(loc.Lat, loc.Lon);

            int hourOffset = settings.DstIndex switch { 1 => 1, 2 => -1, _ => 0 };
            int[] adj = settings.Adjustments is { Length: 6 } ? settings.Adjustments : new int[6];

            DateComponents date = DateComponents.From(day);

            CalculationParameters paramsMethod = settings.MethodIndex switch
            {
                1 => CalculationMethod.UMM_AL_QURA.GetParameters(),
                2 => CalculationMethod.EGYPTIAN.GetParameters(),
                3 => CalculationMethod.KARACHI.GetParameters(),
                4 => CalculationMethod.NORTH_AMERICA.GetParameters(),
                _ => CalculationMethod.MUSLIM_WORLD_LEAGUE.GetParameters()
            };

            var pt = new PrayerTimes(coordinates, date, paramsMethod);

            DateTime Fix(DateTime t, int i) => t.ToLocalTime().AddHours(hourOffset).AddMinutes(adj[i]);

            return new List<PrayerItem>
            {
                new PrayerItem("الفجر",  Fix(pt.Fajr, 0),    "fajr.jpg"),
                new PrayerItem("الشروق", Fix(pt.Sunrise, 1), "sunrise.jpg"),
                new PrayerItem("الظهر",  Fix(pt.Dhuhr, 2),   "dhuhr.jpg"),
                new PrayerItem("العصر",  Fix(pt.Asr, 3),     "asr.jpg"),
                new PrayerItem("المغرب", Fix(pt.Maghrib, 4), "maghrib.jpg"),
                new PrayerItem("العشاء", Fix(pt.Isha, 5),    "isha.jpg")
            };
        }

        private void UpdatePrayerTimes()
        {
            DateTime now = DateTime.Now;

            currentDate = DateTime.Today;
            firedToday.Clear();

            prayersList = BuildPrayers(now);
            tomorrowFajr = BuildPrayers(now.AddDays(1))[0].Time;
            yesterdayIsha = BuildPrayers(now.AddDays(-1))[5].Time;

            TxtHijriDate.Text = HijriCalendarWindow.TodayText();

            SelectCurrentPrayer();
            DisplayCurrentPrayer();
            UpdateCountdown();
        }

        private void SelectCurrentPrayer()
        {
            DateTime now = DateTime.Now;
            int last = -1;

            for (int i = 0; i < prayersList.Count; i++)
            {
                if (i == 1) continue; // الشروق مش صلاة
                if (prayersList[i].Time <= now) last = i;
            }

            if (last == -1)
            {
                currentPrayerIndex = 5;
                showYesterdayIsha = true;
            }
            else
            {
                currentPrayerIndex = last;
                showYesterdayIsha = false;
            }
        }

        private void DisplayCurrentPrayer()
        {
            if (prayersList.Count == 0) return;

            var item = prayersList[currentPrayerIndex];
            DateTime shownTime = (showYesterdayIsha && currentPrayerIndex == 5) ? yesterdayIsha : item.Time;

            TxtPrayerName.Text = item.Name;
            TxtPrayerTime.Text = shownTime.ToString("hh:mm tt");

            if (item.Name == "الشروق")
                TxtHeader.Text = "الشروق";
            else
                TxtHeader.Text = shownTime <= DateTime.Now ? "آخر صلاة" : "الصلاة القادمة";

            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Images", item.ImageFile);
                if (File.Exists(path))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    ImgBackground.Source = bmp;
                }
            }
            catch { }
        }

        private void UpdateCountdown()
        {
            if (prayersList.Count == 0) return;

            DateTime now = DateTime.Now;
            string nextName = "الفجر";
            DateTime nextTime = tomorrowFajr;

            for (int i = 0; i < prayersList.Count; i++)
            {
                if (i == 1) continue;
                if (prayersList[i].Time > now)
                {
                    nextName = prayersList[i].Name;
                    nextTime = prayersList[i].Time;
                    break;
                }
            }

            TimeSpan remaining = nextTime - now;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            TxtRemainingLabel.Text = $"الوقت المتبقي لصلاة {nextName}";
            TxtRemaining.Text = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        // ===================== التنقل بين الصلوات =====================

        private void LeftZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showYesterdayIsha = false;
            currentPrayerIndex--;
            if (currentPrayerIndex < 0) currentPrayerIndex = prayersList.Count - 1;
            DisplayCurrentPrayer();
        }

        private void RightZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showYesterdayIsha = false;
            currentPrayerIndex++;
            if (currentPrayerIndex >= prayersList.Count) currentPrayerIndex = 0;
            DisplayCurrentPrayer();
        }

        // ===================== الإشعار والأذان =====================

        private void SendWindowsNotification(string prayerName)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText("🕌 حان الآن وقت الصلاة")
                    .AddText($"حان الآن وقت صلاة {prayerName}.")
                    .Show();
            }
            catch { }
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                DateTime now = DateTime.Now;

                if (now.Date != currentDate)
                {
                    UpdatePrayerTimes();
                    _ = RefreshLocationAsync();
                    return;
                }

                UpdateCountdown();

                foreach (var p in prayersList)
                {
                    if (p.Name == "الشروق") continue;

                    double diff = (now - p.Time).TotalSeconds;
                    if (diff >= 0 && diff < 60 && firedToday.Add(p.Name))
                    {
                        SendWindowsNotification(p.Name);
                        AzanPlayer.Play(Muadhins.FindFile(settings.MuadhinIndex));
                        SelectCurrentPrayer();
                        DisplayCurrentPrayer();
                        UpdateCountdown();
                    }
                }
            };
            timer.Start();
        }

        // ===================== أيقونة الـ Tray =====================

        private void SetupTrayIcon()
{
    trayIcon = new System.Windows.Forms.NotifyIcon
    {
        Text = "المؤذن",
        Visible = true
    };

    try
    {
        var info = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"));
        if (info != null)
            trayIcon.Icon = new System.Drawing.Icon(info.Stream);
        else
            trayIcon.Icon = System.Drawing.SystemIcons.Application;
    }
    catch { trayIcon.Icon = System.Drawing.SystemIcons.Application; }

    trayIcon.DoubleClick += (s, e) => RestoreFromTray();

    var menu = new System.Windows.Forms.ContextMenuStrip();
    menu.Items.Add("فتح المؤذن", null, (s, e) => RestoreFromTray());
    menu.Items.Add("إيقاف الأذان", null, (s, e) => AzanPlayer.Stop());
    menu.Items.Add("خروج", null, (s, e) =>
    {
        reallyExit = true;
        Close();
    });
    trayIcon.ContextMenuStrip = menu;
}

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!reallyExit)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Normal;
            Activate();

            SelectCurrentPrayer();
            DisplayCurrentPrayer();
            UpdateCountdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            AzanPlayer.Stop();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }

        // ===================== التواصل =====================

        private void Email_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                try { Clipboard.SetText("eslamae420@gmail.com"); } catch { }
                MessageBox.Show(this, "تم نسخ البريد الإلكتروني: eslamae420@gmail.com",
                    "تواصل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            e.Handled = true;
        }

        // ===================== التقويم والإعدادات =====================

        private void BtnCalendar_Click(object sender, RoutedEventArgs e)
        {
            var win = new HijriCalendarWindow { Owner = this };
            win.ShowDialog();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(settings) { Owner = this };
            if (win.ShowDialog() == true)
            {
                settings = win.Result;
                settings.Save();
                StartupManager.Set(settings.RunAtStartup);
                UpdatePrayerTimes();
                _ = RefreshLocationAsync();
            }
        }
    }

    public class PrayerItem
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public string ImageFile { get; set; }

        public PrayerItem(string name, DateTime time, string imageFile)
        {
            Name = name;
            Time = time;
            ImageFile = imageFile;
        }
    }
}