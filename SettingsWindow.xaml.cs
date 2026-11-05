using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace AzanApp
{
    public partial class SettingsWindow : Window
    {
        public AppSettings Result { get; private set; }

        public SettingsWindow(AppSettings current)
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);
            Result = current;

            ChkAuto.IsChecked = current.AutoLocation;
            CmbLocation.SelectedIndex = current.LocationIndex;
            CmbMethod.SelectedIndex = current.MethodIndex;
            CmbDst.SelectedIndex = current.DstIndex;
            CmbLocation.IsEnabled = !current.AutoLocation;
            ChkStartup.IsChecked = current.RunAtStartup;

            // قائمة المؤذنين (بنفس ترتيب ملفات Sounds)
            for (int i = 0; i < Muadhins.Names.Length; i++)
                CmbMuadhin.Items.Add($"{i + 1:00}. {Muadhins.Names[i]}");
            CmbMuadhin.SelectedIndex = Math.Clamp(current.MuadhinIndex, 0, Muadhins.Names.Length - 1);
            UpdateSoundStatus();
            CmbMuadhin.SelectionChanged += (s, e) =>
            {
                AzanPlayer.Stop();
                UpdateSoundStatus();
            };
            Closed += (s, e) => AzanPlayer.Stop();

            var c = current.GetCoordinates();
            TxtCoords.Text = string.Format(CultureInfo.InvariantCulture,
                "الإحداثيات المستخدمة حالياً: {0:F4}, {1:F4}", c.Lat, c.Lon);

            int[] a = current.Adjustments is { Length: 6 } ? current.Adjustments : new int[6];
            TxtAdjFajr.Text = a[0].ToString();
            TxtAdjSunrise.Text = a[1].ToString();
            TxtAdjDhuhr.Text = a[2].ToString();
            TxtAdjAsr.Text = a[3].ToString();
            TxtAdjMaghrib.Text = a[4].ToString();
            TxtAdjIsha.Text = a[5].ToString();
        }

        private void UpdateSoundStatus()
        {
            int i = CmbMuadhin.SelectedIndex;
            if (i < 0) return;

            string? file = Muadhins.FindExact(i);
            if (file != null)
            {
                TxtSoundStatus.Text = $"✅ الملف موجود: {Path.GetFileName(file)}";
                TxtSoundStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
            }
            else
            {
                TxtSoundStatus.Text = $"⚠️ الملف غير موجود. ضع ملف الصوت باسم {Muadhins.FileKey(i)} (مثل {Muadhins.FileKey(i)}.mp3) داخل مجلد Sounds.";
                TxtSoundStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF));
            }
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            string? file = Muadhins.FindExact(CmbMuadhin.SelectedIndex);
            if (file == null)
            {
                UpdateSoundStatus();
                return;
            }
            AzanPlayer.Play(file);
        }

        private void BtnStopPreview_Click(object sender, RoutedEventArgs e)
        {
            AzanPlayer.Stop();
        }

        private static int ParseAdj(string? s) =>
            int.TryParse(s?.Trim(), out int v) ? Math.Clamp(v, -30, 30) : 0;

        private void ChkAuto_Click(object sender, RoutedEventArgs e)
        {
            CmbLocation.IsEnabled = ChkAuto.IsChecked != true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            AzanPlayer.Stop();

            Result = new AppSettings
            {
                AutoLocation = ChkAuto.IsChecked == true,
                LastLat = Result.LastLat,
                LastLon = Result.LastLon,
                LocationIndex = CmbLocation.SelectedIndex,
                MethodIndex = CmbMethod.SelectedIndex,
                DstIndex = CmbDst.SelectedIndex,
                RunAtStartup = ChkStartup.IsChecked == true,
                MuadhinIndex = Math.Max(0, CmbMuadhin.SelectedIndex),
                Adjustments = new[]
                {
                    ParseAdj(TxtAdjFajr.Text),
                    ParseAdj(TxtAdjSunrise.Text),
                    ParseAdj(TxtAdjDhuhr.Text),
                    ParseAdj(TxtAdjAsr.Text),
                    ParseAdj(TxtAdjMaghrib.Text),
                    ParseAdj(TxtAdjIsha.Text)
                }
            };
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

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
    }
}