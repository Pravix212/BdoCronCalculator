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
        Assert.True(GrindSpotDatabase.AllSpots.Count >= 30);

        foreach (var spot in GrindSpotDatabase.AllSpots)
        {
            Assert.False(string.IsNullOrWhiteSpace(spot.Name));
            Assert.False(string.IsNullOrWhiteSpace(spot.Region));
            Assert.True(spot.TrashPrice > 0, $"Spot {spot.Name} must have a positive trash price.");
        }
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
