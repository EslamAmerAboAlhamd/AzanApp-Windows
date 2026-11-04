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

        public static readonly (double Lat, double Lon)[] Locations =
        {
            (21.4225, 39.8262), // مكة
            (30.0444, 31.2357), // القاهرة
            (31.7683, 35.2137), // القدس
            (25.2048, 55.2708), // دبي
            (31.9539, 35.9106)  // عمان
        };

        public (double Lat, double Lon) GetCoordinates() =>
            AutoLocation ? (LastLat, LastLon) : Locations[LocationIndex];

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AzanApp", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
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