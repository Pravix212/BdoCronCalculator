using System;
using System.Collections.Generic;
using System.Linq;

namespace BdoCronCalculator;

public class GrindSpot
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal TrashPrice { get; set; }
    public string RecommendedApDp { get; set; } = string.Empty;

    public string DisplayName => $"{Name} — {TrashPrice:N0}s [{Region}]";
}

public static class GrindSpotDatabase
{
    public static readonly List<GrindSpot> AllSpots = new()
    {
        // ==========================================
        // --- Edania & Demon Realm Zones (Verified) ---
        // ==========================================
        new GrindSpot { Name = "Event Horizon", Region = "Edania", TrashPrice = 196_501, RecommendedApDp = "360 AP / 440 DP" },
        new GrindSpot { Name = "Scales of Judgment", Region = "Edania (3-Party)", TrashPrice = 186_458, RecommendedApDp = "350 AP / 430 DP" },
        new GrindSpot { Name = "Aresion Temple", Region = "Edania", TrashPrice = 182_049, RecommendedApDp = "360 AP / 440 DP" },
        new GrindSpot { Name = "Magaia Temple", Region = "Edania", TrashPrice = 181_042, RecommendedApDp = "350 AP / 430 DP" },
        new GrindSpot { Name = "Gavinya Coastal Cliff", Region = "Edania / Gavinya", TrashPrice = 165_508, RecommendedApDp = "350 AP / 430 DP" },
        new GrindSpot { Name = "Hermesia Inner Castle", Region = "Edania", TrashPrice = 160_539, RecommendedApDp = "340 AP / 430 DP" },
        new GrindSpot { Name = "Aphrodon Temple", Region = "Edania", TrashPrice = 155_127, RecommendedApDp = "330 AP / 420 DP" },
        new GrindSpot { Name = "Tenebraum Castle", Region = "Edania", TrashPrice = 147_630, RecommendedApDp = "340 AP / 430 DP" },
        new GrindSpot { Name = "Orbita Castle", Region = "Edania", TrashPrice = 140_600, RecommendedApDp = "330 AP / 420 DP" },
        new GrindSpot { Name = "Zephyros Castle", Region = "Edania", TrashPrice = 126_980, RecommendedApDp = "320 AP / 420 DP" },
        new GrindSpot { Name = "Nymphamaré Castle", Region = "Edania", TrashPrice = 116_200, RecommendedApDp = "320 AP / 410 DP" },
        new GrindSpot { Name = "Aetherion Castle", Region = "Edania", TrashPrice = 105_640, RecommendedApDp = "320 AP / 410 DP" },
        new GrindSpot { Name = "Dark Energy Floodlands (Zephyros)", Region = "Edania", TrashPrice = 100_507, RecommendedApDp = "310 AP / 400 DP" },
        new GrindSpot { Name = "Dark Energy Floodlands (Orbita)", Region = "Edania", TrashPrice = 100_507, RecommendedApDp = "310 AP / 400 DP" },
        new GrindSpot { Name = "Dark Energy Floodlands (Great Red Spot)", Region = "Edania", TrashPrice = 100_507, RecommendedApDp = "310 AP / 400 DP" },
        new GrindSpot { Name = "Orzekea", Region = "Edania / O'dyllita", TrashPrice = 96_040, RecommendedApDp = "310 AP / 400 DP" },

        // ==========================================
        // --- High-End & Dehkia Zones (Verified) ---
        // ==========================================
        new GrindSpot { Name = "Star's End", Region = "Calpheon", TrashPrice = 155_000, RecommendedApDp = "260 AP / 320 DP" },
        new GrindSpot { Name = "[Dehkia] Gyfin Rhasia Temple (Upper)", Region = "Dehkia / Kamasylvia", TrashPrice = 125_900, RecommendedApDp = "310 AP / 410 DP" },
        new GrindSpot { Name = "Sycraia Ruins Lower Zone (Abyssal)", Region = "Ocean", TrashPrice = 107_900, RecommendedApDp = "280 AP / 340 DP" },
        new GrindSpot { Name = "[Dehkia] Mirumok Ruins", Region = "Dehkia / Kamasylvia", TrashPrice = 101_500, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Ash Forest [Dehkia 2]", Region = "Dehkia II", TrashPrice = 52_500, RecommendedApDp = "330 AP / 430 DP" },
        new GrindSpot { Name = "Tunkuta [Dehkia's Lantern]", Region = "Dehkia / O'dyllita", TrashPrice = 40_000, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Tunkuta (Turos)", Region = "O'dyllita", TrashPrice = 18_000, RecommendedApDp = "270 AP / 330 DP" },
        new GrindSpot { Name = "Bumblin' Buccaneers", Region = "Olvia Academy", TrashPrice = 13_470, RecommendedApDp = "250 AP / 310 DP" }
    };

    public static decimal CalculateTrashSilver(decimal trashCount, decimal unitPrice)
    {
        return Math.Max(0m, checked(trashCount * unitPrice));
    }
}
