using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace CustomControls
{
    public partial class GrayscaleControl : MultiIndicatorControl
    {
        protected override int PointDimension { get { return 2; } }
        [Category("Appearance"), Description("Amount and initial positions (.X:0-100, .Y:0-100) of control points")]
        public override Point[] ValueLocations
        {
            get
            {
                Point[] ip = new Point[nPoints];
                for (int i = 0; i < nPoints; i++)
                    ip[i] = new Point((initialLoc[i].X - leftOffset) * 100 / border.Width,
                                      (border.Bottom - initialLoc[i].Y) * 100 / border.Height);
                return ip;
            }
            set
            {
                if (value.Length < 2)
                    throw new Exception("At least 2 points required for Grayscale control. Enterred " + value.Length);
                SetArrays(value.Length);
                SetLayout();
                for (int i = 0; i < nPoints; i++)
                    initialLoc[i] = new Point(SetX(leftOffset + border.Width * value[i].X / 100), 
                                              SetY(border.Bottom - value[i].Y * border.Height / 100));
                Reset();
            }
        }
        public GrayscaleControl()           
        {
            InitializeComponent();
        }
        protected override void SetBrushes()// sets control background brushes
        {
            if (nPoints == 0)
                return;
            iArea = new Rectangle[] { new Rectangle(border.Left, border.Top, border.Width, border.Height) };
            Color b = Color.Black;
            Color w = Color.White;
            iBrush = new Brush[] { new LinearGradientBrush(new Point(0, border.Bottom), new Point(0, border.Top), b, w) };
            Invalidate();
        }
        protected override void SetIndicatorValues(Point loc) // set control values based on indicators' locations
        {
            if (activePoint < 0 || activePoint > nPoints)
                return;
            if (activePoint == nPoints)     // slider
            {
                int x = SetX(loc.X);
                int dx = x - sliderLoc.X;
                for (int i = 0; i < nPoints; i++)
                    iLoc[i].X = Math.Min(Math.Max(iLoc[i].X + dx, border.Left), border.Right);
                sliderLoc.X = x;
            }
            else
            {
                int yMin = activePoint == LastPointInd ? border.Top : (int)iLoc[activePoint + 1].Y;
                int yMax = activePoint == 0 ? border.Bottom : (int)iLoc[activePoint - 1].Y;
                iLoc[activePoint].Y = loc.Y = Math.Min(Math.Max(loc.Y, yMin), yMax);
                if (activePoint == 0)
                    iLoc[0] = Intersection(iLoc[1], loc);
                else if (activePoint == LastPointInd)
                {
                    int dx = iLoc[LastPointInd].X;
                    iLoc[LastPointInd] = Intersection(iLoc[nPoints - 2], loc);
                    dx = iLoc[LastPointInd].X - dx;
                    for (int i = 1; i < LastPointInd; i++)
                        iLoc[i].X += (2*dx+Math.Sign(dx)) * i / (2*LastPointInd);
                }
                else
                    iLoc[activePoint].X = SetX(loc.X);
            }
            ResetSlider();
        }
        protected override void OnPaint(PaintEventArgs pe)
        {
            if (nPoints == 0)
                return;
            base.OnPaint(pe);
            DrawCommonElements(pe.Graphics);
            pe.Graphics.DrawLines(pen, iLoc);
            for (int i = 0; i < nPoints; i++)
            {
                DrawIndicator(pe.Graphics, i);
                DrawValueLabel(pe.Graphics, i, border.Top + (LastPointInd - i) * border.Height / LastPointInd-7);
            }
        }
    }
}
