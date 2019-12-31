using System;
using System.Drawing;
using System.Windows;

namespace ImageProcessor
{
    class GradientColor
    {
        PointF[] outline;
        System.Windows.Media.Color[] outlineColor;
        Rectangle rect;
        public GradientColor(BitmapAccess src, PointF[] selection, Rectangle box, int smoothing)
        {
            outline = selection;
            rect = box;
            int sz = outline.Length;
            outlineColor = new System.Windows.Media.Color[sz];
            System.Windows.Media.Color[] srcColor = new System.Windows.Media.Color[sz];
            for (int i = 0; i < sz; i++)
                srcColor[i] = src.GetPixel((int)(outline[i].X + box.Left), (int)(outline[i].X + box.Top));
            for (int t = 0; t < sz; t++)
            {
                int r = 0;
                int g = 0;
                int b = 0;
                for (int i = -smoothing; i <= smoothing; i++)
                {
                    int w = smoothing + 1 - Math.Abs(i);
                    int idx = t + i;
                    idx = (idx + sz) % sz;
                    r += srcColor[idx].R * w;
                    g += srcColor[idx].G * w;
                    b += srcColor[idx].B * w;
                }
                r /= (smoothing + 1) / (smoothing + 1);
                g /= (smoothing + 1) / (smoothing + 1);
                b /= (smoothing + 1) / (smoothing + 1);
           //     outlineColor[t] = Color.FromArgb(r, g, b);
            }
        }
        public BitmapAccess CreateBitmap(double angle)
        {
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle); // -
            Vector l = new Vector(dx, dy);
            int sz = outline.Length;
            double dMax = double.MinValue;
            double dMin = double.MaxValue;
            double[] projection = new double[outline.Length];
            for (int i = 0; i < sz; i++)
            {
                double d = outline[i].X * dy - outline[i].Y * dx;
                if (dMax < d)
                    dMax = d;
                if (dMin > d)
                    dMin = d;
                projection[i] = d;
            }
            for (int i=(int)dMin+1; i<= (int)dMax; i++)
            {
                double prevSide = projection[projection.Length - 1]-i;
                double aMin = double.MaxValue;
                double aMax = double.MinValue;
                int jMin = 0;
                int jMax = 0;
                for (int j=0; j< projection.Length; j++)
                {
                    double newSide = projection[j] - i;
                    if (newSide * prevSide <= 0)
                    {
                        Vector p = new Vector(outline[j].X, outline[j].Y);
                        int jPrev = j == 0 ? outline.Length - 1 : j - 1;
                        Vector pPrev = new Vector(outline[jPrev].X, outline[jPrev].Y);
                        Vector v = p - pPrev;
                        double d0 = Vector.CrossProduct(l, v);
                        if (d0 != 0)
                        {
                            double a = (Vector.CrossProduct(p, v) - i * Vector.Multiply(l, v))/d0;
                            if (aMax < a)
                            {
                                aMax = a;
                                jMax = j;
                            }
                            if (aMin > a)
                            {
                                aMin = a;
                                jMin = j;
                            }
                        }
                    }
                    prevSide = newSide;
                }
                if(aMax > aMin)
                {
                    double dist = aMax - aMin;
                    //Color cMin = outlineColor[jMin];
                    //Color cMax = outlineColor[jMax];
                    double pozx = i * dy - rect.Left;
                    double pozy = -i * dx - rect.Top;
                    for (double a = aMin; a < aMax; a++)
                    {
                        int x = (int)(pozx + a * dx);
                        int y = (int)(pozy + a * dy);
                       // pm[y,x]= cMin*a+
                    }
                }
            }
            ByteMatrix[] colors = new ByteMatrix[3];
            return BitmapAccess.CreateFromColorMatrixes(colors);
        }
    }
}
