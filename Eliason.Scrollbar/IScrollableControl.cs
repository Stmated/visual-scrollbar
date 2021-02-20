using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Eliason.Scrollbar
{
    public interface IScrollableControl
    {
        Size Size { get; set; }
        Size ClientSize { get; set; }

        ScrollBars Scrollbars { get; set; }
    }
}