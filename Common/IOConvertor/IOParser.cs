using System;
using System.Collections.Generic;
using System.Windows;

namespace Presentation
{
    public enum InputResult
    {
        None,       // no change
        Changed,    // value changed
        OutOfRange, // value out of range
        NaN         // invalid input
    }
    public static class IOParser
    {   // handles in/out of the form 1.1,0.3;1.34,0.2 or 4;6;6.6;7
        static char pSplitter = ';';    // object separator
        static char cSplitter = ',';    // field separator in object
        public static bool FromString(string input, out InputResult result, ref double val, double min, double max) // valid input (None or Changed)
        {
            if (input == null)
            {
                result = InputResult.NaN;
                return false;       // not updated - no input
            }
            double oldval = val;
            try
            {
                if (input.Length == 0 || input == "-" || input == "." || input == "-.")
                    input = "0";
                val = double.Parse(input);
                if (Math.Abs(val - oldval) < (Math.Abs(val) + Math.Abs(oldval)) * 5.0e-12) 
                {
                    result = InputResult.None;
                    return true;    // updated - same value
                }
                if (val < min || val > max)
                {
                    val = oldval;
                    result = InputResult.OutOfRange;
                    return false;   // not updated - OutOfRange
                }
                result = InputResult.Changed;
                return true;        // updated - new value
            }
            catch
            {
                val = oldval;
                result = InputResult.NaN;
                return false;       // not updated - bad input
            }
        }
        public static bool FromString(string input, out InputResult result, ref InputResult cumulative, ref double val, double min, double max)
        {
            bool valid = FromString(input, out result, ref val, min, max);
            if (result == InputResult.Changed)
                cumulative = InputResult.Changed;
            return valid;
        }
        public static bool FromString(string input, out InputResult result, ref int val, int min, int max)
        {
            if (input == null || input.Length == 0)
            {
                result = InputResult.NaN;
                return false;       // not updated - no input
            }
            int oldval = val;
            try
            {
                val = int.Parse(input);
                if (val == oldval)
                {
                    result = InputResult.None;
                    return true;    // updated - same value
                }
                if (val < min || val > max)
                {
                    val = oldval;
                    result = InputResult.OutOfRange;
                    return false;
                }
                else
                {
                    result = InputResult.Changed;
                    return true;
                }
            }
            catch
            {
                val = oldval;
                result = InputResult.NaN;
                return false;
            }
        }
        public static bool FromString(string input, out InputResult result, ref InputResult cumulative, ref int val, int min, int max)
        {
            bool valid = FromString(input, out result, ref val, min, max);
            if (result == InputResult.Changed)
                cumulative = InputResult.Changed;
            return valid;
        }
        public static bool FromString(string str, out InputResult res, out Point point, double min, double max)
        {
            if (str == null || str.Length == 0)
            {
                point = new Point();
                res = InputResult.NaN;
                return false;
            }
            string[] sa = str.Split(cSplitter);
            double pr = 0;
            double pz = 0;
            InputResult res1;
            InputResult res2 = InputResult.NaN;
            FromString(sa[0], out res1, ref pr, min, max);
            if(sa.Length > 1)
                FromString(sa[1], out res2, ref pz, min, max);
            point=new Point(pr, pz);
            res = (InputResult)Math.Max((int)res1, (int)res2);
            return true;
        }
        public static bool FromString(string input, out InputResult result, out Point[] points, double min, double max)
        {
            if (input == null || input.Length == 0)
            {
                points = Array.Empty<Point>();
                result = InputResult.NaN;
                return false;
            }
            List<Point> ps = new List<Point>();
            string[] cpsa = input.Split(pSplitter);
            Point point;
            result = InputResult.None;
            foreach (string str in cpsa)
            {
                InputResult res;
                bool ok = FromString(str, out res, out point, min, max);
                result = (InputResult)Math.Max((int)res, (int)result);
                if (ok)
                    ps.Add(point);
            }
            points = ps.ToArray();
            return true;
        }
        public static string ToString(Point[] points, string format)
        {
            if (points == null)
                return "";
            string s = "";
            bool first = true;
            foreach (var p in points)
            {
                if (!first)
                    s += pSplitter;
                else
                    first = false;
                s += DisplayText(p.X, format) + cSplitter + DisplayText(p.Y, format);
            }
            return s;
        }
        public static bool FromString(string input, out InputResult result, out double[] list, double min, double max)
        {
            if (input == null || input.Length == 0)
            {
                list = Array.Empty<double>();
                result = InputResult.NaN;
                return false;
            }
            List<double> ps = new List<double>();
            string[] cpsa = input.Split(pSplitter);
            result = InputResult.None;
            double d=0;
            foreach (string str in cpsa)
            {
                InputResult res;
                bool ok = FromString(str, out res, ref d, min, max);
                result = (InputResult)Math.Max((int)res, (int)result);
                if (ok)
                    ps.Add(d);
            }
            list = ps.ToArray();
            return true;
        }
        public static bool FromString(string input, out InputResult result, out double[] values, double min, double max, int size, double def)
        {   // creates 'values[size]' anf fills it from 'input'; extra inputs ignored; extra 'values' filled with 'def'; returns false if parsing 'input' fails
            string[] cpsa = input == null || input.Length == 0 ? Array.Empty<string>() : input.Split(pSplitter);
            bool ret = true;
            values = new double[size];
            result = InputResult.None;
            for (int i = 0; i < cpsa.Length; i++)
            {
                double d = def;
                InputResult res;
                bool ok = FromString(cpsa[i], out res, ref d, min, max);
                result = (InputResult)Math.Max((int)res, (int)result);
                if (i < cpsa.Length && ok)
                    values[i] = d;
                else
                {
                    values[i] = def;
                    ret = false;
                }
                if (result < res)
                    result = res;
            }
            for (int i = cpsa.Length; i < size; i++)
                values[i] = def;
            return ret;
        }
        public static string ToString(double[] list, string format)
        {
            if (list == null)
                return "";
            string s = "";
            bool first = true;
            foreach (var p in list)
            {
                if (!first)
                    s += pSplitter;
                else
                    first = false;
                s += DisplayText(p, format);
            }
            return s;
        }
        public static bool FromString(string input, out string[] list)
        {
            if (input == null || input.Length == 0)
            {
                list = Array.Empty<string>();
                return false;
            }
            list = input.Split(pSplitter);
            return true;
        }
        public static string ToString(string[] list)
        {
            if (list == null)
                return "";
            string s = "";
            bool first = true;
            foreach (var p in list)
            {
                if (!first)
                    s += pSplitter;
                else
                    first = false;
                s += p;
            }
            return s;
        }
        public static void SetSplinePoints(double[] val, double[] src)
        {
            if (val == null || src == null || src.Length == 0)
                return;
            int n = src.Length;
            if (n == 1)
            {
                for (int i = 0; i < val.Length; i++)
                    val[i] = src[0];
                return;
            }
            for (int i = 0; i < val.Length; i++)
            {
                double t = (i + 0.5) / val.Length * (n - 1);
                int ind = (int)t;
                t = t - ind;
                double r = 1 - t;
                double G = r * t * (r - t);
                double lv = src[ind];                                           // value[ind] - left
                double rv = src[ind + 1];                                       // value[ind+1] - right
                double ld = ind == 0 ? 2 * (rv - lv) : rv - src[ind - 1];       // value[ind+1]-value[ind-1] - left difference
                double rd = ind == n - 2 ? 2 * (rv - lv) : src[ind + 2] - lv;   // value[ind+2]-value[ind] - right difference
                val[i] = r * lv + t * rv + G * (lv - rv) + G * (ld + rd) / 4 + r * t * (ld - rd) / 4;
            }
        }
        public static bool SetSplinePoints(double[] val, string text, double min, double max)
        {
            InputResult res;
            double[] vals;
            if (!IOParser.FromString(text, out res, out vals, min, max))
                return false;
            IOParser.SetSplinePoints(val, vals);
            return true;
        }
        public static void SetVortexPoints(double[] val, double[] src)
        {
            if (val == null || src == null || src.Length == 0)
                return;
            int start = 0;
            for (int j = 1; j < src.Length; j++)
            {
                int end = j * val.Length / (src.Length - 1);
                for (int i = start; i < end; i++)
                    val[i] = src[j - 1] + (src[j] - src[j - 1]) * (i - start) / (end - start);
                start = end;
            }
        }
        static string DisplayText(double val, string format)
        {
            string s = val.ToString(format);
            int i=s.Length-1;
            for(; i>=0; i--)
                if(s[i]!='0')
                    break;
            return i == s.Length - 1 ? s : i < 0 ? "0" : s.Remove(i + 1);
        }
    }
}
