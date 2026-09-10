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
}
