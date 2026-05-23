namespace AssistedEcommerce.Api.Constants;

/// <summary>18 degmo ee Muqdisho — zoneId: ZONE-{MAGAC}</summary>
public static class MogadishuDistricts
{
    public static readonly (string En, string So, decimal FeeUsd, int SortOrder)[] All =
    [
        ("Abdicasiis", "Abdicasiis", 5.00m, 1),
        ("Bondhere", "Bondhere", 5.50m, 2),
        ("Dayniile", "Dayniile", 6.00m, 3),
        ("Dharkenley", "Dharkenley", 5.00m, 4),
        ("Hamar-Jajab", "Hamar-Jajab", 4.00m, 5),
        ("Hamar-Weyne", "Hamar-Weyne", 4.00m, 6),
        ("Heliwaa", "Heliwaa", 6.50m, 7),
        ("Hodan", "Hodan", 5.00m, 8),
        ("Howlwadaag", "Howlwadaag", 5.00m, 9),
        ("Karaan", "Karaan", 5.50m, 10),
        ("Kaxda", "Kaxda", 7.00m, 11),
        ("Shangani", "Shangani", 4.50m, 12),
        ("Shibis", "Shibis", 4.50m, 13),
        ("Waberi", "Waberi", 4.50m, 14),
        ("Wadajir", "Wadajir", 5.00m, 15),
        ("Warta Nabadda", "Warta Nabadda", 5.00m, 16),
        ("Wardhiigleey", "Wardhiigleey", 5.00m, 17),
        ("Yaaqshiid", "Yaaqshiid", 5.00m, 18)
    ];

    public static string ToZoneId(string englishName) =>
        $"ZONE-{englishName.ToUpperInvariant().Replace(" ", "-")}";
}
