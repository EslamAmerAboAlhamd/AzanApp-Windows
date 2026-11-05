using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AzanApp
{
    public partial class HijriCalendarWindow : Window
    {
        private static readonly UmAlQuraCalendar Cal = new UmAlQuraCalendar();

        public static readonly string[] HijriMonths =
        {
            "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة",
            "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة"
        };

        private static readonly string[] GregorianMonths =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        private int year;
        private int month;

        public HijriCalendarWindow()
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);
            GoToday();
        }

        // تاريخ اليوم الهجري كنص (للاستخدام في الشاشة الرئيسية)
        public static string TodayText()
        {
            try
            {
                DateTime t = DateTime.Today;
                return $"{Cal.GetDayOfMonth(t)} {HijriMonths[Cal.GetMonth(t) - 1]} {Cal.GetYear(t)} هـ";
            }
            catch { return ""; }
        }

        private void GoToday()
        {
            DateTime t = DateTime.Today;
            year = Cal.GetYear(t);
            month = Cal.GetMonth(t);
            Render();
        }

        private void Render()
        {
            DaysGrid.Children.Clear();

            DateTime first = Cal.ToDateTime(year, month, 1, 0, 0, 0, 0);
            int daysInMonth = Cal.GetDaysInMonth(year, month);
            DateTime last = first.AddDays(daysInMonth - 1);

            TxtMonthTitle.Text = $"{HijriMonths[month - 1]} {year} هـ";
            TxtGregorianRange.Text =
                $"يوافق: {first.Day} {GregorianMonths[first.Month - 1]} {first.Year}م  -  {last.Day} {GregorianMonths[last.Month - 1]} {last.Year}م";

            // الأسبوع بيبدأ بالسبت: السبت=0 ... الجمعة=6
            int offset = ((int)first.DayOfWeek + 1) % 7;
            for (int i = 0; i < offset; i++)
                DaysGrid.Children.Add(new Border());

            DateTime today = DateTime.Today;

            for (int d = 1; d <= daysInMonth; d++)
            {
                DateTime g = first.AddDays(d - 1);
                bool isToday = g.Date == today;
                bool isFriday = g.DayOfWeek == DayOfWeek.Friday;

                string gText = g.Day == 1 || d == 1
                    ? $"{g.Day} {GregorianMonths[g.Month - 1]}"
                    : g.Day.ToString();

                var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                panel.Children.Add(new TextBlock
                {
                    Text = d.ToString(),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = isToday ? new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x1B))
                                         : new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4))
                });
                panel.Children.Add(new TextBlock
                {
                    Text = gText,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = isToday ? new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44))
                                         : new SolidColorBrush(Color.FromRgb(0x7F, 0x84, 0x9C))
                });

                var cell = new Border
                {
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Child = panel,
                    Background = isToday ? new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA))
                               : isFriday ? new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3C))
                               : new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
                };
                DaysGrid.Children.Add(cell);
            }
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) => Move(-1);
        private void BtnNext_Click(object sender, RoutedEventArgs e) => Move(1);
        private void BtnToday_Click(object sender, RoutedEventArgs e) => GoToday();
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Move(int delta)
        {
            int y = year, m = month + delta;
            if (m > 12) { m = 1; y++; }
            if (m < 1) { m = 12; y--; }

            // مدى التقويم المدعوم: 1318 - 1500 هـ
            if (y < 1318 || y > 1500) return;

            year = y;
            month = m;
            Render();
        }
    }
}