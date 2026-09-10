using Microsoft.Maui.Devices;

namespace BdoCronCalculator.Mobile;

public partial class MainPage : ContentPage
{
    private readonly CalculatorEngine _engine = new();

    public MainPage()
    {
        InitializeComponent();
        LoadSettingsIntoUI();
        UpdateUI();
    }

    private void LoadSettingsIntoUI()
    {
        var s = _engine.TaxSettings;
        ValuePackSwitch.IsToggled = s.HasValuePack;
        MerchantRingSwitch.IsToggled = s.HasMerchantRing;
        FamilyFameEntry.Text = s.FamilyFame.ToString();
        UpdateSettingsRatesDisplay();
    }

    private void UpdateSettingsRatesDisplay()
    {
        var s = _engine.TaxSettings;
        decimal payoutPct = s.EffectivePayoutRate * 100m;
        decimal taxPct = s.EffectiveTaxRate * 100m;

        MobilePayoutRateLabel.Text = $"{payoutPct:N2}%";
        MobileTaxRateLabel.Text = $"-{taxPct:N2}%";

        if (s.FamilyFame >= 7000)
            MobileFameBonusLabel.Text = "+1.5% (≥ 7,000 Fame)";
        else if (s.FamilyFame >= 4000)
            MobileFameBonusLabel.Text = "+1.0% (4,000 - 6,999 Fame)";
        else if (s.FamilyFame >= 1000)
            MobileFameBonusLabel.Text = "+0.5% (1,000 - 3,999 Fame)";
        else
            MobileFameBonusLabel.Text = "+0.0% (< 1,000 Fame)";
    }

    private void UpdateUI()
    {
        string display = _engine.FormattedDisplay;
        MainDisplayLabel.Text = display;

        if (display.Length > 15)
        {
            MainDisplayLabel.FontSize = 22;
        }
        else if (display.Length > 11)
        {
            MainDisplayLabel.FontSize = 28;
        }
        else
        {
            MainDisplayLabel.FontSize = 34;
        }

        ExpressionTapeLabel.Text = _engine.ExpressionTape;
        SilverSummaryLabel.Text = _engine.SilverSummary;
    }

    private void TriggerHaptic()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Ignore on platforms without haptic support
        }
    }

    #region Settings Management
    private void OnSettingsClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        SettingsOverlay.IsVisible = !SettingsOverlay.IsVisible;
    }

    private void OnCloseSettingsClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        SettingsOverlay.IsVisible = false;
        _engine.TaxSettings.Save();
    }

    private void OnSettingToggled(object sender, ToggledEventArgs e)
    {
        if (ValuePackSwitch == null || MerchantRingSwitch == null) return;

        _engine.TaxSettings.HasValuePack = ValuePackSwitch.IsToggled;
        _engine.TaxSettings.HasMerchantRing = MerchantRingSwitch.IsToggled;
        _engine.TaxSettings.Save();

        UpdateSettingsRatesDisplay();
    }

    private void OnFamilyFameTextChanged(object sender, TextChangedEventArgs e)
    {
        if (FamilyFameEntry == null) return;

        if (int.TryParse(FamilyFameEntry.Text, out int fame) && fame >= 0)
        {
            _engine.TaxSettings.FamilyFame = fame;
            _engine.TaxSettings.Save();
            UpdateSettingsRatesDisplay();
        }
    }
    #endregion

    private void OnDigitClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        if (sender is Button btn && btn.CommandParameter is string param && param.Length > 0)
        {
            _engine.InputDigit(param[0]);
            UpdateUI();
        }
    }

    private void OnOperatorClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        if (sender is Button btn && btn.CommandParameter is string op)
        {
            _engine.InputOperator(op);
            UpdateUI();
        }
    }

    private void OnEqualsClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.Calculate();
        UpdateUI();
    }

    private void OnCronClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.CalculateCronCost(CalculatorEngine.DefaultVendorCronPrice);
        UpdateUI();
    }

    private void OnOutfitCronClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.CalculateCronCost(CalculatorEngine.OutfitExtractionCronPrice);
        UpdateUI();
    }

    private void OnTaxClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.CalculateMarketTax();
        UpdateUI();
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.Clear();
        UpdateUI();
    }

    private void OnClearEntryClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.ClearEntry();
        UpdateUI();
    }

    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.Backspace();
        UpdateUI();
    }

    private void OnNegateClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.Negate();
        UpdateUI();
    }

    private void OnMultiplyKClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.MultiplyByThousand(1_000);
        UpdateUI();
    }

    private void OnMultiplyMClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.MultiplyByThousand(1_000_000);
        UpdateUI();
    }

    private void OnMultiplyBClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        _engine.MultiplyByThousand(1_000_000_000);
        UpdateUI();
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        TriggerHaptic();
        try
        {
            string copyText = $"{_engine.CurrentValue:N0} Silver ({_engine.SilverSummary})";
            await Clipboard.Default.SetTextAsync(copyText);
            SilverSummaryLabel.Text = "✓ Copied to Clipboard!";
        }
        catch
        {
            // Clipboard fallback
        }
    }
}
