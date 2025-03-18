using System.Configuration;
using System.Data;
using System.Windows;
using Telerik.Windows.Controls;

namespace TelerikValidationTooltipIssue;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Startup+= OnStartup;
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // VisualStudio2013Palette.LoadPreset(VisualStudio2013Palette.ColorVariation.Light);
    }
}