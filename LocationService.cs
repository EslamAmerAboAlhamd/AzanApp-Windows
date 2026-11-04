using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace AzanApp
{
    public class LocationResult
    {
        public (double Lat, double Lon)? Coordinates { get; set; }
        public bool PermissionDenied { get; set; }
    }

    public static class LocationService
    {
        public static async Task<LocationResult> GetLocationAsync()
        {
            var result = new LocationResult();

            try
            {
                var access = await Geolocator.RequestAccessAsync();

                if (access == GeolocationAccessStatus.Allowed)
                {
                    var geo = new Geolocator { DesiredAccuracyInMeters = 5000 };
                    var pos = await geo.GetGeopositionAsync(
                        TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(10));
                    var p = pos.Coordinate.Point.Position;
                    result.Coordinates = (p.Latitude, p.Longitude);
                    return result;
                }

                if (access == GeolocationAccessStatus.Denied)
                    result.PermissionDenied = true;
            }
            catch { }

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                string json = await http.GetStringAsync("https://ipapi.co/json/");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("latitude", out var lat) &&
                    root.TryGetProperty("longitude", out var lon))
                {
                    result.Coordinates = (lat.GetDouble(), lon.GetDouble());
                }
            }
            catch { }

            return result;
        }
    }
}