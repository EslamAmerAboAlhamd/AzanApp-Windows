using System.Diagnostics;
using System.Windows;

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
        }

        private void ChkAuto_Click(object sender, RoutedEventArgs e)
        {
            CmbLocation.IsEnabled = ChkAuto.IsChecked != true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Result = new AppSettings
            {
                AutoLocation = ChkAuto.IsChecked == true,
                LastLat = Result.LastLat,
                LastLon = Result.LastLon,
                LocationIndex = CmbLocation.SelectedIndex,
                MethodIndex = CmbMethod.SelectedIndex,
                DstIndex = CmbDst.SelectedIndex,
                RunAtStartup = ChkStartup.IsChecked == true
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