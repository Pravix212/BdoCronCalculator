using System;
using System.Collections.Generic;

namespace BdoCronCalculator;

public class HammerTarget
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal RequiredCrons { get; set; }
    public string Description { get; set; } = string.Empty;

    public string DisplayName => $"{Name} ({RequiredCrons:N0} Crons)";
}

public class HammerComparisonResult
{
    public HammerTarget Target { get; set; } = new();
    public decimal HammerMarketPrice { get; set; }
    public decimal VendorCronPrice { get; set; } = 3_000_000m;
    public decimal OutfitCronPrice { get; set; } = 2_180_000m;

    public decimal VendorCronCost => Target.RequiredCrons * VendorCronPrice;
    public decimal OutfitCronCost => Target.RequiredCrons * OutfitCronPrice;

    public decimal SavingsVsVendor => VendorCronCost - HammerMarketPrice;
    public decimal SavingsVsOutfit => OutfitCronCost - HammerMarketPrice;

    public string Recommendation
    {
        get
        {
            if (HammerMarketPrice <= 0)
                return "Enter a valid Hammer Market price.";

            if (HammerMarketPrice < OutfitCronCost)
            {
                decimal saved = OutfitCronCost - HammerMarketPrice;
                return $"✅ Use Hammer! Cheaper than Outfits by {CalculatorEngine.FormatSilverSummary(saved)}.";
            }
            if (HammerMarketPrice < VendorCronCost)
            {
                decimal saved = VendorCronCost - HammerMarketPrice;
                return $"⚡ Hammer saves {CalculatorEngine.FormatSilverSummary(saved)} vs Vendor Crons (but Outfits are cheaper).";
            }
            decimal loss = HammerMarketPrice - OutfitCronCost;
            return $"❌ Use Crons! Hammer is more expensive by {CalculatorEngine.FormatSilverSummary(loss)}.";
        }
    }
}

public static class HammerComparisonEngine
{
    public const decimal DefaultHammerPrice = 27_500_000_000m; // ~27.5B market price for Hammer of Precision

    public static readonly List<HammerTarget> Targets = new()
    {
        new HammerTarget { Name = "PEN Deboreka (V)", Category = "Accessory (Precision)", RequiredCrons = 11_500, Description = "Hammer of Precision" },
        new HammerTarget { Name = "TET Deboreka (IV)", Category = "Accessory (Precision)", RequiredCrons = 4_000, Description = "Hammer of Precision" },
        new HammerTarget { Name = "TRI Deboreka (III)", Category = "Accessory (Precision)", RequiredCrons = 1_200, Description = "Hammer of Precision" },

        new HammerTarget { Name = "PEN Sovereign Weapon (X)", Category = "Sovereign Weapon", RequiredCrons = 14_500, Description = "Ancient Hammer / J's Hammer" },
        new HammerTarget { Name = "TET Sovereign Weapon (IX)", Category = "Sovereign Weapon", RequiredCrons = 5_200, Description = "Ancient Hammer / J's Hammer" },
        new HammerTarget { Name = "TRI Sovereign Weapon (VIII)", Category = "Sovereign Weapon", RequiredCrons = 1_800, Description = "Ancient Hammer / J's Hammer" },

        new HammerTarget { Name = "PEN Slumbering Origin Armor (V)", Category = "Fallen God / Primordial", RequiredCrons = 11_500, Description = "Fallen God / Labreska / Dahn / Ator" },
        new HammerTarget { Name = "TET Slumbering Origin Armor (IV)", Category = "Fallen God / Primordial", RequiredCrons = 3_800, Description = "Fallen God / Labreska / Dahn / Ator" },
        new HammerTarget { Name = "TRI Slumbering Origin Armor (III)", Category = "Fallen God / Primordial", RequiredCrons = 1_100, Description = "Fallen God / Labreska / Dahn / Ator" },

        new HammerTarget { Name = "PEN Blackstar Weapon / Armor (V)", Category = "Blackstar", RequiredCrons = 3_670, Description = "J's Hammer of Loyalty" },
        new HammerTarget { Name = "TET Blackstar Weapon / Armor (IV)", Category = "Blackstar", RequiredCrons = 611, Description = "J's Hammer of Loyalty" }
    };

    public static HammerComparisonResult Compare(HammerTarget target, decimal hammerPrice, decimal vendorCronPrice = 3_000_000m, decimal outfitCronPrice = 2_180_000m)
    {
        return new HammerComparisonResult
        {
            Target = target,
            HammerMarketPrice = hammerPrice,
            VendorCronPrice = vendorCronPrice,
            OutfitCronPrice = outfitCronPrice
        };
    }
}
