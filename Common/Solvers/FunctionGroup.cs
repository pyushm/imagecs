using System;
using System.Reflection;
using System.Diagnostics;

namespace Solvers
{
    public static class Fnc
    {
        public delegate double InitialValue(); // values of constants or initial values of variables
        public delegate void ArraySet(double[] values);  // sets double[] values
        public delegate void IntegralSetter(Integral fnc);  // sets Integral function
        public delegate double Assignment(double t, double[] expressions, double[] variables);  // RHS of expressions or equations
        public delegate double Integrand(double b, double t, double[] expressions, double[] variables); // integrands
        public delegate double Integral(Integrand fun, double t, double[] expressions, double[] variables, double from, double to);
        public static double DefaultIntegral(Integrand fun, double t, double[] expressions, double[] variables, double from, double to)
        {
            int n = 100;
            double db = (to - from) / n;
            double b = from;
            double s = fun(b, t, expressions, variables) / 2;
            for (int i = 1; i < n; i++)
            {
                b += db;
                s += fun(b, t, expressions, variables);
            }
            s += fun(to, t, expressions, variables) / 2;
            return s * db;
        }
    }
}