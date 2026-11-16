using System.Globalization;

namespace SkyGlance.Localization;

/// <summary>English / Arabic UI text and date formatting.</summary>
public static class L
{
    public static bool Ar { get; set; }

    static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-GB");
    static readonly CultureInfo Arabic = CreateArabic();

    /// <summary>Arabic month and day names on the Gregorian calendar, with Latin digits.</summary>
    static CultureInfo CreateArabic()
    {
        var c = (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        c.DateTimeFormat.Calendar = new GregorianCalendar();
        c.NumberFormat.DigitSubstitution = DigitShapes.None;
        return c;
    }

    public static CultureInfo Culture => Ar ? Arabic : En;


    static readonly Dictionary<string, (string en, string ar)> D = new()
    {
        ["AppName"] = ("Sky Glance", "سكاي جلانس"),
        ["Loading"] = ("Getting the weather...", "جارٍ جلب حالة الطقس..."),
        ["NoLocation"] = ("Choose a city to see the weather", "اختر مدينة لعرض حالة الطقس"),
        ["Offline"] = ("Couldn't reach Open-Meteo. Showing the last forecast.", "تعذر الوصول إلى Open-Meteo. يُعرض آخر تنبؤ محفوظ."),
        ["OfflineNoData"] = ("Couldn't reach Open-Meteo.", "تعذر الوصول إلى Open-Meteo."),
        ["UpdatedAgo"] = ("Updated {0} min ago", "حُدِّث منذ {0} دقيقة"),
        ["UpdatedNow"] = ("Updated just now", "حُدِّث الآن"),
        ["UpdatedAt"] = ("Updated {0}", "حُدِّث {0}"),
        ["LocalTime"] = ("Local time {0}", "التوقيت المحلي {0}"),
        ["FeelsLike"] = ("Feels like", "الإحساس"),
        ["High"] = ("H", "العظمى"),
        ["Low"] = ("L", "الصغرى"),
        ["Humidity"] = ("Humidity", "الرطوبة"),
        ["Wind"] = ("Wind", "الرياح"),
        ["Gusts"] = ("Gusts", "الهبّات"),
        ["UvIndex"] = ("UV index", "مؤشر الأشعة"),
        ["RainChance"] = ("Rain chance", "فرصة المطر"),
        ["Sunrise"] = ("Sunrise", "الشروق"),
        ["Sunset"] = ("Sunset", "الغروب"),
        ["Pressure"] = ("Pressure", "الضغط"),
        ["NoSun"] = ("None", "لا يوجد"),
        ["Next24Hours"] = ("Next 24 hours", "الساعات الـ24 القادمة"),
        ["SevenDays"] = ("7-day forecast", "توقعات 7 أيام"),
        ["Today"] = ("Today", "اليوم"),
        ["Now"] = ("Now", "الآن"),
        ["Refresh"] = ("Refresh", "تحديث"),
        ["Location"] = ("Location", "الموقع"),
        ["Settings"] = ("Settings", "الإعدادات"),
        ["DataBy"] = ("Weather data by Open-Meteo.com", "بيانات الطقس من Open-Meteo.com"),

        ["ShowWeather"] = ("Show forecast", "عرض التوقعات"),
        ["RefreshNow"] = ("Refresh now", "تحديث الآن"),
        ["ChangeLocation"] = ("Change location...", "تغيير الموقع..."),
        ["TempUnitMenu"] = ("Temperature unit", "وحدة الحرارة"),
        ["CheckForUpdates"] = ("Check for updates", "التحقق من التحديثات"),
        ["StartWithWindows"] = ("Start with Windows", "التشغيل مع ويندوز"),
        ["Exit"] = ("Exit", "خروج"),

        ["General"] = ("General", "عام"),
        ["Language"] = ("Language", "اللغة"),
        ["Units"] = ("Units", "الوحدات"),
        ["TempUnit"] = ("Temperature", "درجة الحرارة"),
        ["WindUnit"] = ("Wind speed", "سرعة الرياح"),
        ["PrecipUnit"] = ("Precipitation", "الهطول"),
        ["Celsius"] = ("Celsius (°C)", "مئوية (°C)"),
        ["Fahrenheit"] = ("Fahrenheit (°F)", "فهرنهايت (°F)"),
        ["WindKmh"] = ("Kilometres per hour (km/h)", "كيلومتر في الساعة (km/h)"),
        ["WindMs"] = ("Metres per second (m/s)", "متر في الثانية (m/s)"),
        ["WindMph"] = ("Miles per hour (mph)", "ميل في الساعة (mph)"),
        ["WindKn"] = ("Knots (kn)", "عقدة (kn)"),
        ["WindBft"] = ("Beaufort scale (Bft)", "مقياس بوفورت (Bft)"),
        ["PrecipMm"] = ("Millimetres (mm)", "مليمتر (mm)"),
        ["PrecipIn"] = ("Inches (in)", "بوصة (in)"),
        ["Taskbar"] = ("Taskbar", "شريط المهام"),
        ["ShowCondition"] = ("Show the condition under the temperature", "عرض وصف الطقس أسفل درجة الحرارة"),
        ["RefreshEvery"] = ("Refresh every", "التحديث كل"),
        ["Minutes"] = ("{0} minutes", "{0} دقيقة"),
        ["WidgetPosition"] = ("Taskbar position", "الموضع في شريط المهام"),
        ["WidgetOffset"] = ("Shift (pixels)", "الإزاحة (بكسل)"),
        ["SideRight"] = ("Right, next to the tray", "يمين، بجوار منطقة الإشعارات"),
        ["SideLeft"] = ("Left side", "الجانب الأيسر"),
        ["Save"] = ("Save", "حفظ"),
        ["Cancel"] = ("Cancel", "إلغاء"),

        ["AutoLocation"] = ("Find my location automatically", "تحديد موقعي تلقائيًا"),
        ["AutoLocationNote"] = ("Uses Windows location when it is on, otherwise a rough location from your IP address.",
                                "يستخدم خدمة الموقع في ويندوز إذا كانت مفعّلة، وإلا فموقعًا تقريبيًا من عنوان IP."),
        ["CurrentLocation"] = ("Current location", "الموقع الحالي"),
        ["NotSet"] = ("Not set yet", "لم يُحدَّد بعد"),
        ["DetectNow"] = ("Detect now", "تحديد الآن"),
        ["Detecting"] = ("Finding your location...", "جارٍ تحديد موقعك..."),
        ["DetectFailed"] = ("Couldn't find your location. Search for a city instead.", "تعذر تحديد موقعك. ابحث عن مدينة بدلًا من ذلك."),
        ["SearchCity"] = ("Search for a city", "البحث عن مدينة"),
        ["SearchHint"] = ("City name, for example Lisbon", "اسم المدينة، مثل لشبونة"),
        ["Search"] = ("Search", "بحث"),
        ["Searching"] = ("Searching...", "جارٍ البحث..."),
        ["NoResults"] = ("No places found.", "لم يُعثر على أماكن."),
        ["UseCity"] = ("Use this city", "استخدام هذه المدينة"),
        ["CityChosen"] = ("Selected {0}. Save to apply.", "تم اختيار {0}. احفظ للتطبيق."),
        ["PrivacyNote"] = ("Your location is only sent to the services listed in the README, without an account or key, and stays in settings.json on this PC.",
                           "لا يُرسَل موقعك إلا إلى الخدمات المذكورة في ملف README، دون حساب أو مفتاح، ويبقى في الملف settings.json على هذا الجهاز."),

        ["Updates"] = ("Updates", "التحديثات"),
        ["AutoCheck"] = ("Check for updates automatically", "التحقق من التحديثات تلقائيًا"),
        ["AutoInstall"] = ("Install updates automatically", "تثبيت التحديثات تلقائيًا"),
        ["CheckNow"] = ("Check now", "تحقق الآن"),
        ["Version"] = ("Version {0}", "الإصدار {0}"),
        ["UpToDate"] = ("You're on the latest version ({0}).", "لديك أحدث إصدار ({0})."),
        ["UpdateAvailable"] = ("Version {0} is available.", "الإصدار {0} متاح."),
        ["Updating"] = ("Updating to {0}...", "جارٍ التحديث إلى {0}..."),
        ["UpdateFailed"] = ("Couldn't install the update. Opening the download page.", "تعذر تثبيت التحديث. سيتم فتح صفحة التنزيل."),
        ["UpdateCheckFailed"] = ("Couldn't check for updates.", "تعذر التحقق من التحديثات."),
        ["Updated"] = ("Updated to version {0}.", "تم التحديث إلى الإصدار {0}."),

        ["Compass0"] = ("N", "ش"), ["Compass1"] = ("NE", "ش ق"), ["Compass2"] = ("E", "ق"), ["Compass3"] = ("SE", "ج ق"),
        ["Compass4"] = ("S", "ج"), ["Compass5"] = ("SW", "ج غ"), ["Compass6"] = ("W", "غ"), ["Compass7"] = ("NW", "ش غ"),

        ["Wmo0"] = ("Clear sky", "سماء صافية"),
        ["Wmo1"] = ("Mainly clear", "صافٍ غالبًا"),
        ["Wmo2"] = ("Partly cloudy", "غائم جزئيًا"),
        ["Wmo3"] = ("Overcast", "غائم"),
        ["Wmo45"] = ("Fog", "ضباب"),
        ["Wmo48"] = ("Freezing fog", "ضباب متجمد"),
        ["Wmo51"] = ("Light drizzle", "رذاذ خفيف"),
        ["Wmo53"] = ("Drizzle", "رذاذ"),
        ["Wmo55"] = ("Heavy drizzle", "رذاذ كثيف"),
        ["Wmo56"] = ("Freezing drizzle", "رذاذ متجمد"),
        ["Wmo57"] = ("Heavy freezing drizzle", "رذاذ متجمد كثيف"),
        ["Wmo61"] = ("Light rain", "مطر خفيف"),
        ["Wmo63"] = ("Rain", "مطر"),
        ["Wmo65"] = ("Heavy rain", "مطر غزير"),
        ["Wmo66"] = ("Freezing rain", "مطر متجمد"),
        ["Wmo67"] = ("Heavy freezing rain", "مطر متجمد غزير"),
        ["Wmo71"] = ("Light snow", "ثلج خفيف"),
        ["Wmo73"] = ("Snow", "ثلج"),
        ["Wmo75"] = ("Heavy snow", "ثلج كثيف"),
        ["Wmo77"] = ("Snow grains", "حبيبات ثلج"),
        ["Wmo80"] = ("Light showers", "زخات خفيفة"),
        ["Wmo81"] = ("Showers", "زخات مطر"),
        ["Wmo82"] = ("Heavy showers", "زخات غزيرة"),
        ["Wmo85"] = ("Snow showers", "زخات ثلج"),
        ["Wmo86"] = ("Heavy snow showers", "زخات ثلج كثيفة"),
        ["Wmo95"] = ("Thunderstorm", "عاصفة رعدية"),
        ["Wmo96"] = ("Thunderstorm with hail", "عاصفة رعدية مع برد"),
        ["Wmo99"] = ("Thunderstorm, heavy hail", "عاصفة رعدية وبرد كثيف"),
        ["WmoUnknown"] = ("Unknown", "غير معروف"),
    };

    public static string T(string key) => D.TryGetValue(key, out var v) ? (Ar ? v.ar : v.en) : key;

    public static string F(string key, params object[] args) => string.Format(Culture, T(key), args);

    public static bool Has(string key) => D.ContainsKey(key);

    public static string Condition(int code) => T(WeatherCode.Key(code));


    /// <summary>"14:30" in both languages.</summary>
    public static string Clock(DateTime t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Hour label in the hourly strip: "14:00".</summary>
    public static string Hour(DateTime t) => t.ToString("HH:00", CultureInfo.InvariantCulture);

    /// <summary>"Tue" / "الثلاثاء".</summary>
    public static string DayName(DateOnly d) => d.ToDateTime(TimeOnly.MinValue).ToString(Ar ? "dddd" : "ddd", Culture);

    /// <summary>"Tuesday 17 November" for the flyout header.</summary>
    public static string LongDay(DateTime t) => t.ToString("dddd d MMMM", Culture);
}
