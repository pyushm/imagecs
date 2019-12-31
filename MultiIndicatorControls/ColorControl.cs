using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace CustomControls
{
    public partial class ColorControl : MultiIndicatorControl
    {
        protected override int PointDimension { get { return 1; } }
        [Category("Appearance"), Description("Amount and initial values (.X:0-100) and colors (.Y:Argb) of control points")]
        public override Point[] ValueLocations  
        {
            get
            {
                Point[] ip=new Point[nPoints];
                for (int i = 0; i < nPoints; i++)
                    ip[i] = new Point((initialLoc[i].X - leftOffset) * 100 / border.Width, colorPoints[i].ToArgb());
                return ip;
            }
            set
            {
                SetArrays(value.Length);
                for (int i = 0; i < nPoints; i++)
                    colorPoints[i] = Color.FromArgb(value[i].Y);
                SetLayout();
                for (int i = 0; i < nPoints; i++)
                {
                    int x = SetX(leftOffset + border.Width * value[i].X / 100);
                    initialLoc[i] = new Point(x, iArea[i].Y + iArea[i].Height / 2);
                }
                Reset();
            }
        }
        public ColorControl()               
        {
            InitializeComponent();
        }
        protected override void SetBrushes()// sets control background brushes
        {
            if (nPoints == 0)
                return;
            int m = (leftOffset + Width) / 2;
            Color c, cc;
            int gg = 170;
            Color g = Color.FromArgb(255, gg, gg, gg);
            int iah = (border.Height + nPoints / 2) / nPoints;
            if (type == Adjustment.Saturation)
            {
                iArea = new Rectangle[nPoints];
                iBrush=new Brush[nPoints];
            }
            //else if (type == Adjustment.Hue)
            //{
            //    iArea = new Rectangle[nPoints * 2];
            //    iBrush = new Brush[nPoints * 2];
            //}
            for (int i = 0; i < nPoints; i++)
            {
                int y = border.Y + i * iah+1;
                if (type == Adjustment.Saturation)
                {
                    iArea[i] = new Rectangle(border.X, y, border.Width, iah);
                    c = MixColors(g, colorPoints[i], (offset+range+1)/2);
                    cc = MixColors(g, colorPoints[i], (offset+1)/2);
                    iBrush[i] = new LinearGradientBrush(new Point(leftOffset - 1, 0), new Point(Width, 0), c, cc);
                }
                //else if (type == Adjustment.Hue)
                //{
                //    if (i > 0)
                //        c = colorPoints[i - 1];
                //    else
                //        c = colorPoints[LastPointInd];
                //    cc = MixColors(c, colorPoints[i], range);
                //    iArea[i] = new Rectangle(border.X + 1, y, (border.Width + 1) / 2, iah);
                //    iArea[i + nPoints] = new Rectangle(border.X + (border.Width + 1) / 2 + 1, y, (border.Width + 1) / 2, iah);
                //    iBrush[i] = new LinearGradientBrush(new Point(leftOffset - 1, 0), new Point(m, 0), cc, colorPoints[i]);
                //    if (i < LastPointInd)
                //        c = colorPoints[i + 1];
                //    else
                //        c = colorPoints[0];
                //    cc = MixColors(c, colorPoints[i], range);
                //    iBrush[i + nPoints] = new LinearGradientBrush(new Point(m - 1, 0), new Point(Width, 0), colorPoints[i], cc);
                //}
            }
            Invalidate();
        }
        protected override void SetIndicatorValues(Point loc)      // set control values based on indicators' locations
        {
            int x = SetX(loc.X);
            if (activePoint < nPoints)
                iLoc[activePoint].X = x;
            else
            {
                int dv = x - sliderLoc.X;
                for (int i = 0; i < nPoints; i++)
                    iLoc[i].X = SetX(iLoc[i].X + dv);
            }
            ResetSlider();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs pe)
        {
            if (nPoints == 0)
                return;
            base.OnPaint(pe);
            DrawCommonElements(pe.Graphics);
            PointF[] l = new PointF[nPoints + 2];
            for (int i = 0; i < nPoints; i++)
                l[i + 1] = iLoc[i];
            float x = (l[1].X + l[nPoints].X) / 2;
            l[0] = new PointF(x, iArea[0].Top);
            l[nPoints + 1] = new PointF(x, iArea[LastPointInd].Bottom - 1);
            pe.Graphics.DrawLines(pen, l);
            for (int i = 0; i < nPoints; i++)
            {
                DrawIndicator(pe.Graphics, i);
                DrawValueLabel(pe.Graphics, i, iArea[i].Y);
            }
        }
    }
}
