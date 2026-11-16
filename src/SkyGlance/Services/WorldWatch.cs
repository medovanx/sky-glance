namespace SkyGlance.Services;

/// <summary>
/// The places checked for "hottest and coldest right now": the world's usual record holders in both hemispheres,
/// so one of them is the hottest or coldest in any season. All are fetched in one Open-Meteo request.
/// </summary>
public static class WorldWatch
{
    public static readonly WatchedSpot[] Spots =
    [
        // Usually the hottest, depending on the season.
        new("Death Valley, United States", "وادي الموت، الولايات المتحدة", 36.46, -116.87),
        new("Phoenix, United States", "فينيكس، الولايات المتحدة", 33.45, -112.07),
        new("Mexicali, Mexico", "مكسيكالي، المكسيك", 32.62, -115.45),
        new("Kuwait City, Kuwait", "مدينة الكويت، الكويت", 29.38, 47.99),
        new("Ahvaz, Iran", "الأهواز، إيران", 31.32, 48.67),
        new("Basra, Iraq", "البصرة، العراق", 30.51, 47.78),
        new("Riyadh, Saudi Arabia", "الرياض، السعودية", 24.71, 46.68),
        new("Dubai, United Arab Emirates", "دبي، الإمارات", 25.2, 55.27),
        new("Aswan, Egypt", "أسوان، مصر", 24.09, 32.9),
        new("Khartoum, Sudan", "الخرطوم، السودان", 15.5, 32.56),
        new("Ouargla, Algeria", "ورقلة، الجزائر", 31.95, 5.33),
        new("Timbuktu, Mali", "تمبكتو، مالي", 16.77, -3.01),
        new("Niamey, Niger", "نيامي، النيجر", 13.51, 2.11),
        new("Jacobabad, Pakistan", "جاكوب آباد، باكستان", 28.28, 68.44),
        new("Maracaibo, Venezuela", "ماراكايبو، فنزويلا", 10.65, -71.64),
        new("Marble Bar, Australia", "ماربل بار، أستراليا", -21.17, 119.75),
        new("Alice Springs, Australia", "أليس سبرينغز، أستراليا", -23.7, 133.88),
        new("Oodnadatta, Australia", "أودناداتا، أستراليا", -27.55, 135.45),
        new("Santiago del Estero, Argentina", "سانتياغو ديل إستيرو، الأرجنتين", -27.78, -64.26),
        new("Kimberley, South Africa", "كيمبرلي، جنوب أفريقيا", -28.74, 24.77),
        // Usually the coldest.
        new("Vostok Station, Antarctica", "محطة فوستوك، القارة القطبية الجنوبية", -78.46, 106.84),
        new("Dome Fuji, Antarctica", "دوم فوجي، القارة القطبية الجنوبية", -77.32, 39.7),
        new("South Pole, Antarctica", "القطب الجنوبي، القارة القطبية الجنوبية", -89.99, 0),
        new("Oymyakon, Russia", "أويمياكون، روسيا", 63.46, 142.79),
        new("Verkhoyansk, Russia", "فيرخويانسك، روسيا", 67.55, 133.39),
        new("Yakutsk, Russia", "ياكوتسك، روسيا", 62.03, 129.73),
        new("Norilsk, Russia", "نوريلسك، روسيا", 69.35, 88.2),
        new("Summit Camp, Greenland", "معسكر القمة، غرينلاند", 72.58, -38.46),
        new("Eureka, Canada", "يوريكا، كندا", 79.99, -85.93),
        new("Alert, Canada", "ألرت، كندا", 82.5, -62.35),
        new("Utqiagvik, United States", "أوتكياغفيك، الولايات المتحدة", 71.29, -156.79),
        new("Ulaanbaatar, Mongolia", "أولان باتور، منغوليا", 47.89, 106.91),
    ];

    public static WatchedSpot Spot(int index) => Spots[Math.Clamp(index, 0, Spots.Length - 1)];
}
