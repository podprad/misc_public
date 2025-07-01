using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml;
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
        
        const string XmlValue = """
                                <Data>
                                    <Colors>
                                        <Color>
                                            <Name>Red</Name>
                                        </Color>
                                        <Color>
                                            <Name>Green</Name>
                                        </Color>
                                        <Color>
                                            <Name>Blue</Name>
                                        </Color>
                                    </Colors>
                                </Data>
                                """;
        
        
        
        var dsm = RibbonReportingTab.DataSourceManager;
        
        using (var textReader = new StringReader(XmlValue))
        {
            var dataSet = new DataSet();
            dataSet.ReadXml(textReader);
            dsm.LoadDataSet(dataSet);   
        }
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
        var isInTable = TextControl.Tables.GetItem() != null;
        var visibility = isInTable ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        TabToolsTab.Visibility = visibility;
        RibbonFormulaTab.Visibility = visibility;
    }
}