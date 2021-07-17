using System;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace ImageProcessor
{
    public struct LabPixel
    {
        float l, alpha, beta; // color components
        public double Sat { get { return Math.Sqrt(alpha * alpha + beta * beta); } }
        // the following functions are based off of the pseudocode found on www.easyrgb.com
        void ToRGB(out byte r, out byte g, out byte b)
        {
            var y = (l + 16) / 116;
            var x = alpha / 500 + y;
            var z = y - beta / 200;

            x = 0.95047f * ((x * x * x > 0.008856f) ? x * x * x : (x - 16 / 116) / 7.787f);
            y = 1.00000f * ((y * y * y > 0.008856f) ? y * y * y : (y - 16 / 116) / 7.787f);
            z = 1.08883f * ((z * z * z > 0.008856f) ? z * z * z : (z - 16 / 116) / 7.787f);

            var dr = x * 3.2406 + y * -1.5372 + z * -0.4986;
            var dg = x * -0.9689 + y * 1.8758 + z * 0.0415;
            var db = x * 0.0557 + y * -0.2040 + z * 1.0570;

            dr = (dr > 0.0031308) ? (1.055 * Math.Pow(dr, 1 / 2.4) - 0.055) : 12.92 * dr;
            dg = (dg > 0.0031308) ? (1.055 * Math.Pow(dg, 1 / 2.4) - 0.055) : 12.92 * dg;
            db = (db > 0.0031308) ? (1.055 * Math.Pow(db, 1 / 2.4) - 0.055) : 12.92 * db;

            r = (byte)(Math.Max(0, Math.Min(1, dr)) * 255);
            g = (byte)(Math.Max(0, Math.Min(1, dg)) * 255);
            b = (byte)(Math.Max(0, Math.Min(1, db)) * 255);
        }
        public LabPixel(byte br, byte bg, byte bb)
        {
            var r = br / 255.0;
            var g = bg / 255.0;
            var b = bb / 255.0;

            r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
            g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
            b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;

            var x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047;
            var y = (r * 0.2126 + g * 0.7152 + b * 0.0722) / 1.00000;
            var z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883;

            x = (x > 0.008856) ? Math.Pow(x, 1 / 3) : (7.787 * x) + 16 / 116;
            y = (y > 0.008856) ? Math.Pow(y, 1 / 3) : (7.787 * y) + 16 / 116;
            z = (z > 0.008856) ? Math.Pow(z, 1 / 3) : (7.787 * z) + 16 / 116;

            l = (float)((116 * y) - 16);
            alpha = (float)(500 * (x - y));
            beta = (float)(200 * (y - z));
        }
        // calculate the perceptual distance between colors in CIELAB
        // https://github.com/THEjoezack/ColorMine/blob/master/ColorMine/ColorSpaces/Comparisons/Cie94Comparison.cs
        public double Diff(LabPixel labA, LabPixel labB)
        {
            var deltaL = labA.l - labB.l;
            var deltaA = labA.alpha - labB.alpha;
            var deltaB = labA.beta - labB.beta;
            var sa = labA.Sat;
            var sb = labB.Sat;
            var deltaC = sa - sb;
            var deltaH = deltaA * deltaA + deltaB * deltaB - deltaC * deltaC;
            deltaH = deltaH < 0 ? 0 : Math.Sqrt(deltaH);
            var sc = 1.0 + 0.045 * sa;
            var sh = 1.0 + 0.015 * sa;
            var deltaLKlsl = deltaL / (1.0);
            var deltaCkcsc = deltaC / (sc);
            var deltaHkhsh = deltaH / (sh);
            var d = deltaLKlsl * deltaLKlsl + deltaCkcsc * deltaCkcsc + deltaHkhsh * deltaHkhsh;
            return d < 0 ? 0 : Math.Sqrt(d);
        }
    }
    //public struct PixelLSH
    //{
    //    public static float Wl = 0.5f;
    //    public static float Ws = 0.3f;
    //    public static float Wh = 1f;
    //    private float l;                         // luminocity [0, 1]
    //    private float s;                         // saturation [0, l]
    //    private float h;                         // hue [0, 1]
    //    private float a;                         // opacity [0, 1]
    //    public static float HueDif(float h1, float h2)
    //    {
    //        float d = Math.Abs(h1 - h2);
    //        return Math.Min(d, 1-d);
    //    }
    //    public static Color FromColor(Color c) { PixelLSH p = new PixelLSH(c); return p.ShaderLSH; }
    //    public PixelLSH(Color c)
    //    {   // have to be same as in shader code
    //        float d = Math.Min(Math.Min(c.R, c.G), c.B);
    //        if (c.R == c.G && c.R == c.B) { l = c.R; s = 0; h = 0; }
    //        else if (c.B > c.G && c.B > c.R)
    //            { l = c.B; s = l - d; h = 1 + (c.G - c.R) / s; }
    //        else if (c.G > c.B && c.G > c.R)
    //            { l = c.G; s = l - d; h = 3 + (c.R - c.B) / s; }
    //        else
    //            { l = c.R; s = l - d; h = 5 + (c.B - c.G) / s; }
    //        l /= Byte.MaxValue;
    //        s /= Byte.MaxValue;
    //        h = h / 6;
    //        a = c.A / Byte.MaxValue;
    //    }
    //    Color ShaderLSH { get { return Color.FromScRgb(a, l, s, h); } }
    //    float Dif(PixelLSH p) { return Wl * Math.Abs(l - p.l) + Ws * Math.Abs(s - p.s) + Wh * HueDif(l, p.l); }
    //    float Dif(Color c) { PixelLSH p = new PixelLSH(c); return Wl * Math.Abs(l - p.l) + Ws * Math.Abs(s - p.s) + Wh * HueDif(l, p.l); }
    //}

    //public struct PixelLSH
    //{                         // based on http://www.wischik.com/lu/programmer/1bpp.html
    //    static byte HLSmax = 252;
    //    static int HLSmaxD3 = HLSmax / 3;
    //    static int HLSmaxD3M2 = 2 * HLSmax / 3;
    //    static int HLSmaxD6 = HLSmax / 6;
    //    static int HLSmaxD12 = HLSmax / 12;
    //    private byte b;                         // blue [0, byte.MaxValue]
    //    private byte g;                         // green [0, byte.MaxValue]
    //    private byte r;                         // red [0, byte.MaxValue]
    //    private byte a;                         // transparency [0, byte.MaxValue]
    //    public byte R { get { return r; } }
    //    public byte G { get { return g; } }
    //    public byte B { get { return b; } }
    //    private byte l;						    // luminocity [0, byte.MaxValue]
    //    private byte s;						    // saturation [0, lum]
    //    private byte h;                         // hue [0, HLSmax]
    //    private byte side;                      // color side: 0 - l,s,h not defined; 1-6 - all defined; 7 - r,g,b not defined
    //    public float Brightness { get { return (float)l / byte.MaxValue; } }    // [0-1]
    //    public float Saturation { get { if (l == 0) return 0; return (float)s / byte.MaxValue; } }  // [0-Brightness]
    //    public float Tint { get { return (float)h / HLSmax; } } // [0-1]
    //    public byte Lum { get { return l; } }
    //    public byte Sat { get { return s; } }
    //    public byte Hue { get { return h; } }
    //    public bool IsRGBDefined { get { return side < 7; } }
    //    public bool IsLSHDefined { get { return side > 0; } }
    //    public PixelLSH(byte r_, byte g_, byte b_)
    //    {
    //        r = r_;
    //        g = g_;
    //        b = b_;
    //        a = byte.MaxValue;
    //        l = s = h = 0;
    //        side = 1;
    //    }
    //    public PixelLSH(float l_, float s_, float h_)
    //    {
    //        r = g = b = 0;
    //        a = byte.MaxValue;
    //        side = 7;
    //        if (l_ < 0)
    //            l = 0;
    //        else if (l_ >= 1)
    //            l = byte.MaxValue;
    //        else
    //            l = (byte)(l_ * byte.MaxValue);
    //        if (s_ < 0)
    //            s = 0;
    //        else if (s_ >= l_)
    //            s = l;
    //        else
    //            s = (byte)(s_ * byte.MaxValue);
    //        h_ = (float)(h_ - Math.Floor(h_));
    //        h = (byte)(h_ * HLSmax);
    //    }
    //    public int HueDifference(PixelLSH bsh)
    //    {   // transforms hue difference to (0-1) interval
    //        int diff = Math.Abs(bsh.h - h);
    //        if (diff > HLSmax / 2)
    //            diff = HLSmax - diff;
    //        int c = Math.Min(bsh.s, s);
    //        return c * diff / HLSmax;
    //    }
    //    public void SetLSH()
    //    {
    //        if (IsLSHDefined)
    //            return;
    //        if (r == g && r == b)
    //        {
    //            l = r;
    //            s = 0;
    //            h = 0;
    //            side = 1;
    //        }
    //        else if (b < g)
    //        {
    //            if (r < b)          // 2	r<b<g
    //            {
    //                l = g;
    //                s = (byte)(l - r);
    //                h = (byte)(HLSmaxD3 - HLSmaxD6 * (b - r) / s);      // [HLSmaxD6, 2*HLSmaxD6]
    //                side = 2;
    //            }
    //            else if (r < g) // 3	b<=r<g
    //            {
    //                l = g;
    //                s = (byte)(l - b);
    //                h = (byte)(HLSmaxD3 + (HLSmaxD6 * (r - b) + s / 2) / s);// [2*HLSmaxD6, 3*HLSmaxD6]
    //                side = 3;
    //            }
    //            else            // 4	b<g<=r
    //            {
    //                l = r;
    //                s = (byte)(l - b);
    //                h = (byte)(HLSmaxD3M2 - (HLSmaxD6 * (g - b) + s / 2) / s);// [3*HLSmaxD6, 4*HLSmaxD6]
    //                side = 4;
    //            }
    //        }
    //        else
    //        {
    //            if (r > b)          // 5	g<=b<r
    //            {
    //                l = r;
    //                s = (byte)(l - g);
    //                h = (byte)(HLSmaxD3M2 + (HLSmaxD6 * (b - g) + s / 2) / s);// [4*HLSmaxD6, 5*HLSmaxD6]
    //                side = 5;
    //            }
    //            else if (r > g) // 6	g<r<=b
    //            {
    //                l = b;
    //                s = (byte)(l - g);
    //                h = (byte)(HLSmax - (HLSmaxD6 * (r - g) + s / 2) / s);  // [5*HLSmaxD6, 6*HLSmaxD6]
    //                side = 6;
    //            }
    //            else            // 1	r<=g<=b
    //            {
    //                l = b;
    //                s = (byte)(l - r);
    //                h = (byte)((HLSmaxD6 * (g - r) + s / 2) / s);           // [0, HLSmaxD6]
    //                side = 1;
    //            }
    //        }
    //    }
    //    public void SetRGB()
    //    {
    //        if (IsRGBDefined)
    //            return;
    //        byte iMax, iMin;
    //        iMax = l;
    //        iMin = (byte)(l - s);
    //        int side = h / HLSmaxD6 + 1;
    //        switch (side)
    //        {
    //            case 1:         // 1	r<g<b
    //                b = iMax;
    //                r = iMin;
    //                g = (byte)(r + (h * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //            case 2:         // 2	r<b<g
    //                g = iMax;
    //                r = iMin;
    //                b = (byte)(r + ((HLSmaxD3 - h) * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //            case 3:         // 3	b<r<g
    //                g = iMax;
    //                b = iMin;
    //                r = (byte)(b + ((h - HLSmaxD3) * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //            case 4:         // 4	b<g<r
    //                r = iMax;
    //                b = iMin;
    //                g = (byte)(b + ((HLSmaxD3M2 - h) * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //            case 5:         // 5	g<b<r
    //                r = iMax;
    //                g = iMin;
    //                b = (byte)(g + ((h - HLSmaxD3M2) * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //            default:        // 6	g<r<b
    //                b = iMax;
    //                g = iMin;
    //                r = (byte)(g + ((HLSmax - h) * s + HLSmaxD12) / HLSmaxD6);
    //                break;
    //        }
    //    }
    //}
}