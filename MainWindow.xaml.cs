using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BdoCronCalculator;

public partial class MainWindow : Window
{
    private readonly CalculatorEngine _engine = new();

    public MainWindow()
    {
        InitializeComponent();
        UpdateUI();
    }

    private void UpdateUI()
    {
        string display = _engine.FormattedDisplay;
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

        ExpressionTapeText.Text = _engine.ExpressionTape;
        SilverSummaryText.Text = _engine.SilverSummary;
    }

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
            PinButton.ToolTip = "Always on Top: Enabled";
        }
        else
        {
            PinIcon.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            PinButton.ToolTip = "Always on Top: Disabled";
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

            case Key.Escape or Key.Delete:
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
