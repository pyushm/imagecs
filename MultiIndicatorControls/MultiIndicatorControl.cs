using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CustomControls
{
    public abstract class MultiIndicatorControl : Control
    {
        protected string title;
        protected int nPoints;                  // number of control points
        protected Point[] initialLoc;           // initial indicators' locations
        protected Point[] iLoc;                 // indicators' locations
        protected Point sliderLoc;              // location of slider indicator 
        protected Rectangle[] iArea;            // indicators' area rectangles
        protected Brush[] iBrush;               // indicators' area brushes
        protected Pen pen = new Pen(Color.Black);
        protected Brush textBrush = new SolidBrush(Color.Black);
        protected int topOffset;                // top offset of indicators area
        protected int leftOffset;               // left offset of indicators area
        protected int bottomOffset;             // bottom offset of indicators area (font_height)
        protected Rectangle border;             // border of indicators' area
        protected float ratio;                  // border width to height ratio
        protected float range = 1;              // value range
        protected float offset = 0;             // value offset (value at left boundary)
        protected int iSize = 5;                // indicator capture size
        protected int mSize = 3;                // indicator marker size
        protected int activePoint = -1;         // index of active control indicator
        Timer valueChangeTimer;                 // timer firing ValueChanged event only after value is the same after valueChangeDelay
        int valueChangeDelay = 1;
        bool timerToStop;                       // request timer to stop on leaving control
        protected Point[] iLocPrev;		        // indicator value at previous valueChangeTimer tick
        protected Point[] iLocLast;		        // indicator value at last ValueChanged firing
        protected Point[] arrow = new Point[]   { new Point(-3, -4), new Point(-7, 0), new Point(-3, 4), new Point(-3, 1), 
            new Point(3, 1), new Point(3, 4), new Point(7, 0), new Point(3, -4), new Point(3, -1), new Point(-3, -1)};
        protected int NValues                   { get { return nPoints * PointDimension; } } // number of output values
        public int LastPointInd                 { get { return nPoints - 1; } }
        protected virtual int PointDimension    { get { return 0; } } // number of coordinates of control point
        [Category("Behavior"), Description("Range of output values")]
        public float Range                      { get { return range; } set { range = value; } }
        [Category("Behavior"), Description("Minimal output value")]
        public float Offset { get { return offset; } set { offset = value; } }
        [Category("Appearance"), Description("The type of color conrol")]
        public string Title { get { return title; } set { title = value; } }
        public float[] Values
        {
            get
            {
                float[] v = new float[NValues];
                for (int i = 0; i < nPoints; i++)
                {
                    v[PointDimension * (i + 1) - 1] = offset + range * (iLoc[i].X - leftOffset) / border.Width;
                    if (PointDimension == 2)
                        v[2 * i] = (border.Bottom - iLoc[i].Y) / (float)border.Height;
                }
                return v;
            }
        }
        public void SetValues(float[] val)
        {
            if (val.Length < NValues)
                return;
            for (int i = 0; i < nPoints; i++)
            {
                iLoc[i].X = SetX((int)(border.Width * (val[PointDimension * (i + 1) - 1] - offset) / range) + leftOffset);
                if (PointDimension == 2)
                    iLoc[i].Y = SetY(border.Bottom - (int)(val[2 * i] * border.Height));
            }
            ResetSlider();
            Invalidate();
            for (int i = 0; i < nPoints; i++)
                iLocPrev[i] = iLoc[i];
        }
        public event EventHandler ValueChanged; // issued when indicators position changed (long enough in one location or key up)
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            if ((specified & BoundsSpecified.Size) == BoundsSpecified.Size)
            {
                iSize = (int)(iSize * factor.Width + 0.5);
                mSize = (int)(mSize * factor.Height + 0.5);
                Width = (int)(Width * factor.Width + 0.5);
                Height = (int)(Height * factor.Height + 0.5);
                topOffset = (int)(Font.Height * 1.25);
                leftOffset = (int)(Font.Height * 1.6);
                bottomOffset = (int)(Font.Height * 0.95);
                int iaw = Width - leftOffset - mSize - 1;
                int iah = nPoints > 1 ? Height - topOffset - bottomOffset : Height - topOffset - 1;
                border = new Rectangle(leftOffset, topOffset, iaw, iah);
                ratio = (float)iaw / iah;
                SetBrushes();
                for (int i = 0; i < arrow.Length; i++)
                    arrow[i] = new Point((int)(arrow[i].X * factor.Width + 0.5), (int)(arrow[i].Y * factor.Height + 0.5));
                for (int i = 0; i < nPoints; i++)
                    iLoc[i] = initialLoc[i];
            }
            if ((specified & BoundsSpecified.Location) == BoundsSpecified.Location)
                Location = new Point((int)(Location.X * factor.Width), (int)(Location.Y * factor.Height));
        }
        public void Reset()
        {
            for (int i = 0; i < nPoints; i++)
                iLoc[i] = initialLoc[i];
            ResetSlider();
            Invalidate();
        }
        protected void ResetSlider()            
        {
            sliderLoc.X = 0;
            for (int i = 0; i < nPoints; i++)
                sliderLoc.X += iLoc[i].X;
            sliderLoc = new Point(sliderLoc.X / nPoints, Height - bottomOffset / 2+2);
        }
        public MultiIndicatorControl()          // base for all multi-value controls
        {
            this.MouseDown += new MouseEventHandler(_MouseDown);
            this.MouseMove += new MouseEventHandler(_MouseMove);
            this.MouseUp += new MouseEventHandler(_MouseUp);
            this.MouseLeave += new EventHandler(_MouseLeave);
            base.Size = new Size(100, 80);
            valueChangeTimer = new Timer();
            valueChangeTimer.Interval = valueChangeDelay;
            valueChangeTimer.Tick += new EventHandler(ValidateSameValue);
        }
        ~MultiIndicatorControl()		        { valueChangeTimer.Dispose(); }
        public void Initialize(int np)    
        {
            nPoints = np;
            iLoc = new Point[nPoints];
            iLocPrev = new Point[nPoints];
            iLocLast = new Point[nPoints];
            topOffset = (int)(Font.Height*1.25);
            leftOffset = (int)(Font.Height * 1.6);
            bottomOffset = (int)(Font.Height * 0.95);
            int iaw = Width - leftOffset - mSize - 1;
            int iah = nPoints > 1 ? Height - topOffset - bottomOffset : Height - topOffset - 1;
            border = new Rectangle(leftOffset, topOffset, iaw, iah);
            ratio = (float)iaw / iah;
            SetBrushes();
            ResetSlider();
            Invalidate();
        }
        protected int SetX(int v)               { if (v > border.Right) return border.Right; if (v < border.Left) return border.Left; return v; }
        protected int SetY(int v)               { if (v > border.Bottom) return border.Bottom; if (v < border.Top) return border.Top; return v; }
        protected Point Intersection(Point p, Point loc)
        {
            int y;
            int x = int.MaxValue;
            if (loc.X > p.X)
                x = border.Right;
            else if (loc.X < p.X)
                x = border.Left;
            if (x != int.MaxValue)
            {
                y = p.Y + (x - p.X) * (loc.Y - p.Y) / (loc.X - p.X);
                if (y >= border.Top && y <= border.Bottom)
                    return new Point(x, y);
            }
            y = int.MaxValue;
            if (loc.Y > p.Y)
                y = border.Bottom;
            else if (loc.Y < p.Y)
                y = border.Top;
            if (y != int.MaxValue)
            {
                x = p.X + (y - p.Y) * (loc.X - p.X) / (loc.Y - p.Y);
                return new Point(x, y);
            }
            return border.Location;
        }
        public abstract float[] ControlPoints { set; }
        protected abstract void SetIndicatorValues(Point p);
        //protected abstract Point[] IndicatorLocations(Point[] vals);
        protected virtual void SetBrushes() { }
        protected void DrawCommonElements(Graphics g)
        {
            for (int i = 0; i < iArea.Length; i++)
                g.FillRectangle(iBrush[i], iArea[i]);
            if (nPoints > 1)
            {
                Point[] shiftedArrow = new Point[arrow.Length];
                for (int i = 0; i < arrow.Length; i++)
                {
                    shiftedArrow[i] = arrow[i];
                    shiftedArrow[i].Offset(sliderLoc);
                }
                g.DrawPolygon(pen, shiftedArrow);
            }
            SizeF textSize = g.MeasureString(title, Font, Width);
            g.DrawString(title, Font, textBrush, 0, 0);
            g.DrawRectangle(pen, border);
        }
        protected void DrawIndicator(Graphics g, int i)
        {
            Rectangle r = new Rectangle((int)iLoc[i].X - mSize, (int)iLoc[i].Y - mSize, 2 * mSize, 2 * mSize);
            g.FillEllipse(new SolidBrush(Color.White), r);
            g.DrawEllipse(pen, r);
        }
        protected void DrawValueLabel(Graphics g, int i, int y)
        {
            string t = ((int)(100 * Values[PointDimension * (i + 1) - 1]+0.5f)).ToString();
            SizeF tSize = g.MeasureString(t, Font, Width);
            g.DrawString(t, Font, textBrush, leftOffset - tSize.Width, y);
        }
        protected Color MixColors(Color c1, Color c2, float f1)
        {
            if (f1 >= 1)
                return c1;
            if (f1 <= 0)
                return c2;
            float f2 = 1 - f1;
            return Color.FromArgb((int)(c1.R * f1 + c2.R * f2), (int)(c1.G * f1 + c2.G * f2), (int)(c1.B * f1 + c2.B * f2));
        }
        void ResetIndicators()                  // resets indicators to final state
        {
            if (activePoint < 0)
                return;
            activePoint = -1;
            //if (ValueChanged != null)
            //    ValueChanged(this, null);
            Invalidate();
        }
        bool IsInProximity(Point p, PointF loc) { return Math.Abs(p.X - loc.X) < iSize && Math.Abs(p.Y - loc.Y) < iSize; }
        int ActiveIndicator(Point p)            // finds active indicator from point
        {
            for (int i = 0; i < nPoints; i++)
                if (IsInProximity(p, iLoc[i]))
                    return i;
            if (IsInProximity(p, sliderLoc))
                return nPoints;
            return -1;
        }
        void _MouseLeave(object s, EventArgs e) { /*ResetIndicators();*/ }
        void _MouseUp(object s, MouseEventArgs e) { ResetIndicators(); timerToStop = true; }
        void _MouseMove(object s, MouseEventArgs e)
        {
            if (activePoint < 0)
                return;
            SetIndicatorValues(e.Location);
            Invalidate();
        }
        void _MouseDown(object s, MouseEventArgs e)
        {
            activePoint = ActiveIndicator(e.Location);
            valueChangeTimer.Start();
            timerToStop = false;
        }
        void ValidateSameValue(object sender, System.EventArgs e)
        {
            if (ValueChanged == null)
                return;
            bool eventToFire = true;
            for (int i = 0; i < nPoints; i++)
            {
                if (iLocPrev[i] != iLoc[i])
                {
                    eventToFire = false;
                    iLocPrev[i] = iLoc[i];
                }
            }
            if (timerToStop)
            {
                valueChangeTimer.Stop();
                eventToFire = true;
            }
            if (!eventToFire)
                return;
            eventToFire = false;
            for (int i = 0; i < nPoints; i++)
            {
                if (iLocLast[i] != iLoc[i])
                {
                    eventToFire = true;
                    iLocLast[i] = iLoc[i];
                }
            }
            if (eventToFire)
                ValueChanged(this, null);
        }
        public string ControlDebugString(string caption)
        {
            string s = caption+' '+Name + ' ';
            foreach (var v in Values)
                s += ", " + v.ToString("f2");
            return s;
        }
    }
}
