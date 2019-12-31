using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace ImageWindows
{
    public enum MaxType
    {
        Mid,
        Range,
        Right,
        Left,
        NoMax,
        Edge
    }
    public class ParabolicInterpolation
    {   // parabolic approximation: finds a, b, c minimizing SUM[(a+b*x+c*x*x - y)**2]
        double a;                   // a coefficient of approximation
        double b;                   // b coefficient of approximation
        double c;                   // c coefficient of approximation
        double xMin;                // minimum of data position
        double xMax;                // maximum of data position
        MaxType maxType;
        double maxLocation;
        public MaxType MaxType      { get { return maxType; } }
        public double MaxValue      { get { return c < 0 ? a + (b + c * maxLocation) * maxLocation : 0; } }
        public double MaxLocation   { get { return maxLocation; } }
        public bool InRange         { get { return maxType == MaxType.Mid || maxType == MaxType.Range; } }
        public bool Mid             { get { return maxType == MaxType.Mid; } }
        public double SlopeAt(double x) { return b + 2 * c * x; }
        public ParabolicInterpolation(MaxType t) { maxType = t; }
        public ParabolicInterpolation(Vector[] data, double cut)
        {
            maxType = MaxType.NoMax;
            double s0 = data.Length;
            double s1 = 0;
            double s2 = 0;
            double s3 = 0;
            double s4 = 0;
            double[] rhs = new double[3];
            for (int i = 0; i < 3; i++)
                rhs[i] = 0;
            xMin = double.MaxValue;
            xMax = double.MinValue;
            for (int i = 0; i < s0; i++)
            {
                double s = data[i].X;
                if (xMin > s)
                    xMin = s;
                if (xMax < s)
                    xMax = s;
                double v = data[i].Y;
                s1 += s;
                s2 += s * s;
                s3 += s * s * s;
                s4 += s * s * s * s;
                rhs[0] += v;
                rhs[1] += v * s;
                rhs[2] += v * s * s;
            }
            double[,] sMatrix = new double[3, 3];
            sMatrix[0, 0] = s0;
            sMatrix[1, 0] = sMatrix[0, 1] = s1;
            sMatrix[2, 0] = sMatrix[1, 1] = sMatrix[0, 2] = s2;
            sMatrix[2, 1] = sMatrix[1, 2] = s3;
            sMatrix[2, 2] = s4;
            double d = Determinant3(sMatrix);
            c = d == 0 ? 1 : Determinant3(sMatrix, rhs, 2) / d;
            if (c < Math.Min(0, rhs[0] * cut / s0))
            {
                a = Determinant3(sMatrix, rhs, 0) / d;
                b = Determinant3(sMatrix, rhs, 1) / d;
                maxLocation = -0.5 * b / c;
                if (maxLocation < xMin)
                {
                    maxType = MaxType.Left;
                    maxLocation = xMin;
                }
                else if (maxLocation > xMax)
                {
                    maxType = MaxType.Right;
                    maxLocation = xMax;
                }
                else if (Math.Abs(maxLocation - (xMax + xMin) / 2) < (xMax - xMin) / 4)
                    maxType = MaxType.Mid;
                else
                    maxType = MaxType.Range;
            }
        }
        double Determinant3(double[,] lhs, double[] rhs, int index)
        {
            double[,] m = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    m[i, j] = j == index ? rhs[i] : lhs[i, j];
            }
            return Determinant3(m);
        }
        double Determinant3(double[,] m)
        {
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) - m[1, 0] * (m[0, 1] * m[2, 2] - m[0, 2] * m[2, 1]) + m[2, 0] * (m[0, 1] * m[1, 2] - m[0, 2] * m[1, 1]);
        }
    }
}
