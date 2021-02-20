using System;
using System.Drawing;
using System.Windows.Forms;

namespace Eliason.Scrollbar
{
    public class AdvVScrollbar : AdvScrollbarBase
    {
        public AdvVScrollbar()
        {
            this.Width = SystemInformation.VerticalScrollBarWidth;
        }

        public override bool Horizontal
        {
            get { return false; }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            double max = this.Maximum;
            var viewPortSize = this.Height;
            if (max - 2 <= viewPortSize)
            {
                this.Visible = false;
                return;
            }

            this.Visible = true;

            var arrowThickness = SystemInformation.VerticalScrollBarArrowHeight;
            var thumbThickness = SystemInformation.VerticalScrollBarThumbHeight;
            const int otherDimensionOffset = 0;

            var value = this.Value; // this.IsScrolling ? this.Value : this.ValueIntegral;

            var viewportScale = viewPortSize / max;
            double barSize = (viewPortSize - (arrowThickness * 2));
            var initialThumbSize = (barSize * viewportScale);
            double thumbSize = Math.Max(thumbThickness, (int)initialThumbSize);
            var trueBarSize = barSize - thumbSize;
            var barScale = (trueBarSize / max);
            var startCoordinate = (((value + (viewPortSize * (value / (max - viewPortSize)))) + (thumbSize / barScale)) * barScale);

            this.trackMultiplicator = (barSize / max);

            var a = ((this.trackMultiplicator) * arrowThickness);
            track = new Rectangle(otherDimensionOffset, arrowThickness, Thickness, (int)barSize);
            thumb = new Rectangle(otherDimensionOffset, (int)(arrowThickness + startCoordinate - thumbSize), Thickness, (int)(thumbSize - a));
            arrow1 = new Rectangle(otherDimensionOffset, 0, Thickness, arrowThickness);
            arrow2 = new Rectangle(otherDimensionOffset, viewPortSize - arrowThickness, Thickness, arrowThickness);
            thumbGrip = new Rectangle(
                thumb.X,
                thumb.Y + (thumb.Height / 2) - (thumbThickness / 2),
                thumb.Width,
                thumbThickness);
        }
    }
}