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

    public string DisplayName => $"{Name} ({TrashPrice:N0}s)";
}

public static class GrindSpotDatabase
{
    public static readonly List<GrindSpot> AllSpots = new()
    {
        // --- Ulukita (High-End End-Game) ---
        new GrindSpot { Name = "Dehkia Ash Forest", Region = "Ulukita / Dehkia", TrashPrice = 32_500, RecommendedApDp = "310 AP / 410 DP" },
        new GrindSpot { Name = "Dehkia Olun's Valley", Region = "Ulukita / Dehkia", TrashPrice = 31_200, RecommendedApDp = "310 AP / 400 DP" },
        new GrindSpot { Name = "Tungrad Ruins", Region = "Ulukita", TrashPrice = 30_500, RecommendedApDp = "320 AP / 420 DP" },
        new GrindSpot { Name = "Yzrahid Highlands", Region = "Ulukita", TrashPrice = 29_400, RecommendedApDp = "310 AP / 420 DP" },
        new GrindSpot { Name = "Darkseeker's Retreat", Region = "Ulukita", TrashPrice = 28_150, RecommendedApDp = "310 AP / 410 DP" },
        new GrindSpot { Name = "City of the Dead", Region = "Ulukita", TrashPrice = 26_300, RecommendedApDp = "300 AP / 400 DP" },

        // --- Dehkia Tier 1 & 2 ---
        new GrindSpot { Name = "Dehkia Thornwood Forest", Region = "Dehkia", TrashPrice = 28_000, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Dehkia Tunkuta (Turos)", Region = "Dehkia", TrashPrice = 27_500, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Dehkia Hystria Ruins", Region = "Dehkia", TrashPrice = 26_000, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Dehkia Aakman Temple", Region = "Dehkia", TrashPrice = 25_500, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Dehkia Cyclops Land", Region = "Dehkia", TrashPrice = 27_000, RecommendedApDp = "300 AP / 390 DP" },

        // --- Calpheon Elvia ---
        new GrindSpot { Name = "Elvia Hexe Sanctuary", Region = "Calpheon Elvia", TrashPrice = 24_200, RecommendedApDp = "290 AP / 380 DP" },
        new GrindSpot { Name = "Elvia Quint Hill (Trolls)", Region = "Calpheon Elvia", TrashPrice = 25_000, RecommendedApDp = "300 AP / 390 DP" },
        new GrindSpot { Name = "Elvia Saunil Camp", Region = "Calpheon Elvia", TrashPrice = 20_500, RecommendedApDp = "270 AP / 340 DP" },
        new GrindSpot { Name = "Elvia Giants Post", Region = "Calpheon Elvia", TrashPrice = 22_100, RecommendedApDp = "280 AP / 360 DP" },
        new GrindSpot { Name = "Elvia Rhutum Outstation", Region = "Calpheon Elvia", TrashPrice = 21_000, RecommendedApDp = "270 AP / 340 DP" },

        // --- Serendia Elvia ---
        new GrindSpot { Name = "Elvia Orc Camp", Region = "Serendia Elvia", TrashPrice = 18_500, RecommendedApDp = "270 AP / 330 DP" },
        new GrindSpot { Name = "Elvia Bloody Monastery", Region = "Serendia Elvia", TrashPrice = 18_500, RecommendedApDp = "280 AP / 340 DP" },
        new GrindSpot { Name = "Elvia Fogans", Region = "Serendia Elvia", TrashPrice = 16_000, RecommendedApDp = "260 AP / 310 DP" },
        new GrindSpot { Name = "Elvia Nagas", Region = "Serendia Elvia", TrashPrice = 16_000, RecommendedApDp = "260 AP / 310 DP" },
        new GrindSpot { Name = "Elvia Biraghi Den", Region = "Serendia Elvia", TrashPrice = 15_000, RecommendedApDp = "250 AP / 300 DP" },
        new GrindSpot { Name = "Elvia Castle Ruins", Region = "Serendia Elvia", TrashPrice = 17_500, RecommendedApDp = "270 AP / 320 DP" },

        // --- Kamasylvia & O'dyllita & Ocean ---
        new GrindSpot { Name = "Crypt of Resting Thoughts", Region = "O'dyllita", TrashPrice = 30_500, RecommendedApDp = "310 AP / 400 DP" },
        new GrindSpot { Name = "Gyfin Rhasia (Underground)", Region = "Kamasylvia", TrashPrice = 21_500, RecommendedApDp = "290 AP / 370 DP" },
        new GrindSpot { Name = "Olun's Valley (Standard)", Region = "O'dyllita", TrashPrice = 22_000, RecommendedApDp = "290 AP / 380 DP" },
        new GrindSpot { Name = "Thornwood Forest", Region = "O'dyllita", TrashPrice = 15_800, RecommendedApDp = "250 AP / 310 DP" },
        new GrindSpot { Name = "Tunkuta (Turos)", Region = "O'dyllita", TrashPrice = 16_500, RecommendedApDp = "270 AP / 330 DP" },
        new GrindSpot { Name = "Ash Forest (Standard)", Region = "Kamasylvia", TrashPrice = 23_000, RecommendedApDp = "290 AP / 370 DP" },
        new GrindSpot { Name = "Sycraia Underwater (Abyssal)", Region = "Ocean", TrashPrice = 18_000, RecommendedApDp = "270 AP / 330 DP" },
        new GrindSpot { Name = "Kratuga Ancient Ruins", Region = "Mediah", TrashPrice = 8_000, RecommendedApDp = "250 AP / 300 DP" },
        new GrindSpot { Name = "Mirumok Ruins", Region = "Kamasylvia", TrashPrice = 17_500, RecommendedApDp = "240 AP / 290 DP" },
        new GrindSpot { Name = "Stars End", Region = "Calpheon", TrashPrice = 15_500, RecommendedApDp = "260 AP / 320 DP" },

        // --- Mountain of Eternal Winter & Drieghan ---
        new GrindSpot { Name = "Jade Starlight Forest", Region = "Eternal Winter", TrashPrice = 18_700, RecommendedApDp = "280 AP / 350 DP" },
        new GrindSpot { Name = "Murrowak's Labyrinth", Region = "Eternal Winter", TrashPrice = 17_800, RecommendedApDp = "270 AP / 340 DP" },
        new GrindSpot { Name = "Blood Wolf Settlement", Region = "Drieghan", TrashPrice = 3_800, RecommendedApDp = "190 AP / 210 DP" },
        new GrindSpot { Name = "Sherekhan Necropolis", Region = "Drieghan", TrashPrice = 4_500, RecommendedApDp = "210 AP / 240 DP" },

        // --- Valencia & Early Mid-Game ---
        new GrindSpot { Name = "Centaurs", Region = "Valencia", TrashPrice = 21_410, RecommendedApDp = "200 AP / 230 DP" },
        new GrindSpot { Name = "Aakman Temple (Standard)", Region = "Valencia", TrashPrice = 12_500, RecommendedApDp = "240 AP / 290 DP" },
        new GrindSpot { Name = "Hystria Ruins (Standard)", Region = "Valencia", TrashPrice = 15_000, RecommendedApDp = "250 AP / 300 DP" },
        new GrindSpot { Name = "Crescent Shrine", Region = "Valencia", TrashPrice = 10_500, RecommendedApDp = "200 AP / 230 DP" },
        new GrindSpot { Name = "Sulfur Mine", Region = "Valencia", TrashPrice = 14_000, RecommendedApDp = "210 AP / 240 DP" },
        new GrindSpot { Name = "Pila Ku Jail", Region = "Valencia", TrashPrice = 13_500, RecommendedApDp = "210 AP / 240 DP" }
    };

    public static decimal CalculateTrashSilver(decimal trashCount, decimal unitPrice)
    {
        return Math.Max(0m, checked(trashCount * unitPrice));
    }
}
