using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using netDxf;
using netDxf.Entities;

namespace netDxf
{
    /// <summary>
    /// drawing Bezier segment representing DXF Non-Uniform Rational B-Splines (NURBS).
    /// </summary>
    public struct Bezier
    {
        public ReadOnlyCollection<SplineVertex> ControlPoints { get { return controlPoints; } }
        internal Bezier(Spline arc)
        {
            //Console.WriteLine("controlPoints={0} knots={1} degree={2} Closed={3} Periodic={4}", 
            //    arc.ControlPoints.Count, arc.Knots.Length, arc.Degree, (arc.IsClosed?'Y':'N'), (arc.IsPeriodic?'Y':'N'));
            //int edge = arc.Degree - 1;
            //if (arc.ControlPoints.Count + 2 * edge == arc.Knots.Length)
            //{
            //    for (int i = 0; i < edge; i++)
            //        Console.WriteLine("Knot={0:0.000}", arc.Knots[i]);
            //    for (int i = 0; i < arc.ControlPoints.Count; i++)
            //        Console.WriteLine("Knot={0:0.000} X={1:0.000} Y={2:0.000} W={3:0.000}",
            //            arc.Knots[i + edge], arc.ControlPoints[i].Location.X, arc.ControlPoints[i].Location.Y, arc.ControlPoints[i].Weigth);
            //    for (int i = arc.ControlPoints.Count + edge; i < arc.ControlPoints.Count + 2*edge; i++)
            //        Console.WriteLine("Knot={0:0.000}", arc.Knots[i]);
            //}
            controlPoints = arc.ControlPoints;
        }
        ReadOnlyCollection<SplineVertex> controlPoints;
        //private List<SplineVertex> controlPoints;
        //private double[] knots;
        //private readonly SplineTypeFlags flags;
        //private readonly short degree;
        //private readonly bool isClosed;
        //private readonly bool isPeriodic;
    }
    /// <summary>
    /// drawing EllipseArc segment representing DXF Ellipse, Arc, or Circle.
    /// </summary>
    public struct EllipseArc
    {   // counter-clockwise arc
        const double dToRad = Math.PI / 180;
        public System.Windows.Point Start { get { return ArcPoint(startAngle * dToRad); } }
        public System.Windows.Point End { get { return ArcPoint(endAngle * dToRad); } }
        public System.Windows.Size Radii { get { return new Size(majorRadius, minorRadius); } }
        public double Rotation { get { return rotation * dToRad; } }
        public bool IsBig { get { return Math.Abs(endAngle - startAngle) > 180; } }
        public bool IsClockwise { get { return false; } }
        public EllipseArc FlipX { get 
        { 
            EllipseArc ret = new EllipseArc(); 
            ret.center = new System.Windows.Point(-center.X, center.Y);
            ret.majorRadius = majorRadius;
            ret.minorRadius = minorRadius;
            ret.rotation = rotation;
            ret.endAngle = 180 - startAngle;
            ret.startAngle = 180 - endAngle;
            return ret;
        } }
        public override string ToString()
        {
            return string.Format("C={0:0.000},{1:0.000} R={2:0.000},{3:0.000} A={4:0.000}<->{5:0.000} r={6:0.000}",
                center.X, center.Y, majorRadius, minorRadius, startAngle, endAngle, rotation) + 
                string.Format(" start={0:0.000},{1:0.000} end={2:0.000},{3:0.000}", Start.X, Start.Y, End.X, End.Y);
        }
        internal EllipseArc(Ellipse arc, double scale, Vector offset)
        {
            center = new System.Windows.Point(arc.Center.X * scale, arc.Center.Y * scale) + offset;
            majorRadius = arc.MajorAxis / 2 * Math.Abs(scale);
            minorRadius = arc.MinorAxis / 2 * Math.Abs(scale);
            startAngle = arc.StartAngle;
            if(arc.StartAngle> arc.EndAngle)
                startAngle -= 360;
            endAngle = arc.EndAngle;
            rotation = arc.Rotation;
        }
        internal EllipseArc(Arc arc, double scale, Vector offset)
        {
            center = new System.Windows.Point(arc.Center.X * scale, arc.Center.Y * scale) + offset;
            majorRadius = arc.Radius * Math.Abs(scale);
            minorRadius = arc.Radius * Math.Abs(scale);
            startAngle = arc.StartAngle;
            if (arc.StartAngle > arc.EndAngle)
                startAngle -= 360;
            endAngle = arc.EndAngle;
            rotation = 0;
        }
        internal EllipseArc(Circle arc, double scale, Vector offset)
        {
            center = new System.Windows.Point(arc.Center.X * scale, arc.Center.Y * scale) + offset;
            majorRadius = arc.Radius * Math.Abs(scale);
            minorRadius = arc.Radius * Math.Abs(scale);
            startAngle = 0;
            endAngle = 360;
            rotation = 0;
        }
        System.Windows.Point ArcPoint(double angle)
        {
            double ca = Math.Cos(angle);
            double sa = Math.Sin(angle);
            double r = majorRadius * minorRadius / Math.Sqrt(minorRadius * minorRadius * ca * ca + majorRadius * majorRadius * sa * sa);
            double x = r * ca;
            double y = r * sa;
            if (rotation != 0)
            {
                double cr = Math.Cos(rotation * dToRad);
                double sr = Math.Sin(rotation * dToRad);
                double t=x;
                x = x * cr - y * sr;
                y = t * sr + y * cr;
            }
            return new System.Windows.Point(center.X + x, center.Y + y);
        }
        System.Windows.Point center;
        double majorRadius;
        double minorRadius;
        double rotation;
        double startAngle;
        double endAngle;
    }
    public struct Segment
    {
        public System.Windows.Point Start { get { return start; } }
        public System.Windows.Point End { get { return end; } }
        public Segment FlipX { get { Segment ret = new Segment(); ret.start = new System.Windows.Point(-start.X, start.Y); ret.end = new System.Windows.Point(-end.X, end.Y); return ret; } }
        internal Segment(Line line, double scale, Vector offset)
        {
            start = new System.Windows.Point(line.StartPoint.X * scale, line.StartPoint.Y * scale) + offset;
            end = new System.Windows.Point(line.EndPoint.X * scale, line.EndPoint.Y * scale) + offset;
        }
        public override string ToString() { return string.Format("start={0:0.000},{1:0.000} end={2:0.000},{3:0.000}", start.X, start.Y, end.X, end.Y); }
        System.Windows.Point start;
        System.Windows.Point end;
    }
    public class DXFDrawingAccess
    {
        List<Segment> lines = null;
        List<EllipseArc> arcs = null;
        List<List<System.Windows.Point>> polyLines = null;
        List<string> warnings = new List<string>();
        DxfDocument dxf = null;
        Rect displayArea;
        double scale = 1;
        public bool Loaded { get { return dxf != null; } }
        public List<Segment> Lines { get { return lines; } }
        public List<EllipseArc> Arcs { get { return arcs; } }
        public List<List<System.Windows.Point>> PolyLines { get { return polyLines; } }
        public List<string> Warnings { get { return warnings; } }
        public DXFDrawingAccess() { }
        bool IsInDisplayArea(Segment s) { return displayArea.Contains(s.Start) || displayArea.Contains(s.End); }
        bool IsInDisplayArea(EllipseArc s) { return displayArea.Contains(s.Start) || displayArea.Contains(s.End); }
        bool IsInDisplayArea(Rect r) { return displayArea.Contains(r.BottomLeft) || displayArea.Contains(r.BottomRight) || displayArea.Contains(r.TopLeft) || displayArea.Contains(r.TopRight); }
        public Rect LoadDxfFile(string fileName, Rect selectedArea, double s, Vector offset)
        {
            displayArea = selectedArea;
            scale = s;
            var rect = Rect.Empty;  // rect including all elements
            warnings.Clear();
            try
            {
                dxf = DxfDocument.Load(fileName);
                if (dxf.Dimensions.Count > 0)
                    warnings.Add("missing " + EntityType.Dimension.ToString() + dxf.Dimensions.Count.ToString());
                if (dxf.Faces3d.Count > 0)
                    warnings.Add("missing " + EntityType.Face3D.ToString() + dxf.Faces3d.Count.ToString());
                if (dxf.Hatches.Count > 0)
                    warnings.Add("missing " + EntityType.Hatch.ToString() + dxf.Hatches.Count.ToString());
                if (dxf.Images.Count > 0)
                    warnings.Add("missing " + EntityType.Image.ToString() + dxf.Images.Count.ToString());
                if (dxf.Inserts.Count > 0)
                    warnings.Add("missing " + EntityType.Insert.ToString() + dxf.Inserts.Count.ToString());
                if (dxf.MLines.Count > 0)
                    warnings.Add("missing " + EntityType.MLine.ToString() + dxf.MLines.Count.ToString());
                if (dxf.MTexts.Count > 0)
                    warnings.Add("missing " + EntityType.MText.ToString() + dxf.MTexts.Count.ToString());
                if (dxf.Points.Count > 0)
                    warnings.Add("missing " + EntityType.Point.ToString() + dxf.Points.Count.ToString());
                if (dxf.PolyfaceMeshes.Count > 0)
                    warnings.Add("missing " + EntityType.PolyfaceMesh.ToString() + dxf.PolyfaceMeshes.Count.ToString());
                if (dxf.Polylines.Count > 0)
                    warnings.Add("missing " + EntityType.Polyline.ToString() + dxf.Polylines.Count.ToString());
                if (dxf.Solids.Count > 0)
                    warnings.Add("missing " + EntityType.Solid.ToString() + dxf.Solids.Count.ToString());
                if (dxf.Texts.Count > 0)
                    warnings.Add("missing " + EntityType.Text.ToString() + dxf.Texts.Count.ToString());
                if (dxf.Rays.Count > 0)
                    warnings.Add("missing " + EntityType.Ray.ToString() + dxf.Rays.Count.ToString());
                if (dxf.XLines.Count > 0)
                    warnings.Add("missing " + EntityType.XLine.ToString() + dxf.XLines.Count.ToString());
                arcs = new List<EllipseArc>();
                foreach (var arc in dxf.Circles)
                {
                    EllipseArc newArc = new EllipseArc(arc, scale, offset);
                    if (IsInDisplayArea(newArc))
                        arcs.Add(newArc);
                }
                foreach (var arc in dxf.Arcs)
                {
                    EllipseArc newArc = new EllipseArc(arc, scale, offset);
                    if (IsInDisplayArea(newArc))
                        arcs.Add(newArc);
                }
                foreach (var arc in dxf.Ellipses)
                {
                    EllipseArc newArc = new EllipseArc(arc, scale, offset);
                    if (IsInDisplayArea(newArc))
                        arcs.Add(newArc);
                }
                polyLines = new List<List<System.Windows.Point>>();
                foreach (var poly in dxf.LwPolylines)
                {
                    if (poly.Vertexes.Count < 2)
                        continue;
                    var polyLine = new List<System.Windows.Point>(poly.Vertexes.Count);
                    var v = poly.Vertexes[0].Location;
                    var p = new System.Windows.Point(v.X * scale, v.Y * scale) + offset;
                    Rect pRect = new Rect(p, p);
                    foreach (var point in poly.Vertexes)
                    {
                        p = new System.Windows.Point(point.Location.X * scale, point.Location.Y * scale) + offset;
                        polyLine.Add(p);
                        pRect.Union(p);
                    }
                    if (IsInDisplayArea(pRect))
                        polyLines.Add(polyLine);
                    rect.Union(pRect);
                }
                foreach (var bspline in dxf.Splines)
                {
                    Bezier bez = new Bezier(bspline);
                    if (bez.ControlPoints.Count < 2)
                        continue;
                    var v = bez.ControlPoints[0].Location;
                    var p = new System.Windows.Point(v.X * scale, v.Y * scale) + offset;
                    Rect pRect = new Rect(p, p); var polyLine = new List<System.Windows.Point>(bez.ControlPoints.Count);
                    foreach (var point in bez.ControlPoints)
                    {
                        p = new System.Windows.Point(point.Location.X * scale, point.Location.Y * scale) + offset;
                        polyLine.Add(p);
                        pRect.Union(p);
                    }
                    if (IsInDisplayArea(pRect))
                        polyLines.Add(polyLine);
                    rect.Union(pRect);
                }
                lines = new List<Segment>();
                foreach (var line in dxf.Lines)
                {
                    var seg = new Segment(line, scale, offset);
                    if (IsInDisplayArea(seg))
                        lines.Add(seg);
                    rect.Union(seg.Start);
                    rect.Union(seg.End);
                }
            }
            catch(Exception ex)
            {
                warnings.Add(ex.Message);
            }
            return rect;
        }
        public void Clear() { dxf = null; }
    }
}
