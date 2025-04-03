using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TxRulerIssues;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ShowWindowed(object sender, RoutedEventArgs e)
    {
        var window = new Window();
        window.Content = new MyEditor();
        window.ShowDialog();
    }

    private void ShowInHost(object sender, RoutedEventArgs e)
    {
        // Just use winforms to show Win32 window. Enough to reproduce the case.
        var form = new Form()
        {
        };

        form.Shown += (s, args) =>
        {
            Wrapper.AttachTo(form.Handle);
        };

        form.ShowDialog();
    }
}