using System.Windows;

namespace TelerikValidationTooltipIssue;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.DataContext = new MainViewModel();
    }
    
    private void AddErrorClicked(object sender, RoutedEventArgs e)
    {
        (this.DataContext as MainViewModel).AddErrors(nameof(MainViewModel.Name), new[] { "Bad value, please correct it." });
    }

    private void ClearErrorClicked(object sender, RoutedEventArgs e)
    {
        (this.DataContext as MainViewModel).ClearErrors(nameof(MainViewModel.Name));
    }
}