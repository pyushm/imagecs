using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text;

namespace Presentation
{
    public class PlotCurve       // polyline on canvas
    {
        public CurveSet Set { get; private set; }           // curve group attached to canvas area
        int ind = -1;                                       // index of the curve in Set (-1  - not attached)
        public HistoryCurves History { get; private set; }  // history traces accompanied polyline
        public Polyline Curve { get; private set; }         // polyiline to display
        public float[] Raw { get; private set; }            // raw values to display
        public Brush Brush { get { return Curve.Stroke; } set { Curve.Stroke = value; if (LinkedLabel != null) LinkedLabel.Foreground = Brush; } }
        public string Name { get; private set; }
        public Color Color { set { Curve.Stroke = new SolidColorBrush(value); if (LinkedLabel != null) LinkedLabel.Foreground = Brush; } }
        public LinkedLabel LinkedLabel { get; private set; } // label controling display of curve and history
        public double Thickness { set { Curve.StrokeThickness = value; } }
        //public PlotCurve(string name, CurveSet cs, Color? c = null)
        //{
        //    Name = name;
        //    Curve = new Polyline();
        //    Curve.Points = new PointCollection();
        //    LinkedLabel = new LinkedLabel(this, Name);
        //    Color = c ?? cs.PlotArea.Properties.AutoColor(cs.NCurves);
        //    ind = AttachTo(cs);
        //}
        public PlotCurve(string name, CurveSet cs, float[] val, Brush brush = null)
        {
            Name = name;
            Curve = new Polyline();
            Curve.Points = new PointCollection();
            LinkedLabel = new LinkedLabel(this, Name);
            Brush = brush ?? new SolidColorBrush(cs.PlotArea.Properties.AutoColor(cs.PlotArea.NCurves));
            ind = AttachTo(cs);
            SetRawPoints(val);
        }
        public void AddCaption(double x, double y, string text = null) { Set.PlotArea.Add(new Caption(text ?? Name, x, y, Brush)); }
        public int AttachTo(CurveSet cs)
        {
            if (cs == Set)
                return ind;
            if (Set != null)
            {
                Set.Curves.Remove(this);
                var uic = Set.PlotArea.Children;
                uic.Remove(Curve);
                uic.Remove(LinkedLabel);
                uic.Remove(History.Curves);
            }
            Set = cs;
            if (Set != null)
            {
                Set.Curves.Add(this);
                return Set.Curves.Count - 1;
            }
            return -1;
        }
        void DrawCurve()
        {
            if (Set == null)
                return;
            var pa = Set.PlotArea;
            Curve.StrokeThickness = pa.Properties.CurveThickness;
            var uic = pa.Children;
            uic.Add(Curve);
            if (LinkedLabel != null)
            {
                LinkedLabel.Helper = pa.TextHelper;
                uic.Add(LinkedLabel);
                double offset = pa.TickLength < 0 ? 7 - pa.TickLength : 7;
                LinkedLabel.SetPosition(Set.Left ? -offset - 3 - LinkedLabel.TextWidth : pa.Width + offset, (ind+1) * pa.TextHelper.TextHeight); // default
                LinkedLabel.Foreground = Brush;
                LinkedLabel.LinkedUI = Curve;
                if (History != null)
                {
                    LinkedLabel.SuplementUI = History.Curves;
                    uic.Add(History.Curves);
                }
            }
        }
        public void AddHistoryCurves()
        {
            if (History == null)
            {
                History = new HistoryCurves(Set, Brush, Curve.StrokeThickness / 5);
                LinkedLabel.SuplementUI = History.Curves;
            }
        }
        public void ClearHistory() { History?.Clear(); }
        public void ClearKeepHistory() { Curve.Points.Clear(); }
        public void Clear() { Curve.Points.Clear(); History?.Clear(); }
        public void AppendHistory(string hmarker = "") { History.AddCurve(Curve.Points, hmarker); }
        public void SetRawPoints(float[] data) { Raw = data; if(Raw != null) Curve.Points = new PointCollection(new Point[Raw.Length]); }
        public void Draw()
        {
            for (int i = 0; i < Raw.Length; i++)
                SetPoint(i, Set.PlotArea.XValues[i], Raw[i]);
            DrawCurve();
        }
        public void SetPoint(int i, double x, double y) { Curve.Points[i] = Set.ToPlot(x, y); }
        public void AddPoint(double x, double y) { Curve.Points.Add(Set.ToPlot(x, y)); }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("R\t" + Name);
            for (int i = 0; i < Raw.Length; i++)
                sb.Append(Environment.NewLine + string.Format("{0:0.0000}\t{1:0.0000}", Set.PlotArea.XValues[i], Raw[i]));
            return sb.ToString();
        }
    }
    public class HistoryCurves         // polyline collection on canvas
    {
        public Path Curves { get; private set; } = new Path();
        public List<string> Markers { get; private set; } = new List<string>();
        public HistoryCurves(CurveSet group, Brush brush, double thickness)
        {
            Curves.Data = new PathGeometry();
            Curves.Stroke = brush;
            Curves.StrokeThickness = thickness;
            group.Add(Curves);
        }
        public void AddCurve(PointCollection points, string marker = "")
        {
            if (points == null || points.Count < 2)
                return;
            PathFigure curve = new PathFigure();
            curve.StartPoint = points[0];
            curve.Segments.Add(new PolyLineSegment(points, true));
            ((PathGeometry)Curves.Data).Figures.Add(curve);
            Markers.Add(marker);
        }
        public void Clear() { ((PathGeometry)Curves.Data).Figures.Clear(); }
    }
    public class CurveSet        // collection of curves with same Y-scale axis representing their scaling
    {
        ScaleLabel minLabel;
        ScaleLabel maxLabel;
        public bool Left { get; private set; }              // left axis
        public List<PlotCurve> Curves { get; private set; } = new List<PlotCurve>();
        public int NCurves => Curves.Count;
        public PlotArea PlotArea { get; private set; }
        public Range YRange { get; private set; } = new Range();
        double Scale { get { return PlotArea.Height / YRange.Length; } } // pixels/value
        public bool Valid => YRange.IsValid; 
        public CurveSet(PlotArea a, bool left_ = true) { PlotArea = a; Left = left_; }
        public Point ToPlot(double x, double y) { return new Point(PlotArea.ToPlot(x), PlotArea.Height - Scale * (y - YRange.Min)); }
        public void Add(UIElement uie) { PlotArea.Add(uie); }
        public void Remove(UIElement uie) { PlotArea.Remove(uie); }
        public void Clear() { foreach (var c in Curves) c.Clear(); }
        public void ClearKeepHistory() { foreach (var c in Curves) c.ClearKeepHistory(); }
        public void SetRange(double nval, double xval, bool round = true, bool include0 = true)
        {   // sets Y-range for side
            YRange = new Range(round, nval, xval, include0);
            DrawMinMaxLebels();
        }
        public void SetRange(Range dataRange, bool include0 = true)
        {   // sets Y-range for side
            YRange = new Range(true, dataRange.Min, dataRange.Max, include0);
            DrawMinMaxLebels();
        }
        void CreateVerticalScaleLabels(double xOffset, Range yr)
        {
            minLabel = CreateVerticalAxisLabel(PlotArea.Height, xOffset, yr.Min);
            maxLabel = CreateVerticalAxisLabel(0, xOffset, yr.Max);
        }
        ScaleLabel CreateVerticalAxisLabel(double y, double xOffset, double val)
        {
            double xPos = Left ? -xOffset : PlotArea.Width + xOffset;
            ScaleLabel tb = new ScaleLabel(new Point(xPos, y - PlotArea.TextHelper.TextHeight/2), val, PlotArea);
            tb.Foreground = PlotArea.GridBrush;
            return tb;
        }
        public void DrawMinMaxLebels()
        {
            if (!Valid)
                return;
            CreateVerticalScaleLabels(PlotArea.TextHelper.TextHeight / 3, YRange);
            PlotArea.Add(minLabel);
            PlotArea.Add(maxLabel);
        }
    }
}
