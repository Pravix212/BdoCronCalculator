using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    private UpdateInfo? _pendingUpdate = null;

    public MainWindow()
    {
        InitializeComponent();

        if (_engine.TaxSettings.WindowWidth >= MinWidth)
            Width = _engine.TaxSettings.WindowWidth;
        if (_engine.TaxSettings.WindowHeight >= MinHeight)
            Height = _engine.TaxSettings.WindowHeight;

        SizeChanged += MainWindow_SizeChanged;

        _isInitialized = true;
        LoadSettingsIntoUI();
        UpdateUI();
        _ = CheckForAppUpdatesAsync();
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
        if (_isInitialized && WindowState == WindowState.Normal)
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

        ApplyBackgroundOpacity(s.BackgroundOpacity);

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

        if (SettingsOverlay != null)
            SettingsOverlay.Background = new SolidColorBrush(Color.FromArgb(Math.Max((byte)230, darkAlpha), 0x13, 0x15, 0x18));
    }

    private void UpdateSettingsRatesDisplay()
    {
        if (!_isInitialized) return;

        var s = _engine.TaxSettings;
        decimal payoutPct = s.EffectivePayoutRate * 100m;
        decimal taxPct = s.EffectiveTaxRate * 100m;

        if (SettingsPayoutRateText != null)
            SettingsPayoutRateText.Text = $"{payoutPct:N2}%";

        if (SettingsTaxRateText != null)
            SettingsTaxRateText.Text = $"-{taxPct:N2}%";

        if (FameBonusLabel != null)
        {
            if (s.FamilyFame >= 7000)
                FameBonusLabel.Text = "+1.5% (≥ 7,000 Fame)";
            else if (s.FamilyFame >= 4000)
                FameBonusLabel.Text = "+1.0% (4,000 - 6,999 Fame)";
            else if (s.FamilyFame >= 1000)
                FameBonusLabel.Text = "+0.5% (1,000 - 3,999 Fame)";
            else
                FameBonusLabel.Text = "+0.0% (< 1,000 Fame)";
        }

        if (TaxButton != null)
            TaxButton.ToolTip = $"Calculate Market Net Profit ({payoutPct:N2}% Payout | -{taxPct:N2}% Tax)";
    }

    private void UpdateUI()
    {
        if (!_isInitialized) return;

        string display = _engine.FormattedDisplay;
        if (MainDisplayText != null)
        {
            MainDisplayText.Text = display;

            // Auto-scale font size for huge numbers
            if (display.Length > 16)
            {
                MainDisplayText.FontSize = 20;
            }
            else if (display.Length > 12)
            {
                MainDisplayText.FontSize = 24;
            }
            else
            {
                MainDisplayText.FontSize = 32;
            }
        }

        if (ExpressionTapeText != null)
            ExpressionTapeText.Text = _engine.ExpressionTape;

        if (SilverSummaryText != null)
            SilverSummaryText.Text = _engine.SilverSummary;
    }

    #region Settings Management
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsOverlay == null) return;
        SettingsOverlay.Visibility = SettingsOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsOverlay != null)
            SettingsOverlay.Visibility = Visibility.Collapsed;
        _engine.TaxSettings.Save();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized) return;

        if (OpacityValueLabel != null)
            OpacityValueLabel.Text = $"{(int)e.NewValue}%";

        _engine.TaxSettings.BackgroundOpacity = e.NewValue;
        ApplyBackgroundOpacity(e.NewValue);
        _engine.TaxSettings.Save();
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || ValuePackCheckBox == null || MerchantRingCheckBox == null) return;

        _engine.TaxSettings.HasValuePack = ValuePackCheckBox.IsChecked == true;
        _engine.TaxSettings.HasMerchantRing = MerchantRingCheckBox.IsChecked == true;
        _engine.TaxSettings.Save();

        UpdateSettingsRatesDisplay();
    }

    private void FamilyFameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || FamilyFameTextBox == null) return;

        if (int.TryParse(FamilyFameTextBox.Text, out int fame) && fame >= 0)
        {
            _engine.TaxSettings.FamilyFame = fame;
            _engine.TaxSettings.Save();
            UpdateSettingsRatesDisplay();
        }
    }
    #endregion

    #region Window Titlebar Controls
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        if (Topmost)
        {
            PinIcon.Foreground = (SolidColorBrush)FindResource("AccentCron");
            PinButton.ToolTip = "Always on Top (Enabled)";
        }
        else
        {
            PinIcon.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            PinButton.ToolTip = "Always on Top (Disabled)";
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
    #endregion

    #region Calculator Actions
    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && tag.Length > 0)
        {
            _engine.InputDigit(tag[0]);
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
                // Pressing T triggers Tax calculation!
                _engine.CalculateMarketTax();
                break;

            case Key.C when Keyboard.Modifiers == ModifierKeys.None:
                // Pressing C triggers Cron calculation!
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
