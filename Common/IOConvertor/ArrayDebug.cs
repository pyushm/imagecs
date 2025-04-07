using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;

namespace Presentation
{
    public static class ArrayDebug
    {   // functions allow to pass double[] variable name to output 
        // T item parameter has to be specified in all calls as: new { DBL_A }
        // where DBL_A is avariable of type double[]
        public static int First { get; set; } =0;
        public static int Last { get; set; } = int.MaxValue;
        public static string format { get; set; } = "\t{0,9:f5}";
        public static string ToString<T>(T item, double coef) where T : class
        {
            var param = typeof(T).GetProperties()[0];
            object o = param.GetValue(item, null);
            double[] da = o as double[];
            if (da == null)
                return param.Name + " not an array";
            int first = First < 0 ? First + da.Length : First;
            first = Math.Max(0, first);
            int last = Math.Min(da.Length, Last);
            string s = param.Name + '[' + first + '-' + last + "]";
            if (first >= last)
                return s;
            if (coef != 1)
                s += '*' + coef.ToString("g");
            StringBuilder sb = new StringBuilder();
            for (int i = first; i < last; i++)
                sb.AppendFormat(format, da[i] * coef);
            return s + sb.ToString();
        }
        public static string ToString<T>(T item) where T : class { return ToString<T>(item, 1); }
        public static string CheckRange<T>(T item, double min, double max, bool strict, bool checkLast) where T : class
        {
            var param = typeof(T).GetProperties()[0];
            object o = param.GetValue(item, null);
            double[] a = o as double[];
            if (a == null)
                return "";
            bool ale0 = false;
            bool agt1 = false;
            bool aNAN = false;
            int n = checkLast ? a.Length : a.Length - 1;
            for (int i = 0; i < n; i++)
            {
                if (double.IsNaN(a[i])) 
                    aNAN = true;
                else if (strict)
                {
                    if (a[i] <= min) ale0 = true;
                    if (a[i] >= max) agt1 = true;
                }
                else
                {
                    if (a[i] < min) ale0 = true;
                    if (a[i] > max) agt1 = true;
                }
            }
            string less = strict ? "<=min, " : "<min, ";
            string more = strict ? ">=max, " : ">max, ";
            return (ale0 ? param.Name + less : "") + (agt1 ? param.Name + more : "") + (aNAN ? param.Name + "NaN, " : "");
        }
        public static string CheckRangeStrict<T>(T item, double min, double max) where T : class { return CheckRange<T>(item, min, max, true, false); }
        public static string CheckRange<T>(T item, double min, double max) where T : class { return CheckRange<T>(item, min, max, false, false); }
        //public static double Integral0(double[] f) // values defined @ =dr*i
        //{
        //    int count = f.Length;
        //    double ft = (f[0] + (3 * count - 1) * f[count - 1]) / 6;
        //    for (int i = 1; i < count; i++)
        //        ft += f[i] * i;
        //    return ft * dr * dr;
        //}
        //public static double Integral05(double[] f) // values defined @ =dr*(i+0.5)
        //{
        //    int count = f.Length;
        //    double ft = 0;
        //    for (int i = 0; i < count; i++)
        //        ft += f[i] * (i + 0.5);
        //    return ft * dr * dr;
        //}
    }
}
