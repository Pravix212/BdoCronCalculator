using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BdoCronCalculator;

public partial class MainWindow : Window
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private readonly CalculatorEngine _engine = new();
    private bool _isInitialized = false;
    private bool _isHudMode = false;
    private double _preHudWidth = 350.0;
    private double _preHudHeight = 580.0;
    private UpdateInfo? _pendingUpdate = null;

    public MainWindow()
    {
        InitializeComponent();

        if (_engine.TaxSettings.WindowWidth >= MinWidth)
            Width = _engine.TaxSettings.WindowWidth;
        if (_engine.TaxSettings.WindowHeight >= MinHeight)
            Height = _engine.TaxSettings.WindowHeight;

        SizeChanged += MainWindow_SizeChanged;

        InitializeToolViews();

        _isInitialized = true;
        LoadSettingsIntoUI();
        UpdateUI();
        _ = CheckForAppUpdatesAsync();
    }

    private void InitializeToolViews()
    {
        // 1. Populate Grind Spot ComboBox
        if (GrindSpotComboBox != null)
        {
            GrindSpotComboBox.ItemsSource = GrindSpotDatabase.AllSpots;
            if (GrindSpotDatabase.AllSpots.Count > 0)
            {
                GrindSpotComboBox.SelectedIndex = 0;
            }
        }

        // 2. Populate Hammer Targets ComboBox
        if (HammerTargetComboBox != null)
        {
            HammerTargetComboBox.ItemsSource = HammerComparisonEngine.Targets;
            if (HammerComparisonEngine.Targets.Count > 0)
            {
                HammerTargetComboBox.SelectedIndex = 0;
            }
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var helper = new WindowInteropHelper(this);
        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            int x = lParam.ToInt32() & 0xffff;
            int y = (lParam.ToInt32() >> 16) & 0xffff;

            if (x > 32767) x -= 65536;
            if (y > 32767) y -= 65536;

            var screenPoint = new Point(x, y);
            var clientPoint = PointFromScreen(screenPoint);

            const int resizeBorder = 8;

            bool left = clientPoint.X <= resizeBorder;
            bool right = clientPoint.X >= ActualWidth - resizeBorder;
            bool top = clientPoint.Y <= resizeBorder;
            bool bottom = clientPoint.Y >= ActualHeight - resizeBorder;

            if (top && left) { handled = true; return (IntPtr)HTTOPLEFT; }
            if (top && right) { handled = true; return (IntPtr)HTTOPRIGHT; }
            if (bottom && left) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
            if (bottom && right) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
            if (left) { handled = true; return (IntPtr)HTLEFT; }
            if (right) { handled = true; return (IntPtr)HTRIGHT; }
            if (top) { handled = true; return (IntPtr)HTTOP; }
            if (bottom) { handled = true; return (IntPtr)HTBOTTOM; }
        }

        return IntPtr.Zero;
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isInitialized && !_isHudMode && WindowState == WindowState.Normal)
        {
            _engine.TaxSettings.WindowWidth = ActualWidth;
            _engine.TaxSettings.WindowHeight = ActualHeight;
            _engine.TaxSettings.Save();
        }
    }

    private async Task CheckForAppUpdatesAsync()
    {
        try
        {
            var update = await UpdateService.CheckForUpdatesAsync();
            if (update != null && update.HasUpdate)
            {
                _pendingUpdate = update;
                Dispatcher.Invoke(() =>
                {
                    if (UpdateBanner != null && UpdateBannerText != null)
                    {
                        UpdateBannerText.Text = $"{update.LatestVersion} available!";
                        UpdateBanner.Visibility = Visibility.Visible;
                    }
                });
            }
        }
        catch
        {
            // Silent fallback
        }
    }

    private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate == null) return;

        if (!string.IsNullOrEmpty(_pendingUpdate.ExeDownloadUrl))
        {
            try
            {
                UpdateNowButton.IsEnabled = false;
                UpdateNowButton.Content = "Downloading...";
                await UpdateService.DownloadAndApplyWindowsUpdateAsync(_pendingUpdate.ExeDownloadUrl, progress =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateNowButton.Content = $"{progress}%";
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to apply update automatically:\n{ex.Message}\n\nOpening download page instead.",
                                "Update Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                if (!string.IsNullOrEmpty(_pendingUpdate.ReleaseUrl))
                {
                    Process.Start(new ProcessStartInfo(_pendingUpdate.ReleaseUrl) { UseShellExecute = true });
                }
                UpdateBanner.Visibility = Visibility.Collapsed;
            }
        }
        else if (!string.IsNullOrEmpty(_pendingUpdate.ReleaseUrl))
        {
            Process.Start(new ProcessStartInfo(_pendingUpdate.ReleaseUrl) { UseShellExecute = true });
            UpdateBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void DismissUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (UpdateBanner != null)
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadSettingsIntoUI()
    {
        var s = _engine.TaxSettings;
        if (ValuePackCheckBox != null) ValuePackCheckBox.IsChecked = s.HasValuePack;
        if (MerchantRingCheckBox != null) MerchantRingCheckBox.IsChecked = s.HasMerchantRing;
        if (FamilyFameTextBox != null) FamilyFameTextBox.Text = s.FamilyFame.ToString();

        if (OpacitySlider != null)
        {
            OpacitySlider.Value = s.BackgroundOpacity;
            if (OpacityValueLabel != null)
                OpacityValueLabel.Text = $"{(int)s.BackgroundOpacity}%";
        }

        if (ButtonOpacitySlider != null)
        {
            ButtonOpacitySlider.Value = s.ButtonOpacity;
            if (ButtonOpacityValueLabel != null)
                ButtonOpacityValueLabel.Text = $"{(int)s.ButtonOpacity}%";
        }

        ApplyBackgroundOpacity(s.BackgroundOpacity);
        ApplyButtonOpacity(s.ButtonOpacity);

        var curVer = UpdateService.GetCurrentVersion();
        if (AppVersionLabel != null)
            AppVersionLabel.Text = $" (v{curVer.Major}.{curVer.Minor}.{curVer.Build})";

        UpdateSettingsRatesDisplay();
    }

    public void ApplyBackgroundOpacity(double opacityPercent)
    {
        double factor = Math.Clamp(opacityPercent / 100.0, 0.0, 1.0);
        byte darkAlpha = (byte)(factor * 255);
        byte panelAlpha = (byte)(Math.Clamp(factor * 1.05, 0.0, 1.0) * 255);
        byte displayAlpha = (byte)(Math.Clamp(factor * 0.95, 0.0, 1.0) * 255);

        if (MainOuterBorder != null)
            MainOuterBorder.Background = new SolidColorBrush(Color.FromArgb(darkAlpha, 0x13, 0x15, 0x18));

        if (TitleBarBorder != null)
            TitleBarBorder.Background = new SolidColorBrush(Color.FromArgb(panelAlpha, 0x1c, 0x1f, 0x24));

        if (DisplayBorder != null)
            DisplayBorder.Background = new SolidColorBrush(Color.FromArgb(displayAlpha, 0x10, 0x12, 0x15));

        if (ActionBarBorder != null)
            ActionBarBorder.Background = new SolidColorBrush(Color.FromArgb(panelAlpha, 0x1c, 0x1f, 0x24));

        if (FooterBorder != null)
            FooterBorder.Background = new SolidColorBrush(Color.FromArgb(panelAlpha, 0x1c, 0x1f, 0x24));

        if (HudView != null)
            HudView.Background = new SolidColorBrush(Color.FromArgb(displayAlpha, 0x10, 0x12, 0x15));

        if (GrindOverlay != null)
            GrindOverlay.Background = new SolidColorBrush(Color.FromArgb(Math.Max((byte)230, darkAlpha), 0x13, 0x15, 0x18));

        if (HammerOverlay != null)
            HammerOverlay.Background = new SolidColorBrush(Color.FromArgb(Math.Max((byte)230, darkAlpha), 0x13, 0x15, 0x18));

        if (SettingsOverlay != null)
            SettingsOverlay.Background = new SolidColorBrush(Color.FromArgb(Math.Max((byte)230, darkAlpha), 0x13, 0x15, 0x18));
    }

    public void ApplyButtonOpacity(double opacityPercent)
    {
        double factor = Math.Clamp(opacityPercent / 100.0, 0.0, 1.0);
        byte btnAlpha = (byte)(factor * 255);
        byte hoverAlpha = (byte)Math.Min(255, factor * 255 + 40);
        byte pressAlpha = (byte)Math.Max(0, factor * 255 - 20);

        Application.Current.Resources["BgButton"] = new SolidColorBrush(Color.FromArgb(btnAlpha, 0x25, 0x2a, 0x32));
        Application.Current.Resources["BgButtonHover"] = new SolidColorBrush(Color.FromArgb(hoverAlpha, 0x32, 0x38, 0x42));
        Application.Current.Resources["BgButtonPressed"] = new SolidColorBrush(Color.FromArgb(pressAlpha, 0x1e, 0x22, 0x28));

        Application.Current.Resources["BgOpButton"] = new SolidColorBrush(Color.FromArgb(btnAlpha, 0x20, 0x2b, 0x38));
        Application.Current.Resources["BgOpButtonHover"] = new SolidColorBrush(Color.FromArgb(hoverAlpha, 0x28, 0x39, 0x4a));
        Application.Current.Resources["BgOpButtonPressed"] = new SolidColorBrush(Color.FromArgb(pressAlpha, 0x19, 0x23, 0x2e));

        Application.Current.Resources["BgCronButton"] = new SolidColorBrush(Color.FromArgb(btnAlpha, 0x12, 0x35, 0x45));
        Application.Current.Resources["BgTaxButton"] = new SolidColorBrush(Color.FromArgb(btnAlpha, 0x2d, 0x22, 0x08));
        Application.Current.Resources["BgEqualsButton"] = new SolidColorBrush(Color.FromArgb(btnAlpha, 0x00, 0x83, 0x8f));
    }

    private void UpdateSettingsRatesDisplay()
    {
        if (!_isInitialized) return;

        var s = _engine.TaxSettings;
        decimal payoutPct = s.EffectivePayoutRate * 100m;
        decimal taxPct = s.EffectiveTaxRate * 100m;

        if (SettingsPayoutRateText != null)
            SettingsPayoutRateText.Text = $"{payoutPct:F2}%";

        if (SettingsTaxRateText != null)
            SettingsTaxRateText.Text = $"-{taxPct:F2}%";

        if (FameBonusLabel != null)
        {
            decimal famePct = s.GetFameBonusRate() * 100m;
            FameBonusLabel.Text = $"+{famePct:F1}% ({s.FamilyFame:N0} Fame)";
        }
    }

    private void UpdateUI()
    {
        if (MainDisplayText != null)
            MainDisplayText.Text = _engine.FormattedDisplay;

        if (ExpressionTapeText != null)
            ExpressionTapeText.Text = _engine.ExpressionTape;

        if (SilverSummaryText != null)
            SilverSummaryText.Text = _engine.SilverSummary;

        if (HudDisplayText != null)
            HudDisplayText.Text = _engine.FormattedDisplay;

        if (HudSilverText != null)
            HudSilverText.Text = $"💰 {_engine.SilverSummary}";
    }

    #region Window Title Bar & Resizing
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        if (PinIcon != null)
        {
            PinIcon.Foreground = Topmost ? (Brush)FindResource("AccentCron") : (Brush)FindResource("TextSecondary");
        }
        if (PinButton != null)
        {
            PinButton.ToolTip = Topmost ? "Always on Top (Enabled)" : "Always on Top (Disabled)";
        }
    }

    private void HudButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleHudMode();
    }

    private void ToggleHudMode()
    {
        _isHudMode = !_isHudMode;

        if (_isHudMode)
        {
            // Enter HUD mini mode
            _preHudWidth = ActualWidth;
            _preHudHeight = ActualHeight;

            StandardView.Visibility = Visibility.Collapsed;
            SettingsOverlay.Visibility = Visibility.Collapsed;
            GrindOverlay.Visibility = Visibility.Collapsed;
            HammerOverlay.Visibility = Visibility.Collapsed;
            HudView.Visibility = Visibility.Visible;

            MinHeight = 54;
            Height = 84;
            Width = Math.Max(320, Width * 0.9);
            Topmost = true;
        }
        else
        {
            // Restore standard mode
            HudView.Visibility = Visibility.Collapsed;
            StandardView.Visibility = Visibility.Visible;

            MinHeight = 450;
            Width = _preHudWidth;
            Height = _preHudHeight;
        }

        UpdateUI();
    }

    private void ResizeGripThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newWidth = ActualWidth + e.HorizontalChange;
        double newHeight = ActualHeight + e.VerticalChange;

        if (newWidth >= MinWidth)
            Width = newWidth;
        if (newHeight >= MinHeight)
            Height = newHeight;
    }

    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newWidth = ActualWidth + e.HorizontalChange;
        if (newWidth >= MinWidth) Width = newWidth;
    }

    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newHeight = ActualHeight + e.VerticalChange;
        if (newHeight >= MinHeight) Height = newHeight;
    }

    private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newWidth = ActualWidth - e.HorizontalChange;
        if (newWidth >= MinWidth)
        {
            Left += e.HorizontalChange;
            Width = newWidth;
        }
    }

    private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newHeight = ActualHeight - e.VerticalChange;
        if (newHeight >= MinHeight)
        {
            Top += e.VerticalChange;
            Height = newHeight;
        }
    }

    private void ResizeBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeBottom_DragDelta(sender, e);
        ResizeLeft_DragDelta(sender, e);
    }

    private void ResizeTopRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeTop_DragDelta(sender, e);
        ResizeRight_DragDelta(sender, e);
    }

    private void ResizeTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeTop_DragDelta(sender, e);
        ResizeLeft_DragDelta(sender, e);
    }
    #endregion

    #region Grind Spot Trash Calculator
    private void GrindToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (GrindOverlay == null) return;
        SettingsOverlay.Visibility = Visibility.Collapsed;
        HammerOverlay.Visibility = Visibility.Collapsed;
        GrindOverlay.Visibility = GrindOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (GrindOverlay.Visibility == Visibility.Visible)
        {
            RecalculateGrindLoot();
        }
    }

    private void CloseGrindOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (GrindOverlay != null) GrindOverlay.Visibility = Visibility.Collapsed;
    }

    private void GrindSpotComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GrindSpotComboBox.SelectedItem is GrindSpot spot)
        {
            if (GrindSpotInfoLabel != null)
                GrindSpotInfoLabel.Text = $"Recommended: {spot.RecommendedApDp}";
            if (GrindRegionLabel != null)
                GrindRegionLabel.Text = $"Region: {spot.Region}";
            if (TrashPriceTextBox != null)
                TrashPriceTextBox.Text = spot.TrashPrice.ToString(CultureInfo.InvariantCulture);

            RecalculateGrindLoot();
        }
    }

    private void TrashInputs_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        RecalculateGrindLoot();
    }

    private void AddTrash_5k_Click(object sender, RoutedEventArgs e) => AddTrashAmount(5_000);
    private void AddTrash_10k_Click(object sender, RoutedEventArgs e) => AddTrashAmount(10_000);
    private void AddTrash_20k_Click(object sender, RoutedEventArgs e) => AddTrashAmount(20_000);
    private void AddTrash_50k_Click(object sender, RoutedEventArgs e) => AddTrashAmount(50_000);

    private void ClearTrash_Click(object sender, RoutedEventArgs e)
    {
        if (TrashCountTextBox != null) TrashCountTextBox.Text = "0";
        if (ExtraSilverTextBox != null) ExtraSilverTextBox.Text = "0";
        RecalculateGrindLoot();
    }

    private void AddTrashAmount(decimal amount)
    {
        if (TrashCountTextBox == null) return;
        if (decimal.TryParse(TrashCountTextBox.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cur))
        {
            TrashCountTextBox.Text = (cur + amount).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            TrashCountTextBox.Text = amount.ToString(CultureInfo.InvariantCulture);
        }
        RecalculateGrindLoot();
    }

    private void RecalculateGrindLoot()
    {
        if (TrashCountTextBox == null || TrashPriceTextBox == null || GrindTotalSilverText == null) return;

        decimal.TryParse(TrashCountTextBox.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal trashCount);
        decimal.TryParse(TrashPriceTextBox.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice);
        decimal.TryParse(ExtraSilverTextBox?.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal extraSilver);

        decimal trashSilver = GrindSpotDatabase.CalculateTrashSilver(trashCount, unitPrice);
        decimal totalSilver = trashSilver + Math.Max(0m, extraSilver);

        GrindTotalSilverText.Text = $"{totalSilver:N0} Silver";
        if (GrindFormattedSummaryText != null)
        {
            string detail = extraSilver > 0 ? $" (Trash: {trashSilver:N0} + Extra: {extraSilver:N0})" : "";
            GrindFormattedSummaryText.Text = $"{CalculatorEngine.FormatSilverSummary(totalSilver)}{detail}";
        }
    }

    private void SendGrindToCalc_Click(object sender, RoutedEventArgs e)
    {
        decimal.TryParse(TrashCountTextBox?.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal trashCount);
        decimal.TryParse(TrashPriceTextBox?.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice);
        decimal.TryParse(ExtraSilverTextBox?.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal extraSilver);

        decimal totalSilver = GrindSpotDatabase.CalculateTrashSilver(trashCount, unitPrice) + Math.Max(0m, extraSilver);

        string spotName = (GrindSpotComboBox?.SelectedItem as GrindSpot)?.Name ?? "Grind Loot";
        _engine.SetCurrentValue(totalSilver, $"{spotName} ({trashCount:N0} Trash) =");

        if (GrindOverlay != null) GrindOverlay.Visibility = Visibility.Collapsed;
        UpdateUI();
    }
    #endregion

    #region Hammer vs. Cron Tool
    private void HammerToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (HammerOverlay == null) return;
        SettingsOverlay.Visibility = Visibility.Collapsed;
        GrindOverlay.Visibility = Visibility.Collapsed;
        HammerOverlay.Visibility = HammerOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (HammerOverlay.Visibility == Visibility.Visible)
        {
            RecalculateHammerComparison();
        }
    }

    private void CloseHammerOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (HammerOverlay != null) HammerOverlay.Visibility = Visibility.Collapsed;
    }

    private void HammerTargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HammerTargetComboBox.SelectedItem is HammerTarget target)
        {
            if (HammerTargetInfoLabel != null)
                HammerTargetInfoLabel.Text = $"{target.Category} • {target.Description}";

            RecalculateHammerComparison();
        }
    }

    private void HammerPrice_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        RecalculateHammerComparison();
    }

    private void RecalculateHammerComparison()
    {
        if (HammerTargetComboBox == null || HammerPriceTextBox == null) return;

        if (HammerTargetComboBox.SelectedItem is not HammerTarget target) return;

        decimal.TryParse(HammerPriceTextBox.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal hammerPrice);

        var result = HammerComparisonEngine.Compare(target, hammerPrice);

        if (HammerVendorCostLabel != null)
            HammerVendorCostLabel.Text = $"{result.VendorCronCost / 1_000_000_000m:F2} B";

        if (HammerOutfitCostLabel != null)
            HammerOutfitCostLabel.Text = $"{result.OutfitCronCost / 1_000_000_000m:F2} B";

        if (HammerMarketCostLabel != null)
            HammerMarketCostLabel.Text = $"{hammerPrice / 1_000_000_000m:F2} B";

        if (HammerRecommendationText != null)
            HammerRecommendationText.Text = result.Recommendation;

        if (HammerRecommendationBorder != null)
        {
            if (hammerPrice < result.OutfitCronCost)
                HammerRecommendationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4c, 0xaf, 0x50)); // Green
            else if (hammerPrice < result.VendorCronCost)
                HammerRecommendationBorder.BorderBrush = (Brush)FindResource("AccentGold"); // Yellow/Gold
            else
                HammerRecommendationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xff, 0x52, 0x52)); // Red
        }
    }

    private void SendHammerToCalc_Click(object sender, RoutedEventArgs e)
    {
        if (HammerTargetComboBox?.SelectedItem is not HammerTarget target) return;

        decimal.TryParse(HammerPriceTextBox?.Text.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal hammerPrice);
        var result = HammerComparisonEngine.Compare(target, hammerPrice);

        // Pipe the Vendor Cron cost or Hammer price into the main calculator
        _engine.SetCurrentValue(result.VendorCronCost, $"{target.Name} (Vendor Crons) =");

        if (HammerOverlay != null) HammerOverlay.Visibility = Visibility.Collapsed;
        UpdateUI();
    }
    #endregion

    #region Settings Overlay & Sliders
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        GrindOverlay.Visibility = Visibility.Collapsed;
        HammerOverlay.Visibility = Visibility.Collapsed;
        SettingsOverlay.Visibility = SettingsOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;

        _engine.TaxSettings.HasValuePack = ValuePackCheckBox.IsChecked ?? true;
        _engine.TaxSettings.HasMerchantRing = MerchantRingCheckBox.IsChecked ?? false;
        _engine.TaxSettings.Save();

        UpdateSettingsRatesDisplay();
    }

    private void FamilyFameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;

        if (int.TryParse(FamilyFameTextBox.Text, out int fame))
        {
            _engine.TaxSettings.FamilyFame = Math.Max(0, fame);
            _engine.TaxSettings.Save();
            UpdateSettingsRatesDisplay();
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized) return;

        double val = Math.Round(e.NewValue);
        _engine.TaxSettings.BackgroundOpacity = val;
        _engine.TaxSettings.Save();

        if (OpacityValueLabel != null)
            OpacityValueLabel.Text = $"{(int)val}%";

        ApplyBackgroundOpacity(val);
    }

    private void ButtonOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized) return;

        double val = Math.Round(e.NewValue);
        _engine.TaxSettings.ButtonOpacity = val;
        _engine.TaxSettings.Save();

        if (ButtonOpacityValueLabel != null)
            ButtonOpacityValueLabel.Text = $"{(int)val}%";

        ApplyButtonOpacity(val);
    }
    #endregion

    #region Calculator Button Handlers
    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string digitStr && digitStr.Length > 0)
        {
            _engine.InputDigit(digitStr[0]);
            UpdateUI();
        }
    }

    private void Operator_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string op)
        {
            _engine.InputOperator(op);
            UpdateUI();
        }
    }

    private void Equals_Click(object sender, RoutedEventArgs e)
    {
        _engine.Calculate();
        UpdateUI();
    }

    private void CronButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.CalculateCronCost(CalculatorEngine.DefaultVendorCronPrice);
        UpdateUI();
    }

    private void OutfitCronButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.CalculateCronCost(CalculatorEngine.OutfitExtractionCronPrice);
        UpdateUI();
    }

    private void TaxButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.CalculateMarketTax();
        UpdateUI();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _engine.Clear();
        UpdateUI();
    }

    private void ClearEntry_Click(object sender, RoutedEventArgs e)
    {
        _engine.ClearEntry();
        UpdateUI();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        _engine.Backspace();
        UpdateUI();
    }

    private void Negate_Click(object sender, RoutedEventArgs e)
    {
        _engine.Negate();
        UpdateUI();
    }

    private void Multiply_k_Click(object sender, RoutedEventArgs e)
    {
        _engine.MultiplyByThousand(1_000);
        UpdateUI();
    }

    private void Multiply_M_Click(object sender, RoutedEventArgs e)
    {
        _engine.MultiplyByThousand(1_000_000);
        UpdateUI();
    }

    private void Multiply_B_Click(object sender, RoutedEventArgs e)
    {
        _engine.MultiplyByThousand(1_000_000_000);
        UpdateUI();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string copyText = $"{_engine.CurrentValue:N0} Silver ({_engine.SilverSummary})";
            Clipboard.SetText(copyText);
            SilverSummaryText.Text = "✓ Copied to Clipboard!";
            if (HudSilverText != null)
                HudSilverText.Text = "✓ Copied!";
        }
        catch
        {
            // Clipboard access fallback
        }
    }
    #endregion

    #region Keyboard Navigation
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            Copy_Click(sender, e);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.H or Key.F9:
                ToggleHudMode();
                e.Handled = true;
                return;

            case Key.D0 or Key.NumPad0:
                _engine.InputDigit('0');
                break;
            case Key.D1 or Key.NumPad1:
                _engine.InputDigit('1');
                break;
            case Key.D2 or Key.NumPad2:
                _engine.InputDigit('2');
                break;
            case Key.D3 or Key.NumPad3:
                _engine.InputDigit('3');
                break;
            case Key.D4 or Key.NumPad4:
                _engine.InputDigit('4');
                break;
            case Key.D5 or Key.NumPad5:
                _engine.InputDigit('5');
                break;
            case Key.D6 or Key.NumPad6:
                _engine.InputDigit('6');
                break;
            case Key.D7 or Key.NumPad7:
                _engine.InputDigit('7');
                break;
            case Key.D8 or Key.NumPad8 when Keyboard.Modifiers != ModifierKeys.Shift:
                _engine.InputDigit('8');
                break;
            case Key.D9 or Key.NumPad9:
                _engine.InputDigit('9');
                break;
            case Key.Decimal or Key.OemPeriod:
                _engine.InputDigit('.');
                break;

            case Key.Add:
            case Key.OemPlus:
                _engine.InputOperator("+");
                break;
            case Key.Subtract:
            case Key.OemMinus:
                _engine.InputOperator("-");
                break;
            case Key.Multiply:
            case Key.D8 when Keyboard.Modifiers == ModifierKeys.Shift:
                _engine.InputOperator("×");
                break;
            case Key.Divide:
            case Key.OemQuestion:
                _engine.InputOperator("÷");
                break;

            case Key.Enter or Key.Return:
                _engine.Calculate();
                break;

            case Key.Back:
                _engine.Backspace();
                break;

            case Key.Escape:
                if (SettingsOverlay.Visibility == Visibility.Visible)
                {
                    SettingsOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
                if (GrindOverlay.Visibility == Visibility.Visible)
                {
                    GrindOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
                if (HammerOverlay.Visibility == Visibility.Visible)
                {
                    HammerOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
                if (_isHudMode)
                {
                    ToggleHudMode();
                    e.Handled = true;
                    return;
                }
                _engine.Clear();
                break;

            case Key.Delete:
                _engine.Clear();
                break;

            case Key.K:
                _engine.MultiplyByThousand(1_000);
                break;
            case Key.M:
                _engine.MultiplyByThousand(1_000_000);
                break;
            case Key.B:
                _engine.MultiplyByThousand(1_000_000_000);
                break;

            case Key.T when Keyboard.Modifiers == ModifierKeys.None:
                _engine.CalculateMarketTax();
                break;

            case Key.C when Keyboard.Modifiers == ModifierKeys.None:
                _engine.CalculateCronCost(CalculatorEngine.DefaultVendorCronPrice);
                break;

            default:
                return;
        }

        UpdateUI();
        e.Handled = true;
    }
    #endregion
}
