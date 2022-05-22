using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text;

namespace ImageProcessor
{
    public enum PointProperty
    {
        Selected = 1,
        Sharp = 2,
        Transparent = 4,
    }
    public enum Link
    {
        None,                       // not linked 
        Sharp,                      // linked to previous
        Smooth,                     // smoothly linked to previous
        Self,                       // linked to own end
        Closed                      // smoothly linked to own end
    }
    [Serializable]
    public struct StrokePoint           // holds untransformed data & handles PressureFactor as property
    {   // PressureFactor =: 0 - normal; 1*res - selected; 2*res - sharp; 4*res - starts transparent segment
        static float res = 0.1f;
        static int Key(float val) { return (int)(val / res + 0.5f); }
        internal static bool HasProperty(StylusPoint p, PointProperty a) { return (Key(p.PressureFactor) & (int)a) == (int)a; }
        internal static string ToString(StylusPoint p)
        {
            string selected = HasProperty(p, PointProperty.Selected) ? "Selected" : "Not Selected";
            string sharp = HasProperty(p, PointProperty.Sharp) ? "Sharp" : "Smooth";
            string transparent = HasProperty(p, PointProperty.Transparent) ? "Transparent" : "Visible";
            return p.X.ToString("f0") + ',' + p.Y.ToString("f0") + ' ' + Key(p.PressureFactor) + " {" + selected + ", " + sharp + ", " + transparent + "} ";
        }
        float x;                        // absolute (not related to canvas)
        float y;                        // absolute (not related to canvas)
        int properties;                 // sum of PointProperty
        public float X { get { return x; } }
        public float Y { get { return y; } }
        public int Properties { get { return properties; } }
        internal StrokePoint(StylusPoint sp, Matrix matrix)
        {
            Point p = matrix.Transform(new Point(sp.X, sp.Y));
            x = (float)p.X;
            y = (float)p.Y;
            properties = Key(sp.PressureFactor);
        }
        internal StylusPoint ToStylusPoint(Matrix matrix)
        {
            Point p = matrix.Transform(new Point(x, y));
            return new StylusPoint(p.X, p.Y, properties * res);
        }
        internal StylusPoint SwitchProperty(PointProperty a, StylusPoint p) { properties ^= (int)a; p.PressureFactor = properties * res; return p; }
        internal bool HasProperty(PointProperty a) { return (properties & (int)a) == (int)a; }
    }
    [Serializable]
    public struct StorePoint                // holds untransformed data & handles PressureFactor as property
    {
        internal float x;                   // absolute (not related to canvas)
        internal float y;                   // absolute (not related to canvas)
        internal int properties;            // sum of PointProperty
        internal StorePoint(Point p, int props)
        {
            x = (float)p.X;
            y = (float)p.Y;
            properties = props;
        }
    }
    [Serializable]
    public class StorePointCollection
    {   // used for permanent storing; changing fields will make storred data unreadable
        static uint ToUINT(Color color)
        {
            uint uic = color.A;
            uic = uic << 8;
            uic += color.B;
            uic = uic << 8;
            uic += color.G;
            uic = uic << 8;
            uic += color.R;
            return uic;
        }
        internal uint color;
        internal float thickness;
        internal StorePoint[] points;
        public StorePointCollection(FlexiblePolygon stroke)
        {
            color = ToUINT(stroke.Color);
            thickness = (float)stroke.Thickness;
            points = new StorePoint[stroke.Count];
            for (int i = 0; i < stroke.Count; i++)
                points[i] = new StorePoint(stroke.Poly[i], stroke.Property(i));
        }
    }
    public class StorePointDecoder
    {
        static Color FromUINT(uint color)
        {
            byte a = (byte)((color & 0xff000000) >> 24);
            if (a == 0)
                a = byte.MaxValue;
            byte b = (byte)((color & 0x00ff0000) >> 16);
            byte g = (byte)((color & 0x0000ff00) >> 8);
            byte r = (byte)(color & 0x000000ff);
            return Color.FromArgb(a, r, g, b);
        }
        public List<Point> poly { get; private set; }
        public List<int> properties { get; private set; } // Selected = 1, Sharp = 2, Transparent = 4,
        public Color color { get; private set; }
        public double thickness { get; private set; }
        public StorePointDecoder(StorePointCollection spc)
        {
            color = FromUINT(spc.color);
            thickness = spc.thickness;
            poly = new List<Point>(spc.points.Length);
            properties = new List<int>(spc.points.Length);
            foreach (var sp in spc.points)
            {
                poly.Add(new Point(sp.x, sp.y));
                properties.Add(sp.properties);
            }
        }
    }
    public struct PointProperties
    {   // Selected = 1, Sharp = 2, Transparent = 4
        public const int Normal = 0;    // indicates point with no propeties
        public const int Selected = 1;  // indicates point selected for operation
        public const int Sharp = 2;     // indicates sharp turn at point
        public const int Transparent = 4; // indicates transparent segment
        public static implicit operator int(PointProperties d) { return d.properties; }
        public static implicit operator PointProperties(int d) { return new PointProperties(d); }
        int properties;                 // sum of PointProperty
        internal PointProperties(int prop) { properties = prop; }
        internal PointProperties Add(int a) { properties |= a; return properties; }
        internal PointProperties Remove(int a) { properties &= ~a; return properties; }
        internal PointProperties Inverse(int a) { properties ^= a; return properties; }
        internal bool Has(int a) { return (properties & a) == a; }
        public override string ToString()
        {
            string selected = Has(Selected) ? "1" : "0";
            string sharp = Has(Sharp) ? "1" : "0";
            string show = Has(Transparent) ? "0" : "1";
            return selected + sharp + show;
        }
    }
    public class FlexiblePolygon : Polygon
    {
        public static PolygonSmoother Smoother;
        internal int pathOffsetInd;
        List<PointProperties> proprties;
        public Pen Pen { get; private set; }
        public Color Color { get; private set; }
        public double Thickness { get { return Pen != null ? Pen.Thickness : 1; } }
        internal bool Closed { get; private set; }
        internal PathGeometry PathGeometry { get; set; }
        public void SetPen(Color color, double thickness) { Color = color; Pen = thickness > 0 ? new Pen(new SolidColorBrush(color), thickness) : null; }
        static Point BezierPoint(Point p0, BezierSegment segm, double t)
        {
            Point p1 = segm.Point1;
            Point p2 = segm.Point2;
            Point p3 = segm.Point3;
            double r = 1 - t;
            double c0 = r * r * r;
            double c1 = 3 * r * r * t;
            double c2 = 3 * t * t * r;
            double c3 = t * t * t;
            double x = c0 * p0.X + c1 * p1.X + c2 * p2.X + c3 * p3.X;
            double y = c0 * p0.Y + c1 * p1.Y + c2 * p2.Y + c3 * p3.Y;
            return new Point(x, y);
        }
        public static FlexiblePolygon CreateFromRect(Polygon controlPoints)
        {
            Polygon poly = new Polygon(controlPoints.Target);
            poly.Poly.AddRange(controlPoints.Poly);
            return new FlexiblePolygon(poly, PointProperties.Sharp);
        }
        public FlexiblePolygon(StorePointDecoder spd, IntSize sz) : base(spd.poly, sz)
        {
            InitPoly();
            foreach (var p in spd.properties)
                proprties.Add(p);
            SetPen(spd.color, spd.thickness);
        }
        public FlexiblePolygon(Polygon controlPoints, Color color, double thickness) : base(controlPoints)
        {
            InitPoly();
            for (int i = 0; i < Poly.Count; i++)
                proprties.Add(PointProperties.Normal);
            SetPen(color, thickness);
        }
        public FlexiblePolygon(Polygon controlPoints, int pp = PointProperties.Normal) : base(controlPoints)
        {
            InitPoly();
            for (int i = 0; i < Poly.Count; i++)
                proprties.Add(pp);
        }
        void InitPoly()
        {
            Closed = Poly.Count > 0 && Poly[0] == Poly[Poly.Count - 1];
            if (Closed)
                Poly.RemoveAt(Poly.Count - 1);
            proprties = new List<PointProperties>(Poly.Count);
        }
        public void AddPoint(int i, Point p) { Poly.Insert(i, p); proprties.Insert(i, PointProperties.Normal); }
        public void RemovePoint(int ind) { Poly.RemoveAt(ind); proprties.RemoveAt(ind); }
        public void AddProperty(int ind, int prop) { proprties[ind] = proprties[ind].Add(prop); }
        public void AddProperty(int prop) { for (int i = 0; i < proprties.Count; i++) AddProperty(i, prop); }
        public void RemoveProperty(int ind, int prop) { proprties[ind] = proprties[ind].Remove(prop); }
        public void RemoveProperty(int prop) { for (int i = 0; i < proprties.Count; i++) RemoveProperty(i, prop); }
        public void InverseProperty(int ind, int prop) { proprties[ind] = proprties[ind].Inverse(prop); }
        public bool HasProperty(int ind, int prop) { return proprties[ind].Has(prop); }
        public PointProperties Property(int ind) { return proprties[ind]; }
        public int HitPointTest(Point tp)
        {
            for (int i = 0; i < Poly.Count; i++)
                if ((tp - ToDrawing.Value.Transform(Poly[i])).LengthSquared < Smoother.l2max)
                    return i;
            return -1;
        }
        public int HitContourTest(Point tp) { return ProximityTest(tp, 2); }
        public int ProximityTest(Point tp) { return ProximityTest(tp, 6); }
        public int ProximityTest(Point tp, double range)
        {
            int ind = pathOffsetInd;
            if (PathGeometry == null)
                return -1;
            foreach (PathFigure pf in PathGeometry.Figures)
            {
                Point p0 = pf.StartPoint;
                for (int i = 0; i < pf.Segments.Count; i++)
                {
                    BezierSegment bsegm = pf.Segments[i] is BezierSegment ? pf.Segments[i] as BezierSegment : null;
                    LineSegment lsegm = pf.Segments[i] is LineSegment ? pf.Segments[i] as LineSegment : null;
                    Point pend = bsegm != null ? bsegm.Point3 : lsegm != null ? lsegm.Point : p0;
                    double d = (pend - p0).Length;
                    double dt = Smoother.MarkerSize / ((pend - p0).Length + 2);
                    if (bsegm != null)
                    {
                        for (double t = 0; t < 1; t += dt)
                            if ((BezierPoint(p0, bsegm, t) - tp).LengthSquared < range * Smoother.l2max)
                                return ind < Count ? ind : ind - Count;
                    }
                    if (lsegm != null)
                    {
                        Vector dv = pend - p0;
                        for (double t = 0; t < 1; t += dt)
                            if ((p0 + dv*t - tp).LengthSquared < range * Smoother.l2max)
                                return ind < Count ? ind : ind - Count;
                    }
                    ind++;
                    p0 = pend;
                }
            }
            return -1;
        }
        public Polygon Contour()
        {   // contour of smoothed polygon (e.g. for selection)
            List<Point> pl = new List<Point>();
            PathGeometry = Smoother.SmoothPath(this, Poly.ToArray()); // in image coordinates
            foreach (PathFigure pf in PathGeometry.Figures)
            {
                Point p0 = pf.StartPoint;
                pl.Add(p0);
                for (int i = 0; i < pf.Segments.Count; i++)
                {
                    if (pf.Segments[i] is BezierSegment)
                    {
                        BezierSegment segm = pf.Segments[i] as BezierSegment;
                        double d = (segm.Point3 - p0).Length;
                        int n = (int)(d / Smoother.BezierPointsDistance);
                        if (n < 2)
                            continue;
                        double dt = 1.0 / n;
                        if (pl != null)
                            for (double t = 0; t < 1; t += dt)
                                pl.Add(BezierPoint(p0, segm, t));
                        p0 = segm.Point3;
                    }
                    else if (pf.Segments[i] is LineSegment)
                        p0 = (pf.Segments[i] as LineSegment).Point;
                }
                pl.Add(p0);
            }
            Polygon pol = new Polygon(this);
            pol.Poly = pl;
            return pol;
        }
        public bool MoveSelectedPoints(Vector d)
        {
            List<int> selected = new List<int>();
            for (int i = 0; i < Poly.Count; i++)
                if (proprties[i].Has(PointProperties.Selected))
                    selected.Add(i);
            if (selected.Count == 1)
            {
                int si = selected[0];
                Poly[si] += d;
                Point siCanvas = ToDrawing.Value.Transform(Poly[si]);
                for (int i = 0; i < Poly.Count; i++)
                    if (i != si && (siCanvas - ToDrawing.Value.Transform(Poly[i])).LengthSquared < Smoother.l2max)
                    {
                        RemovePoint(si);
                        return true;
                    }
                return false;
            }
            for (int s = 0; s < selected.Count; s++)
                Poly[selected[s]] += d;
            return false;
        }
        bool ValidIndex(int ind) { return ind >= 0 && ind < Poly.Count; }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)
        {
            if (Pen == null)
                Pen = pen;
            if (!IsEmpty)
                Smoother.DrawPoly(g, this, brush!=null);
            //Debug.WriteLine(ToPropertiesString());
            //Debug.WriteLine(ToGeometryString(true));
        }
        public string ToPropertiesString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("points=" + Poly.Count + " proprties=" + proprties.Count);
            for (int i = 0; i < Math.Min(proprties.Count, Poly.Count); i++)
            {
                sb.Append(i);
                sb.Append(' ');
                sb.Append(proprties[i].ToString());
                sb.Append("-p=");
                sb.Append(Poly[i].X.ToString("f1"));
                sb.Append(',');
                sb.AppendLine(Poly[i].Y.ToString("f1"));
            }
            return sb.ToString();
        }
        public string ToGeometryString(bool canvasGeom)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("pathGeometry " + PathGeometry.Figures.Count + " figures");
            for (int j = 0; j < PathGeometry.Figures.Count; j++)
            {
                PathFigure pf = PathGeometry.Figures[j];
                sb.AppendLine("figure " + j + ", " + pf.Segments.Count + " Segments");
                Point p0 = canvasGeom ? FromDrawing.Transform(pf.StartPoint) : pf.StartPoint;
                sb.Append("Start [");
                sb.Append(p0.X.ToString("f1"));
                sb.Append(',');
                sb.Append(p0.Y.ToString("f1"));
                sb.AppendLine("]");
                for (int i = 0; i < pf.Segments.Count; i++)
                {
                    Point p3 = ((BezierSegment)pf.Segments[i]).Point3;
                    if(canvasGeom)
                        p3 = FromDrawing.Transform(p3);
                    sb.Append("BezSeg " + i + " [");
                    sb.Append(p3.X.ToString("f1"));
                    sb.Append(',');
                    sb.Append(p3.Y.ToString("f1"));
                    sb.AppendLine("]");
                }
            }
            return sb.ToString();
        }
    }
    public class PolygonSmoother
    {
        [ThreadStatic]
        Brush smoothMarkerBrush = new SolidColorBrush(Colors.White);
        [ThreadStatic]
        Brush sharpMarkerBrush = new SolidColorBrush(Colors.Gray);
        [ThreadStatic]
        Brush selectedMarkerBrush = new SolidColorBrush(Colors.DarkRed);
        [ThreadStatic]
        double smoothness;
        readonly public double l2max;
        readonly public double BezierPointsDistance = 2;    // length of polygon segment built from BezierSegment
        public double MarkerSize { get; private set; }
        public PolygonSmoother(double smoothness_, double markerSize_)
        {
            smoothness = smoothness_;
            MarkerSize = markerSize_;
            l2max = MarkerSize * MarkerSize;
        }
        public void DrawPoly(DrawingContext g, FlexiblePolygon curve, bool showMarkers)
        {
            Point[] points = curve.Poly.ToArray();
            if(curve.ToDrawing != null)
                curve.ToDrawing.Value.Transform(points);
            curve.PathGeometry = SmoothPath(curve, points); // points, proprties, closed); // in canvas coordinates
            g.DrawGeometry(null, curve.Pen, curve.PathGeometry);
            if(showMarkers)
                for (int i = 0; i < curve.Poly.Count; i++)
                    DrawMarker(g, points[i], curve.Property(i), curve.Pen);
        }
        internal PathGeometry SmoothPath(FlexiblePolygon curve, Point[] points)
        {
            List<PathFigure> paths = new List<PathFigure>();
            int nPoints = points.Length;
            if (nPoints < 2)
                return new PathGeometry();
            List<Point> pc = new List<Point>(nPoints);
            int lastInd = nPoints - 1;
            int beginInd = -1;
            if(!curve.Closed)
            {
                curve.AddProperty(0, PointProperties.Sharp);
                curve.AddProperty(lastInd, PointProperties.Sharp);
            }
            for (int i = 0; i < nPoints; i++)
                if (curve.Property(i).Has(PointProperties.Sharp))
                {
                    beginInd = i;
                    break;
                }
            if (beginInd < 0)
            {   // no sharp points
                curve.pathOffsetInd = 0;
                paths.Add(new PathFigure(points[0], CubicSpline(points, true), false));
                return new PathGeometry(paths);
            }
            Point begin = points[beginInd];
            curve.pathOffsetInd = beginInd;
            pc.Add(begin);
            int last = curve.Closed ? nPoints + beginInd + 1 : nPoints + beginInd;
            for (int i = beginInd + 1; i < last; i++)
            {
                int ind = i < nPoints ? i : i - nPoints;
                Point end = points[ind];
                pc.Add(end);
                if (curve.Property(ind).Has(PointProperties.Sharp))
                {
                    if(pc.Count == 2)
                        paths.Add(new PathFigure(begin, new LineSegment[] { new LineSegment(end, true)}, false));
                    else
                        paths.Add(new PathFigure(begin, CubicSpline(pc.ToArray(), false), false));
                    pc.Clear();
                    begin = end;
                    pc.Add(begin);
                }
            }
            return new PathGeometry(paths);
        }
        BezierSegment[] CubicSpline(Point[] points, bool smoothClosed)
        {   // smoothClosed == true implies that first and last points are the same
            if (points.Length < 2)
                return new BezierSegment[0];
            if (points.Length == 2)
                return new BezierSegment[] { new BezierSegment(points[0], points[1], points[1], true) };
            int lastPind = points.Length - 1;
            int nSegm = smoothClosed ? points.Length : lastPind;
            BezierSegment[] bsa = new BezierSegment[nSegm];
            Vector smoothCloseDirection = ControlDirection(points[lastPind], points[1]);
            Point pm = points[0];
            Point pi = points[0];
            Point pp = points[1];
            double segmentLength = (pp - pi).Length;
            Point firstControl = smoothClosed ? pi + smoothCloseDirection * segmentLength : pi;
            Point secondControl;
            for (int i = 1; i < nSegm; i++)
            {
                pm = pi;
                pi = pp;
                pp = i < lastPind ? points[i + 1] : points[0];
                Vector smoothDirection = ControlDirection(pm, pp);
                secondControl = pi - smoothDirection * segmentLength;
                bsa[i - 1] = new BezierSegment(firstControl, secondControl, pi, true);
                segmentLength = (pp - pi).Length;
                firstControl = pi + smoothDirection * segmentLength;
            }
            if (smoothClosed)
            {
                secondControl = pp - smoothCloseDirection * segmentLength;
                bsa[nSegm - 1] = new BezierSegment(firstControl, secondControl, pp, true);
            }
            else
                bsa[nSegm - 1] = new BezierSegment(firstControl, pp, pp, true);
            return bsa;
        }
        Vector ControlDirection(Point p1, Point p2)
        {
            Vector v = p2 - p1;
            v.Normalize();
            return smoothness * v;
        }
        public void DrawMarker(DrawingContext drawingContext, Point point, PointProperties prop, Pen pen)
        {
            if (prop.Has(PointProperties.Transparent))
                return;
            Brush brush = prop.Has(PointProperties.Selected) ? selectedMarkerBrush :
                prop.Has(PointProperties.Sharp) ? sharpMarkerBrush : smoothMarkerBrush;
            double d = MarkerSize / 2;
            drawingContext.DrawEllipse(brush, pen, point, d, d);
        }
    }
 }