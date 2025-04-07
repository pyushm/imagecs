using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.ComponentModel;

namespace Presentation
{
    public class Range
    {
        static int[] va = { 0, 10, 12, 15, 20, 25, 30, 40, 50, 60, 70, 80, 100 };
        public double Min { get; internal set; }
        public double Max { get; internal set; }
        public double Length => Max - Min;
        public bool IsValid => Length > 0;
        public double TickStep { get; private set; }
        public double FirstTick => Math.Ceiling(Min / TickStep) * TickStep;
        public override string ToString() => string.Format("[{0} : {1}]", Min, Max);
        public bool IsInRange(double v) => Min <= v && v <= Max;
        public Range() { Min = double.MaxValue; Max = double.MinValue; }
        public Range(bool round, Array arr) { SetMinMax(arr); Initialize(round, Min, Max); }
        public Range(bool round, double min, double max, bool include0 = true) => Initialize(round, include0 && min > 0 ? 0 : min, include0 && max < 0 ? 0 : max);
        public Range(bool round, Range src) => Initialize(round, src.Min, src.Max);
        public Range(double min, double max, bool include0 = true) => Initialize(false, include0 && min > 0 ? 0 : min, include0 && max < 0 ? 0 : max);
        void Initialize(bool round, double min, double max)
        {
            Min = min;
            Max = max;
            if (round)
                Round();
            if (!IsValid)
                return;
            var Scale10 = GetScale10();
            var l = Length / Scale10;
            TickStep = l < 20 ? 1 : l < 40 ? 2 : 5;
            TickStep *= Scale10;
        }
        public double GetScale10()
        {   // power of 10 normalizing Length to [10 : 100] - may change after rounding
            int e = 100;
            e = (int)(Math.Log10(Length) + e) - e - 1;
            return Math.Pow(10, e);
        }  
        bool SetMinMax(Array arr)
        {
            Min = double.MaxValue;
            Max = double.MinValue;
            if (arr == null)
                return false;
            try
            {
                foreach (object o in arr)
                {
                    double v = Convert.ToDouble(o);
                    if (Min > v)
                        Min = v;
                    if (Max < v)
                        Max = v;
                }
            }
            catch { }
            return IsValid;
        }
        int roundUp(double v)
        {
            int i = 0;
            for (; i < va.Length; i++)
                if (v <= va[i])
                    break;
            return va[i];
        }
        int roundDown(double v)
        {
            int i = va.Length - 1;
            for (; i >=0 ; i--)
                if (v >= va[i])
                    break;
            return va[i];
        }
        void Round()
        {
            bool bothPos = Min > 0;  // all >0
            bool bothNeg = Max < 0;  // all <0
            var Scale10 = GetScale10();
            var nc = Min / Scale10;
            var xc = Max / Scale10;
            if(bothNeg)
            {   // converted to bothPos
                nc = -xc;
                xc = -nc;
            }
            int n = (int)Math.Floor(nc);
            int x = (int)Math.Ceiling(xc);
            n = n < 0 ? -roundUp(-n) : roundDown(n);
            if (bothPos)
            {
                if (x - n > 100)
                    x = n + 110;
                else
                    x = n + (x - n > 100 ? 110 : roundUp(x - n));
            }
            else
                x = roundUp(x);
            if (bothNeg)
            {
                var t = -x;
                x = -n;
                n = t;
            }
            var l = x - n;
            TickStep = l < 20 ? 1 : l < 40 ? 2 : 5;
            TickStep *= Scale10;
            Min = Scale10 * n;
            Max = Scale10 * x;
        }
    }
}
