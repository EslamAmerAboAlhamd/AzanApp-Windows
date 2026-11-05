using System;
using System.IO;
using NAudio.Wave;

namespace AzanApp
{
    public static class Muadhins
    {
        // الترتيب هنا هو ترتيب القائمة ورقم ملف الصوت (01, 02, ...)
        public static readonly string[] Names =
        {
            "المدينة المنورة قديم - عصام بخاري",
            "المدينة المنورة جديد - محمد القصاص",
            "ياسر الدوسري",
            "ناصر القطامي",
            "مشاري العفاسي - عجم",
            "الشيخ محمد رفعت رحمه الله",
            "أذان تركي",
            "عراقي - عبدالسميع الشيخ",
            "عبدالرحمن الهندي",
            "الشيخ عبدالباسط عبدالصمد رحمه الله"
        };

        public static string SoundsFolder => Path.Combine(AppContext.BaseDirectory, "Sounds");

        public static string FileKey(int index) => (index + 1).ToString("00");

        // الملف الخاص بالمؤذن بالظبط (من غير بديل)
        public static string? FindExact(int index)
        {
            try
            {
                if (Directory.Exists(SoundsFolder))
                {
                    var files = Directory.GetFiles(SoundsFolder, FileKey(index) + ".*");
                    if (files.Length > 0) return files[0];
                }
            }
            catch { }
            return null;
        }

        // الملف المستخدم وقت الأذان: ملف المؤذن، وإلا azan.mp3 القديم
        public static string? FindFile(int index)
        {
            string? exact = FindExact(index);
            if (exact != null) return exact;

            string legacy = Path.Combine(AppContext.BaseDirectory, "azan.mp3");
            return File.Exists(legacy) ? legacy : null;
        }
    }

    public static class AzanPlayer
    {
        private static WaveOut? device;
        private static AudioFileReader? reader;

        public static void Play(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                Stop();

                var r = new AudioFileReader(path);
                var d = new WaveOut();
                reader = r;
                device = d;

                d.PlaybackStopped += (s, e) =>
                {
                    // نتجاهل الأجهزة القديمة علشان ما نقفلش الصوت الجديد
                    if (ReferenceEquals(device, d)) Stop();
                };

                d.Init(r);
                d.Play();
            }
            catch { Stop(); }
        }

        public static void Stop()
        {
            var d = device;
            var r = reader;
            device = null;
            reader = null;

            try { d?.Dispose(); } catch { }
            try { r?.Dispose(); } catch { }
        }
    }
}