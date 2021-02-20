using System;
using System.Drawing;

namespace Eliason.Scrollbar
{
    public class PaintScrollbarBackgroundArgs : EventArgs
    {
        public PaintScrollbarBackgroundArgs(IntPtr gHandle, Rectangle scrollbarRect)
        {
            this.GraphicsHandle = gHandle;
            this.ScrollbarRect = scrollbarRect;
        }

        public IntPtr GraphicsHandle { get; private set; }
        public Rectangle ScrollbarRect { get; private set; }
    }
}