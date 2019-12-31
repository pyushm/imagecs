using System;
using System.Windows;

namespace ShaderEffects
{
    class ControlPoint
    {
        Rect range;                 // range of allowed indicator locations as a fraction of indicator area 
        Mobility type;              // defines freedom of movement
        string helpFormat;          // tooltip string format (allows do display value)
        Point initialLoc;           // location at creation and reset
        Point loc;                  // current point location
        double valueRange;          // coefficient of value transform
        double valueOffset;         // offset of value transform
        Mobility valueRangeEffect;  // specifies in which direction value range works
        int valueRangeIndex;        // index of minMax array defining valueRange and valueRangeIndex
        double MinX                 { get { return range.Left; } }
        double MinY                 { get { return range.Top; } }
        double MaxX                 { get { return range.Right; } }
        double MaxY                 { get { return range.Bottom; } }
        bool IsValueRangeVertical   { get { return valueRangeEffect == Mobility.Area || valueRangeEffect == Mobility.Vertical; } }
        bool IsValueRangeHorizontal { get { return valueRangeEffect == Mobility.Area || valueRangeEffect == Mobility.Horizontal; } }
        internal double RangeX      { get { return range.Width; } }
        internal double RangeY      { get { return range.Height; } }
        internal double ValueX      { get { double vx = (loc.X - MinX) / RangeX; return IsValueRangeHorizontal ? valueOffset + valueRange * vx : vx; } }
        internal double ValueY      { get { double vy = (MaxY - loc.Y) / RangeY; return IsValueRangeVertical ? valueOffset + valueRange * vy : vy; } }
        internal double Value       { get { return type == Mobility.Vertical ? ValueY : ValueX; } }
        internal Point Loc          { get { return loc; } }
        internal int ValueRangeIndex{ get { return valueRangeIndex; } }
        internal Mobility Mobility  { get { return type; } }
        internal ControlPoint(Mobility type_, Rect range_, Point loc_, int rangeIndex, string helpFormat_)
        {
            type = type_;
            range = range_;
            initialLoc = loc = loc_;
            valueRangeIndex = rangeIndex;
            helpFormat = helpFormat_;
            valueRange = 1;
            valueOffset = 0;
        }
        double XPosition(double vx) { return IsValueRangeHorizontal ? (vx - valueOffset) * RangeX / valueRange + MinX : vx * RangeX + MinX; }
        double YPosition(double vy) { return IsValueRangeVertical ? (valueOffset - vy) * RangeY / valueRange + MaxY : vy * RangeY + MaxY; }
        double XPositionInRange(double pos) { return pos > MaxX ? MaxX : pos < MinX ? MinX : pos; }
        double YPositionInRange(double pos) { return pos > MaxY ? MaxY : pos < MinY ? MinY : pos; }
        Point LocationOnBorder(Point p, double posx, double posy)
        {
            double y;
            double x = double.MaxValue;
            if (posx > p.X)
                x = MaxX;
            else if (posx < p.X)
                x = MinX;
            if (x != int.MaxValue)
            {
                y = p.Y + (x - p.X) * (posy - p.Y) / (posx - p.X);
                if (y >= MinY && y <= MaxY)
                    return new Point(x, y);
            }
            y = double.MaxValue;
            if (posy > p.Y)
                y = MaxY;
            else if (posy < p.Y)
                y = MinY;
            if (y != int.MaxValue)
            {
                x = p.X + (y - p.Y) * (posx - p.X) / (posy - p.Y);
                return new Point(x, y);
            }
            return range.Location;
        }
        internal void SetToInitial() { loc = initialLoc; }
        internal void SetValueRange(Point minMax)
        {
            double vx = ValueX;
            double vy = ValueY;
            valueOffset = minMax.X;
            valueRange = minMax.Y - minMax.X;
            switch (Mobility)
            {
                case Mobility.Border:
                case Mobility.Area:
                    SetValues(vx, vy);
                    break;
                case Mobility.Horizontal:
                    SetValue(vx);
                    break;
                case Mobility.Vertical:
                    SetValue(vy);
                    break;
            }
        }
        internal void SetValue(double val)
        {
            if (type == Mobility.Horizontal)
                SetLocationX(XPosition(val));
            else if (type == Mobility.Vertical)
                SetLocationY(YPosition(val));
        }
        internal void SetValues(double valx, double valy)
        {
            if (type == Mobility.Area || type == Mobility.Border)
                SetLocation(XPosition(valx), YPosition(valy));
        }
        internal void SetLocationX(double pos) { loc.X = XPositionInRange(pos); }
        internal void SetLocationY(double pos) { loc.Y = YPositionInRange(pos); }
        internal void SetLocation(double posx, double posy)
        {
            if (type == Mobility.Area)
            {
                loc.X = XPositionInRange(posx);
                loc.Y = YPositionInRange(posy);
            }
            else if (type == Mobility.Border)
                loc = LocationOnBorder(new Point((MinX + MaxX) / 2, (MinY + MaxY) / 2), posx, posy);
        }
        internal void SetLocationOnBorder(Point pivotCenter, double posx, double posy)
        {
            if (type == Mobility.Border)
                loc = LocationOnBorder(pivotCenter, posx, posy);
        }
        public override string ToString()
        {
            return type == Mobility.Area || type == Mobility.Border ?
                string.Format(helpFormat, ValueX.ToString("f2"), ValueY.ToString("f2")) :
                string.Format(helpFormat, Value.ToString("f2"));
        }
    }
    class ControlPointCollection
    {
        ControlPoint[] indicators;  // initial indicator values
        Point[] minMax;
        int nValues;                // number of output values (2 for area & indicatorArea; 1 for others)
        internal Point[] MinMax     { get { return minMax; } set { minMax = value; SetValueRanges(); } }
        internal ControlPoint this[int i] { get { return indicators[i]; } }
        internal ControlPoint[] Indicators { get { return indicators; } }
        internal ControlPointCollection(ControlPoint[] indicators_)
        {
            indicators = indicators_;
            nValues = 0;
            foreach (ControlPoint cp in indicators)
            {
                switch (cp.Mobility)
                {
                    case Mobility.Border:
                    case Mobility.Area:
                        nValues += 2;
                        break;
                    case Mobility.Horizontal:
                    case Mobility.Vertical:
                        nValues += 1;
                        break;
                }
            }
        }
        public double[] GetValues() 
        {
            double[] val = new double[nValues];
            int index = 0;
            foreach (ControlPoint cp in indicators)
            {
                switch (cp.Mobility)
                {
                    case Mobility.Border:
                    case Mobility.Area:
                        val[index++] = cp.ValueX;
                        val[index++] = cp.ValueY;
                        break;
                    case Mobility.Horizontal:
                    case Mobility.Vertical:
                        val[index++] = cp.Value;
                        break;
                }
            }
            return val;
        }
        public void SetValues(double[] val)
        {
            int index = 0;
            try
            {
                foreach (ControlPoint cp in indicators)
                {
                    switch (cp.Mobility)
                    {
                        case Mobility.Border:
                        case Mobility.Area:
                            cp.SetValues(val[index++], val[index++]);
                            break;
                        case Mobility.Horizontal:
                        case Mobility.Vertical:
                            cp.SetValue(val[index++]);
                            break;
                    }
                }
            }
            catch { }
        }
        public void SetToInitial(bool all)
        {
            foreach (ControlPoint cp in indicators)
                if (all || cp.Mobility == Mobility.HorizontalCommon)
                    cp.SetToInitial();
        }
        void SetValueRanges()       
        {
            foreach (ControlPoint cp in indicators)
            {
                int ind = cp.ValueRangeIndex;
                if (ind >= 0 && ind < minMax.Length)
                    cp.SetValueRange(minMax[ind]);
            }
        }
        internal int LastPointInd { get { return indicators.Length - 1; } }
        internal void SetIndicatorValues(Point newLocation, Rect area, int index)
        {
            if (index < 0)
                return;
            Point newPoint = new Point((newLocation.X - area.Left) / area.Width, (newLocation.Y - area.Top) / area.Height);
            ControlPoint ai = indicators[index];
            //Console.Write(newLocation.X.ToString("f0")+" moves index=" + activeIndex + " from " + ai.ToString());
            if (ai.Mobility == Mobility.Area || ai.Mobility == Mobility.Border)
                ai.SetLocation(newPoint.X, newPoint.Y);
            if (ai.Mobility == Mobility.Horizontal)
                ai.SetLocationX(newPoint.X);
            else if (ai.Mobility == Mobility.HorizontalCommon)
            {
                double dv = newPoint.X - ai.Loc.X;
                ai.SetLocationX(newPoint.X);
                for (int i = 0; i < indicators.Length; i++)
                    if (i != index)
                        indicators[i].SetLocationX(indicators[i].Loc.X + dv);
            }
            //Console.WriteLine(" to "+ai.ToString());
        }
        public override string ToString()
        {
            string s = "ControlPointCollection=" + indicators.Length + Environment.NewLine;
            foreach (ControlPoint cp in indicators)
                s += cp.ToString() + Environment.NewLine;
            return s;
        }
    }
}
