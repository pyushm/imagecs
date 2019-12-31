using System;
using System.Drawing; //PointF
using System.Windows;

namespace ImageProcessor
{
    public class Morph
    {
        float[] segmentLength;
        PointF[] segmentDirection;
        PointF[] vortex;
        PointF shift;
        float size;
        public Morph(PointF[] corner_, PointF shift_)
        {
            vortex = corner_;
            shift = shift_;
            size = 2 * (float)Math.Sqrt(shift.X * shift.X + shift.Y * shift.Y); // = 2 |shift|
            if (vortex.Length == 0)
            {
                segmentLength = new float[0];
                segmentDirection = new PointF[0];
            }
            else if (vortex.Length == 1)
            {
                segmentLength = new float[] {0};
                segmentDirection = new PointF[] { new PointF(1, 0) };
            }
            else
            {
                PointF prev = vortex[0];
                segmentLength = new float[vortex.Length - 1];
                segmentDirection = new PointF[vortex.Length - 1];
                for (int r = 0; r < segmentLength.Length; r++)
                {
                    PointF next= vortex[r + 1];
                    float dx=next.X-prev.X;
                    float dy=next.Y-prev.Y;
                    segmentLength[r] = (float)Math.Sqrt(dx * dx + dy * dy);
                    segmentDirection[r] = new PointF(dx / segmentLength[r], dy / segmentLength[r]);
                    prev = next;
                }
            }
        }
        float Coef(int i, int j)        // shifht smoothing coeff [0, 1]
        {
            float coef = 0;
            for (int r = 0; r < segmentLength.Length; r++)
            {   // for each vortex point
                float coef1;
                float rx=i- vortex[r].X;
                float ry=j- vortex[r].Y;             // vector to vortex: R={rx,ry}
                float x = segmentDirection[r].X * rx + segmentDirection[r].Y * ry;  // R*segmentDirection
                float y = Math.Abs(segmentDirection[r].X * ry - segmentDirection[r].Y * rx);    // [R x segmentDirection]
                float l = segmentLength[r];
                if (y >= size || x < -size)
                    continue;
                else if (x < 0)
                {
                    if (r == 0)
                        continue;
                    coef1 = 1 - (float)Math.Sqrt(x * x + y * y) / size;
                }
                else if (x <= l)
                {
                    coef1 = 1 - y / size;
                    if (r == 0 && x < l / 2)
                        coef1 *= 2 * x / l;
                    if (r== segmentLength.Length - 1 && x > l / 2)
                        coef1 *= 2 - 2 * x / l;
                }
                else if (x < l + size)
                {
                    if (r == segmentLength.Length - 1)
                        continue;
                    coef1 = 1 - (float)Math.Sqrt((x - l) * (x - l) + y * y) / size;
                }
                else
                    continue;
                if (coef < coef1)
                    coef = coef1;
            }
            return coef;
        }
        //public BitmapAccess Apply(BitmapAccess src) 
        //{
        //    PixelMatrix cola = Apply(src.PixelMatrix());
        //    return BitmapAccess.CreatePArgbBitmap(cola);
        //}
        //PixelMatrix Apply(PixelMatrix cola)   
        //{
        //    PixelMatrix res = new PixelMatrix(cola);
        //    for (int i = 0; i < res.Width; i++)
        //    {
        //        for (int j = 0; j < res.Height; j++)
        //        {
        //            float coef=Coef(i, j);
        //            if (coef > 0)
        //                res[i, j] = cola.Pixel(j + coef * shift.Y, i + coef * shift.X);
        //            //byte br = (byte)(255 * (1 - coef));
        //            //res[i, j] = new Pixel(br, br, br);
        //        }
        //    }
        //    return res;
        //}
    }
}
