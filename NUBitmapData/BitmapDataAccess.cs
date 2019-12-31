using System;
using System.Diagnostics;
//using System.Collections.Generic;

namespace System.Windows.Media.Imaging  // it is an extension to this namespace
{
    public class BitmapDataAccess1 
    {
        static PixelFormat[] supportedFormats = { PixelFormats.Bgr24, PixelFormats.Bgr32, PixelFormats.Bgra32, 
            PixelFormats.Gray8, PixelFormats.Pbgra32, PixelFormats.Rgb24, PixelFormats.Indexed8};
        static public bool Supported(PixelFormat pf)
        {
            foreach (PixelFormat supported in supportedFormats)
                if (supported == pf)
                    return true;
            return false;
        }
        BitmapSource source;
        int w;
        int h;
        PixelFormat format;
        int stride;
        int bytespp;            // all supported formats have exact number of bytes per pixel in rawImage
        protected int colorspp; // number of color bytes per pixel (not includes alpha byte); =0 for indexed
        protected byte[] rawImage;
        public byte[] RawImage          { get { return rawImage; } }
        public int Width                { get { return w; } }
        public int Height               { get { return h; } }
        public PixelFormat Format       { get { return format; } }
        public BitmapPalette Palette    { get { return source.Palette; } }
        public int Bytespp              { get { return bytespp; } }
        public int Stride               { get { return stride; } }
        public int DataLength           { get { return stride * h; } }
        public int Colorspp             { get { return colorspp; } }
        protected BitmapDataAccess1(BitmapDataAccess1 bData)
        {
            source = bData.source;
            w=bData.w;
            h=bData.h;
            format=bData.format;
            stride=bData.stride;
            bytespp=bData.bytespp;
            colorspp = bData.colorspp;
            rawImage = bData.rawImage;
        }
        public BitmapDataAccess1(BitmapSource src) { Initialize(src, null); }
        public BitmapDataAccess1(BitmapSource src, Transform transform) { Initialize(src, transform); }
        void Initialize(BitmapSource src, Transform transform)
        {
            //Debug.Assert(false);
            format = src.Format;
            if (!Supported(format))
                throw new Exception("Unsupported bitmap format " + format.ToString());
            source = src;
            SetTransform(transform);
            colorspp = Math.Min(3, Bytespp);  // forth is opacity - irrelevant
            if (Format == PixelFormats.Indexed8 && Palette != null)
                colorspp = 0;
        }
        public void SetTransform(Transform transform)
        {
            try
            {
                BitmapSource bm = transform == null || transform == Transform.Identity ? source : new TransformedBitmap(source, transform);
                w = bm.PixelWidth;
                h = bm.PixelHeight;
                stride = (w * format.BitsPerPixel + 7) / 8;
                bytespp = format.BitsPerPixel / 8; // all supported formats have exact number of bytes per pixel
                rawImage = new byte[DataLength];
                bm.CopyPixels(rawImage, stride, 0);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }
        public BitmapSource ToBitmap(BitmapPalette palette) { return BitmapSource.Create(w, h, 96, 96, format, palette, rawImage, stride); }
        public byte[] GetPixel(int i, int j)
        {
            byte[] data =new byte[bytespp];
            int ind = j * stride + i * bytespp;
            for (i = 0; i < bytespp; i++ )
                data[i] = rawImage[ind++];
            return data;
        }
        public Color GetColorFromPixel(Point pt)
        {
            int i = (int)pt.X;
            int j = (int)pt.Y;
            if (i < 0 || i >= Width || j < 0 || j >= Height)
                return Colors.Black;
            byte[] ba = GetPixel(i, j);
            if (colorspp == 0)
                return Palette.Colors[ba[0]];
            if (colorspp == 1)
                return Color.FromArgb(255, ba[0], ba[0], ba[0]);
            if (colorspp == 3)
                return Color.FromArgb(255, ba[2], ba[1], ba[0]);
            return Color.FromArgb(ba[3], ba[2], ba[1], ba[0]);
        }
        public Color GetColor(Point pt)
        {
            return GetColorFromPixel(new Point(pt.X * source.DpiX / 96, pt.Y * source.DpiY / 96));
        }
        public void SetPixel(int i, int j, byte[] data)
        {
            int ind = j * stride + i * bytespp;
            for (i = 0; i < Math.Min(bytespp, data.Length); i++)
                rawImage[ind++] = data[i];
        }
        //public BitmapData ScaledData(float scale, Copy oper)
        //{
        //    int width = (int)(w * scale + 0.5f);
        //    int height = (int)(h * scale + 0.5f);
        //    BitmapData cm = new BitmapData(width, height, format);
        //    cm.SetData(this, scale, oper);
        //    return cm;
        //}
        //public void SetData(BitmapData src)
        //{             // direct data copy
        //    int ind = 0;
        //    int inds = 0;
        //    for (int j = 0; j < Math.Min(h, src.h); j++)
        //    {
        //        int si=inds;
        //        int end=ind+Math.Min(stride, src.stride);
        //        for (int i = ind; i < end; )
        //            rawImage[i++] = src.rawImage[si++];
        //        ind += stride;
        //        inds += src.stride;
        //    }
        //}
        //public void SetData(BitmapData src, float scale, Copy oper)
        //{
        //    if (scale == 1 || oper==Copy.Same)
        //    {
        //        SetData(src);
        //        return;
        //    }
        //    if (oper == Copy.Interpolare)
        //    {
        //        for (int i = 0; i < DataLength; )   // data interpolation
        //        {
        //            int indx = (i % stride) / bytespp;
        //            int indy = i / stride;
        //            byte[] c = src.InterpolatedColor(indx / scale, indy / scale);
        //            for (int ci = 0; ci < bytespp; ci++)
        //                rawImage[i++] = c[ci];
        //        }
        //    }
        //    else if (oper == Copy.Nearest)
        //    {
        //        for (int i = 0; i < DataLength; )   // data interpolation
        //        {
        //            int indx = (i % stride) / bytespp;
        //            int indy = i / stride;
        //            byte[] c = src.NearestColor(indx / scale, indy / scale);
        //            for (int ci = 0; ci < bytespp; ci++)
        //                rawImage[i++] = c[ci];
        //        }
        //    }
        //}
        //public byte[] InterpolatedColor(float x, float y)
        //{
        //    if (x < 0 || y < 0 || x >= w - 1 || y >= h - 1)
        //        return new byte[] { 0, 0, 0, 0 };
        //    int i = (int)x;
        //    float px = x - i;
        //    float mx = 1 - px;
        //    int j = (int)y;
        //    float py = y - j;
        //    float my = 1 - py;
        //    float mm = my * mx;
        //    float pm = my * px;
        //    float mp = py * mx;
        //    float pp = py * px;
        //    int ind1 = j * stride + i * bytespp;
        //    int ind2 = ind1 + bytespp;
        //    int ind3 = ind1 + stride;
        //    int ind4 = ind3 + bytespp;
        //    float b = mm * rawImage[ind1++] + pm * rawImage[ind2++] + mp * rawImage[ind3++] + pp * rawImage[ind4++];
        //    if (bytespp == 1)
        //        return new byte[] { (byte)b };
        //    float g = mm * rawImage[ind1++] + pm * rawImage[ind2++] + mp * rawImage[ind3++] + pp * rawImage[ind4++];
        //    float r = mm * rawImage[ind1++] + pm * rawImage[ind2++] + mp * rawImage[ind3++] + pp * rawImage[ind4++];
        //    if (bytespp == 3)
        //        return new byte[] { (byte)b, (byte)g, (byte)r };
        //    float a = mm * rawImage[ind1] + pm * rawImage[ind2] + mp * rawImage[ind3] + pp * rawImage[ind4];
        //    return new byte[] { (byte)b, (byte)g, (byte)r, (byte)a };
        //}
        //public byte[] NearestColor(float x, float y)
        //{
        //    int i = (int)(x + 0.5f);
        //    int j = (int)(y + 0.5f);
        //    if (x < 0 || y < 0 || x > w - 1 || y > h - 1)
        //        return new byte[] { 0, 0, 0, 0 };
        //    int ind = j * stride + i * bytespp;
        //    byte[] data = new byte[bytespp];
        //    for (i = 0; i < bytespp; i++)
        //        data[i] = rawImage[ind++];
        //    return data;
        //}
        //public BitmapSource ApplyPixelOffset(byte offset, Color doNotChange)
        //{
        //    try
        //    {
        //        byte[] rawImageMod = new byte[3 * w * h];
        //        byte[] rawImageGray = new byte[w * h];
        //        for (int jr = 0; jr < h; jr++)
        //        {
        //            for (int ir = 0; ir < Width; ir++)
        //            {
        //                int ind = jr * Stride + ir * Bytespp;
        //                int indMod = 3 * (jr * w + ir);
        //                int indGray = jr * w + ir;
        //                if (colorspp == 0)
        //                {
        //                    Color c = Palette.Colors[rawImage[ind]];
        //                    if (c == doNotChange)
        //                    {
        //                        rawImageMod[indMod++] = c.B;
        //                        rawImageMod[indMod++] = c.G;
        //                        rawImageMod[indMod++] = c.R;
        //                        rawImageGray[indGray] = (byte)((c.B + c.G + c.R) / 3);
        //                    }
        //                    else
        //                    {
        //                        rawImageMod[indMod++] = (byte)Math.Min(c.B + offset, 255);
        //                        rawImageMod[indMod++] = (byte)Math.Min(c.G + offset, 255);
        //                        rawImageMod[indMod++] = (byte)Math.Min(c.R + offset, 255);
        //                        rawImageGray[indGray] = (byte)Math.Min((c.B + c.G + c.R) / 3 + offset, 255);
        //                    }
        //                }
        //                else if (colorspp == 1)
        //                {
        //                    byte l = rawImage[ind];
        //                    if (l == doNotChange.R && l == doNotChange.G && l == doNotChange.B)
        //                    {
        //                        rawImageMod[indMod++] = rawImageMod[indMod++] = rawImageMod[indMod++] = l;
        //                        rawImageGray[indGray] = l;
        //                    }
        //                    else
        //                    {
        //                        rawImageMod[indMod++] = rawImageMod[indMod++] = rawImageMod[indMod++] = (byte)Math.Min(l + offset, 255);
        //                        rawImageGray[indGray] = (byte)Math.Min(l + offset, 255);
        //                    }
        //                }
        //                else
        //                {
        //                    byte b = rawImage[ind++];
        //                    byte g = rawImage[ind++];
        //                    byte r = rawImage[ind++];
        //                    if (r == doNotChange.R && g == doNotChange.G && b == doNotChange.B)
        //                    {
        //                        rawImageMod[indMod++] = b;
        //                        rawImageMod[indMod++] = g;
        //                        rawImageMod[indMod++] = r;
        //                        rawImageGray[indGray] = (byte)((b + g + r) / 3);
        //                    }
        //                    else
        //                    {
        //                        rawImageMod[indMod++] = (byte)Math.Min(b + offset, 255);
        //                        rawImageMod[indMod++] = (byte)Math.Min(g + offset, 255);
        //                        rawImageMod[indMod++] = (byte)Math.Min(r + offset, 255);
        //                        rawImageGray[indGray] = (byte)Math.Min((b + g + r) / 3 + offset, 255);
        //                    }
        //                }
        //            }
        //        }
        //        //return BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Gray8, null, rawImageGray, Width);
        //        return BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr24, null, rawImageMod, 3*w);
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        Console.WriteLine(ex.StackTrace);
        //        return null;
        //    }
        //}
    }
}