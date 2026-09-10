using System;
using System.Globalization;

namespace BdoCronCalculator;

public class CalculatorEngine
{
    public const decimal DefaultVendorCronPrice = 3_000_000m;
    public const decimal OutfitExtractionCronPrice = 2_180_000m;

    private decimal _currentCronPrice = DefaultVendorCronPrice;
    private decimal? _storedOperand = null;
    private string? _pendingOperator = null;
    private bool _isNewEntry = true;
    private string _currentInput = "0";

    public decimal CurrentCronPrice
    {
        get => _currentCronPrice;
        set => _currentCronPrice = value > 0 ? value : DefaultVendorCronPrice;
    }

    public string CurrentInput => _currentInput;
    public string ExpressionTape { get; private set; } = string.Empty;

    public string FormattedDisplay
    {
        get
        {
            if (decimal.TryParse(_currentInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            {
                if (_currentInput.Contains('.'))
                {
                    string[] parts = _currentInput.Split('.');
                    if (decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal intPart))
                    {
                        return $"{intPart.ToString("N0", CultureInfo.InvariantCulture)}.{parts[1]}";
                    }
                }
                return val.ToString("N0", CultureInfo.InvariantCulture);
            }
            return _currentInput;
        }
    }

    public decimal CurrentValue
    {
        get
        {
            if (decimal.TryParse(_currentInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            {
                return val;
            }
            return 0m;
        }
    }

    public string SilverSummary => FormatSilverSummary(CurrentValue);

    public void InputDigit(char digit)
    {
        if (digit == '.')
        {
            if (_isNewEntry)
            {
                _currentInput = "0.";
                _isNewEntry = false;
            }
            else if (!_currentInput.Contains('.'))
            {
                _currentInput += ".";
            }
            return;
        }

        if (!char.IsDigit(digit))
        {
            return;
        }

        if (_isNewEntry || _currentInput == "0")
        {
            _currentInput = digit.ToString();
            _isNewEntry = false;
        }
        else
        {
            if (_currentInput.Length < 18) // prevent overflow
            {
                _currentInput += digit;
            }
        }
    }

    public void InputOperator(string op)
    {
        decimal current = CurrentValue;

        if (_storedOperand.HasValue && !_isNewEntry)
        {
            current = ExecuteOperation(_storedOperand.Value, current, _pendingOperator);
            _currentInput = current.ToString(CultureInfo.InvariantCulture);
        }

        _storedOperand = current;
        _pendingOperator = op;
        ExpressionTape = $"{current.ToString("N0", CultureInfo.InvariantCulture)} {op}";
        _isNewEntry = true;
    }

    public void Calculate()
    {
        if (!_storedOperand.HasValue || string.IsNullOrEmpty(_pendingOperator))
        {
            ExpressionTape = $"{CurrentValue.ToString("N0", CultureInfo.InvariantCulture)} =";
            _isNewEntry = true;
            return;
        }

        decimal right = CurrentValue;
        decimal left = _storedOperand.Value;
        decimal result = ExecuteOperation(left, right, _pendingOperator);

        ExpressionTape = $"{left.ToString("N0", CultureInfo.InvariantCulture)} {_pendingOperator} {right.ToString("N0", CultureInfo.InvariantCulture)} =";
        _currentInput = result.ToString(CultureInfo.InvariantCulture);
        _storedOperand = null;
        _pendingOperator = null;
        _isNewEntry = true;
    }

    public void CalculateCronCost(decimal? customCronPrice = null)
    {
        decimal price = customCronPrice ?? _currentCronPrice;
        decimal count = CurrentValue;
        decimal totalSilver = count * price;

        ExpressionTape = $"{count.ToString("N0", CultureInfo.InvariantCulture)} Crons × {price.ToString("N0", CultureInfo.InvariantCulture)} =";
        _currentInput = totalSilver.ToString(CultureInfo.InvariantCulture);
        _storedOperand = null;
        _pendingOperator = null;
        _isNewEntry = true;
    }

    public void Clear()
    {
        _currentInput = "0";
        _storedOperand = null;
        _pendingOperator = null;
        ExpressionTape = string.Empty;
        _isNewEntry = true;
    }

    public void ClearEntry()
    {
        _currentInput = "0";
        _isNewEntry = true;
    }

    public void Backspace()
    {
        if (_isNewEntry)
        {
            return;
        }

        if (_currentInput.Length > 1)
        {
            _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
            if (_currentInput == "-" || _currentInput == "")
            {
                _currentInput = "0";
                _isNewEntry = true;
            }
        }
        else
        {
            _currentInput = "0";
            _isNewEntry = true;
        }
    }

    public void Negate()
    {
        if (_currentInput == "0") return;

        if (_currentInput.StartsWith('-'))
        {
            _currentInput = _currentInput.Substring(1);
        }
        else
        {
            _currentInput = "-" + _currentInput;
        }
    }

    public void Percentage()
    {
        decimal val = CurrentValue / 100m;
        _currentInput = val.ToString(CultureInfo.InvariantCulture);
        _isNewEntry = true;
    }

    public void MultiplyByThousand(int multiplier)
    {
        decimal val = CurrentValue * multiplier;
        _currentInput = val.ToString(CultureInfo.InvariantCulture);
        _isNewEntry = true;
    }

    private static decimal ExecuteOperation(decimal left, decimal right, string? op)
    {
        return op switch
        {
            "+" => left + right,
            "-" => left - right,
            "×" or "*" => left * right,
            "÷" or "/" => right != 0m ? left / right : 0m,
            _ => right
        };
    }

    public static string FormatSilverSummary(decimal value)
    {
        if (value == 0m)
        {
            return "0 Silver";
        }

        decimal abs = Math.Abs(value);
        string sign = value < 0 ? "-" : "";

        if (abs >= 1_000_000_000_000m) // Trillion
        {
            decimal t = abs / 1_000_000_000_000m;
            decimal b = abs / 1_000_000_000m;
            return $"{sign}{t.ToString("N2", CultureInfo.InvariantCulture)} Trillion Silver ({b.ToString("N0", CultureInfo.InvariantCulture)} B)";
        }
        if (abs >= 1_000_000_000m) // Billion
        {
            decimal b = abs / 1_000_000_000m;
            decimal m = abs / 1_000_000m;
            return $"{sign}{b.ToString("N2", CultureInfo.InvariantCulture)} Billion Silver ({m.ToString("N0", CultureInfo.InvariantCulture)} M)";
        }
        if (abs >= 1_000_000m) // Million
        {
            decimal m = abs / 1_000_000m;
            return $"{sign}{m.ToString("N2", CultureInfo.InvariantCulture)} Million Silver ({m.ToString("N0", CultureInfo.InvariantCulture)}M)";
        }
        if (abs >= 1_000m) // Thousand
        {
            decimal k = abs / 1_000m;
            return $"{sign}{k.ToString("N1", CultureInfo.InvariantCulture)}k Silver ({abs.ToString("N0", CultureInfo.InvariantCulture)})";
        }

        return $"{sign}{abs.ToString("N0", CultureInfo.InvariantCulture)} Silver";
    }
}
