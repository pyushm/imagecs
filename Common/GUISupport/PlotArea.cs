using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Presentation
{
    public delegate void RedrawNotify();
    public class PlotProperties
    {
        static public Color Black = Color.FromRgb(0, 0, 0);
        static public Color Red = Color.FromRgb(200, 0, 0);
        static public Color Green = Color.FromRgb(0, 150, 0);
        static public Color Blue = Color.FromRgb(0, 0, 200);
        static public Color Cyan = Color.FromRgb(0, 130, 150);
        static public Color Purple = Color.FromRgb(192, 0, 192);
        static public Color Orange = Color.FromRgb(240, 150, 30);
        static public Color Yellow = Color.FromRgb(190, 170, 0);
        public Color[] Colors { get; set; }
        public double CurveThickness { get; set; } = 1;
        public double BorderThickness { get; set; } = 1;
        public double MeshThickness { get; set; } = 0.1;
        public double LineThickness { get; set; } = 0.3;
        public double TickLength { get; set; } = 5;
        public bool TicksInside { get; set; } = true;
        public PlotProperties() { Colors = new Color[] { Blue, Purple, Red, Cyan, Green, Orange, Black, Yellow }; }
        public Color AutoColor(int i) { return Colors[i % Colors.Length]; }
        public string Label(double val)
        {
            //return val.ToString();
            if (val == 0)
                return "0";
            bool negative = val < 0;
            if (negative)
                val = -val;
            int e = (int)Math.Floor(Math.Log10(val));
            var vn = val / Math.Pow(10, e); // 1-10
            string[] fa = { "f0", "f1", "f2", "f3", "f4" };
            int i = 0;
            for (; i < fa.Length - 1; i++)
                if (double.Parse(val.ToString(fa[i])) == val)
                    break;
            string label = e > 4 ? vn.ToString(fa[i]) + 'e' + e.ToString() :
                    e > -3 ? val.ToString(fa[i]) :
                             vn.ToString(fa[i]) + 'e' + e.ToString();
            if (negative)
                label = '-' + label;
            return label;
        }
    }
    public struct Side
    {
        public const int None = 0;
        public const int Top = 1;
        public const int Left = 2;
        public const int Bottom = 4;
        public const int Right = 8;
        public const int XAxis = 16;
        public int side;   // ticks location
        public static implicit operator int(Side s) => s.side;
        public static implicit operator Side(int s) => new Side(s);
        public Side(int s) { side = s; }
        public static Side Vertical(int s = Left | Right) { return new Side(s); }
        public static Side Horizontal(int s = XAxis) { return new Side(s); }
        public bool isR => (side & Right) == Right;
        public bool isL => (side & Left) == Left;
        public bool isV => isR || isL;
        public bool isT => (side & Top) == Top;
        public bool isB => (side & Bottom) == Bottom;
        public bool isX => (side & XAxis) == XAxis;
        public bool isH => isT || isB || isX;
    }
    public class PlotArea       // area with 2 sets of curves scaled as left and right axis
    {
        double axisPos;         // x axis pos - fraction of height (0 - bottom, 1 - top)
        bool boxFrame = true;   // always box frame
        Side vTicks;            // vertical ticks
        Side hTicks;
        Canvas canvas;          // plot frame placed on canvas edges; canvas needs margin to put outside elements
        public TextHelper TextHelper { get; set; } = TextHelper.CaptionHelper;
        public PlotProperties Properties { get; private set; } = new PlotProperties();
        public TextBlock xTitle;
        public double Width => canvas.Width;
        public double Height => canvas.Height;
        double dy => Height * YRange.TickStep / YRange.Length;
        double y0 => Height * (YRange.FirstTick - YRange.Min) / YRange.Length;
        double dx => Width * XRange.TickStep / XRange.Length;
        double x0 => Width * (XRange.FirstTick - XRange.Min) / XRange.Length;
        PathFigureCollection figures;
        NumericLabel[] xLabels = new NumericLabel[0];
        public int XMarkersCount => xLabels.Length;
        public RedrawNotify RedrawNotify;
        public UIElementCollection Children => canvas.Children;
        public CurveSet LeftSet { get; private set; }
        public CurveSet RightSet { get; private set; }
        public double TickLength { get; private set; }
        public Brush GridBrush { get; set; } = new SolidColorBrush(Colors.Black);
        public Range XRange { get; set; }
        public Range YRange { get; private set; } = new Range(0, 100);  // Y Range defining vertical ticks; default 10 eqully distributed between top and bottom
        public float[] XValues { get; private set; } // X-value array for all curves in a plot
        public double ScaleX => Width / XRange.Length;
        public int NCurves => LeftSet.NCurves + RightSet.NCurves;
        public PlotArea(Canvas c, Thickness margin, double fontSize = 14, FontFamily font = null)
        {
            canvas = c;
            canvas.Margin = margin;
            TextHelper.FontSize = fontSize;
            if(font != null) TextHelper.FontFamily = font;
            LeftSet = new CurveSet(this, true);
            RightSet = new CurveSet(this, false);
        }
        public double ToPlot(double x) { return ScaleX * (x - XRange.Min); }
        public void Clear() { LeftSet?.Clear(); RightSet?.Clear(); }    // removes all curves with history
        public void ClearKeepHistory() { LeftSet?.ClearKeepHistory(); RightSet?.ClearKeepHistory(); }
        public void RedrawFrame()
        {
            Children.Clear();
            DrawGridLines();
            DrawHorizontalTicks();
            DrawXTitle();
            LeftSet.DrawMinMaxLebels();
            RightSet.DrawMinMaxLebels();
        }
        public void SetXTitle(string xName)
        {
            if (xName == null || xName.Length == 0)
                return;
            xTitle = new TextBlock() { Text = xName };
            xTitle.FontSize = TextHelper.FontSize;
            DrawXTitle();
        }
        void DrawXTitle()
        {
            if (xTitle == null) 
                return;
            canvas.Children.Add(xTitle);
            Canvas.SetLeft(xTitle, xLabels[xLabels.Length / 2].TextCenter.X);
            Canvas.SetTop(xTitle, Height + 22);
        }
        public void Add(UIElement uie) { if(uie != null) canvas.Children.Add(uie); }
        public void Remove(UIElement uie) { canvas.Children.Remove(uie); }
        public void AddGridLines(Side horizontal, Side vertical, double xAxisPos = 0)
        {
            vTicks = vertical;
            hTicks = horizontal;
            axisPos = Math.Min(1, Math.Max(xAxisPos, 0));
        }
        public void SetYTicksToRange(Side side)
        {
            Range r = side == Side.Left? LeftSet?.YRange : side == Side.Right ? RightSet?.YRange : null;
            if (r != null)
                YRange = r;
        }
        void DrawGridLines()
        { 
            TickLength = Properties.TickLength;
            PathGeometry geom = new PathGeometry();
            figures = geom.Figures;
            figures.Add(CreateFrame(boxFrame)); // || hTicks.side != Side.Bottom || vTicks.side != Side.Left); 
            if (TickLength != 0)
                foreach (var pf in CreateVerticalTicks())
                    figures.Add(pf);
            Path path = new Path();
            path.Data = geom;
            path.Stroke = GridBrush;
            path.StrokeThickness = Properties.BorderThickness;
            if (hTicks != Side.None || vTicks != Side.None)
            {
                Path meshPath = new Path();
                PathGeometry meshGeom = new PathGeometry();
                meshGeom.Figures.Add(CreateMesh());
                meshPath.Data = meshGeom;
                meshPath.Stroke = GridBrush;
                meshPath.StrokeThickness = Properties.BorderThickness / 8;
                canvas.Children.Add(meshPath);
            }
            canvas.Children.Add(path);
        }
        PathFigure CreateMesh()
        {
            PathFigure mesh = new PathFigure();
            mesh.StartPoint = new Point(x0, 0);
            for (double x = x0; x < Width; x+=dx)
            {   // vertical lines
                if (x>x0)
                    mesh.Segments.Add(new LineSegment(new Point(x, 0), false));
                mesh.Segments.Add(new LineSegment(new Point(x, Height), true));
            }
            for (double y = y0; y < Height; y += dy)   
            {
                mesh.Segments.Add(new LineSegment(new Point(0, y), false));
                mesh.Segments.Add(new LineSegment(new Point(Width, y), true));
            }
            return mesh;
        }
        PathFigure CreateFrame(bool box)
        {
            PathFigure axis = new PathFigure();
            if (axisPos != 0 && axisPos != 1)
            {   // x-axix line
                axis.StartPoint = new Point(0, Height * (1 - axisPos));
                axis.Segments.Add(new LineSegment(new Point(Width, Height * (1 - axisPos)), true));
            }
            axis.Segments.Add(new LineSegment(new Point(0, 0), false));
            axis.Segments.Add(new LineSegment(new Point(0, Height), true)); // left vertical border
            if (box)
            {
                axis.Segments.Add(new LineSegment(new Point(Width, Height), true));
                axis.Segments.Add(new LineSegment(new Point(Width, 0), true));
                axis.Segments.Add(new LineSegment(new Point(0, 0), true));
            }
            return axis;
        }
        void DrawHorizontalTicks()
        {
            if (TickLength != 0)
                foreach (var pf in CreateHorizontalTicks())
                    figures.Add(pf);
            for (int i = 0; i < xLabels.Length; i++)
            {   // setting X labels text
                var v = xLabels[i].TextCenter.X / ScaleX + XRange.Min;
                xLabels[i].SetValue(Math.Abs(v) / XRange.TickStep < 1.0e-6 ? 0 : v);
            }
        }
        List<PathFigure> CreateVerticalTicks()
        {
            List<PathFigure> pfl = new List<PathFigure>();
            if (vTicks.isL) pfl.Add(Ticks(Side.Left, y0, dy));
            if (vTicks.isR) pfl.Add(Ticks(Side.Right, y0, dy));
            return pfl;
        }
        List<PathFigure> CreateHorizontalTicks()
        {
            List<PathFigure> pfl = new List<PathFigure>();
            if (hTicks.isT) pfl.Add(Ticks(Side.Top, x0, dx));
            if (hTicks.isB) pfl.Add(Ticks(Side.Bottom, x0, dx));
            if (hTicks.isX) pfl.Add(TicksWithLabels(Side.XAxis, x0, dx, ref xLabels));
            return pfl;
        }
        PathFigure Ticks(Side ticks, double l0, double dist) { return Ticks(ticks, l0, dist, out List<double> markers, out double pos); }
        PathFigure Ticks(Side ticks, double l0, double dist, out List<double> markers, out double pos)
        {
            bool hor = ticks.isH;
            PathFigure tf = new PathFigure();
            double axisLength = hor ? Width : Height;               // axis length
            pos = ticks.isT ? 0 : ticks.isX ? Height * (1 - axisPos) : ticks.isB ? Height : ticks.isL ? 0 : Width;// position in orthogonal direction
            markers = new List<double>();
            if (dist > axisLength)
                return tf;
            double dir = Properties.TicksInside == ticks.isT || ticks.isL ? 1 : -1;
            double end = pos + dir * TickLength;                // end tick position
            tf.StartPoint = new Point(l0, pos);
            for (double l = l0; l <= axisLength; l+= dist)
            {
                tf.Segments.Add(new LineSegment(hor ? new Point(l, end) : new Point(end, l), false));
                tf.Segments.Add(new LineSegment(hor ? new Point(l, pos) : new Point(pos, l), true));
                markers.Add(l);
            }
            return tf;
        }
        PathFigure TicksWithLabels(Side ticks, double l0, double dist, ref NumericLabel[] labels)
        {
            var pf = Ticks(ticks, l0, dist, out List<double> markers, out double pos);
            if (labels != null)
            {
                int nLab = dist < 50 ? (markers.Count + 1) / 2 : markers.Count;
                int di = dist < 50 ? 2 : 1;
                labels = new NumericLabel[nLab];
                bool ends = axisPos == 0 || axisPos == 1;
                bool hor = ticks.isH;
                for (int i = 0; i < labels.Length; i++)
                {
                    double x = hor ? markers[i * di] : ticks.isL ? pos - 8 : pos + 8;
                    double y = !hor ? markers[i * di] : ticks.isT ? pos - 10 : pos + 10;
                    labels[i] = new NumericLabel(new Point(x, y), "g3");
                    labels[i].FontSize = TextHelper.FontSize;
                    if ((i > 0 && i < labels.Length - 1) || ends)
                        canvas.Children.Add(labels[i]);
                }
            }
            return pf;
        }
        public int SetXValues(float[] xvs, bool extendPointsToRange = false)
        {   // sets XValues to a part of xvs fitting Range
            if (!XRange.IsValid)
                return 0;
            List<float> xv = new List<float>();
            foreach (float x in xvs)
            {
                if (x > XRange.Max)
                    break;
                if (x >= XRange.Min)
                    xv.Add(x);
            }
            XValues = xv.ToArray();
            if (extendPointsToRange)
            {
                var dx = XValues[1] - XValues[0];
                List<float> left = new List<float>();
                List<float> right = new List<float>();
                int i = 1;
                float x;
                while ((x = XValues[0] - (i++) * dx) >= XRange.Min) left.Add(x);
                i = 1;
                while ((x = XValues[XValues.Length - 1] + (i++) * dx) <= XRange.Max) right.Add(x);
                if (left.Count + right.Count > 0)
                {
                    List<float> tot = new List<float>();
                    if (left.Count > 0)
                    {
                        var la = left.ToArray();
                        Array.Reverse(la);
                        tot.AddRange(la);
                    }
                    tot.AddRange(XValues);
                    tot.AddRange(right);
                    XValues = tot.ToArray();
                }
            }
            return XValues.Length;
        }
        public void Resize(Rect rect) { Resize(rect.Width, rect.Height); }
        public void Resize(double w, double h)
        {
            canvas.Width = w;
            canvas.Height = h;
            canvas.InvalidateVisual();
            foreach (UIElement e in canvas.Children)
                e.InvalidateVisual();
        }
        //PathFigure CreateVerticalAxis(bool left, int nTicks, double tickLength)
        //{
        //    PathFigure axis = new PathFigure();
        //    double d = canvas.Height / nTicks;
        //    double x = left ? 0 : canvas.Width;
        //    double t = left ? tickLength : -tickLength;
        //    axis.StartPoint = new Point(x, canvas.Height);
        //    axis.Segments.Add(new LineSegment(new Point(x, 0), true));
        //    if (t != 0)
        //    {
        //        for (int i = 0; i <= nTicks; i++)           // vertical
        //        {
        //            double y = i * d;
        //            axis.Segments.Add(new LineSegment(new Point(x + t, y), false));
        //            axis.Segments.Add(new LineSegment(new Point(x, y), true));
        //        }
        //    }
        //    return axis;
        //}
    }
}
