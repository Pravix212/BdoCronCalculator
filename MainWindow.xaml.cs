using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        // Reserved for future tool initialization
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
            BracketsOverlay.Visibility = Visibility.Collapsed;
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

    #region Brackets Tool
    // ── Data models ─────────────────────────────────────────────────────────
    private class MasteryBracketRow
    {
        public string MasteryLabel { get; set; } = "";
        public string BracketSummary { get; set; } = "";
        public System.Windows.Media.Brush RowForeground { get; set; } = System.Windows.Media.Brushes.Gray;
    }

    private class ApBracketRow
    {
        public string RangeLabel { get; set; } = "";
        public string BonusApText { get; set; } = "";
        public string TotalApText { get; set; } = "";
        public System.Windows.Media.Brush RowForeground { get; set; } = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush RowBackground { get; set; } = System.Windows.Media.Brushes.Transparent;
    }

    // ── State ────────────────────────────────────────────────────────────────
    private System.Text.Json.JsonDocument? _masteryJson;
    private System.Text.Json.JsonDocument? _apJson;

    // ── JSON loading ─────────────────────────────────────────────────────────
    private void EnsureBracketsLoaded()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();

        if (_masteryJson == null)
        {
            string masteryPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Brackets", "mastery_brackets.json");
            if (System.IO.File.Exists(masteryPath))
            {
                _masteryJson = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(masteryPath));
            }
            else
            {
                using var stream = asm.GetManifestResourceStream("BdoCronCalculator.Brackets.mastery_brackets.json");
                if (stream != null)
                    _masteryJson = System.Text.Json.JsonDocument.Parse(stream);
            }
        }
        if (_apJson == null)
        {
            string apPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Brackets", "ap_brackets.json");
            if (System.IO.File.Exists(apPath))
            {
                _apJson = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(apPath));
            }
            else
            {
                using var stream = asm.GetManifestResourceStream("BdoCronCalculator.Brackets.ap_brackets.json");
                if (stream != null)
                    _apJson = System.Text.Json.JsonDocument.Parse(stream);
            }
        }
    }

    // ── Overlay open/close ────────────────────────────────────────────────────
    private void BracketsToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (BracketsOverlay == null) return;
        SettingsOverlay.Visibility = Visibility.Collapsed;
        BracketsOverlay.Visibility = BracketsOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (BracketsOverlay.Visibility == Visibility.Visible)
        {
            EnsureBracketsLoaded();
            InitBracketsIfNeeded();
        }
    }

    private void CloseBracketsOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (BracketsOverlay != null) BracketsOverlay.Visibility = Visibility.Collapsed;
    }

    // ── One-time initialization ───────────────────────────────────────────────
    private bool _bracketsInitialized = false;
    private void InitBracketsIfNeeded()
    {
        if (_bracketsInitialized) return;
        _bracketsInitialized = true;

        // Populate lifeskill dropdown
        var skills = new[] { "Gathering", "Fishing", "Cooking", "Alchemy", "Processing", "Hunting", "Sailing", "Horse Training" };
        LifeskillComboBox.ItemsSource = skills;
        LifeskillComboBox.SelectedIndex = 0;

        // Render AP table (static)
        RefreshApTable(apValue: null);
    }

    // ── Mode toggle ───────────────────────────────────────────────────────────
    private void MasteryModeButton_Click(object sender, RoutedEventArgs e)
    {
        MasteryScrollView.Visibility = Visibility.Visible;
        ApScrollView.Visibility = Visibility.Collapsed;
        MasteryModeButton.Style = (Style)FindResource("EqualsButtonStyle");
        ApModeButton.Style = (Style)FindResource("CalcButtonStyle");
    }

    private void ApModeButton_Click(object sender, RoutedEventArgs e)
    {
        MasteryScrollView.Visibility = Visibility.Collapsed;
        ApScrollView.Visibility = Visibility.Visible;
        MasteryModeButton.Style = (Style)FindResource("CalcButtonStyle");
        ApModeButton.Style = (Style)FindResource("EqualsButtonStyle");
        RefreshApTable(apValue: ParseIntBox(ApInputTextBox));
    }

    // ── Mastery logic ─────────────────────────────────────────────────────────
    private void LifeskillComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshMasteryTable();
    }

    private void MasteryScoreTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        RefreshMasteryTable();
    }

    private void RefreshMasteryTable()
    {
        if (_masteryJson == null || LifeskillComboBox.SelectedItem == null) return;

        string skill = LifeskillComboBox.SelectedItem.ToString()!.ToLower().Replace(" ", "_");
        int? playerMastery = ParseIntBox(MasteryScoreTextBox);

        if (!_masteryJson.RootElement.TryGetProperty(skill, out var arr)) return;

        var rows = new System.Collections.Generic.List<MasteryBracketRow>();
        int activeTier = -1;
        int nextTierMastery = -1;

        var entries = arr.EnumerateArray().ToList();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            int mastery = entry.GetProperty("mastery").GetInt32();
            bool isActive = playerMastery.HasValue && playerMastery.Value >= mastery
                            && (i == entries.Count - 1 || playerMastery.Value < entries[i + 1].GetProperty("mastery").GetInt32());

            if (isActive)
            {
                activeTier = mastery;
                if (i + 1 < entries.Count)
                    nextTierMastery = entries[i + 1].GetProperty("mastery").GetInt32();
            }

            string summary = BuildMasterySummary(entry, skill);
            rows.Add(new MasteryBracketRow
            {
                MasteryLabel = $"{mastery}",
                BracketSummary = summary,
                RowForeground = isActive
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xe5, 0x76))
                    : (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xa0, 0xa8, 0xb4))
            });
        }

        MasteryBracketList.ItemsSource = rows;

        if (playerMastery.HasValue && activeTier >= 0)
        {
            int delta = nextTierMastery > 0 ? nextTierMastery - playerMastery.Value : 0;
            MasteryActiveBracketLabel.Text = nextTierMastery > 0
                ? $"Active: {activeTier} mastery  |  Next tier: +{delta} needed"
                : $"Active: {activeTier} mastery  |  Max tier reached!";
        }
        else
        {
            MasteryActiveBracketLabel.Text = "Active bracket: —";
        }
    }

    private static string BuildMasterySummary(System.Text.Json.JsonElement entry, string skill)
    {
        if (skill == "gathering")
        {
            string cChance = entry.GetProperty("common").GetProperty("chance").GetString() ?? "";
            string cDrop = entry.GetProperty("common").GetProperty("drop_amount_increase").GetString() ?? "";
            string sChance = entry.GetProperty("special").GetProperty("chance").GetString() ?? "";
            string sDrop = entry.GetProperty("special").GetProperty("drop_amount_increase").GetString() ?? "";
            string rChance = entry.GetProperty("rare").GetProperty("chance").GetString() ?? "";
            string rDrop = entry.GetProperty("rare").GetProperty("drop_amount_increase").GetString() ?? "";
            return $"Common: {cChance} / +{cDrop}  |  Special: {sChance} / +{sDrop}  |  Rare: {rChance} / +{rDrop}";
        }
        else
        {
            // Standard lifeskills: chance + exp_bonus or similar flat fields
            var fields = new System.Text.StringBuilder();
            foreach (var prop in entry.EnumerateObject())
            {
                if (prop.Name == "mastery") continue;
                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    fields.Append($"{prop.Name}: {prop.Value.GetString()}  ");
            }
            return fields.ToString().Trim();
        }
    }

    // ── AP logic ──────────────────────────────────────────────────────────────
    private void ApInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        int? ap = ParseIntBox(ApInputTextBox);
        int? goalAp = ParseIntBox(ApGoalTextBox);
        RefreshApTable(ap);
        RefreshApSummary(ap, goalAp);
    }

    private void RefreshApTable(int? apValue)
    {
        if (_apJson == null) return;

        var rows = new System.Collections.Generic.List<ApBracketRow>();
        var activeColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xe5, 0x76));
        var dimColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xa0, 0xa8, 0xb4));
        var activeBg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0x00, 0xe5, 0x76));

        foreach (var entry in _apJson.RootElement.EnumerateArray())
        {
            int minAp = entry.GetProperty("min_ap").GetInt32();
            int maxAp = entry.GetProperty("max_ap").GetInt32();
            int bonusAp = entry.GetProperty("bonus_ap").GetInt32();
            int totalAp = entry.GetProperty("total_attack_ap").GetInt32();

            bool isActive = apValue.HasValue && apValue.Value >= minAp && apValue.Value <= maxAp;

            rows.Add(new ApBracketRow
            {
                RangeLabel = maxAp >= 9999 ? $"{minAp}+" : $"{minAp}–{maxAp}",
                BonusApText = $"+{bonusAp} AP",
                TotalApText = $"= {totalAp} AP",
                RowForeground = isActive ? activeColor : dimColor,
                RowBackground = isActive ? activeBg : System.Windows.Media.Brushes.Transparent
            });
        }

        ApBracketList.ItemsSource = rows;
    }

    private void RefreshApSummary(int? ap, int? goalAp)
    {
        if (!ap.HasValue || _apJson == null)
        {
            ApCurrentBracketText.Text = "Enter your AP above";
            ApNextBracketText.Text = "—";
            ApGoalDeltaText.Text = "";
            return;
        }

        System.Text.Json.JsonElement? current = null;
        System.Text.Json.JsonElement? next = null;
        var allEntries = _apJson.RootElement.EnumerateArray().ToList();

        for (int i = 0; i < allEntries.Count; i++)
        {
            int min = allEntries[i].GetProperty("min_ap").GetInt32();
            int max = allEntries[i].GetProperty("max_ap").GetInt32();
            if (ap.Value >= min && ap.Value <= max)
            {
                current = allEntries[i];
                if (i + 1 < allEntries.Count) next = allEntries[i + 1];
                break;
            }
        }

        if (current == null)
        {
            ApCurrentBracketText.Text = "No bracket found";
            ApNextBracketText.Text = "—";
            ApGoalDeltaText.Text = "";
            return;
        }

        int bonusAp = current.Value.GetProperty("bonus_ap").GetInt32();
        int totalAttack = current.Value.GetProperty("total_attack_ap").GetInt32();
        ApCurrentBracketText.Text = $"+{bonusAp} Bonus AP  (Total: {totalAttack} AP)";

        if (next.HasValue)
        {
            int nextMin = next.Value.GetProperty("min_ap").GetInt32();
            int nextBonus = next.Value.GetProperty("bonus_ap").GetInt32();
            ApNextBracketText.Text = $"Next tier: {nextMin} AP → +{nextBonus} Bonus AP  ({nextMin - ap.Value} AP needed)";
        }
        else
        {
            ApNextBracketText.Text = "Max AP bracket reached!";
        }

        if (goalAp.HasValue)
        {
            int delta = goalAp.Value - ap.Value;
            ApGoalDeltaText.Text = delta > 0
                ? $"🎯 Goal: {goalAp.Value} AP  →  need +{delta} AP"
                : $"🎯 Goal {goalAp.Value} AP already reached!";
        }
        else
        {
            ApGoalDeltaText.Text = "";
        }
    }

    // ── Utility ───────────────────────────────────────────────────────────────
    private static int? ParseIntBox(TextBox box)
    {
        if (box == null) return null;
        string text = box.Text?.Trim() ?? "";
        if (int.TryParse(text, out int val) && val >= 0) return val;
        return null;
    }
    #endregion




    #region Settings Overlay & Sliders
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        BracketsOverlay.Visibility = Visibility.Collapsed;
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
        // If user is currently typing inside any TextBox, let the TextBox handle all input
        if (Keyboard.FocusedElement is TextBox || e.OriginalSource is TextBox)
        {
            if (e.Key == Key.Escape)
            {
                if (SettingsOverlay.Visibility == Visibility.Visible)
                {
                    SettingsOverlay.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
            }
            return; // Allow typing 'c', 'b', 'k', 'm', 't', digits, backspace without triggering hotkeys
        }

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
