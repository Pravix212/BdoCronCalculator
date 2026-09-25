using BdoCronCalculator;
using System.Linq;
using Xunit;

namespace GFNWindowMover.Tests;

public sealed class GrindAndHammerTests
{
    [Fact]
    public void GrindSpotDatabase_HasValidSpots_NoNegativeOrZeroPrices()
    {
        Assert.NotEmpty(GrindSpotDatabase.AllSpots);
        Assert.True(GrindSpotDatabase.AllSpots.Count >= 20, $"Database has {GrindSpotDatabase.AllSpots.Count} spots, expected >= 20");

        foreach (var spot in GrindSpotDatabase.AllSpots)
        {
            Assert.False(string.IsNullOrWhiteSpace(spot.Name));
            Assert.False(string.IsNullOrWhiteSpace(spot.Region));
            Assert.True(spot.TrashPrice > 0, $"Spot {spot.Name} must have a positive trash price.");
        }
    }

    [Theory]
    [InlineData("tunkuta", 2)] // Tunkuta, Tunkuta [Dehkia's Lantern]
    [InlineData("dehkia", 4)] // Ash Forest [Dehkia 2], [Dehkia] Gyfin Rhasia, [Dehkia] Mirumok, Tunkuta [Dehkia's Lantern]
    [InlineData("star's end", 1)] // Star's End
    [InlineData("sycraia", 1)] // Sycraia Ruins Lower Zone (Abyssal)
    [InlineData("buccaneers", 1)] // Olvia Academy Bumblin' Buccaneers
    [InlineData("edania", 16)] // All 16 Edania spots
    public void GrindSpotDatabase_SearchFiltering_ReturnsExpectedMatches(string query, int minimumMatches)
    {
        var filtered = GrindSpotDatabase.AllSpots
            .Where(s => s.Name.ToLowerInvariant().Contains(query) || s.Region.ToLowerInvariant().Contains(query))
            .ToList();

        Assert.True(filtered.Count >= minimumMatches, $"Query '{query}' returned {filtered.Count} matches, expected at least {minimumMatches}");
    }

    [Fact]
    public void GrindSpotDatabase_VerifiedSpots_PricesAreAccurate()
    {
        var buccaneers = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Bumblin' Buccaneers"));
        Assert.NotNull(buccaneers);
        Assert.Equal(13_470m, buccaneers.TrashPrice);

        var aetherion = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Aetherion"));
        Assert.NotNull(aetherion);
        Assert.Equal(105_640m, aetherion.TrashPrice);

        var aphrodon = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Aphrodon"));
        Assert.NotNull(aphrodon);
        Assert.Equal(155_127m, aphrodon.TrashPrice);

        var eventHorizon = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Event Horizon"));
        Assert.NotNull(eventHorizon);
        Assert.Equal(196_501m, eventHorizon.TrashPrice);

        var scales = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Scales of Judgment"));
        Assert.NotNull(scales);
        Assert.Equal(186_458m, scales.TrashPrice);

        var aresion = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Aresion"));
        Assert.NotNull(aresion);
        Assert.Equal(182_049m, aresion.TrashPrice);

        var magaia = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Magaia"));
        Assert.NotNull(magaia);
        Assert.Equal(181_042m, magaia.TrashPrice);

        var gavinya = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Gavinya Coastal Cliff"));
        Assert.NotNull(gavinya);
        Assert.Equal(165_508m, gavinya.TrashPrice);

        var starsEnd = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Star's End"));
        Assert.NotNull(starsEnd);
        Assert.Equal(155_000m, starsEnd.TrashPrice);

        var sycraia = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Sycraia Ruins Lower Zone"));
        Assert.NotNull(sycraia);
        Assert.Equal(107_900m, sycraia.TrashPrice);

        var dehkiaAsh2 = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Ash Forest [Dehkia 2]"));
        Assert.NotNull(dehkiaAsh2);
        Assert.Equal(52_500m, dehkiaAsh2.TrashPrice);

        var dehkiaTunkuta = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name.Contains("Tunkuta [Dehkia's Lantern]"));
        Assert.NotNull(dehkiaTunkuta);
        Assert.Equal(40_000m, dehkiaTunkuta.TrashPrice);

        var tunkuta = GrindSpotDatabase.AllSpots.FirstOrDefault(s => s.Name == "Tunkuta (Turos)");
        Assert.NotNull(tunkuta);
        Assert.Equal(18_000m, tunkuta.TrashPrice);
    }

    [Fact]
    public void GrindSpotDatabase_CalculateTrashSilver_AccuratelyComputesHighVolume()
    {
        // Dehkia Ash Forest (32,500 per piece) with 30,000 trash
        decimal trashCount = 30_000m;
        decimal unitPrice = 32_500m;
        decimal expectedSilver = 975_000_000m;

        decimal total = GrindSpotDatabase.CalculateTrashSilver(trashCount, unitPrice);
        Assert.Equal(expectedSilver, total);
        Assert.Contains("975.00 Million Silver", CalculatorEngine.FormatSilverSummary(total));
    }

    [Fact]
    public void HammerComparisonEngine_ContainsMajorEnhancementTargets()
    {
        Assert.NotEmpty(HammerComparisonEngine.Targets);

        var deboV = HammerComparisonEngine.Targets.FirstOrDefault(t => t.Name.Contains("PEN Deboreka"));
        Assert.NotNull(deboV);
        Assert.Equal(11_500m, deboV.RequiredCrons);

        var sovV = HammerComparisonEngine.Targets.FirstOrDefault(t => t.Name.Contains("PEN Sovereign"));
        Assert.NotNull(sovV);
        Assert.Equal(14_500m, sovV.RequiredCrons);

        var fgV = HammerComparisonEngine.Targets.FirstOrDefault(t => t.Name.Contains("PEN Slumbering Origin"));
        Assert.NotNull(fgV);
        Assert.Equal(11_500m, fgV.RequiredCrons);

        var bsV = HammerComparisonEngine.Targets.FirstOrDefault(t => t.Name.Contains("PEN Blackstar"));
        Assert.NotNull(bsV);
        Assert.Equal(3_670m, bsV.RequiredCrons);
    }

    [Fact]
    public void HammerComparison_PenDeboreka_CalculatesCostsAndSavings()
    {
        var deboV = HammerComparisonEngine.Targets.First(t => t.Name.Contains("PEN Deboreka"));
        decimal hammerPrice = 27_500_000_000m; // 27.5B

        var result = HammerComparisonEngine.Compare(deboV, hammerPrice);

        // 11,500 * 3,000,000 = 34,500,000,000 (34.5B)
        Assert.Equal(34_500_000_000m, result.VendorCronCost);

        // 11,500 * 2,180,000 = 25,070,000,000 (25.07B)
        Assert.Equal(25_070_000_000m, result.OutfitCronCost);

        // Savings vs vendor = 34.5B - 27.5B = 7.0B
        Assert.Equal(7_000_000_000m, result.SavingsVsVendor);

        // Hammer is between 25.07B and 34.5B -> Vendor savings notice
        Assert.Contains("Hammer saves", result.Recommendation);
    }

    [Fact]
    public void HammerComparison_WhenHammerCheaperThanOutfits_RecommendsHammer()
    {
        var deboV = HammerComparisonEngine.Targets.First(t => t.Name.Contains("PEN Deboreka"));
        decimal cheapHammerPrice = 22_000_000_000m; // 22.0B

        var result = HammerComparisonEngine.Compare(deboV, cheapHammerPrice);

        Assert.Contains("✅ Use Hammer!", result.Recommendation);
    }

    [Fact]
    public void HammerComparison_WhenCronsCheaperThanHammer_RecommendsCrons()
    {
        var deboIV = HammerComparisonEngine.Targets.First(t => t.Name.Contains("TET Deboreka")); // 4,000 Crons = 8.72B Outfit, 12B Vendor
        decimal expensiveHammerPrice = 27_500_000_000m;

        var result = HammerComparisonEngine.Compare(deboIV, expensiveHammerPrice);

        Assert.Contains("❌ Use Crons!", result.Recommendation);
    }

    [Fact]
    public void CalculatorEngine_SetCurrentValue_UpdatesStateCleanly()
    {
        var engine = new CalculatorEngine();
        engine.SetCurrentValue(1_250_000_000m, "Dehkia Ash Forest (30k Trash) =");

        Assert.Equal(1_250_000_000m, engine.CurrentValue);
        Assert.Equal("1,250,000,000", engine.FormattedDisplay);
        Assert.Equal("Dehkia Ash Forest (30k Trash) =", engine.ExpressionTape);
        Assert.Contains("1.25 Billion Silver", engine.SilverSummary);
    }
}
