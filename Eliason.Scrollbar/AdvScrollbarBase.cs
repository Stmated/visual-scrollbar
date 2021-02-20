using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Eliason.Common;
using Eliason.Scrollbar.Native;

namespace Eliason.Scrollbar
{
    public abstract class AdvScrollbarBase : Control, IDisposable
    {
        protected Rectangle arrow1;
        private bool _arrow1Down;
        private bool _arrow1Hover;
        protected Rectangle arrow2;
        private bool _arrow2Down;
        private bool _arrow2Hover;
        private SafeHandleGDI _hbitmapArrow1, _hbitmapArrow1Hot, _hbitmapArrow1Pressed;
        private SafeHandleGDI _hbitmapArrow2, _hbitmapArrow2Hot, _hbitmapArrow2Pressed;
        private SafeHandleGDI _hbitmapThumb, _hbitmapThumbHot, _hbitmapThumbPressed;
        private SafeHandleGDI _hbitmapTrack, _hbitmapTrackHot, _hbitmapTrackPressed;
        private int _maximumBase;
        private int _maximumPadding;
        private int _scrollingStartPosition;
        private int _scrollingStartValue;
        protected Rectangle thumb;
        private bool _thumbDown;
        protected Rectangle thumbGrip;
        private bool _thumbHover;
        protected Rectangle track;
        private bool _trackDown;
        private bool _trackHover;
        protected double trackMultiplicator;
        private readonly Timer _mouseDownTimer;

        protected AdvScrollbarBase(/*IScrollableControl control,bool horizontal */)
        {
            //this.Control = control;
            //this.Horizontal = horizontal;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            this.SmallChange = 1;
            this.LargeChange = SystemInformation.MouseWheelScrollLines;
            //this.Enabled = true;

            this._mouseDownTimer = new Timer();
            this._mouseDownTimer.Tick += this.mouseDownTimer_Tick;
            this._mouseDownTimer.Interval = SystemInformation.KeyboardSpeed;
            this._mouseDownTimer.Enabled = false;
        }

        public bool IsScrolling { get; set; }
        public bool IsStepping { get; set; }

        public int Thickness
        {
            get { return this.Horizontal ? this.Height : this.Width; }
        }

        public int Value { get; private set; }

        public int ValueIntegral
        {
            get { return GetValueIntegral(this.Value, this.SmallChange); }
        }

        public int Minimum
        {
            get { return 0; }
        }

        public int Maximum
        {
            get { return this._maximumBase; }

            set
            {
                this._maximumBase = value;

                if (this.Value > this.Maximum)
                {
                    this.SetValue(this.Maximum, ValueChangedBy.Unspecified);
                }

                this.DisposeDynamicBuffer();
                this.PerformLayout();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible == false)
            {
                this.SetValue(0, ValueChangedBy.Unspecified);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            this.DisposeDynamicBuffer();
        }

        public int SmallChange { get; set; }
        public int LargeChange { get; set; }

        /// <summary>
        /// Gets the full Rectangle size of the scollbar
        /// </summary>
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle(
                    this.arrow1.Left,
                    this.arrow1.Top,
                    this.arrow2.Right - this.arrow1.Left,
                    this.arrow2.Bottom - this.arrow1.Top);
            }
        }

        /// <summary>
        /// Gets the so-called client area of the scrollbar rectangle.
        /// This means that it is the size of the scrollbar with an extra padding, spacing, etc, removed.
        /// </summary>
        public Rectangle TrackRectangle
        {
            get
            {
                return this.track;
            }
        }

        public abstract bool Horizontal { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.DisposeDynamicBuffer();

            this._hbitmapArrow1 = DisposeBuffer(this._hbitmapArrow1);
            this._hbitmapArrow1Hot = DisposeBuffer(this._hbitmapArrow1Hot);
            this._hbitmapArrow1Pressed = DisposeBuffer(this._hbitmapArrow1Pressed);

            this._hbitmapArrow2 = DisposeBuffer(this._hbitmapArrow2);
            this._hbitmapArrow2Hot = DisposeBuffer(this._hbitmapArrow2Hot);
            this._hbitmapArrow2Pressed = DisposeBuffer(this._hbitmapArrow2Pressed);
        }

        public event EventHandler<PaintScrollbarBackgroundArgs> PaintBackgroundOverlay;

        private static int GetValueIntegral(int value, int smallChange)
        {
            float mod = (value % smallChange);

            if (float.IsNaN(mod))
            {
                // If the modulus is by 0 or on 0, then the number is NaN and hence invalid,
                // and we should just use the regular value.
            }
            else
            {
                value = mod > smallChange / 2d ? Convert.ToInt32(value + (smallChange - mod)) : Convert.ToInt32(value - mod);
            }

            return value;
        }

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        public void SetValue(int value, ValueChangedBy by)
        {
            var pre = this.Value;
            //var trackPadding = (this.Horizontal
            //    ? (this.thumb.Width + (this.arrow1.Width * 2))
            //    : (this.thumb.Height + (this.arrow1.Height * 2))
            //);

            var padding = (this.Horizontal ? this.track.Width + this.arrow2.Width : this.track.Height + this.arrow2.Height) + 1;
            var newValue = Math.Max(0, Math.Min(this.Maximum - padding, value));

            //var newValue = Math.Max(0, Math.Min(this.Maximum - trackPadding, value));

            if (pre == newValue)
            {
                return;
            }

            this.Value = newValue;

            // TODO: Remove! The location of the track should be calculated inside OnPaint, not in Layout() -- it should only be called when "Maximum" changes
            this.PerformLayout();

            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, new ValueChangedEventArgs { By = by });
            }

            this.Invalidate();
        }

        public void DisposeDynamicBuffer(bool valueChange = false)
        {
            if (valueChange == false)
            {
                this._hbitmapTrack = DisposeBuffer(this._hbitmapTrack);
                this._hbitmapTrackHot = DisposeBuffer(this._hbitmapTrackHot);
                this._hbitmapTrackPressed = DisposeBuffer(this._hbitmapTrackPressed);

                this._hbitmapThumb = DisposeBuffer(this._hbitmapThumb);
                this._hbitmapThumbHot = DisposeBuffer(this._hbitmapThumbHot);
                this._hbitmapThumbPressed = DisposeBuffer(this._hbitmapThumbPressed);
            }
        }

        private static SafeHandleGDI DisposeBuffer(SafeHandleGDI hbitmap)
        {
            if (hbitmap != null)
            {
                hbitmap.Dispose();
            }

            return null;
        }

        private SafeHandleGDI GetBufferTrack()
        {
            ScrollBarState state;
            SafeHandleGDI target;

            if (this._trackDown)
            {
                state = ScrollBarState.Pressed;
                target = this._hbitmapTrackPressed;
            }
            else if (this._trackHover)
            {
                state = ScrollBarState.Hot;
                target = this._hbitmapTrackHot;
            }
            else
            {
                state = ScrollBarState.Normal;
                target = this._hbitmapTrack;
            }

            if (target != null)
            {
                return target;
            }

            if (this.track.Width <= 0 || this.track.Height <= 0)
            {
                return null;
            }

            using (var bmp = new Bitmap(this.track.Width, this.track.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    if (ScrollBarRenderer.IsSupported)
                    {
                        if (this.Horizontal)
                        {
                            ScrollBarRenderer.DrawRightHorizontalTrack(g, new Rectangle(0, 0, bmp.Width, bmp.Height), state);
                        }
                        else
                        {
                            ScrollBarRenderer.DrawUpperVerticalTrack(g, new Rectangle(0, 0, bmp.Width, bmp.Height), state);
                        }
                    }
                    else
                    {
                        var bgColor1 = state == ScrollBarState.Pressed ? Color.Black : SystemColors.ControlLightLight;
                        var bgColor2 = state == ScrollBarState.Pressed
                            ? SystemColors.ControlDarkDark
                            : SystemColors.Control;

                        using (var b = new HatchBrush(HatchStyle.Percent50, bgColor1, bgColor2))
                        {
                            g.FillRectangle(b, 0, 0, bmp.Width, bmp.Height);
                        }
                    }
                }

                target = new SafeHandleGDI(bmp.GetHbitmap());
            }

            if (this._trackDown)
            {
                return this._hbitmapTrackPressed = target;
            }

            if (this._trackHover)
            {
                return this._hbitmapTrackHot = target;
            }

            return this._hbitmapTrack = target;
        }

        private SafeHandleGDI GetBufferArrow1()
        {
            SafeHandleGDI target;

            if (this._arrow1Down)
            {
                target = this._hbitmapArrow1Pressed;
            }
            else if (this._arrow1Hover)
            {
                target = this._hbitmapArrow1Hot;
            }
            else
            {
                target = this._hbitmapArrow1;
            }

            if (target != null)
            {
                return target;
            }

            if (this.arrow1.Width <= 0 || this.arrow1.Height <= 0)
            {
                return null;
            }

            using (var bmp = new Bitmap(this.arrow1.Width, this.arrow1.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    if (ScrollBarRenderer.IsSupported)
                    {
                        if (this.Horizontal)
                        {
                            var s4 = this._arrow1Down
                                ? ScrollBarArrowButtonState.LeftPressed
                                : this._arrow1Hover
                                    ? ScrollBarArrowButtonState.LeftHot
                                    : ScrollBarArrowButtonState.LeftNormal;
                            ScrollBarRenderer.DrawArrowButton(g, new Rectangle(0, 0, this.arrow1.Width, this.arrow1.Height), s4);
                        }
                        else
                        {
                            var s4 = this._arrow1Down
                                ? ScrollBarArrowButtonState.UpPressed
                                : this._arrow1Hover
                                    ? ScrollBarArrowButtonState.UpHot
                                    : ScrollBarArrowButtonState.UpNormal;
                            ScrollBarRenderer.DrawArrowButton(g, new Rectangle(0, 0, this.arrow1.Width, this.arrow1.Height), s4);
                        }
                    }
                    else
                    {
                        ControlPaint.DrawScrollButton(
                            g,
                            new Rectangle(0, 0, this.arrow1.Width, this.arrow1.Height),
                            this.Horizontal ? ScrollButton.Left : ScrollButton.Up,
                            this._arrow1Down
                                ? ButtonState.Pushed
                                : ButtonState.Normal);
                    }
                }

                target = new SafeHandleGDI(bmp.GetHbitmap());
            }

            if (this._arrow1Down)
            {
                return this._hbitmapArrow1Pressed = target;
            }

            if (this._arrow1Hover)
            {
                return this._hbitmapArrow1Hot = target;
            }

            return this._hbitmapArrow1 = target;
        }

        private SafeHandleGDI GetBufferArrow2()
        {
            SafeHandleGDI target;

            if (this._arrow2Down)
            {
                target = this._hbitmapArrow2Pressed;
            }
            else if (this._arrow2Hover)
            {
                target = this._hbitmapArrow2Hot;
            }
            else
            {
                target = this._hbitmapArrow2;
            }

            if (target != null)
            {
                return target;
            }

            if (this.arrow2.Width <= 0 || this.arrow2.Height <= 0)
            {
                return null;
            }

            using (var bmp = new Bitmap(this.arrow2.Width, this.arrow2.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    if (ScrollBarRenderer.IsSupported)
                    {
                        if (this.Horizontal)
                        {
                            var s4 = this._arrow2Down
                                ? ScrollBarArrowButtonState.RightPressed
                                : this._arrow2Hover
                                    ? ScrollBarArrowButtonState.RightHot
                                    : ScrollBarArrowButtonState.RightNormal;
                            ScrollBarRenderer.DrawArrowButton(g, new Rectangle(0, 0, this.arrow2.Width, this.arrow2.Height), s4);
                        }
                        else
                        {
                            var s4 = this._arrow2Down
                                ? ScrollBarArrowButtonState.DownPressed
                                : this._arrow2Hover
                                    ? ScrollBarArrowButtonState.DownHot
                                    : ScrollBarArrowButtonState.DownNormal;
                            ScrollBarRenderer.DrawArrowButton(g, new Rectangle(0, 0, this.arrow2.Width, this.arrow2.Height), s4);
                        }
                    }
                    else
                    {
                        ControlPaint.DrawScrollButton(
                            g,
                            new Rectangle(0, 0, this.arrow2.Width, this.arrow2.Height),
                            this.Horizontal ? ScrollButton.Right : ScrollButton.Down,
                            this._arrow2Down
                                ? ButtonState.Pushed
                                : ButtonState.Normal);
                    }
                }

                target = new SafeHandleGDI(bmp.GetHbitmap());
            }

            if (this._arrow2Down)
            {
                return this._hbitmapArrow2Pressed = target;
            }

            if (this._arrow2Hover)
            {
                return this._hbitmapArrow2Hot = target;
            }

            return this._hbitmapArrow2 = target;
        }

        private SafeHandleGDI GetBufferThumb()
        {
            ScrollBarState state;
            SafeHandleGDI target;

            if (this._thumbDown)
            {
                state = ScrollBarState.Pressed;
                target = this._hbitmapThumbPressed;
            }
            else if (this._thumbHover)
            {
                state = ScrollBarState.Hot;
                target = this._hbitmapThumbHot;
            }
            else
            {
                state = ScrollBarState.Normal;
                target = this._hbitmapThumb;
            }

            if (target != null)
            {
                return target;
            }

            if (this.thumb.Height <= 0)
            {
                return null;
            }

            var w = this.thumb.Width;
            var h = this.thumb.Height;

            if (w <= 0 || h <= 0)
            {
                return null;
            }

            using (var bmp = new Bitmap(w, h))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    if (ScrollBarRenderer.IsSupported)
                    {
                        if (this.Horizontal)
                        {
                            ScrollBarRenderer.DrawHorizontalThumb(g, new Rectangle(-1, 0, bmp.Width + 2, bmp.Height), state);
                            ScrollBarRenderer.DrawHorizontalThumbGrip(
                                g,
                                new Rectangle(
                                    (bmp.Width / 2) - (this.thumbGrip.Width / 2),
                                    (bmp.Height / 2) - (this.thumbGrip.Height / 2),
                                    this.thumbGrip.Width,
                                    this.thumbGrip.Height),
                                state);
                        }
                        else
                        {
                            ScrollBarRenderer.DrawVerticalThumb(g, new Rectangle(0, -1, bmp.Width, bmp.Height + 2), state);
                            ScrollBarRenderer.DrawVerticalThumbGrip(
                                g,
                                new Rectangle(
                                    (bmp.Width / 2) - (this.thumbGrip.Width / 2),
                                    (bmp.Height / 2) - (this.thumbGrip.Height / 2),
                                    this.thumbGrip.Width,
                                    this.thumbGrip.Height),
                                state);
                        }
                    }
                    else
                    {
                        ControlPaint.DrawButton(g, new Rectangle(0, 0, bmp.Width, bmp.Height), ButtonState.Normal);
                    }
                }

                target = new SafeHandleGDI(bmp.GetHbitmap());
            }

            if (this._thumbDown)
            {
                return this._hbitmapThumbPressed = target;
            }

            if (this._thumbHover)
            {
                return this._hbitmapThumbHot = target;
            }

            return this._hbitmapThumb = target;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            //if (this.Control.Scrollbars == ScrollBars.None)
            //{
            //    this.Visible = false;
            //    return;
            //}

            //if (this.Control.Scrollbars != ScrollBars.Both)
            //{
            //    this.Visible = ((this.Horizontal && this.Control.Scrollbars == ScrollBars.Horizontal)
            //                    || (this.Horizontal == false && this.Control.Scrollbars == ScrollBars.Vertical));
            //}

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (this.Visible == false)
            {
                return;
            }

            var hdc = IntPtr.Zero;
            try
            {
                hdc = e.Graphics.GetHdc();
                this.PaintNative(hdc);
            }
            finally
            {
                if (hdc != IntPtr.Zero)
                {
                    e.Graphics.ReleaseHdc(hdc);
                }
            }
        }

        public void PaintNative(IntPtr hdc)
        {
            var bmpHdc = SafeNativeMethods.CreateCompatibleDC(IntPtr.Zero);

            var bTrack = this.GetBufferTrack();
            var bA1 = this.GetBufferArrow1();
            var bA2 = this.GetBufferArrow2();
            var bThumb = this.GetBufferThumb();

            if (bTrack != null)
            {
                SafeNativeMethods.SelectObject(bmpHdc, bTrack.DangerousGetHandle());
                SafeNativeMethods.BitBlt(hdc, this.track.Left, this.track.Top, this.track.Width, this.track.Height, bmpHdc, 0, 0, NativeConstants.SRCCOPY);
            }

            if (bA1 != null)
            {
                SafeNativeMethods.SelectObject(bmpHdc, bA1.DangerousGetHandle());
                SafeNativeMethods.BitBlt(hdc, this.arrow1.Left, this.arrow1.Top, this.arrow1.Width, this.arrow1.Height, bmpHdc, 0, 0, NativeConstants.SRCCOPY);
            }

            if (bA2 != null)
            {
                SafeNativeMethods.SelectObject(bmpHdc, bA2.DangerousGetHandle());
                SafeNativeMethods.BitBlt(hdc, this.arrow2.Left, this.arrow2.Top, this.arrow2.Width, this.arrow2.Height, bmpHdc, 0, 0, NativeConstants.SRCCOPY);
            }

            if (bThumb != null)
            {
                SafeNativeMethods.SelectObject(bmpHdc, bThumb.DangerousGetHandle());
                SafeNativeMethods.BitBlt(hdc, this.thumb.Left, this.thumb.Top, this.thumb.Width, this.thumb.Height, bmpHdc, 0, 0, NativeConstants.SRCCOPY);
            }

            if (this.PaintBackgroundOverlay != null)
            {
                this.PaintBackgroundOverlay(this, new PaintScrollbarBackgroundArgs(hdc, this.thumb));
            }

            SafeNativeMethods.SelectObject(bmpHdc, IntPtr.Zero);
            SafeNativeMethods.DeleteDC(bmpHdc);
        }

        //public abstract void Layout();

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var p = e.Location;
            base.OnMouseMove(e);

            bool preTrackHover = this._trackHover,
                preThumbHover = this._thumbHover,
                preArrow1Hover = this._arrow1Hover,
                preArrow2Hover = this._arrow2Hover;

            this._trackHover = this.IsScrolling == false && this.track.Contains(p);
            this._thumbHover = this.thumb.Contains(p);
            this._arrow1Hover = this.IsScrolling == false && this.arrow1.Contains(p);
            this._arrow2Hover = this.IsScrolling == false && this.arrow2.Contains(p);

            if (this.IsScrolling)
            {
                var diff = (this.Horizontal ? p.X : p.Y) - this._scrollingStartPosition;

                if (diff != 0)
                {
                    var change = diff / this.trackMultiplicator;
                    this.SetValue(this._scrollingStartValue + (int)change, ValueChangedBy.MouseMove);

                    //return true;
                }
            }

            if (preTrackHover != this._trackHover || preThumbHover != this._thumbHover || preArrow1Hover != this._arrow1Hover || preArrow2Hover != this._arrow2Hover)
            {
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.DoMouseDown(e.Location);
        }

        private void DoMouseDown(Point p)
        {
            this._previousClickLocation = p;
            this._arrow1Down = this.arrow1.Contains(p);
            this._arrow2Down = this.arrow2.Contains(p);

            if (this.IsStepping == false)
            {
                this.IsScrolling = this._thumbDown = this.thumb.Contains(p);
                this.IsStepping = this._arrow1Down || this._arrow2Down;

                if (this.IsStepping)
                {
                    if (this._mouseDownTimer.Enabled == false)
                    {
                        this._previousClickLocation = p;
                        this._mouseDownTimerCalls = 0;
                        this._mouseDownTimer.Start();
                    }
                }
            }

            this._trackDown = this._thumbDown == false && this._arrow1Down == false && this._arrow2Down == false && this.track.Contains(p);

            if (this._trackDown)
            {
                // Clicked on a random place on the track, and should set that to the vision center.

                var v = this.Horizontal ? p.X - this.track.Left : p.Y - this.track.Top;
                this.SetValue((int)(v / this.trackMultiplicator), ValueChangedBy.MouseClick);
                this._trackDown = false;
                this.IsScrolling = true;
            }

            if (this.IsScrolling)
            {
                this._scrollingStartPosition = this.Horizontal ? p.X : p.Y;
                this._scrollingStartValue = this.Value;
            }
            else if (this._arrow1Down)
            {
                // Clicked the first arrow, which should decrease the value.
                this.SetValue(this.Value - this.SmallChange, ValueChangedBy.MouseClick);
            }
            else if (this._arrow2Down)
            {
                // Clicked the second arrow, which should increase the value.
                this.SetValue(this.Value + this.SmallChange, ValueChangedBy.MouseClick);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            //var change = this._trackDown || this._thumbDown || this._arrow1Down || this._arrow2Down;

            this._trackDown = false;
            this._thumbDown = false;
            this._arrow1Down = false;
            this._arrow2Down = false;

            this.IsScrolling = false;
            this.IsStepping = false;
            //this.SetValue(this.ValueIntegral, ValueChangedBy.Unspecified);
            //this.Layout();

            // TODO: Can this call be safely removed?
            //this.PerformLayout();

            this._mouseDownTimer.Stop();
            this.Invalidate();

            //return change;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (this.IsScrolling == false && this.IsStepping == false)
            {
                this._mouseDownTimer.Stop();
                this.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, -10, -10, 0));
                //this.Invalidate();
            }
        }

        private Point _previousClickLocation;
        private int _mouseDownTimerCalls;

        private void mouseDownTimer_Tick(object sender, EventArgs e)
        {
            this._mouseDownTimerCalls++;

            if (this._mouseDownTimerCalls > SystemInformation.KeyboardDelay * 10)
            {
                var args = new MouseEventArgs(MouseButtons.None, 1, this._previousClickLocation.X, this._previousClickLocation.Y, 0);
                this.OnMouseDown(args);

                //if (this.HorizontalScroll.MouseDown(this._previousClickLocation) || this.VerticalScroll.MouseDown(this._previousClickLocation))
                //{
                //    Invalidate();
                //}
            }
        }
    }
}