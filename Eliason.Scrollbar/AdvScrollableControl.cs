#region

using System;
using System.Drawing;
using System.Windows.Forms;
using Eliason.Common;
using Eliason.Scrollbar.Native;

#endregion

namespace Eliason.Scrollbar
{
    public class AdvScrollableControl : Control, IScrollableControl
    {
        private const int WHEEL_DELTA = 120;

        private int _wheelPos;
        private Control _control;

        public AdvScrollableControl()
        {
            SetStyle(ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.Selectable, true);

            this.VerticalScroll = new AdvVScrollbar();
            this.HorizontalScroll = new AdvHScrollbar();

            this.Scrollbars = ScrollBars.Both;

            this.Controls.Add(this.VerticalScroll);
            this.Controls.Add(this.HorizontalScroll);
        }

        public Control Control
        {
            get { return this._control; }

            set
            {
                if (this._control != null)
                {
                    throw new ArgumentException("There is already another Control attached to this scrollable control");
                }

                this._control = value;
                if (value != null)
                {
                    this.Controls.Add(value);
                }
            }
        }

        public ScrollBars Scrollbars { get; set; }
        public AdvVScrollbar VerticalScroll { get; private set; }
        public AdvHScrollbar HorizontalScroll { get; private set; }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            var paddingHeight = (this.HorizontalScroll.Visible ? this.HorizontalScroll.Thickness : 0);

            var paddingWidth = (this.VerticalScroll.Visible ? this.VerticalScroll.Thickness : 0);

            this.HorizontalScroll.SuspendLayout();
            this.HorizontalScroll.Location = new Point(0, this.Height - paddingHeight);
            this.HorizontalScroll.Width = this.Width - paddingWidth;
            this.HorizontalScroll.ResumeLayout();

            this.VerticalScroll.SuspendLayout();
            this.VerticalScroll.Location = new Point(this.Width - paddingWidth, 0);
            this.VerticalScroll.Height = this.Height - paddingHeight;
            this.VerticalScroll.ResumeLayout();

            // Set the containing control to the client size
            if (this.Control != null)
            {
                this.Control.Location = new Point(0, 0);

                this.Control.Size = new Size(
                    this.HorizontalScroll.Visible ? this.HorizontalScroll.Width : this.Width,
                    this.VerticalScroll.Visible ? this.VerticalScroll.Height : this.Height
                    );
            }
        }

        protected virtual Cursor GetDefaultCursor()
        {
            return Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.VerticalScroll.IsScrolling == false && this.HorizontalScroll.IsScrolling == false)
            {
                if (this.ClientRectangle.Contains(e.Location) == false)
                {
                    this.Cursor = Cursors.Arrow;
                }
                else
                {
                    this.Cursor = this.GetDefaultCursor();
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None)
            {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            AdvScrollbarBase scrollbar;

            switch (this.Scrollbars)
            {
                case ScrollBars.Vertical:
                case ScrollBars.Both:
                    scrollbar = this.VerticalScroll;
                    break;
                default:
                    scrollbar = this.HorizontalScroll;
                    break;
            }

            if (scrollbar.Visible == false)
            {
                // If the given scrollbar is not visible, then there's no need to do any calculations.
                return;
            }

            this._wheelPos += e.Delta;
            var change = 0;

            while (this._wheelPos >= WHEEL_DELTA)
            {
                change -= scrollbar.LargeChange;
                this._wheelPos -= WHEEL_DELTA;
            }

            while (this._wheelPos <= -WHEEL_DELTA)
            {
                change += scrollbar.LargeChange;
                this._wheelPos += WHEEL_DELTA;
            }

            scrollbar.SetValue(scrollbar.Value + change, ValueChangedBy.MouseWheel);
            scrollbar.SetValue(scrollbar.ValueIntegral, ValueChangedBy.MouseWheel);

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.HorizontalScroll != null)
            {
                this.HorizontalScroll.Dispose();
                this.HorizontalScroll = null;
            }

            if (this.VerticalScroll != null)
            {
                this.VerticalScroll.Dispose();
                this.VerticalScroll = null;
            }
        }

        public void ScrollToPoint(Point p, bool force, bool ignoreWidth)
        {
            if (ignoreWidth == false)
            {
                if (p.X < this.HorizontalScroll.Value)
                {
                    this.HorizontalScroll.SetValue(Math.Max(this.HorizontalScroll.Minimum, p.X - this.VerticalScroll.SmallChange), ValueChangedBy.Unspecified);
                }
                else if (p.X >= this.HorizontalScroll.Value + this.ClientRectangle.Width)
                {
                    var newValue = Math.Min(this.HorizontalScroll.Maximum, p.X);

                    if (newValue > this.HorizontalScroll.Value)
                    {
                        this.HorizontalScroll.SetValue(newValue - this.ClientSize.Width + this.VerticalScroll.SmallChange, ValueChangedBy.Unspecified);
                    }
                    else
                    {
                        this.HorizontalScroll.SetValue(newValue + this.VerticalScroll.SmallChange, ValueChangedBy.Unspecified);
                    }
                }
            }
            else
            {
                this.HorizontalScroll.SetValue(0, ValueChangedBy.Unspecified);
            }

            if (p.Y - this.VerticalScroll.SmallChange < this.VerticalScroll.ValueIntegral)
            {
                this.VerticalScroll.SetValue(
                    Math.Max(this.VerticalScroll.Minimum, (p.Y) - (this.VerticalScroll.SmallChange)),
                    ValueChangedBy.Unspecified);
            }
            else if (force == false && p.Y + this.VerticalScroll.SmallChange > this.VerticalScroll.ValueIntegral + this.ClientRectangle.Height)
            {
                this.VerticalScroll.SetValue(
                    Math.Min(this.VerticalScroll.Maximum, (p.Y) + (this.VerticalScroll.SmallChange * 2) - this.ClientRectangle.Height),
                    ValueChangedBy.Unspecified);
            }
            else if (force)
            {
                this.VerticalScroll.SetValue(p.Y, ValueChangedBy.Unspecified);
            }

            Invalidate();
        }
    }
}