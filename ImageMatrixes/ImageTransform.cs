using System;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace ImageProcessor
{
    public struct IntSize
    {
        public IntSize(int w, int h) { Width = w; Height = h; }
        public Size Size { get { return new Size(Width, Height); } }
        public IntSize XYmirrored { get { return new IntSize(Height, Width); } }
        public double Average { get { return (Width + Height) / 2.0; } }
        public double WtoH { get { return (double)Width / Height; } }
        public int Width;
        public int Height;
        public override string ToString() { return Width.ToString() + 'x' + Height; }
    }
    public class InterpolationFunction
    {  // points array is a sequence of argument-value pairs (arguments have to be in assending order)
        float[] points;
        float minValue = float.MinValue;
        float maxValue = float.MaxValue;
        int np;
        public bool IsLinear
        {
            get
            {
                for (int i = 0; i < points.Length; i = i + 2)
                    if (points[i] != points[i + 1])
                        return false;
                return true;
            }
        }
        public bool IsNullValue
        {
            get
            {
                for (int i = 0; i < points.Length; i = i + 2)
                    if (Math.Abs(points[i + 1])>0.01f)
                        return false;
                return true;
            }
        }
        public float[] Points { get { return points; } }
        public InterpolationFunction(float[] points_) { points = points_; Validate(); }
        public InterpolationFunction(float[] points_, float minValue_, float maxValue_) { points = points_; minValue = minValue_; maxValue = maxValue_; Validate(); }
        public float Apply(float arg)           
        {
            float res = points[1];
            if (np > 1)
            {
                if (arg >= points[2 * np - 2])
                    res = points[2 * np - 3] + (arg - points[2 * np - 4]) * (points[2 * np - 1] - points[2 * np - 3]) / (points[2 * np - 2] - points[2 * np - 4]);
                //res = points[2 * np - 1];
                else
                {
                    //float prevX = float.MinValue;
                    //float prevY = res;
                    float prevX = points[3];
                    float prevY = points[2];
                    for (int i = 0; i < np; i++)
                    {
                        float x = points[2 * i];
                        float y = points[2 * i + 1];
                        if (arg < x || i == np-1)
                        {
                            res = prevY + (arg - prevX) * (y - prevY) / (x - prevX);
                            break;
                        }
                        prevX = x;
                        prevY = y;
                    }
                }
            }
            return Math.Max(minValue, Math.Min(maxValue, res));
        }
        void Validate()                         
        {
            if (points == null || points.Length == 0)
                throw new Exception("Interpolation points not defined");
            np = points.Length / 2;
            if (np * 2 != points.Length)
                throw new Exception("Last interpolation points does not have value");
            float prev = float.MinValue;
            for (int i = 0; i < np; i++)
            {
                if (points[2 * i] < prev)
                    throw new Exception("Arguments of interpolation points are not in assending order");
                prev = points[2 * i];
            }
        }
        public InterpolationFunction Clone()
        {
            return new InterpolationFunction((float[])points.Clone(), minValue, maxValue);
        }
        public override string ToString()
        {
            string str = "";
            for (int i = 0; i < np; i++)
            {
                str += points[2 * i].ToString("f2") + "->" + points[2 * i+1].ToString("f2");
                if (i < np-1)
                str += ',';
            }
            return str;
        }
        public bool Equals(InterpolationFunction other)
        {
            if (other==null || np != other.np)
                return false;
            for (int i = 0; i < points.Length; i++)
                if (points[i] != other.points[i])
                    return false;
            return true;
        }
    }
    public class ColorTransform                 
    {
        static public Color unsetColor = Color.FromArgb(0, 0, 0, 0);
        static float saturationNormCoef = 3;
        static public Color ColorNull { get { return unsetColor; } }
        static public ColorTransform BWTransform
        {
            get
            {
                ColorTransform BWTransform = new ColorTransform();
                BWTransform.sat = 0;
                return BWTransform;
            }
        }
        InterpolationFunction brightnessAdjustment;
        InterpolationFunction transparencyAdjustment;
        float rCoeff;	            // red brightness adjustment
        float gCoeff;	            // green brightness adjustment
        float bCoeff;	            // blue brightness adjustment
        float sat;                  // saturation adjustment
        Color transparentColor;     // specifies color set as transparent
        Color matchPattern;         // specifies color relative to which transform is applied 
        public InterpolationFunction BrightnessAdjustment { get { return brightnessAdjustment; } }
        public InterpolationFunction TransparencyAdjustment { get { return transparencyAdjustment; } }
        public Color TransparentColor { get { return transparentColor; } set { transparentColor = value; } }
        public Color Pattern { get { return matchPattern; } set { matchPattern = value; } }
        public float RCoef { get { return rCoeff; } }
        public float GCoef { get { return gCoeff; } }
        public float BCoef { get { return bCoeff; } }
        public float Sat { get { return sat; } }
        public bool IsIdentical { get { return rCoeff == 1 && gCoeff == 1 && bCoeff == 1 && sat == 1 && 
                    brightnessAdjustment.IsLinear && transparencyAdjustment.IsNullValue && !IsColorSet; } }
        public bool IsColorSet { get { return transparentColor != unsetColor; } }
        public ColorTransform() { Set(); }
        public ColorTransform(Color color) { Set(); transparentColor = color; }
        public void CopyFrom(ColorTransform src)
        {
            rCoeff = src.rCoeff;
            bCoeff = src.bCoeff;
            gCoeff = src.gCoeff;
            sat = src.sat;
            brightnessAdjustment = src.brightnessAdjustment.Clone();
            transparencyAdjustment = src.transparencyAdjustment.Clone();
            transparentColor = src.transparentColor;
            matchPattern = src.matchPattern;
        }
        public bool Set()
        {
            rCoeff = gCoeff = bCoeff = sat = 1;
            SetBrightnessValues(new float[] { 0, 0, 0.5f, 0, 1, 0 });
            SetTransparencyValues(new float[] { 0, 0, 1, 0 });
            transparentColor = unsetColor;
            matchPattern = unsetColor;
            return true;
        }
        public bool Set(float[] brightnessValues)
        {
            return SetBrightnessValues(brightnessValues);
        }
        public bool Set(float[] brightnessValues, float[] saturationValues)
        {
            bool changed = SetBrightnessValues(brightnessValues);
            return SetColorValues(saturationValues) || changed;
        }
        public bool Set(float[] brightnessValues, float[] saturationValues, float[] singleSlopeValue)
        {
            transparentColor = unsetColor;
            matchPattern = unsetColor;
            bool changed = SetBrightnessValues(brightnessValues);
            changed = SetColorValues(saturationValues) || changed;
            return SetTransparencyValues(singleSlopeValue) || changed;
        }
        bool SetBrightnessValues(float[] brightnessValues)
        {
            float[] ba = new float[brightnessValues.Length];
            for (int i = 0; i < ba.Length; i = i + 2)
            {
                ba[i] = brightnessValues[i];
                ba[i + 1] = ba[i] + brightnessValues[i + 1];
            }
            InterpolationFunction newAdjustment = new InterpolationFunction(ba, 0, 1);
            bool same = newAdjustment.Equals(brightnessAdjustment);
            brightnessAdjustment = newAdjustment;
            return !same;
        }
        public float[] BrightnessValues
        {
            get
            {
                float[] fa = new float[brightnessAdjustment.Points.Length];
                for (int i = 0; i < fa.Length; i = i + 2)
                {
                    fa[i] = brightnessAdjustment.Points[i];
                    fa[i + 1] = brightnessAdjustment.Points[i + 1] - fa[i];
                }
                return fa;
            }
        }
        public float OldBrightness { get { return BrightnessValues[2]; } }
        public float NewBrightness { get { return BrightnessValues[3] + BrightnessValues[2]; } }
        public double DarkCoef { get { return (BrightnessValues[3] - BrightnessValues[1]) / (BrightnessValues[2] - BrightnessValues[0] + 0.0001) + 1; } }
        public double BrightCoef { get { return (BrightnessValues[5] - BrightnessValues[3]) / (BrightnessValues[4] - BrightnessValues[2] + 0.0001) + 1; } }
        bool SetColorValues(float[] saturationValues)
        {
            Debug.Assert(saturationValues.Length == 3);
            float nsat = (saturationValues[0] + saturationValues[1] + saturationValues[2]) / 3;
            float nrCoeff = Math.Max(0, (saturationValues[0] - nsat) / saturationNormCoef + 1);
            float ngCoeff = Math.Max(0, (saturationValues[1] - nsat) / saturationNormCoef + 1);
            float nbCoeff = Math.Max(0, (saturationValues[2] - nsat) / saturationNormCoef + 1);
            nsat++;
            bool changed = sat != nsat || rCoeff != nrCoeff || gCoeff != ngCoeff || bCoeff != nbCoeff;
            sat = nsat;
            rCoeff = nrCoeff;
            gCoeff = ngCoeff;
            bCoeff = nbCoeff;
            return changed;
        }
        public float[] ColorValues
        {
            get
            {
                float[] fa = new float[3];
                fa[0] = saturationNormCoef * (rCoeff - 1) + sat - 1;
                fa[1] = saturationNormCoef * (gCoeff - 1) + sat - 1;
                fa[2] = saturationNormCoef * (bCoeff - 1) + sat - 1;
                return fa;
            }
        }
        bool SetTransparencyValues(float[] transparency)
        {
            float[] ba = new float[transparency.Length];
            for (int i = 0; i < ba.Length; i = i + 2)
            {
                ba[i] = transparency[i];
                ba[i + 1] = transparency[i + 1];
            }
            InterpolationFunction newAdjustment = new InterpolationFunction(ba, 0, 1);
            bool same = newAdjustment.Equals(transparencyAdjustment);
            transparencyAdjustment = newAdjustment;
            return !same;
        }
        public float[] TransparencyValues
        {
            get
            {
                float[] fa = new float[transparencyAdjustment.Points.Length];
                for (int i = 0; i < fa.Length; i = i + 2)
                {
                    fa[i] = transparencyAdjustment.Points[i];
                    fa[i + 1] = transparencyAdjustment.Points[i + 1] ;
                }
                return fa;
            }
        }
        public double Opacity { get { return 1 - TransparencyValues[1]; } }
        public double OpacitySlope { get { return (TransparencyValues[3] - TransparencyValues[1]) / (TransparencyValues[2] - TransparencyValues[0] + 0.0001); } }
        public byte Apply(byte p) { return (byte)(brightnessAdjustment.Apply((float)p / byte.MaxValue) * byte.MaxValue); }
        public void Apply(ref byte a, ref byte r, ref byte g, ref byte b)
        {
            if (a == 0 || (IsColorSet && transparentColor.R == r && transparentColor.G == g && transparentColor.B == b))
            {
                a = r = g = b = 0;
                return;
            }
            if (a != byte.MaxValue)
            {
                r = (byte)Math.Min(byte.MaxValue, r * byte.MaxValue / a);
                g = (byte)Math.Min(byte.MaxValue, g * byte.MaxValue / a);
                b = (byte)Math.Min(byte.MaxValue, b * byte.MaxValue / a);
            }
            int lbr = Math.Max(Math.Max(r, g), b);
            int l = lbr;
            if (matchPattern.A>0)
            {
                r -= matchPattern.R;
                g -= matchPattern.G;
                b -= matchPattern.B;
                l = Math.Max(Math.Max(r, g), b);
            }
            float br = lbr / (float)byte.MaxValue + 0.0001f;
            float brcoef = brightnessAdjustment.Apply(br)/br;
            r = (byte)Math.Min(byte.MaxValue, Math.Max(0, (matchPattern.R + (l - (l - r) * sat) * brcoef) * rCoeff));
            g = (byte)Math.Min(byte.MaxValue, Math.Max(0, (matchPattern.G + (l - (l - g) * sat) * brcoef) * gCoeff));
            b = (byte)Math.Min(byte.MaxValue, Math.Max(0, (matchPattern.B + (l - (l - b) * sat) * brcoef) * bCoeff));
            float acoef = 1 - Math.Min(1, Math.Max(0, transparencyAdjustment.Apply(br)));
            a = (byte)(a * acoef);
            acoef = a / (float)byte.MaxValue;
            r = (byte)(r * acoef);
            g = (byte)(g * acoef);
            b = (byte)(b * acoef);
        }
        public override string ToString()
        {
            string str = "r=" + rCoeff.ToString("f2") + " g=" + gCoeff.ToString("f2") + " b=" + bCoeff.ToString("f2") + " sat=" + sat.ToString("f2")+
                " brightness=" +brightnessAdjustment.ToString() + " transparency=" + transparencyAdjustment.ToString() + ':' + transparentColor.ToString();
            return str;
        }
    }
}
