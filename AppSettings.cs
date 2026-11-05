using System;
using System.IO;
using System.Text.Json;

namespace AzanApp
{
    public class AppSettings
    {
        public bool AutoLocation { get; set; } = true;
        public double LastLat { get; set; } = 30.0444;
        public double LastLon { get; set; } = 31.2357;

        public int LocationIndex { get; set; } = 1;
        public int MethodIndex { get; set; } = 2;
        public int DstIndex { get; set; } = 0;

        public bool RunAtStartup { get; set; } = false;

        // رقم المؤذن المختار (0 = الأول)
        public int MuadhinIndex { get; set; } = 0;

        // ضبط دقائق كل صلاة: الفجر، الشروق، الظهر، العصر، المغرب، العشاء
        public int[] Adjustments { get; set; } = new int[6];

        public static readonly (double Lat, double Lon)[] Locations =
        {
            (21.4225, 39.8262), // 0 مكة
            (30.0444, 31.2357), // 1 القاهرة
            (31.7683, 35.2137), // 2 القدس
            (25.2048, 55.2708), // 3 دبي
            (31.9539, 35.9106), // 4 عمان
            (31.2001, 29.9187), // 5 الإسكندرية
            (28.0871, 30.7618), // 6 المنيا
            (27.1783, 31.1859), // 7 أسيوط
            (25.6872, 32.6396), // 8 الأقصر
            (24.0889, 32.8998), // 9 أسوان
            (31.0364, 31.3807), // 10 المنصورة
            (30.7865, 31.0004)  // 11 طنطا
        };

        public (double Lat, double Lon) GetCoordinates() =>
            AutoLocation ? (LastLat, LastLon) : Locations[Math.Clamp(LocationIndex, 0, Locations.Length - 1)];

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AzanApp", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                    if (s.Adjustments == null || s.Adjustments.Length != 6) s.Adjustments = new int[6];
                    s.MuadhinIndex = Math.Clamp(s.MuadhinIndex, 0, Muadhins.Names.Length - 1);
                    return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
            }
            catch { }
        }
    }
}