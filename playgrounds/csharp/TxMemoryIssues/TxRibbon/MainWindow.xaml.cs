using System.Windows;
using System.Windows.Input;
using TXTextControl;

namespace TxRibbon;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.Closed += OnWindowClosed; 

        this.PreviewKeyDown += (sender, args) =>
        {
            if (args.Key == Key.Escape)
            {
                this.Close();
            }
        };
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        TextControl.InputPositionChanged -= M_txTextControlOnInputPositionChanged;
        TextControl.FrameSelected -= OnFrameSelected;
        TextControl.FrameDeselected -= OnFrameDeselected;
    }

    private void TextControl_Loaded_MainWindow(object sender, RoutedEventArgs e)
    {
        TextControl.InputPositionChanged += M_txTextControlOnInputPositionChanged;
        TextControl.FrameSelected += OnFrameSelected;
        TextControl.FrameDeselected += OnFrameDeselected;
    }

    private void OnFrameDeselected(object sender, FrameEventArgs e)
    {
        FrameToolsTab.Visibility = System.Windows.Visibility.Collapsed;
    }

    private void OnFrameSelected(object sender, FrameEventArgs e)
    {
        FrameToolsTab.Visibility = System.Windows.Visibility.Visible;
    }

    private void M_txTextControlOnInputPositionChanged(object? sender, EventArgs e)
    {
        TabToolsTab.Visibility = TextControl.Tables.GetItem() != null
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }
}