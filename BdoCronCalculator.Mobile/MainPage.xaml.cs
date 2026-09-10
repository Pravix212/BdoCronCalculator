using Microsoft.Maui.Devices;

namespace BdoCronCalculator.Mobile;

public partial class MainPage : ContentPage
{
    private readonly CalculatorEngine _engine = new();

    public MainPage()
    {
        InitializeComponent();
        UpdateUI();
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
