using System;
using System.Windows;

namespace BdoCronCalculator;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"An error occurred:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                            "BDO Cron Calculator Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
