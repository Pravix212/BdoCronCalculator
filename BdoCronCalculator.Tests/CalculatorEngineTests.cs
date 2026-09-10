using BdoCronCalculator;
using Xunit;

namespace GFNWindowMover.Tests;

public sealed class CalculatorEngineTests
{
    [Fact]
    public void VendorCronCalculation_MultipliesBy3Million()
    {
        var engine = new CalculatorEngine();
        // Input 1350
        engine.InputDigit('1');
        engine.InputDigit('3');
        engine.InputDigit('5');
        engine.InputDigit('0');

        Assert.Equal(1350m, engine.CurrentValue);

        // Click Cron Button
        engine.CalculateCronCost(CalculatorEngine.DefaultVendorCronPrice);

        Assert.Equal(4_050_000_000m, engine.CurrentValue);
        Assert.Equal("4,050,000,000", engine.FormattedDisplay);
        Assert.Contains("4.05 Billion Silver", engine.SilverSummary);
    }

    [Fact]
    public void OutfitCronCalculation_MultipliesBy2Point18Million()
    {
        var engine = new CalculatorEngine();
        engine.InputDigit('1');
        engine.InputDigit('3');
        engine.InputDigit('5');
        engine.InputDigit('0');

        engine.CalculateCronCost(CalculatorEngine.OutfitExtractionCronPrice);

        Assert.Equal(2_943_000_000m, engine.CurrentValue);
        Assert.Contains("2.94 Billion Silver", engine.SilverSummary);
    }

    [Fact]
    public void StandardArithmetic_AdditionAndMultiplication()
    {
        var engine = new CalculatorEngine();
        engine.InputDigit('5');
        engine.InputDigit('0');
        engine.InputDigit('0');

        engine.InputOperator("+");

        engine.InputDigit('2');
        engine.InputDigit('5');
        engine.InputDigit('0');

        engine.Calculate();

        Assert.Equal(750m, engine.CurrentValue);
    }

    [Fact]
    public void QuickMultipliers_K_M_B_WorkAccurately()
    {
        var engine = new CalculatorEngine();
        engine.InputDigit('5');

        engine.MultiplyByThousand(1_000_000); // 5 Million
        Assert.Equal(5_000_000m, engine.CurrentValue);
        Assert.Contains("5.00 Million Silver", engine.SilverSummary);

        engine.MultiplyByThousand(1_000); // 5 Billion
        Assert.Equal(5_000_000_000m, engine.CurrentValue);
        Assert.Contains("5.00 Billion Silver", engine.SilverSummary);
    }

    [Fact]
    public void BackspaceAndClear_FunctionProperly()
    {
        var engine = new CalculatorEngine();
        engine.InputDigit('1');
        engine.InputDigit('2');
        engine.InputDigit('3');

        engine.Backspace();
        Assert.Equal(12m, engine.CurrentValue);

        engine.Clear();
        Assert.Equal(0m, engine.CurrentValue);
        Assert.Equal("0", engine.FormattedDisplay);
    }

    [Theory]
    [InlineData(4_050_000_000, "4.05 Billion Silver (4,050 M)")]
    [InlineData(850_000_000, "850.00 Million Silver (850M)")]
    [InlineData(1_500_000_000_000, "1.50 Trillion Silver (1,500 B)")]
    [InlineData(50_000, "50.0k Silver (50,000)")]
    [InlineData(0, "0 Silver")]
    public void FormatSilverSummary_FormatsCorrectUnits(decimal value, string expected)
    {
        string formatted = CalculatorEngine.FormatSilverSummary(value);
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void MarketTaxCalculation_MatchesGarmothBenchmark()
    {
        // Garmoth test case: 5 x 280,000,000 = 1,400,000,000 gross
        // Settings: Value Pack (Yes), Rich Merchant Ring (No), Fame = 11,406 (+1.5%)
        // Effective payout = 0.65 * (1 + 0.30 + 0.015) = 0.65 * 1.315 = 0.85475 (85.475%)
        // Net profit = 1,400,000,000 * 0.85475 = 1,196,650,000
        // Market tax = 1,400,000,000 - 1,196,650,000 = 203,350,000
        var engine = new CalculatorEngine();
        engine.TaxSettings = new MarketTaxSettings
        {
            HasValuePack = true,
            HasMerchantRing = false,
            FamilyFame = 11406
        };

        // Input 1,400,000,000
        engine.InputDigit('1');
        engine.InputDigit('4');
        engine.MultiplyByThousand(100_000_000); // 1,400,000,000

        Assert.Equal(1_400_000_000m, engine.CurrentValue);

        // Click Tax Button
        engine.CalculateMarketTax();

        Assert.Equal(1_196_650_000m, engine.CurrentValue);
        Assert.Contains("1.20 Billion Silver", engine.SilverSummary);
        Assert.Equal("1,196,650,000", engine.FormattedDisplay);
        Assert.Contains("14.53%", engine.ExpressionTape);
    }

    [Fact]
    public void MarketTaxSettings_TierRatesAreAccurate()
    {
        var settings = new MarketTaxSettings();

        // Base no buffs
        settings.HasValuePack = false;
        settings.HasMerchantRing = false;
        settings.FamilyFame = 500;
        Assert.Equal(0.65m, settings.EffectivePayoutRate);
        Assert.Equal(0.35m, settings.EffectiveTaxRate);

        // Tier 1 fame: 1000 - 3999 (+0.5%)
        settings.FamilyFame = 2500;
        Assert.Equal(0.005m, settings.GetFameBonusRate());
        Assert.Equal(0.65m * 1.005m, settings.EffectivePayoutRate);

        // Tier 2 fame: 4000 - 6999 (+1.0%)
        settings.FamilyFame = 5000;
        Assert.Equal(0.010m, settings.GetFameBonusRate());

        // Tier 3 fame: >= 7000 (+1.5%) + Value Pack (+30%) + Merchant Ring (+5%)
        settings.FamilyFame = 9000;
        settings.HasValuePack = true;
        settings.HasMerchantRing = true;
        Assert.Equal(0.015m, settings.GetFameBonusRate());
        Assert.Equal(0.365m, settings.TaxBonusRate);
        Assert.Equal(0.65m * 1.365m, settings.EffectivePayoutRate); // 88.725%
        Assert.Equal(0.11275m, settings.EffectiveTaxRate); // 11.275%
    }

    [Fact]
    public void Arithmetic_WhenOverflows_SetsOverflowMessageWithoutCrashing()
    {
        var engine = new CalculatorEngine();
        engine.InputDigit('1');
        for (int i = 0; i < 15; i++)
        {
            engine.InputDigit('0');
        }
        engine.MultiplyByThousand(1_000_000_000); // 10^24
        engine.MultiplyByThousand(1_000_000_000); // Exceeds decimal.MaxValue

        Assert.Equal("Overflow (Number too large)", engine.ExpressionTape);
        Assert.Equal(0m, engine.CurrentValue);
    }
}
