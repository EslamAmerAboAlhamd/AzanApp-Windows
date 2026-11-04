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
using NAudio.Wave;
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

        private WaveOut? outputDevice;
        private AudioFileReader? audioFile;

        private bool promptedThisSession = false;
        private bool awaitingPermission = false;

        private System.Windows.Forms.NotifyIcon? trayIcon;
        private bool reallyExit = false;

        public MainWindow()
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);

            // التشغيل مع ويندوز: يفتح مصغّراً في الـ Tray
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

        private void UpdatePrayerTimes()
        {
            var loc = settings.GetCoordinates();
            var coordinates = new Coordinates(loc.Lat, loc.Lon);

            int hourOffset = settings.DstIndex switch { 1 => 1, 2 => -1, _ => 0 };

            currentDate = DateTime.Today;
            firedToday.Clear();
            DateComponents date = DateComponents.From(DateTime.Now);

            CalculationParameters paramsMethod = settings.MethodIndex switch
            {
                1 => CalculationMethod.UMM_AL_QURA.GetParameters(),
                2 => CalculationMethod.EGYPTIAN.GetParameters(),
                3 => CalculationMethod.KARACHI.GetParameters(),
                4 => CalculationMethod.NORTH_AMERICA.GetParameters(),
                _ => CalculationMethod.MUSLIM_WORLD_LEAGUE.GetParameters()
            };

            var pt = new PrayerTimes(coordinates, date, paramsMethod);

            DateTime Fix(DateTime t) => t.ToLocalTime().AddHours(hourOffset);

            prayersList = new List<PrayerItem>
            {
                new PrayerItem("الفجر",  Fix(pt.Fajr),    "fajr.jpg"),
                new PrayerItem("الشروق", Fix(pt.Sunrise), "sunrise.jpg"),
                new PrayerItem("الظهر",  Fix(pt.Dhuhr),   "dhuhr.jpg"),
                new PrayerItem("العصر",  Fix(pt.Asr),     "asr.jpg"),
                new PrayerItem("المغرب", Fix(pt.Maghrib), "maghrib.jpg"),
                new PrayerItem("العشاء", Fix(pt.Isha),    "isha.jpg")
            };

            SelectNextUpcomingPrayer();
            DisplayCurrentPrayer();
        }

        private void SelectNextUpcomingPrayer()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < prayersList.Count; i++)
            {
                if (prayersList[i].Time > now)
                {
                    currentPrayerIndex = i;
                    return;
                }
            }
            currentPrayerIndex = 0;
        }

        private void DisplayCurrentPrayer()
        {
            if (prayersList.Count == 0) return;

            var item = prayersList[currentPrayerIndex];
            TxtPrayerName.Text = item.Name;
            TxtPrayerTime.Text = item.Time.ToString("hh:mm tt");

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

        // ===================== التنقل بين الصلوات =====================

        private void LeftZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            currentPrayerIndex--;
            if (currentPrayerIndex < 0) currentPrayerIndex = prayersList.Count - 1;
            DisplayCurrentPrayer();
        }

        private void RightZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
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

                foreach (var p in prayersList)
                {
                    if (p.Name == "الشروق") continue;

                    double diff = (now - p.Time).TotalSeconds;
                    if (diff >= 0 && diff < 60 && firedToday.Add(p.Name))
                    {
                        SendWindowsNotification(p.Name);
                        PlayAzanSound();
                        SelectNextUpcomingPrayer();
                        DisplayCurrentPrayer();
                    }
                }
            };
            timer.Start();
        }

        private void PlayAzanSound()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "azan.mp3");
                if (!File.Exists(path)) return;

                DisposeAudio();
                audioFile = new AudioFileReader(path);
                outputDevice = new WaveOut();
                outputDevice.PlaybackStopped += (s, e) => DisposeAudio();
                outputDevice.Init(audioFile);
                outputDevice.Play();
            }
            catch { }
        }

        private void DisposeAudio()
        {
            outputDevice?.Dispose();
            outputDevice = null;
            audioFile?.Dispose();
            audioFile = null;
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
                string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                    trayIcon.Icon = new System.Drawing.Icon(iconPath);
                else
                    trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch { trayIcon.Icon = System.Drawing.SystemIcons.Application; }

            trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("فتح المؤذن", null, (s, e) => RestoreFromTray());
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
        }

        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            DisposeAudio();
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

        // ===================== الإعدادات =====================

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