using System;
using System.Drawing;
using System.Windows.Forms;

namespace Eliason.Scrollbar
{
    public class AdvHScrollbar : AdvScrollbarBase
    {
        public AdvHScrollbar()
        {
            this.Height = SystemInformation.HorizontalScrollBarHeight;
        }

        public override bool Horizontal
        {
            get { return true; }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            double max = this.Maximum;
            var viewPortSize = this.Width;
            if (max - 2 <= viewPortSize)
            {
                this.Visible = false;
                return;
            }

            this.Visible = true;

            var arrowThickness = SystemInformation.HorizontalScrollBarArrowWidth;
            var thumbThickness = SystemInformation.HorizontalScrollBarThumbWidth;
            const int otherDimensionOffset = 0;

            var value = this.Value; //this.IsScrolling ? this.Value : this.ValueIntegral;

            var viewportScale = viewPortSize / max;
            double barSize = (viewPortSize - (arrowThickness * 2));
            var initialThumbSize = (barSize * viewportScale);
            double thumbSize = Math.Max(thumbThickness, (int)initialThumbSize);
            var trueBarSize = barSize - thumbSize;
            var barScale = (trueBarSize / max);
            var startCoordinate = (((value + (viewPortSize * (value / (max - viewPortSize)))) + (thumbSize / barScale)) * barScale);

            this.trackMultiplicator = (barSize / max);

            var a = ((this.trackMultiplicator) * arrowThickness);
            track = new Rectangle(arrowThickness, otherDimensionOffset, (int)(barSize), Thickness);
            thumb = new Rectangle((int)(arrowThickness + startCoordinate - thumbSize), otherDimensionOffset, (int)(thumbSize - a), Thickness);
            arrow1 = new Rectangle(0, otherDimensionOffset, arrowThickness, Thickness);
            arrow2 = new Rectangle(track.Right, otherDimensionOffset, arrowThickness, Thickness);
            thumbGrip = new Rectangle(
                thumb.X + (thumb.Width / 2) - (thumbThickness / 2),
                thumb.Y,
                thumbThickness,
                thumb.Height);
        }
    }
}