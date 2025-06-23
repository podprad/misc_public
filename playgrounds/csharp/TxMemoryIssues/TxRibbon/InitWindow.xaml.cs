using System.Windows;

namespace TxRibbon;

public partial class InitWindow : Window
{
    public InitWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.ShowDialog();
        
        // Tried hard ;)
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}