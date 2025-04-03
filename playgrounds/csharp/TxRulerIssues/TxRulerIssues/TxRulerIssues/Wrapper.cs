using System.Windows.Interop;
using Vanara.PInvoke;

namespace TxRulerIssues;

public class Wrapper
{
    public static void AttachTo(IntPtr parent)
    {
        var parameters = new HwndSourceParameters
        {
            ParentWindow = parent,
            WindowStyle = 0x40000000 | 0x10000000,
            WindowName = "RandomName",
            Width = 800,
            Height = 600,
        };
        
        var hwndSource = new HwndSource(parameters);
        hwndSource.RootVisual = new MyEditor();
        
        User32.SetParent(hwndSource.Handle, parent);
    }
}