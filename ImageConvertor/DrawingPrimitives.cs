using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text;
using System.Diagnostics;

namespace ImageProcessor
{
    public enum MouseOperation
    {   // Mouse action indexes: 0-3 - control points: TL=0, TR=1, BR=2, BL=3; 4-7 control lines: Top=4, Right=5, Bottom=6, Left=7  
        Left = 7,
        Bottom = 6,
        Right = 5,
        Top = 4,
        VortexBL = 3,
        VortexBR = 2,
        VortexTR = 1,
        VortexTL = 0,
        None = -1,
        OpCenter = -2,         // move center of operation 
        Move = -3,             // move image (MouseButton.Left)
        Rotate = -4,           // rotate image (MouseButton.Right)
        Scale = -5,            // scale image (MouseButton.Middle)
        Add = -6,              // add point
        Stroke = -7,           // stroke editing 
        ViewPoint = -8,        // 3d view distortion 
        Morph = -9,            // local shift distortion 
    }
    public abstract class Primitive          // primitive elements which can be drawn with pen and brush
    {
        MatrixTransform toDrawing;  // used instead of Matrix to allow null
        MatrixTransform fromDrawing;
        public MatrixTransform ToDrawing
        {
            get { return toDrawing; }
            set { toDrawing = value; if (toDrawing != null) fromDrawing = (MatrixTransform)toDrawing.Inverse; }
        }
        public MatrixTransform FromDrawing
        {
            get { return fromDrawing; }
            set { fromDrawing = value; if (fromDrawing != null) toDrawing = (MatrixTransform)fromDrawing.Inverse; }
        }
        public bool Scaled { get { return toDrawing != null; } }
        public abstract void Draw(DrawingContext g, Brush brush, Pen pen);
        public DrawingVisual ToVisual(Brush brush, Pen pen)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            Draw(drawingContext, brush, pen);
            drawingContext.Close();
            return drawingVisual;
        }
    }
    public class PolygonBuilder
    {   // builds stroke control points from input at locations of maximum curvature
        double maxLength;           // maximum segment length
        double angle = 0.3;         // max angle per segment in radians
        double[] dist;              // accummulated length along path dist[0]=0
        double totDist;             // total accummulated length along path totDist=dist[last]
        int nPoints;
        bool closed;
        double[] range;             // length range estimate from local curvature (1/2R)
        int[] flag;                 // 1 - used point; -1 removed point; 0 - unknown
        public double baseLength;   // accuracy (in drawing units) to build smooth stroke
        public PolygonBuilder() { baseLength = 10; }
        public Polygon CreateEquidistantContour(Polygon src)
        { // averaging with traingle distance-based weight, points added to reduce long segments
            double[] l = src.AccummulatedLength();
            double len = l[src.Count - 1]; ;
            if (baseLength < 2 || len < baseLength)
                return src;
            List<Point> pc = new List<Point>();
            int npc = src.IsClosed ? src.Count - 1 : src.Count;
            double lpos = -baseLength;
            for (int t = 0; t < npc; t++)
            {
                if (t < npc - 1 && l[t + 1] - lpos < baseLength + 1)
                    continue;
                double tw = src.IsClosed ? baseLength : Math.Min(Math.Min(baseLength, len - l[t] + 1), l[t] + 1); // total weight
                double wm = tw;     // max weight
                double sx = src[t].X * wm;
                double sy = src[t].Y * wm;
                double dc = l[t];   // l-location of averaging center
                for (int i = 1; i < npc; i++)
                {   // positive
                    int idx = t + i;
                    if (!src.IsClosed && idx >= npc)
                        break;
                    if (idx >= npc)
                        idx -= npc;
                    double d = idx > t ? l[idx] : l[idx] + len;
                    double w = wm - d + dc;
                    if (w < 0)
                        break;
                    sx += src[idx].X * w;
                    sy += src[idx].Y * w;
                    tw += w;
                }
                for (int i = 1; i < npc; i++)
                {   // negative
                    int idx = t - i;
                    if (!src.IsClosed && idx < 0)
                        break;
                    if (idx < 0)
                        idx += npc;
                    double d = idx < t ? l[idx] : l[idx] - len;
                    double w = wm + d - dc;
                    if (w < 0)
                        break;
                    sx += src[idx].X * w;
                    sy += src[idx].Y * w;
                    tw += w;
                }
                pc.Add(new Point(sx / tw, sy / tw));
                lpos = l[t];
            }
            if (src.IsClosed)
                pc.Add(pc[0]);
            Polygon res = new Polygon(src);
            res.Poly = pc;
            l = res.AccummulatedLength();
            lpos = l[res.Count - 1];
            for (int i = res.Count - 2; i >= 0; i--)
            {
                double d = lpos - l[i];
                if (d > 1.5 * baseLength)
                {
                    int n = (int)(d / baseLength + 0.5);
                    Vector v = (res[i + 1] - res[i]) / n;
                    Point pn = res[i + 1];
                    for (int j = 1; j < n; j++)
                    {
                        pn -= v;
                        res.Poly.Insert(i + 1, pn);
                    }
                }
                lpos = l[i];
            }
            return res;
        }
        void ClearPoints(int i, double dd)
        {
            int j = i;
            double lpos = dist[i];
            double d;
            int ind;
            do
            {
                j++;
                if (j < nPoints)
                {
                    d = dist[j];
                    ind = j;
                }
                else if (closed)
                {
                    ind = j - nPoints;
                    d = dist[ind] + totDist;
                }
                else
                    break;
                if (flag[ind] != 0)
                    break;
                flag[ind] = -1;
            } while (d < lpos + dd);
            j = i;
            do
            {
                j--;
                if (j >= 0)
                {
                    d = dist[j];
                    ind = j;
                }
                else if (closed)
                {
                    ind = j + nPoints;
                    d = dist[ind] - totDist;
                }
                else
                    break;
                if (flag[ind] != 0)
                    break;
                flag[ind] = -1;
            } while (d > lpos - dd);
        }
        public Polygon CreateShortPolygon(Polygon polygon)// divides curve into almost strait (d<maxDeviation) segmants
        {
            if (polygon.IsEmpty)
                return null;
            Polygon src = CreateEquidistantContour(polygon);
            //Debug.WriteLine("original "+polygon.ToPointsString());
            //Debug.WriteLine("smoothed "+src.ToPointsString());
            dist = src.AccummulatedLength();
            nPoints = dist.Length;
            totDist = dist[nPoints - 1];
            maxLength = totDist / 10;
            closed = src.IsClosed;
            range = new double[nPoints]; // curvature (1/2R)
            Point pm = src[0];
            Point p = src[1];
            Point pp;
            double minRange = totDist;
            int minRangeInd = 0;
            for (int i = 2; i < nPoints; i++)
            {
                pp = src[i];
                Vector vp = pp - p;
                Vector vm = pm - p;
                double cp = Vector.CrossProduct(vp, vm);
                range[i - 1] = 1 / (Math.Abs(cp) / (vp.Length * vm.Length * (vp - vm).Length) / angle + 1 / maxLength);
                if (minRange > range[i - 1])
                {
                    minRange = range[i - 1];
                    minRangeInd = i - 1;
                }
                pm = p;
                p = pp;
            }
            if (closed)
            {
                pm = src[src.Count - 2];
                p = src[0];
                pp = src[1];
                Vector vp = pp - p;
                Vector vm = pm - p;
                double cp = Vector.CrossProduct(vp, vm);
                range[0] = range[nPoints - 1] = 1 / (Math.Abs(cp) / (vp.Length * vm.Length * (vp - vm).Length) / angle + 1 / maxLength);
                if (minRange > range[0])
                {
                    minRange = range[0];
                    minRangeInd = 0;
                }
            }
            else
                range[0] = range[nPoints - 1] = 0;
            double avRange = 0;
            foreach (double r in range)
                avRange += r;
            avRange /= nPoints;

            flag = new int[nPoints];
            if (!closed)
                flag[0] = flag[nPoints - 1] = 1;
            while (minRangeInd >= 0)
            {
                flag[minRangeInd] = 1;
                ClearPoints(minRangeInd, minRange);
                minRangeInd = -1;
                minRange = totDist;
                for (int i = 0; i < nPoints; i++)
                    if (flag[i] == 0 && minRange > range[i])
                    {
                        minRange = range[i];
                        minRangeInd = i;
                    }
            }
            List<Point> cps = new List<Point>();
            for (int i = 0; i < nPoints; i++)
                if (flag[i] > 0)
                    cps.Add(src[i]);
            if (closed)
                cps.Add(cps[0]);
            Polygon pol = new Polygon(polygon);
            pol.Poly = cps;
            return pol;
        }
    }
    public class Polygon : Primitive  
    {   // fixed point set, no closing needed   
        static PolygonBuilder builder = new PolygonBuilder();
        protected List<Point> poly = new List<Point>();
        public IntSize Target   { get; }// added points are limited to inside target rectangle {0,0,w,h}
        public List<Point> Poly { get { return poly; } set { poly = value; } }
        public bool IsClosed    { get { return First == Last; } }
        public Point First      { get { return Count>0 ? poly[0] : new Point(); } }
        public Point Last       { get; private set; }
        public bool IsEmpty     { get { return Count < 3; } }
        public int Count        { get { return poly.Count; } }
        public double TotalLength()
        {
            double l = 0;
            for (int i = 1; i < Count; i++)
                l += (poly[i] - poly[i - 1]).Length;
            return l;
        }
        public Point this[int i] { get { return poly[i]; } set { poly[i] = value; } }
        public Polygon(IntSize range) { Target = range; Last = new Point(-10, -10); }
        public Polygon(Polygon points)
        {
            poly = points.poly;
            Last = Count > 0 ? poly[Count-1] : new Point(-10, -10);
            Target = points.Target;
        }
        public Polygon(List<Point> points, IntSize range)
        {
            poly = points;
            Last = Count > 0 ? poly[Count - 1] : new Point(-10, -10);
            Target = range;
        }
        public void Add(Point p)
        {
            if ((Last - p).LengthSquared < 2)
                return;
            int x = p.X < 0 ? -1 : p.X > Target.Width ? 1 : 0;
            int y = p.Y < 0 ? -1 : p.Y > Target.Height ? 1 : 0;
            if (x != 0 && y != 0)
                return;
            if (x == 0)
            {
                if (y > 0)
                    p.Y = Target.Height;
                if (y < 0)
                    p.Y = 0;
            }
            else
            {
                if (x > 0)
                    p.X = Target.Width;
                if (x < 0)
                    p.X = 0;
            }
            poly.Add(p);
            Last = p;
        }
        public void Close(Point p) { Add(p); Add(First); }
        public void Clear()     { poly.Clear(); }
        public Int32Rect IntRect(int border)
        {
            if (IsEmpty)
                return new Int32Rect();
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            foreach (var p in poly)
            {
                if (minX > p.X)
                    minX = (int)p.X;
                if (minY > p.Y)
                    minY = (int)p.Y;
                if (maxX < p.X)
                    maxX = (int)p.X;
                if (maxY < p.Y)
                    maxY = (int)p.Y;
            }
            minX -= 1 + border;
            minY -= 1 + border;
            maxX += 2 + border;
            maxY += 2 + border;
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (maxX > Target.Width) maxX = Target.Width;
            if (maxY > Target.Height) maxY = Target.Height;
            return new Int32Rect(minX, minY, maxX - minX, maxY - minY);
        }
        public Polygon CreateEquidistantContour(double length) { builder.baseLength = length; return builder.CreateEquidistantContour(this); }
        public Polygon CreateShortPolygon(double length) { builder.baseLength = length; return builder.CreateShortPolygon(this); }
        public double[] AccummulatedLength()
        {
            double[] l = new double[Count];
            if (Count > 0)
            {
                l[0] = 0;
                for (int i = 1; i < Count; i++)
                    l[i] = l[i - 1] + (poly[i] - poly[i - 1]).Length;
            }
            return l;
        }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)
        {
            if (IsEmpty)
                return;
            Point[] points = poly.ToArray();
            ToDrawing.Value.Transform(points);
            PathSegment ps = new PolyLineSegment(points, true); // true will draw poly line
            Geometry geom = new PathGeometry(new PathFigure[] { new PathFigure(points[0], new PathSegment[] { ps }, false) });
            g.DrawGeometry(brush, pen, geom);
        }
        public string ToPointString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("points=" + poly.Count);
            for (int i = 0; i < poly.Count; i++)
            {
                sb.Append(i);
                sb.Append(' ');
                sb.Append(poly[i].X.ToString("f1"));
                sb.Append(',');
                sb.AppendLine(poly[i].Y.ToString("f1"));
            }
            return sb.ToString();
        }
    }
    public class Cross : Primitive
    {
        double d = 4;
        public Point Location;
        public Cross(Point location) { Location = location; }
        public Cross(Point location, double size) { Location = location; d = size; }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)
        {
            g.DrawLine(pen, new Point(Location.X - d, Location.Y), new Point(Location.X + d, Location.Y));
            g.DrawLine(pen, new Point(Location.X, Location.Y - d), new Point(Location.X, Location.Y + d));
        }
    }
    public abstract class MouseAction : Primitive
    {
        protected const int mSize = 6;          // operation marker drawing size; capture size is twice larger
        protected bool changed = true;          // true if indicators changed 
        public Point Center = new Point();      // center indicator location in canvas coordinates
        public double CatchSize = 10;
        protected static Vector[] controls = new Vector[] { new Vector(-1, -1), new Vector(1, -1), new Vector(1, 1), new Vector(-1, 1) };
        static int Ncontrols = controls.Length; // number of control points;
        protected Point[] iLoc = new Point[Ncontrols]; // indicators' locations
        public static bool IsLineOperation(MouseOperation mouseAction) { return (int)mouseAction >= Ncontrols; }
        public static bool IsControlOperation(int mouseActionInd) { return mouseActionInd >= 0; }
        public void MoveCenter(Vector d) { Center.X += d.X; Center.Y += d.Y; changed = true; }
        public virtual MouseOperation OperationFromPoint(Point p)  // finds MouseOperation from point
        {
            if ((p - Center).LengthSquared < 2 * mSize * mSize)
                return MouseOperation.OpCenter;
            for (int i = 0; i < controls.Length; i++)
            {   // vortex index
                if ((p - iLoc[i]).LengthSquared < 2 * mSize * mSize)
                    return (MouseOperation)i;
            }
            return OperationFromLine(p);
        }
        public virtual MouseOperation OperationFromLine(Point p)   // finds MouseOperation from line
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Vector pp = (Vector)p;
                double minDist = double.MaxValue;
                int ind = int.MaxValue;
                for (int i = 0; i < controls.Length; i++)
                {   // edge (line connecting vortexes) index
                    Vector pb = (Vector)iLoc[i];
                    Vector pe = (Vector)(i == controls.Length - 1 ? iLoc[0] : iLoc[i + 1]);
                    Vector v = pe - pb;
                    double proj = v * pp;
                    if (proj > pe * v || proj < pb * v)
                        continue;
                    double d = Math.Abs(Vector.CrossProduct(v, pp - pb));
                    if (d < v.Length * CatchSize && minDist > d)
                    {
                        minDist = d;
                        ind = i;
                    }
                }
                if(ind != int.MaxValue)
                    return (MouseOperation)(ind + controls.Length);
            }
            return OperationFromMouse;
        }
        public static MouseOperation OperationFromMouse
        {
            get
            {
                return  Mouse.RightButton == MouseButtonState.Pressed ? MouseOperation.Rotate :
                        Mouse.LeftButton == MouseButtonState.Pressed ? MouseOperation.Move :
                        Mouse.MiddleButton == MouseButtonState.Pressed ? MouseOperation.Scale : MouseOperation.None;
            }
        }
        public double RadiansFromPoints(Point current, Point old)  // angle between vectors in degrees
        {
            double oldA = Math.Atan2(old.Y - Center.Y, old.X - Center.X);
            double newA = Math.Atan2(current.Y - Center.Y, current.X - Center.X);
            return newA - oldA;
        }
        public double ScaleFromPoints(Point current, Point old)  // ratio of vector lengths
        {
            double oldD = (old - Center).Length;
            return oldD != 0 ? (current - Center).Length / oldD : 1;
        }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)   // draws votexes
        {
            for (int i = 0; i < controls.Length; i++)
                g.DrawEllipse(brush, null, iLoc[i], mSize, mSize);
            Cross c = new Cross(Center);
            c.Draw(g, brush, pen);
        }
    }
    public class MorthPoint
    {
        public Vector Shift;
        public Point Center;
        public Size Size;
        public double Angle = 0;
        public MorthPoint(Point center, double radius) { Center = center; Size = new Size(radius, radius); }
    }
    public class MorphControl : MouseAction // morph point control
    {
        MorthPoint morthPoint;
        public MorphControl(MorthPoint mp) { morthPoint = mp; Center = mp.Center; }
        Point shiftPos { get { return morthPoint.Size.Width * morthPoint.Shift + Center; } } // shift indicator location in canvas coordinates
        public override MouseOperation OperationFromPoint(Point p)  // finds MouseOperation from point
        {
            if ((p - shiftPos).LengthSquared < 2 * mSize * mSize)
                return MouseOperation.Morph;
            return base.OperationFromPoint(p);
        }
        public Vector SetShift(Point p) { return morthPoint.Shift = (p - Center) / morthPoint.Size.Width; }
        public void SetMorphArea(MouseOperation mouseAction, Point current, Point old)
        {
            int mouseActionInd = (int)mouseAction;
            if (mouseActionInd < 0 || mouseActionInd >= iLoc.Length)
                return;
            if (mouseActionInd == 0)    // angle not implemented
                return;
            if (mouseActionInd == 1)
            {
                Center += (current - old);
                morthPoint.Center = Center;
                return;
            }
            var r = ScaleFromPoints(current, old);
            morthPoint.Size.Height *= r;
            morthPoint.Size.Width *= r;
        }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)   // draws votexes
        {
            double a = morthPoint.Angle + Math.PI/4;
            double w = morthPoint.Size.Width;
            double h = morthPoint.Size.Height;
            for (int i = 0; i < controls.Length; i++)
                iLoc[i] = new Point((Math.Cos(a) * w * controls[i].X - Math.Sin(a) * h * controls[i].Y) + Center.X,
                                    (Math.Sin(a) * w * controls[i].X + Math.Cos(a) * h * controls[i].Y) + Center.Y);
            g.DrawEllipse(brush, pen, Center, w, h);
            g.DrawEllipse(brush, null, shiftPos, mSize, mSize);
            base.Draw(g, brush, pen);
        }
    }
    public class CropRect : MouseAction
    {
        double left=0;
        double right;
        double top =0;
        double bottom;
        public Rect Rect { get { return new Rect(left, top, Math.Max(right - left, 1), Math.Max(bottom - top, 1)); } }
        public CropRect(IntSize size, double x, double y)
        {
            Center = new Point(x, y);
            left = Center.X - size.Width / 2.0;
            right = Center.X + size.Width / 2.0;
            top = Center.Y - size.Height / 2.0; ;
            bottom = Center.Y + size.Height / 2.0;
            CatchSize = (size.Width + size.Height) / 10.0;
        }
        public void SetToDrawingTransform(double scale, double w, double h)
        {
            double offsetX = w / 2 - Center.X * scale;
            double offsetY = h / 2 - Center.Y * scale;
            ToDrawing = new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY);
        }
        public bool Update(MouseOperation mouseAction, Point d)
        {
            int mouseActionInd = (int)mouseAction;
            if (mouseActionInd < iLoc.Length)
                return false;
            mouseActionInd -= iLoc.Length;
            if (mouseActionInd == 0) top = (int)d.Y;
            if (mouseActionInd == 1) right = (int)d.X;
            if (mouseActionInd == 2) bottom = (int)d.Y;
            if (mouseActionInd == 3) left = (int)d.X;
            return true;
        }
        public override MouseOperation OperationFromLine(Point p) { return Scaled ? base.OperationFromLine(p) : OperationFromMouse; }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)
        {
            for (int i = 0; i < controls.Length; i++)
                iLoc[i] = new Point((controls[i].X < 0 ? left : right), (controls[i].Y < 0 ? top : bottom));
            Point[] dp = (Point[])iLoc.Clone();
            if (Scaled)
                ToDrawing.Value.Transform(dp);
            PathSegment ps = new PolyLineSegment(dp, true); // true will draw poly line
            Geometry geom = new PathGeometry(new PathFigure[] { new PathFigure(dp[0], new PathSegment[] { ps }, true) });
            //Geometry geom = new RectangleGeometry(Rect);
            g.DrawGeometry(null, pen, geom);
        }
        public override string ToString() { return ((int)(Rect.Width + 0.5)).ToString() + 'x' + (int)(Rect.Height + 0.5); }
    }
    public class MatrixControl : MouseAction
    {   // matrix is represented as a product of rotation (R), shear (H), scale (S), and aspect (A) matrixes
        //       | cosF  -sinF |      | 1  h |      | s  0 |      | 1/a  0 |
        //     R=|             |    H=|      |    S=|      |    A=|        |
        //       | sinF   cosF |      | h  1 |      | 0  s |      |  0   a |
        // S & R are non-distortion transforms; A & H are distortion transforms
        // scale, rotate, aspect, and shear operations leave center in place
        public Vector ViewDistortion { get; protected set; }
        Matrix matrix = new Matrix();
        protected double quadrantSize = 80; // input quadrant size
        public double RenderScale = 1;  // active layer render scale
        double xFlip = 1;               // normal =1; flipped =-1:
        double angle = 0;               // in radians
        double shear = 0;
        double aspect = 1;
        public double Flip { get { return xFlip; } }
        public double Angle { get { return angle; } set { angle = value; changed = true; } }
        public double Shear { get { return shear; } set { shear = value; changed = true; } }
        public double Aspect { get { return aspect; } set { aspect = value; changed = true; } }
        public MatrixControl() { }
        public MatrixControl(double flip, double scale, double angle_, double aspect_, double shear_, Point center)
        {
            xFlip = flip;
            RenderScale = scale;
            angle = angle_;
            aspect = aspect_;
            shear = shear_;
            Center = center;
        }
        public MatrixControl Clone()
        {
            MatrixControl src = new MatrixControl();
            src.Center = Center;
            src.RenderScale = RenderScale; 
            src.xFlip = xFlip;
            src.angle = angle;
            src.shear = shear;
            src.aspect = aspect;
            return src;
        }
        public void FlipX() { xFlip = -xFlip; changed = true; }
        public void FlipY() { xFlip = -xFlip; angle += Math.PI; changed = true; }
        public void RotateRight() { angle -= Math.PI/2; changed = true; }
        public void RotateLeft() { angle += Math.PI/2; changed = true; }
        public Matrix GeometryMatrix { get { if (changed) UpdateMatrix(); return matrix; } }
        public void RotateAngle(double a) { angle -= a; changed = true; }
        public void ScaleAt(double coef, Point center)
        {
            RenderScale *= coef;
            MoveCenter((center - Center)* coef);
        }
        public void RotateAt(double a, Point center)
        {
            angle -= a;
            double c = Math.Cos(a);
            double s = Math.Sin(a);
            double dx = center.X - Center.X;
            double dy = center.Y - Center.Y;
            Vector v = new Vector(c * dx + s * dy, -s * dx + c * dy);
            MoveCenter(v);
        }
        public double SetShearAndAspect(MouseOperation mouseAction, Vector d)
        {
            int mouseActionInd = (int)mouseAction;
            if (mouseActionInd<0) // only vortexes or lines processed
                return 0;
            double inc;
            if (mouseActionInd < iLoc.Length)   // vortexes
            {
                Vector loc = iLoc[mouseActionInd] - Center;
                inc = d * loc / loc.LengthSquared;
                shear += xFlip * inc * (1 - 2 * (mouseActionInd % 2));
                shear = Math.Max(-0.9, Math.Min(0.9, shear));
            }
            else // lines
            {
                mouseActionInd -= iLoc.Length;
                int next = mouseActionInd == iLoc.Length - 1 ? 0 : mouseActionInd + 1;
                Vector loc = ((iLoc[mouseActionInd] - Center) + (iLoc[next] - Center))/2;
                inc = d * loc / loc.LengthSquared;
                aspect /= 1 - inc * (1 - 2 * (mouseActionInd % 2));
            }
            changed = true;
            //Debug.WriteLine(" Aspect=" + aspect.ToString() + " Shear=" + shear.ToString());
            return inc;
        }
        public bool ApplyShearAndAspectIncrement(MouseOperation mouseAction, double inc)
        {
            int mouseActionInd = (int)mouseAction;
            if (mouseActionInd < 0)
                return false;
            if (mouseActionInd < iLoc.Length)
                shear += xFlip * inc * (1 - 2 * (mouseActionInd % 2));
            else
                aspect /= 1 - inc * (1 - 2 * (mouseActionInd % 2));
            changed = true;
            //Debug.WriteLine(" Aspect=" + aspect.ToString() + " Shear=" + shear.ToString());
            return true;
        }
        public virtual Vector SetViewDistortion(Point p) { return new Vector(); }
        void UpdateMatrix()
        {
            double ca = Math.Cos(angle);
            double sa = Math.Sin(angle);
            double shc = 1/Math.Sqrt(1 - shear * shear);
            // rotation last
            //             | cos/a-h*sin*a  h*cos/a-sin*a |   | M11  M21 |
            //     M=R*A*H=|                              | = |          |
            //             | h*cos*a+sin/a  cos*a+h*sin/a |   | M12  M22 |
            matrix.M11 = xFlip * (ca / aspect + sa * shear * aspect) * shc;
            matrix.M12 = xFlip * (ca * shear * aspect - sa / aspect) * shc;
            matrix.M21 = (ca * shear / aspect + sa * aspect) * shc;
            matrix.M22 = (ca * aspect - sa * shear / aspect) * shc;
            matrix.OffsetX = Center.X - matrix.M11 * Center.X - matrix.M21 * Center.Y;
            matrix.OffsetY = Center.Y - matrix.M12 * Center.X - matrix.M22 * Center.Y;
            //Debug.WriteLine("scale=" + RenderScale.ToString() + " angle=" + angle.ToString() + " aspect=" + aspect.ToString() + " shear=" + shear.ToString());
            for (int i = 0; i < controls.Length; i++)
                iLoc[i] = new Point((matrix.M11 * controls[i].X + matrix.M21 * controls[i].Y) * quadrantSize + Center.X,
                                    (matrix.M12 * controls[i].X + matrix.M22 * controls[i].Y) * quadrantSize + Center.Y);
            changed = false;
        }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)
        {
            if (changed)
                UpdateMatrix();
            PathSegment ps = new PolyLineSegment(iLoc, true); // true will draw poly line
            Geometry geom = new PathGeometry(new PathFigure[] { new PathFigure(iLoc[0], new PathSegment[] { ps }, true) });
            g.DrawGeometry(null, pen, geom);
            base.Draw(g, brush, pen);
        }
        public override string ToString()
        {
            return "center=[" + Center.X.ToString("f1") + ',' + Center.Y.ToString("f1") + "] flip=" + (xFlip > 0 ? 'N' : 'Y') + " scale=" +
                RenderScale.ToString("f2") + " angle=" + angle.ToString("f2") + " shear=" + shear.ToString("f2") + " aspect=" + aspect.ToString("f2");
        }
    }
    public class DistortionControl : MatrixControl  // saving in VisualLayerData NOT IMPLEMENTED
    {
        public override Vector SetViewDistortion(Point p) { return ViewDistortion = (p - Center)/ quadrantSize; }
        Point distortionPos { get { return quadrantSize * ViewDistortion + Center; } } // view point indicator location in canvas coordinates
        public override MouseOperation OperationFromPoint(Point p)  // finds MouseOperation from point
        {
            if ((p - distortionPos).LengthSquared < 2 * mSize * mSize)
                return MouseOperation.ViewPoint;
            return base.OperationFromPoint(p);
        }
        public override void Draw(DrawingContext g, Brush brush, Pen pen)   // draws votexes
        {
            base.Draw(g, brush, pen);
            g.DrawEllipse(brush, null, distortionPos, mSize, mSize);
        }
    }
}