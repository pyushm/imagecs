using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace ImageWindows
{
    public enum MatrixPointType
    {
        Unassigned,     // initial state
        Removed,        // state after first use
        Edge,           // point on edge 
        NoDirection,    // 
        Slope,          // point on slope (dx, dy) points to higher values 
        Ridge,          // point on ridge (dx, dy) is ridge direction and strength
        Chain,          // part of chain
    }
    public class ContourPoint
    {
        static public System.Windows.Vector operator -(ContourPoint v1, ContourPoint v2) { return v1.location - v2.location; }
        Vector location;
        double val;
        public Vector Location { get { return location; } }
        public double X { get { return location.X; } }
        public double Y { get { return location.Y; } }
        public double Value { get { return val; } }
        public ContourPoint(Vector l, double v) { location = l; val = v; }
        public override string ToString() { return "loc=(" + X.ToString("f1") + ',' + Y.ToString("f1") + ") val=" + val.ToString("f0"); }
    }
    public class ContourSegment : ContourPoint
    {
        static public int NAveraging = 5;// curvature is averaged over NAveraging previous points
        Vector segment;                 // vector from previous location to this location
        Vector normal;                  // normalized vector perpendicular to segment in the direction of positive curvature
        double curvature;               // 1/R, where R circle is radius connecting 2 segments; clocwise - positive
        double averageCurvature;
        double averageDeviation;
        double segmentLength;
        internal ContourSegment Next;
        public Vector Segment { get { return segment; } }
        public Vector Normal { get { return normal; } }
        public double SegmentLength { get { return segmentLength; } }
        public double AverageCurvature { get { return averageCurvature; } }
        public double AverageDeviation { get { return averageDeviation; } }
        internal ContourSegment(MatrixPoint p, Vector v) : base(new Vector(p.X + v.X, p.Y + v.Y), p.Val)
        {
            segment = v;
            segmentLength = segment.Length;
            curvature = 0;
            normal = new Vector(-segment.Y, segment.X);
            normal.Normalize();
            averageCurvature = 0;
            averageDeviation = 0.5 / segmentLength / segmentLength; // search size (2 * SL^2 * AD) ~ 1
            Next = null;
        }
        internal ContourSegment(Vector loc, double val, ContourSegment prev) : base(loc, val)
        {
            segment = loc - prev.Location;
            segmentLength = segment.Length;
            curvature = 2 * Vector.CrossProduct(prev.segment, segment) / (prev.segmentLength * segmentLength *(prev.segment + segment).Length);
            normal = new Vector(-segment.Y, segment.X);
            normal.Normalize();
            double c = (1 - 1.0 / NAveraging);
            averageCurvature = curvature / NAveraging + c * prev.averageCurvature;
            averageDeviation = Math.Abs(curvature - averageCurvature) / NAveraging + c * prev.averageDeviation;
            Next = null;
        }
        public bool Crosses(ContourPoint p)
        {
            Vector d = this - p;
            return d.Length < segmentLength;
            //double projection = d * segment;
            //double n = d * normal;
            //return projection > 0 && projection <= segmentLength && Math.Abs(n) < segmentLength;
        }
        internal string DumpChain()
        {
            string res = "Chain" + Environment.NewLine;
            ContourSegment p = this;
            while (p != null)
            {
                res += p.ToString() + Environment.NewLine;
                p = p.Next;
            }
            return res;
        }
    }
    public class MatrixPoint
    { 
        //static public Direction operator -(MatrixPoint v1, MatrixPoint v2) { return new Direction(v1.X - v2.X, v1.Y - v2.Y); }
        static internal Comparison<MatrixPoint> Comparison = delegate(MatrixPoint p1, MatrixPoint p2) { return Math.Sign(p2.val - p1.val); };
        float val;
        ushort x, y;						// location
        internal MatrixPointType Type;
        internal ushort X                   { get { return x; } }
        internal ushort Y                   { get { return y; } }
        internal float Val                  { get { return val; } }
        internal MatrixPoint(ushort i, ushort j, float val_)
        {
            x = i;
            y = j;
            val = val_;
            Type = MatrixPointType.Unassigned;
        }
    }
    public class Contour
    {
        static int Key(MatrixPoint p) { return (p.X << 16) | p.Y; }
        static int Key(ushort i, ushort j) { return (i << 16) | j; }
        Vector n1, n2;                      // slope directions from last point type asignment
        double searchScale = 1;                // ridge search radius coefficient from last ridge point search
        Hashtable pixelTable;               // the way to find pixel by its coordinates
        int scale;                          // scale of initial ridge search
        ValueMartix src;
        List<MatrixPoint> pixels;  // 
        List<ContourPoint[]> chains;
        double interpolationCut = -0.1;
        double clearSize=2.5;
        double chainStartCut;
        float[,] matrix;
        string DumpPoints()
        {
            string res = "Points " + pixelTable.Count+Environment.NewLine;
            IDictionaryEnumerator en=pixelTable.GetEnumerator();
            en.Reset();
            while (en.MoveNext())
            {
                int k=(int)en.Key;
                MatrixPoint p = (MatrixPoint)en.Value;
                res += k.ToString()+" "+p.ToString() + Environment.NewLine;
            }
            return res;
        }
        public Contour(ValueMartix src_, int scale_)
        {
            scale = scale_;
            src = src_;
            matrix = src.Matrix;
        }
        public ContourPoint[][] Ridge(double minCoef, double maxCoef, bool inverse)
        {
            chains = new List<ContourPoint[]>();
            float maxValue = (float)(3 * src.DataRange.average);
            double minRidge = inverse ? maxValue - src.DataRange.average : src.DataRange.average;
            minRidge *= minCoef;
            pixels = new List<MatrixPoint>();
            pixelTable = new Hashtable();
            for (int i = 0; i < src.Width; i++)
                for (int j = 0; j < src.Height; j++)
                {
                    if (inverse)
                        matrix[j, i] = maxValue - matrix[j, i];
                    float fl = matrix[j, i];
                    if (fl > minRidge)
                    {
                        MatrixPoint mp = new MatrixPoint((ushort)i, (ushort)j, fl);
                        pixels.Add(mp);
                        pixelTable.Add(Key(mp), mp);
                    }
                }
            pixels.Sort(MatrixPoint.Comparison);
            double max = inverse ? maxValue - src.DataRange.min : src.DataRange.max;
            chainStartCut = max * maxCoef + minRidge * (1 - maxCoef);
            ContourSegment.NAveraging = 3;
            Console.WriteLine("NEW PARAMETERS: minRidge=" + minRidge.ToString("f0") + " chainStartCut=" + chainStartCut.ToString("f1"));
            BuildRidges();
            return chains.ToArray();
        }
        void BuildRidges()
        {
            foreach (MatrixPoint mp in pixels)
            {
                if (mp.Type != MatrixPointType.Unassigned)
                    continue;
                if (mp.Val < chainStartCut)
                    break;
                Vector ridgeDirection = GetRidgeDirection(mp, scale);
                RemovePoint(mp);
                if (mp.Type != MatrixPointType.Ridge)
                    continue;
                ContourSegment chainTopf = new ContourSegment(mp, ridgeDirection);
                bool closed = BuildChain(chainTopf);
                ContourSegment chainTopb = closed ? null : new ContourSegment(mp, -ridgeDirection);
                if (chainTopb != null)
                    BuildChain(chainTopb);
                ContourPoint[] contour = ContourPointCollection(chainTopf, chainTopb);
                if (contour.Length > 3)
                {
                    chains.Add(contour);
                    ClearPath(contour, closed);
                    //Console.WriteLine("contour " + ind++ + " length=" + contour.Length);
                    //foreach (ContourPoint cp in contour)
                    //    Console.WriteLine(cp.ToString());
                }
            }
        }
        ContourPoint[] ContourPointCollection(ContourSegment forward, ContourSegment backward)
        {
            if (forward == null && backward == null)
                return new ContourPoint[0];
            int countf = 0;
            ContourSegment chain = forward;
            while (chain != null)
            {
                chain = chain.Next;
                countf++;
            }
            int countb = 0;
            chain = backward == null ? null : backward.Next;
            while (chain != null)
            {
                chain = chain.Next;
                countb++;
            }
            ContourPoint[] points = new ContourPoint[countf + countb];
            chain = forward;
            for (int i = 0; i < countf; i++)
            {
                points[i + countb] = chain;
                chain = chain.Next;
            }
            chain = backward == null ? null : backward.Next;
            for (int i = 1; i <= countb; i++)
            {
                points[countb - i] = chain;
                chain = chain.Next;
            }
            return points;
        }
        bool BuildChain(ContourSegment chainTop)
        {
            ContourSegment next = chainTop;
            ContourSegment prev = next;
            while (true)
            {
                if (CloseToEdge(next))
                    break;
                prev = next;
                next = FindRidgePoint(prev);
                if(next != null && next.Crosses(chainTop))
                    return true;
                if (next == null)
                    return false;
                prev.Next = next;
            }
            return false;
        }
        ContourSegment FindRidgePoint(ContourSegment cp)
        {
            double l2 = cp.SegmentLength * cp.SegmentLength;
            double f = searchScale / (1 + 0.25 * cp.AverageCurvature * cp.AverageCurvature * l2);
            Vector x = f * cp.Segment + 0.5 * f * cp.AverageCurvature * l2 * cp.Normal;
            Vector n = new Vector(-x.Y, x.X);
            n *= Math.Max(2 * cp.SegmentLength * cp.AverageDeviation, 1.2/n.Length);
            Vector l = cp.Location + x;
            ParabolicInterpolation interpolation = Interpolation(l, n);
            if(double.IsNaN(interpolation.MaxLocation))
                return null;
            bool div = false;
            if (!interpolation.InRange)
            {
                div = true;
                l=cp.Location + x * 0.5;
                //n *= 0.7;
                interpolation = Interpolation(l, n);
                if (!interpolation.InRange)
                    return null;
            }
            double maxAt=interpolation.MaxLocation - 2;
            ContourSegment maxPoint = new ContourSegment(l + maxAt * n, interpolation.MaxValue, cp);
            searchScale = (interpolation.Mid ? 0.7 : 0.5) * (div ? 0.7 : 1) / Math.Sqrt(maxPoint.AverageDeviation) / maxPoint.SegmentLength;
            //Console.WriteLine(maxPoint.ToString() + " Segment=(" + cp.Segment.X.ToString("f1") + ',' + cp.Segment.Y.ToString("f1") +
            //    ") x=(" + x.X.ToString("f1") + ',' + x.Y.ToString("f1") + ") n=(" + n.X.ToString("f1") + ',' + n.Y.ToString("f1") +
            //    ") Length=" + n.Length.ToString("f1") + " max@" + maxAt.ToString("f1") + " SS=" + searchScale.ToString("f1") + " div=" + div);
            return maxPoint;
        }
        ParabolicInterpolation Interpolation(Vector l, Vector n)
        {
            Vector[] searchPoints = new Vector[5];
            for (int i = -2; i <= 2; i++)
                searchPoints[i+2] = l + i * n;
            Vector[] searchValues = GetValues(searchPoints);
            return searchValues.Length == 0 ? new ParabolicInterpolation(MaxType.Edge) : new ParabolicInterpolation(searchValues, interpolationCut);
        }
        Vector[] GetValues(Vector[] points)
        {
            Vector[] values = new Vector[points.Length];
            for(int k=0; k<points.Length; k++)
            {
                try
                {
                    Vector p = points[k];
                    int i = (int)p.X;
                    int j = (int)p.Y;
                    if (i < 0 || i+1 >= src.Width || j < 0 || j+1 >= src.Height)
                        return new Vector[0];
                    double px = p.X - i;
                    double mx = 1 - px;
                    double py = p.Y - j;
                    double my = 1 - py;
                    double mm = my * mx;
                    double pm = my * px;
                    double mp = py * mx;
                    double pp = py * px;
                    values[k] = new Vector(k, mm * matrix[j, i] + pm * matrix[j, i + 1] + mp * matrix[j + 1, i] + pp * matrix[j + 1, i + 1]);
                }
                catch { }
            }
            return values;
        }
        void ClearPath(ContourPoint[] contour, bool closed)
        {
            for(int i=1; i<contour.Length; i++)
                ExcludeSegment(contour[i - 1], contour[i]);
            if(closed)
                ExcludeSegment(contour[contour.Length - 1], contour[0]);
        }
        void ExcludeSegment(ContourPoint p1, ContourPoint p2)
        {
            Vector vl = p2 - p1;
            vl /= vl.Length;
            ushort im = (ushort)Math.Max(0, Math.Min(p1.X, p2.X) - clearSize);
            ushort ip = (ushort)Math.Min(src.Width, Math.Max(p1.X, p2.X) + clearSize + 1);
            ushort jm = (ushort)Math.Max(0, Math.Min(p1.Y, p2.Y) - clearSize);
            ushort jp = (ushort)Math.Min(src.Height, Math.Max(p1.Y, p2.Y) + clearSize + 1);
            for (ushort i = im; i < ip; i++)
                for (ushort j = jm; j <= jp; j++)
                {
                    //    Console.WriteLine("FindPoint=" + loc.X + ',' + loc.Y + " key=" + k + " no point");
                    if(!ValidIndex(i, j))
                        continue;
                    Vector vp = new Vector(i - p1.X, j - p1.Y);
                    double d = Math.Abs(Vector.CrossProduct(vp, vl));
                    if (d > clearSize)
                        continue;
                    //matrix[j, i] = 0;
                    int k = Key(i, j);
                    MatrixPoint p = (MatrixPoint)pixelTable[k];
                    if (p != null)
                    {
                        //Console.WriteLine("ClearPath key=" + k + " " + p.ToString());
                        //if (p.Type == MatrixPointType.Unassigned)
                        //    p.Type = MatrixPointType.Removed;
                        //else
                            p.Type = MatrixPointType.Removed;
                        pixelTable.Remove(k);
                    }
                }
        }
        bool ValidIndex(int i, int j) { return i >= 0 && j >= 0 && i < src.Width && j < src.Height; }
        //MatrixPoint FindPoint(Vector loc)
        //{
        //    if (loc.X < 0 || loc.X >= src.Width || loc.Y < 0 || loc.Y >= src.Height)
        //        return null;
        //    int k = Key((ushort)loc.X, (ushort)loc.Y);
        //    MatrixPoint p = (MatrixPoint)pixelTable[k];
        //    if (p != null)
        //    {
        //        //Console.WriteLine("Find removed key=" + k + " " + p.ToString());
        //        //if (p.Type == MatrixPointType.Unassigned)
        //        //    p.Type = MatrixPointType.Removed;
        //        //else
        //            p.Type = MatrixPointType.Removed;
        //        pixelTable.Remove(k);
        //    }
        //    //else
        //    //    Console.WriteLine("FindPoint=" + loc.X + ',' + loc.Y + " key=" + k + " no point");
        //    return p;
        //}
        void RemovePoint(MatrixPoint p)
        {
            int k = Key(p);
            pixelTable.Remove(k);
            //Console.WriteLine("RemovePoint key=" + k + p.ToString());
        }
        bool CloseToEdge(ContourSegment p)
        {
            double s = p.SegmentLength/2;
            return p.X < s || p.X >= src.Width - s || p.Y < s || p.Y >= src.Height - s;
        }
        Vector GetRidgeDirection(MatrixPoint mp, int size)
        {
            try
            {
                MatrixPointType t = AssignType(mp, size);
                if (t == MatrixPointType.Slope || t == MatrixPointType.Ridge)
                {
                    double l1 = n1.Length;
                    double l2 = n2.Length;
                    Vector n = mp.Type == MatrixPointType.Slope ? n1 + n2 : n1 - n2;
                    double l = l1 > 0 && l2 > 0 ? n.Length * Math.Max(Math.Abs(Vector.CrossProduct(n1, n2)) / l1 / l2, 0.4) : n.Length;
                    return new Vector(n.Y / l, -n.X / l);
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            return new Vector();
        }
        MatrixPointType AssignType(MatrixPoint mp, int size)
        {
            if (mp.Type == MatrixPointType.Chain)
                return mp.Type;
            int im = mp.X - size;
            int ip = mp.X + size;
            int jm = mp.Y - size;
            int jp = mp.Y + size;
            if (im < 0 || ip >= src.Width || jm < 0 || jp >= src.Height)
                return mp.Type = MatrixPointType.Edge;
            float[] sa = new float[4];
            float d00 = matrix[mp.Y, mp.X];
            float dmm = matrix[jm, im];
            float d0m = matrix[jm, mp.X];
            float dpm = matrix[jm, ip];
            float dm0 = matrix[mp.Y, im];
            float dp0 = matrix[mp.Y, ip];
            float dmp = matrix[jp, im];
            float d0p = matrix[jp, mp.X];
            float dpp = matrix[jp, ip];
            sa[0] = dmm + dpp;
            sa[1] = dm0 + dp0;
            sa[2] = dmp + dpm;
            sa[3] = d0m + d0p;
            float max = 0;
            int maxIndex = -1;
            float nextMax = 0;
            int nextMaxIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (max < sa[i])
                {
                    nextMax = max;
                    nextMaxIndex = maxIndex;
                    max = sa[i];
                    maxIndex = i;
                }
                else if (nextMax < sa[i])
                {
                    nextMax = sa[i];
                    nextMaxIndex = i;
                }
            }
            if (maxIndex == -1) // all 0 -> flat
                mp.Type = MatrixPointType.NoDirection;
            if (nextMaxIndex == -1)
                nextMaxIndex = maxIndex;
            if (Math.Abs(maxIndex - nextMaxIndex) == 2) // unable to assign 
                return mp.Type=MatrixPointType.NoDirection;
            int caseIndex;
            float sd1, sxd1, syd1, sx1, sy1, sd2, sxd2, syd2, sx2, sy2;
            if ((maxIndex == 0 && nextMaxIndex == 3) || (maxIndex == 3 && nextMaxIndex == 0))
                caseIndex = 3;
            else
                caseIndex = (maxIndex + nextMaxIndex) / 2;
            switch (caseIndex)
            {
                case 0:
                    sd1 = d00 + dm0 + dmp + d0p + dpp;
                    sxd1 = dpp - dm0 - dmp;
                    syd1 = dmp + d0p + dpp;
                    sx1 = -1;
                    sy1 = 3;
                    sd2 = d00 + dp0 + dpm + d0m + dmm;
                    sxd2 = dp0 + dpm - dmm;
                    syd2 = -dmm - d0m - dpm;
                    sx2 = 1;
                    sy2 = -3;
                    break;
                case 1:
                    sd1 = d00 + dmp + d0p + dpp + dp0;
                    sxd1 = dpp + dp0 - dmp;
                    syd1 = dmp + d0p + dpp;
                    sx1 = 1;
                    sy1 = 3;
                    sd2 = d00 + dpm + d0m + dmm + dm0;
                    sxd2 = dpm - dmm - dm0;
                    syd2 = -dmm - d0m - dpm;
                    sx2 = -1;
                    sy2 = -3;
                    break;
                case 2:
                    sd1 = d00 + d0p + dpp + dp0 + dpm;
                    sxd1 = dpp + dp0 + dpm;
                    syd1 = d0p + dpp - dpm;
                    sx1 = 3;
                    sy1 = 1;
                    sd2 = d00 + d0m + dmm + dm0 + dmp;
                    sxd2 = -dmp - dm0 - dmm;
                    syd2 = dmp - dmm - d0m;
                    sx2 = -3;
                    sy2 = -1;
                    break;
                default:						// case 3
                    sd1 = d00 + dpp + dp0 + dpm + d0m;
                    sxd1 = dpp + dp0 + dpm;
                    syd1 = dpp - dpm - d0m;
                    sx1 = 3;
                    sy1 = -1;
                    sd2 = d00 + dmm + dm0 + dmp + d0p;
                    sxd2 = -dmm - dm0 - dmp;
                    syd2 = dmp + d0p - dmm;
                    sx2 = -3;
                    sy2 = 1;
                    break;
            }
            double v1 = 0.6 * sd1 - 0.2 * (sx1 * sxd1 + sy1 * syd1);
            n1 = (new Vector(sxd1 - sx1 * v1, syd1 - sy1 * v1)) / 3.0;
            double v2 = 0.6 * sd2 - 0.2 * (sx2 * sxd2 + sy2 * syd2);
            n2 = (new Vector(sxd2 - sx2 * v2, syd2 - sy2 * v2)) / 3.0;
            return mp.Type=n1 * n2 >= 0 ? MatrixPointType.Slope : MatrixPointType.Ridge;
        }
        public ByteMatrix Visualize(int compression)
        {
            ByteMatrix bm = new ByteMatrix(src.Height, src.Width);
            for (int i = 0; i < src.Height; i++)
                for (int j = 0; j < src.Width; j++)
                    bm.Data[i, j] = byte.MaxValue;
            foreach (MatrixPoint cp in pixels)
            {
                byte c = byte.MaxValue;
                switch (cp.Type)
                {
                    case MatrixPointType.Removed:
                        c = 128;
                        break;
                    case MatrixPointType.Unassigned:
                        c = 192;
                        break;
                    //case MatrixPointType.Unassigned:
                    //    c = 224;
                    //    break;
                    case MatrixPointType.Slope:
                        c = 160;
                        break;
                    case MatrixPointType.Edge:
                        c = 128;
                        break;
                    case MatrixPointType.Ridge:
                    case MatrixPointType.Chain:
                        c = 0;
                        break;
                }
                bm.Data[cp.Y, cp.X] = c;
            }
            foreach(ContourPoint[] chain in chains)
                foreach (ContourPoint cp in chain)
                    bm.Data[(int)(cp.X + 0.5), (int)(cp.Y + 0.5)] = 0;
            return bm.ExpandByInterpolation(compression);
        }
    }
}
