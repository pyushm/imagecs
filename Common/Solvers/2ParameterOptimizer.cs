using System;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

namespace Solvers
{
    public delegate double Function2(double p1, double p2);
    public struct OptimizationPoint
    {
        public const int Dimensions = 2;
        public static Function2 function;
        public double v { get; private set; }
        public double p1 { get; private set; }
        public double p2 { get; private set; }
        public double measure { get; set; } // content specific measure
        public Point p { get { return new Point(p1, p2); } }
        public OptimizationPoint(Point p, double extr) : this() { p1 = p.X; p2 = p.Y; v = extr; }
        public OptimizationPoint(Point p) : this() { p1 = p.X; p2 = p.Y; v = function(p1, p2); }
        public override string ToString() { return "p1=" + p1.ToString("f4") + " p2=" + p2.ToString("f4") + " v=" + v.ToString("f4"); }
    }
    public class QuadraticApproximation  // calculated quadratic approximation of fanction F(x,y) using 6 x,y points
    {
        public const int N = (OptimizationPoint.Dimensions + 1) * (OptimizationPoint.Dimensions + 2) / 2;
        double[] qc = new double[N];
        public QuadraticApproximation(List<OptimizationPoint> points)
        {
            if (points.Count < N)
                return;
            double[,] mat = new double[N, N];
            double[] rhs = new double[N];
            for (int i = 0; i < N; i++)
            {
                OptimizationPoint op = points[i];
                rhs[i] = op.v;
                mat[i, 0] = 1;
                mat[i, 1] = op.p1;
                mat[i, 2] = op.p2;
                mat[i, 3] = op.p1 * op.p1;
                mat[i, 4] = op.p1 * op.p2;
                mat[i, 5] = op.p2 * op.p2;
            }
            SquareMatrix sm = new SquareMatrix(mat);
            qc = sm.LinearSystemSolve(rhs);
            //string used = "used points: ";
            //for (int i = 0; i < N; i++)
            //    used += rhs[i].ToString("f2") + "=" + QuadraticValue(points[i].p).ToString("f2") + ", ";
            //Debug.WriteLine(used);
        }
        public double QuadraticValue(Point p) { return qc[0] + qc[1] * p.X + qc[2] * p.Y + qc[3] * p.X * p.X + qc[4] * p.X * p.Y + qc[5] * p.Y * p.Y; }
        public Vector Gradient(Point p) { return new Vector(qc[1] + qc[3] * p.X * 2 + qc[4] * p.Y, qc[2] + qc[4] * p.X + qc[5] * p.Y * 2); }
        public Vector NextStep(Point p) 
        {
            Vector g = Gradient(p);
            double t = 0.5 * g.LengthSquared / (qc[3] * g.X * g.X + qc[4] * g.X * g.Y + qc[5] * g.Y * g.Y);
            return g * t;
        }
        public OptimizationPoint CheckExtremum(bool searchMax)
        {   // short axis is rotated at anfle fi=atan2(qc[4], qc[5] - qc[3])/2
            double q4 = qc[4] * qc[4];
            double q35 = qc[3] + qc[5];
            double D = 4 * qc[3] * qc[5] - q4;
            bool max = q35 < 0;
            q35 = Math.Abs(q35);
            q4 = Math.Sqrt((qc[3] - qc[5]) * (qc[3] - qc[5]) + q4);
            double D2A = q35 + q4;
            double D2B = q35 - q4;
            Point ep = new Point((qc[2] * qc[4] - 2 * qc[1] * qc[5]) / D, (qc[1] * qc[4] - 2 * qc[2] * qc[3]) / D);
            OptimizationPoint qextremum = new OptimizationPoint(ep, QuadraticValue(ep));
            qextremum.measure = D < 0 ? 0 : max == searchMax ? 1 : -1;
            Debug.WriteLine("T=" + qextremum.measure.ToString("f0") + ' ' + qextremum.ToString() + " D=" + D.ToString("f2") + " D2A=" + D2A.ToString("f2") + " D2B=" + D2B.ToString("f2"));
            return qextremum;
        }
    }
    public class Optimizer2Parameters
    {
        static Function2 test = delegate(double x, double y) { return x * x * x * x - 2 * x + x * y - y * y * y + y * y * y * y; };
        public static OptimizationPoint Test(double p1, double p2, double dp, double delta)
        {
            Optimizer2Parameters op = new Optimizer2Parameters(p1, p2, dp, test, false);
            return op.FindExtremum(delta);
        }
        Comparison<OptimizationPoint> measureComparison = delegate(OptimizationPoint p1, OptimizationPoint p2)
        {
            double dif = p1.measure - p2.measure;
            return dif > 0 ? 1 : dif == 0 ? 0 : -1;
        };
        List<OptimizationPoint> searchPoints = new List<OptimizationPoint>();
        int extremumType;
        bool searchMax;
        OptimizationPoint extremum;
        Vector lastShift;
        bool MatchSearch(int t) { return t != 0 && t < 0 == searchMax; }
        public Optimizer2Parameters(double p1, double p2, double dp, Function2 f, bool max)
        {
            OptimizationPoint.function = f;
            searchMax = max;
            extremumType = max ? -1 : 1;
            extremum = new OptimizationPoint(new Point(), max ? double.MinValue : double.MaxValue);
            double v0 = AddOptimizationPoint(new Point(p1, p2)).v;
            double d1 = AddOptimizationPoint(new Point(p1 + dp, p2)).v - v0;
            double d2 = AddOptimizationPoint(new Point(p1, p2 + dp)).v - v0;
            lastShift = new Vector(d1 / dp, d2 / dp); // max slope direction
            lastShift = -dp * extremumType / lastShift.Length * lastShift;
            Point p = extremum.p + lastShift;
            AddOptimizationPoint(p);
            Vector n = new Vector(lastShift.Y, -lastShift.X);
            AddOptimizationPoint(p + n);
            AddOptimizationPoint(p - n);
        }
        OptimizationPoint AddOptimizationPoint(Point p)
        {
            OptimizationPoint op = new OptimizationPoint(p);
            searchPoints.Add(op);
            double dif = op.v - extremum.v;
            op.measure = dif == 0 ? 0 : dif > 0 == searchMax ? 1 : -1;
            Debug.WriteLine((op.measure > 0 ? " extremum " : "new point ") + op.ToString());
            if (op.measure > 0)
                extremum = op;
            return op;
        }
        double SortByDistanceToExtremum()
        {
            if (searchPoints.Count == 1)
                return 0;
            for (int i = 0; i < searchPoints.Count; i++)
            {
                OptimizationPoint op = searchPoints[i];
                op.measure = (searchPoints[i].p - extremum.p).Length;
                searchPoints[i] = op;
            }
            searchPoints.Sort(measureComparison);
            int np = Math.Min(QuadraticApproximation.N, searchPoints.Count);
            double d = 0;
            for (int i = 1; i < np; i++)
                d += searchPoints[i].measure;
            return d / (np - 1);
        }
        double MinDistance(Point p)
        {
            double md = double.MaxValue;
            foreach (var op in searchPoints)
            {
                double d = (p - op.p).Length;
                if (md > d)
                    md = d;
            }
            return md;
        }
        public OptimizationPoint FindExtremum(double delta)
        {
            Vector norm = new Vector(); // forced normal
            double dist = lastShift.Length;
            Vector move;
            int steps = 0;
            double totalDistance = 0;
            int successes = 0;
            QuadraticApproximation extremumApproximation;
            double maxGrow = 2.5;
            do
            {
                steps++;
                double d = SortByDistanceToExtremum(); // average distance of extremum from used points
                extremumApproximation = new QuadraticApproximation(searchPoints);
                if (norm.LengthSquared == 0)
                {
                    move = extremumApproximation.NextStep(extremum.p);
                    if (move.Length > maxGrow * d)
                        move = maxGrow * d / move.Length * move;
                }
                else
                {
                    move = norm;
                    norm = new Vector();
                }
                lastShift = -extremumType * move;
                Point p = extremum.p + lastShift;
                while (MinDistance(p) < d / 10) // points have not to be too close as copmared to average of most close 
                {
                    if (lastShift.Length < d / 5)
                        lastShift *= 1.2;   // grow
                    else
                        lastShift = new Vector(lastShift.X + lastShift.Y / 10, lastShift.Y - lastShift.X / 10); // rotate
                    p = extremum.p + lastShift;
                }
                dist = lastShift.Length;
                totalDistance += dist;
                OptimizationPoint added = AddOptimizationPoint(p);
                if (added.measure <= 0)
                    norm = new Vector(lastShift.Y / 2, -lastShift.X / 2);
                else
                    successes++;
            } while (dist > delta);
            //Debug.WriteLine(" steps=" + steps.ToString() + " successes=" + successes.ToString() + " totalDistance=" + totalDistance.ToString("f2"));
            extremumApproximation.CheckExtremum(searchMax);
            Debug.WriteLine("SOLUTION " + extremum.ToString());
            return extremum;
        }
    }
}