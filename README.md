# 🕌 المؤذن | AzanApp

تطبيق سطح مكتب لويندوز يعرض مواقيت الصلاة بخلفيات جميلة، ويشغّل الأذان وينبّهك بإشعار عند دخول وقت كل صلاة.

A Windows desktop app (WPF / .NET 9) that shows prayer times with beautiful backgrounds, plays the Adhan and sends a notification when each prayer time arrives.

---

## ✨ المميزات | Features

- 🕐 عرض الصلاة القادمة ووقتها، مع التنقل بين الصلوات بالضغط على يمين/يسار الشاشة
- 📍 تحديد الموقع تلقائياً (موقع ويندوز، وبديل تقريبي من الـ IP) أو اختيار المدينة يدوياً
- 🧮 عدة طرق لحساب المواقيت: رابطة العالم الإسلامي، أم القرى، الهيئة المصرية، كراتشي، ISNA
- 🔔 إشعار ويندوز وتشغيل الأذان عند دخول الوقت
- 🖼️ خلفية مخصصة لكل صلاة
- 🔽 يعمل في الخلفية من أيقونة شريط النظام (System Tray)
- 🚀 خيار التشغيل التلقائي مع بدء ويندوز
- 🌙 شريط عنوان داكن وخط Cairo
- 💾 حفظ الإعدادات تلقائياً

## 📋 المتطلبات | Requirements

- Windows 10 (الإصدار 1809) أو أحدث / Windows 10 (1809) or later
- معالج 64-bit / 64-bit processor

## 📥 التثبيت | Installation

1. اذهب إلى صفحة [Releases](../../releases).
2. حمّل أحدث ملف `AzanApp_Setup_x.x.x.exe`.
3. شغّله واتبع خطوات التثبيت.

> قد يعرض ويندوز تحذير **SmartScreen** لأن التطبيق غير موقّع رقمياً. اضغط **More info** ثم **Run anyway**.

## 🛠️ البناء من المصدر | Build from source

المتطلبات: [.NET 9 SDK](https://dotnet.microsoft.com/download)

```powershell
git clone https://github.com/EslamAmerAboAlhamd/AzanApp-Windows.git
cd AzanApp-Windows
dotnet run
```

لإنشاء نسخة مستقلة جاهزة للتوزيع:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

ثم افتح `AzanApp.iss` ببرنامج [Inno Setup](https://jrsoftware.org/isinfo.php) واضغط `Ctrl+F9` لإنشاء ملف التثبيت.

## 📁 هيكل المشروع | Project structure

```
AzanApp/
├── MainWindow.xaml(.cs)       الشاشة الرئيسية
├── SettingsWindow.xaml(.cs)   نافذة الإعدادات
├── AppSettings.cs             حفظ وتحميل الإعدادات
├── LocationService.cs         تحديد الموقع
├── StartupManager.cs          التشغيل مع ويندوز
├── DarkTitleBar.cs            شريط العنوان الداكن
├── AzanApp.iss                سكريبت ملف التثبيت (Inno Setup)
├── Images/                    صور الخلفيات
├── Fonts/                     خط Cairo
└── azan.mp3                   ملف الأذان
```

## ⚙️ الإعدادات | Settings

تُحفظ الإعدادات في:

```
%AppData%\AzanApp\settings.json
```

## 📦 المكتبات المستخدمة | Built with

- [Adhan](https://github.com/batoulapps/adhan-dotnet): حساب مواقيت الصلاة
- [NAudio](https://github.com/naudio/NAudio): تشغيل الصوت
- [Microsoft.Toolkit.Uwp.Notifications](https://learn.microsoft.com/windows/apps/design/shell/tiles-and-notifications/): إشعارات ويندوز
- خط [Cairo](https://fonts.google.com/specimen/Cairo) (ترخيص OFL)

## ⚠️ ملاحظة | Disclaimer

المواقيت محسوبة تقريبياً بحسب الموقع وطريقة الحساب المختارة، فيُرجى التأكد منها من الجهة الرسمية في بلدك.

Prayer times are calculated approximations. Please verify with your local authority.

## 🐞 التواصل والإبلاغ عن المشاكل | Contact

للتواصل أو الإبلاغ عن مشكلة: **eslamae420@gmail.com**
أو افتح [Issue](../../issues) جديدة.

## 📄 الترخيص | License

© 2026-2027 Eslam Amer. جميع الحقوق محفوظة / All rights reserved.
