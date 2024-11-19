using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ImageProcessor
{
    public enum FilterType
    {
        MedianFilter,
        WaveletFilter,
        ConeAverage,
        CrossAverage,
        CrossDifference,
        SharrGradient,
    }
    public class BitmapAccess
    {   // base class for accessing Bitmap in WPF. BitmapAccess is used to avoid collision with System.Drawing.Bitmap
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        static public byte[] SerializeFrame(BitmapFrame bf, bool exact)
        {
            BitmapEncoder bitmapEncoder = exact ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder();
            var jenc = bitmapEncoder as JpegBitmapEncoder;
            if (jenc != null) // ~87 for 3 Mpixels, ~93 fpr 1 Mpixel, 99 for 0
                jenc.QualityLevel = Math.Min((int)(77 + 55 / (bf.Width * bf.Height / 1.0e6 + 2.5)), 100);
            bitmapEncoder.Frames.Add(bf);
            MemoryStream ms = new MemoryStream();
            bitmapEncoder.Save(ms);
            return ms.ToArray();
        }
        static PixelFormat[] supportedFormats = { PixelFormats.Bgr24, PixelFormats.Bgr32, PixelFormats.Bgra32,
            PixelFormats.Gray8, PixelFormats.Pbgra32, PixelFormats.Rgb24, PixelFormats.Indexed8};
        static public bool Supported(PixelFormat pf)
        {
            foreach (PixelFormat supported in supportedFormats)
                if (supported == pf)
                    return true;
            return false;
        }
        public string Path              { get; private set; }
        protected bool isIndexed;
        public IntPtr DataPtr           { get { return Source.BackBuffer; } }
        public int Width                { get; private set; }   // bitmap pixel width
        public int Height               { get; private set; }   // bitmap pixel height
        public PixelFormat PixelFormat  { get { return Source.Format; } }
        public BitmapPalette Palette    { get { return Source.Palette; } }
        public int Bytespp              { get; private set; }   // all supported formats have exact number of bytes per pixel in rawImage
        public int Stride               { get { return Source.BackBufferStride; } }
        public int DataLength           { get { return Stride * Height; } }
        public WriteableBitmap Source   { get; private set; }   // image source
        public static BitmapAccess CreateFromColorMatrixes(ByteMatrix[] colors)
        {
            if (colors.Length == 0)
                return null;
            int width = colors[0].Width;
            int height = colors[0].Height;
            WriteableBitmap bmn = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            bmn.Lock();
            Chank[] chanks = Chank.CreateChanks(height, 500, null, bmn);
            unsafe
            {
                //foreach (var chank in chanks)
                Parallel.ForEach(chanks, (chank) =>
                {
                    byte* ptr0 = (byte*)chank.ToData;
                    for (int i = chank.StartRow; i < chank.EndRow; i++)
                    {
                        byte* ptrn = ptr0;
                        ptr0 += chank.ToStride;
                        if (colors.Length == 1)
                        {
                            ByteMatrix b = colors[0];
                            for (ushort j = 0; j < width; j++)
                            {
                                *ptrn++ = b[i, j];
                                *ptrn++ = b[i, j];
                                *ptrn++ = b[i, j];
                                *ptrn++ = byte.MaxValue;
                            }
                        }
                        else if (colors.Length == 3)
                        {
                            ByteMatrix b = colors[0];
                            ByteMatrix g = colors[1];
                            ByteMatrix r = colors[2];
                            for (ushort j = 0; j < width; j++)
                            {
                                *ptrn++ = b[i, j];
                                *ptrn++ = g[i, j];
                                *ptrn++ = r[i, j];
                                *ptrn++ = byte.MaxValue;
                            }
                        }
                        else
                        {
                            ByteMatrix b = colors[0];
                            ByteMatrix g = colors[1];
                            ByteMatrix r = colors[2];
                            ByteMatrix a = colors[3];
                            for (ushort j = 0; j < width; j++)
                            {
                                *ptrn++ = b[i, j];
                                *ptrn++ = g[i, j];
                                *ptrn++ = r[i, j];
                                *ptrn++ = a[i, j];
                            }
                        }
                    }
                //}
                });
            }
            bmn.Unlock();
            return new BitmapAccess(bmn);
        }
        public static BitmapAccess Create8bppIndexedBitmap(ByteMatrix bwImage, InterpolationFunction func)
        {
            int nc = 256;
            List<Color> colors = new List<Color>(nc);
            for (int i = 0; i < nc; i++)
            {
                float acoef = func == null ? 1 : 1 - Math.Min(1, Math.Max(0, func.Apply(i / (float)byte.MaxValue)));
                byte a = (byte)(byte.MaxValue * acoef);
                byte b = (byte)i;
                colors.Add(Color.FromArgb(a, b, b, b));
            }
            BitmapPalette palette = new BitmapPalette(colors);
            WriteableBitmap bm = new WriteableBitmap(bwImage.Width, bwImage.Height, 96, 96, PixelFormats.Indexed8, palette);
            unsafe
            {
                byte* ptr = (byte*)bm.BackBuffer;
                for (int i = 0; i < bwImage.Height; i++)
                {
                    for (int j = 0; j < bwImage.Width; j++)
                        *ptr++ = bwImage[i, j];
                    for (int j = bwImage.Width; j < bm.BackBufferStride; j++)
                        *ptr++ = 0;
                }
            }
            return new BitmapAccess(bm);
        }
        public static BitmapAccess LoadImage(string fullPath, bool encrypted, int maxWidth=0)
        {
            byte[] imageBytes = DataAccess.ReadFile(fullPath, encrypted);
            return imageBytes.Length > 0 ? new BitmapAccess(new MemoryStream(imageBytes), maxWidth, fullPath) : null;
        }
        public static BitmapImage CreateHashImage(string fullPath, bool encrypted, IntSize size)
        {
            byte[] imageBytes = DataAccess.ReadFile(fullPath, encrypted);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(imageBytes);
            bi.DecodePixelWidth = size.Width;
            bi.DecodePixelHeight = size.Height;
            bi.EndInit();
            return bi;
        }
        public BitmapAccess(MemoryStream dataStream, int maxWidth, string path)
        {   // loading image from memory array
            Path = path;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = dataStream;
            bi.DecodePixelWidth = maxWidth;
            //bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // removed to process Bgra32
            bi.EndInit();
            Initialize(new WriteableBitmap(bi), null);
        }
        //public BitmapAccess(System.Drawing.Bitmap src)
        //{ // example of working with old Bitmap
        //    BitmapSource bs;
        //    IntPtr hBitmap = src.GetHbitmap();
        //    try { bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); }
        //    catch { bs = null; }
        //    finally { DeleteObject(hBitmap); }
        //    Initialize(new WriteableBitmap(bs), null);
        //}
        public BitmapAccess(BitmapSource bs, Transform transform = null)
        {
            Path = "";
            if (bs == null)
                throw new ArgumentNullException("bitmap = null");
            Initialize(new WriteableBitmap(bs), transform);
        }
        public BitmapAccess(int w, int h)
        {
            Path = "";
            if (w <=0 || h <= 0)
                throw new ArgumentNullException("bitmap dimetsion <=0");
           Initialize(new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null), null);
        }
        public double ScaleReducingImageTo(double maxSize)
        {
            double scalex = maxSize / Width;
            double scaley = maxSize / Height;
            if (scalex >= 1 && scaley >= 1)
                return 1; // image already smaller than maxSize
            return scalex > scaley ? scaley : scalex;
        }
        public string SaveToFile(string fullPath, bool exact, bool encrypt)
        {
            try
            {
                byte[] ba = SerializeFrame(BitmapFrame.Create(Source), exact);
                DataAccess.WriteFile(fullPath, ba, encrypt);
                return "";
            }
            catch (Exception ex) { return fullPath + Environment.NewLine + ex.Message; }
        }
        void Initialize(WriteableBitmap src, Transform transform)
        {
            Source = src;
            Bytespp = (PixelFormat.BitsPerPixel + 7) / 8;// all supported formats have exact number of bytes per pixel
            isIndexed = PixelFormat == PixelFormats.Indexed8 && Palette != null;
            Width = Source.PixelWidth;
            Height = Source.PixelHeight;
            if (transform != null && transform != Transform.Identity)
                Source = new WriteableBitmap(new TransformedBitmap(Source, transform));
        }
        public Color BorderColor()
        {
            Color borderColor = GetPixel(0, 0);
            for (int i = 0; i < Width; i++)
                if (GetPixel(i, 0) != borderColor || GetPixel(i, Height - 1) != borderColor)
                    return ColorTransform.ColorNull;
            for (int j = 0; j < Height; j++)
                if (GetPixel(0, j) != borderColor || GetPixel(Width - 1, j) != borderColor)
                    return ColorTransform.ColorNull;
            return borderColor;
        }
        public ByteMatrix TransparencyMask()
        {   // returns null if !Pbgra32 or not all pixels on perimeter are transparent
            if (PixelFormat != PixelFormats.Pbgra32)
                return null;
            //for (int i = 0; i < Width; i++)
            //    if (GetPixel(i, 0).A != 0 || GetPixel(i, Height - 1).A != 0)
            //        return null;
            //for (int j = 0; j < Height; j++)
            //    if (GetPixel(0, j).A != 0 || GetPixel(Width - 1, j).A != 0)
            //        return null;
            ByteMatrix transparencyMask = new ByteMatrix(Height, Width);
            Chank[] chanks = Chank.CreateChanks(Height, 500, Source, null);
            int w = Width;
            unsafe
            {
                //foreach(var chank in chanks)
                Parallel.ForEach(chanks, (chank) =>
                {
                    byte* ptr = (byte*)chank.FromData;
                    for (int i = chank.StartRow; i < chank.EndRow; i++)
                    {
                        for (ushort j = 0; j < w; j++)
                        {
                            ptr+=3; // skip colors
                            transparencyMask[i, j] = *ptr++;
                        }
                    }
                //}
                });
            }
            return transparencyMask;
        }
        public byte[] GetBytes(int i, int j)
        {
            byte[] data = new byte[Bytespp];
            int ind = j * Stride + i * Bytespp;
            if (ind >= 0 && ind < DataLength)
            {
                unsafe
                {
                    byte* bp = (byte*)(DataPtr + ind);
                    for (int c = 0; c < Bytespp; c++)
                        data[c] = *bp++;
                }
            }
            return data;
        }
        public Color GetPixel(int i, int j) { return ColorFromBytes(GetBytes(i, j)); }
        Color ColorFromBytes(byte[] ba)
        { 
            return isIndexed ? Palette.Colors[ba[0]] : Bytespp == 1 ? Color.FromArgb(255, ba[0], ba[0], ba[0]) :
                Bytespp == 3 ? Color.FromArgb(255, ba[2], ba[1], ba[0]) : Color.FromArgb(ba[3], ba[2], ba[1], ba[0]);
        }
        public BitmapAccess Clone() { return new BitmapAccess(Source.Clone()); }
        public Color GetColorFromPixel(Point pt)
        {
            int i = (int)pt.X;
            int j = (int)pt.Y;
            if (i < 0 || i >= Width || j < 0 || j >= Height)
                return Colors.Black;
            return GetPixel(i, j);
        }
        public Color GetColor(Point pt) { return GetColorFromPixel(new Point(pt.X * Source.DpiX / 96, pt.Y * Source.DpiY / 96)); }
        public void UpdateTransparency(ByteMatrix mask)
        {
            if (PixelFormat != PixelFormats.Pbgra32 || mask == null)
                return;
            if (Width != mask.Width || Height != mask.Height)
                return;
            Source.Lock();
            Chank[] chanks = Chank.CreateChanks(Height, 700, Source, null);
            float norm = (float)byte.MaxValue;
            unsafe
            {
                //foreach(var chank in chanks)
                Parallel.ForEach(chanks, (chank) =>
                {
                    byte* ptr = (byte*)chank.FromData;
                    for (int i = chank.StartRow; i < chank.EndRow; i++)
                    {
                        for (ushort j = 0; j < Width; j++)
                        {
                            float coef = mask[i, j] / norm;
                            *ptr = (byte)(*ptr * coef); // b
                            ptr++;
                            *ptr = (byte)(*ptr * coef); // g
                            ptr++;
                            *ptr = (byte)(*ptr * coef); // r
                            ptr++;
                            *ptr = (byte)(byte.MaxValue * coef);// ignore image transparency
                            ptr++;
                        }
                    }
                    //}
                });
            }
            Source.Unlock();
        }
        public void Overwrite(BitmapAccess src, int x0, int y0, int scale)
        {   // overwrites bitmap with src starting at x0, y0; scale (e.g. 1, 2, 3) reduces size of from
            WriteableBitmap from = src.Source;
            WriteableBitmap to = Source;
            if (from == null || to == null || Bytespp < 4 || from.Format != PixelFormats.Pbgra32)
            {
                bool borderSet = false;
                if (PixelFormat != PixelFormats.Pbgra32)
                    Source = to = CreateAdjustedPArgbBitmap(null, null, ref borderSet);
            }
            from.Lock();
            to.Lock();
            int h = from.PixelHeight / scale;
            int beginRow = Math.Max(0, y0);
            int endRow = Math.Min(y0 + h, to.PixelHeight);
            int w = from.PixelWidth / scale;
            int beginCol = Math.Max(0, x0);
            int endCol = Math.Min(x0 + w, to.PixelWidth);
            unsafe
            {
                for (int j = beginRow; j < endRow; j++)
                {
                    int fromRow = (j - y0) * scale;
                    uint* fromRowPtr = (uint*)from.BackBuffer + fromRow * from.PixelWidth;
                    uint* toPtr = (uint*)to.BackBuffer + beginCol + j * to.PixelWidth;
                    for (int i = beginCol; i < endCol; i++)
                    {
                        uint* fromPtr = fromRowPtr + (i - x0) * scale;
                        if (*fromPtr > 0)
                            *toPtr = *fromPtr;
                        toPtr++;
                    }
                }
            }
            from.Unlock();
            to.Unlock();
        }
        public BitmapAccess DeleteSelection(Int32Rect cropRect, List<Point> edge)
        {
            Source.Lock();
            unsafe
            {
                uint* ptr = (uint*)Source.BackBuffer + cropRect.Y * Source.PixelWidth;
                int beginRow = Math.Max(0, cropRect.Y);
                int endRow = Math.Min(Source.PixelHeight, cropRect.Y + cropRect.Height);
                int beginCol = Math.Max(0, cropRect.X);
                int endCol = Math.Min(Source.PixelWidth, cropRect.X + cropRect.Width);
                for (int r = beginRow; r < endRow; r++)
                {
                    List<double> nodes = new List<double>(); //  Build a list of row crossing nodes.
                    int jj = edge.Count - 1;
                    int ii = 0;
                    for (; ii < edge.Count; ii++)
                    {
                        if (edge[ii].Y < r && edge[jj].Y >= r || edge[jj].Y < r && edge[ii].Y >= r)
                            nodes.Add(edge[ii].X + (r - edge[ii].Y) / (edge[jj].Y - edge[ii].Y) * (edge[jj].X - edge[ii].X));
                        jj = ii;
                    }
                    if (nodes.Count == 0)
                        continue;
                    ii = 0; //  Sort the nodes, via a simple “Bubble” sort.
                    while (ii < nodes.Count - 1)
                    {
                        if (nodes[ii] > nodes[ii + 1])
                        {
                            double swap = nodes[ii];
                            nodes[ii] = nodes[ii + 1];
                            nodes[ii + 1] = swap;
                            if (ii != 0)
                                ii--;
                        }
                        else
                            ii++;
                    }   
                    int nodeInd = 0;
                    uint* ptr1 = ptr + beginCol;
                    for (int j = beginCol; j < endCol; j++)
                    {
                        while (nodeInd < nodes.Count && (nodeInd % 2 == 0 ? nodes[nodeInd] < j : nodes[nodeInd] <= j))
                            nodeInd++;
                        if (nodeInd % 2 != 0)
                            *ptr1 = 0xFFFFFFFF; // white pixels inside contour
                        ptr1++;
                    }
                    ptr += Source.PixelWidth;
                }
            }
            Source.Unlock();
            return new BitmapAccess(Source);
        }
        public BitmapAccess SetSelectionBitmap(Int32Rect cropRect, List<Point> edge, bool clearOutside)
        { 
            WriteableBitmap clip = new WriteableBitmap(cropRect.Width, cropRect.Height, 96, 96, PixelFormats.Pbgra32, null);
            if (edge == null || edge.Count < 4)
                return new BitmapAccess(clip);
            //Debug.WriteLine(cropRect.ToString());
            //Debug.WriteLine("clip: begin=" + clip.BackBuffer + " length=" + clip.BackBufferStride * clip.PixelHeight);
            //Debug.WriteLine("source: begin=" + clip.BackBuffer + " length=" + source.BackBufferStride * source.PixelHeight);
            Chank[] chanks = Chank.CreateChanks(cropRect.Height, 700, Source, clip);
            clip.Lock();
            try
            {
                unsafe
                {
                    foreach (var chank in chanks)
                    //Parallel.ForEach(chanks, (chank) =>   // runtime error "One or more errors occurred"
                    {
                        //Debug.WriteLine(chank.ToString());
                        uint* uptr = (uint*)chank.ToData;
                        for (int i = chank.StartRow; i < chank.EndRow; i++)
                        {
                            List<double> nodes = new List<double>(); //  Build a list of row crossing nodes.
                            int r = cropRect.Y + i;
                            int jj = edge.Count - 1;
                            int ii = 0;
                            for (; ii < edge.Count; ii++)
                            {
                                //if (edge[ii].Y < r && edge[jj].Y >= r || edge[jj].Y < r && edge[ii].Y >= r)
                                //    nodes.Add(edge[ii].X - cropRect.X + (r - edge[ii].Y) / (edge[jj].Y - edge[ii].Y) * (edge[jj].X - edge[ii].X));
                                if (edge[ii].Y - 0.5 < r && edge[jj].Y - 0.5 >= r || edge[jj].Y - 0.5 < r && edge[ii].Y - 0.5 >= r)
                                    nodes.Add(edge[ii].X - 0.5 - cropRect.X + (r - edge[ii].Y + 0.5) / (edge[jj].Y - edge[ii].Y) * (edge[jj].X - edge[ii].X));
                                jj = ii;
                            }
                            ii = 0; //  Sort the nodes, via a simple “Bubble” sort.
                            while (ii < nodes.Count - 1)
                            {
                                if (nodes[ii] > nodes[ii + 1])
                                {
                                    double swap = nodes[ii];
                                    nodes[ii] = nodes[ii + 1];
                                    nodes[ii + 1] = swap;
                                    if (ii != 0)
                                        ii--;
                                }
                                else
                                    ii++;
                            }
                            int nodeInd = 0;
                            byte* bptr = (byte*)(chank.FromData + Stride * (i - chank.StartRow + cropRect.Y) + Bytespp * cropRect.X);
                           // Debug.WriteLine(" toPos=" + ((long)(IntPtr)uptr - (long)clip.BackBuffer) + " fromPos=" + ((long)(IntPtr)bptr - (long)source.BackBuffer));
                            for (ushort j = 0; j < clip.PixelWidth; j++)
                            {
                                while (nodeInd < nodes.Count && nodes[nodeInd] < j)
                                    nodeInd++;
                                if (nodeInd % 2 == 0) // pixels outside contour
                                    *uptr = clearOutside ? 0x00FFFFFF : *(uint*)bptr & 0x00FFFFFF;
                                else // remove transparency
                                    *uptr = *(uint*)bptr | 0xFF000000;
                                uptr++;
                                bptr += Bytespp;
                            }
                        }
                    }
                    //});
                }
            }
            finally { clip.Unlock(); }
            return new BitmapAccess(clip);
        }
        public ByteMatrix[] CreateColorMatrixes()
        {
            BitmapPalette palette = Source.Palette;
            Source.Lock();
            Chank[] chanks = Chank.CreateChanks(Height, 500, Source, null);
            int nm = isIndexed ? 3 : Bytespp;
            ByteMatrix[] bmpa = new ByteMatrix[nm];
            for (int i = 0; i < nm; i++)
                bmpa[i] = new ByteMatrix(Height, Width);
            int w = Width;
            unsafe
            {
                //foreach(var chank in chanks)
                Parallel.ForEach(chanks, (chank) =>
                {   // setting original ByteMatrixes
                    byte* ptr0 = (byte*)chank.FromData;
                    if (Bytespp == 1)
                    {
                        if(isIndexed)
                        {
                            ByteMatrix b = bmpa[0];
                            ByteMatrix g = bmpa[1];
                            ByteMatrix r = bmpa[2];
                            for (int i = chank.StartRow; i < chank.EndRow; i++)
                            {
                                for (ushort j = 0; j < w; j++)
                                {
                                    byte* ptr = ptr0;
                                    ptr0 += chank.FromStride;
                                    Color c = palette.Colors[*ptr++];
                                    b[i, j] = c.B;
                                    g[i, j] = c.G;
                                    r[i, j] = c.R;
                                }
                            }
                        }
                        else
                        {
                            ByteMatrix b = bmpa[0];
                            for (int i = chank.StartRow; i < chank.EndRow; i++)
                            {
                                byte* ptr = ptr0;
                                ptr0 += chank.FromStride;
                                for (ushort j = 0; j < w; j++)
                                    b[i, j] = *ptr++;
                            }
                        }

                    }
                    else if (Bytespp == 3)
                    {
                        ByteMatrix b = bmpa[0];
                        ByteMatrix g = bmpa[1];
                        ByteMatrix r = bmpa[2];
                        for (int i = chank.StartRow; i < chank.EndRow; i++)
                        {
                            byte* ptr = ptr0;
                            ptr0 += chank.FromStride;
                            for (ushort j = 0; j < w; j++)
                            {
                                b[i, j] = *ptr++;
                                g[i, j] = *ptr++;
                                r[i, j] = *ptr++;
                            }
                        }
                    }
                    else
                    {
                        ByteMatrix b = bmpa[0];
                        ByteMatrix g = bmpa[1];
                        ByteMatrix r = bmpa[2];
                        ByteMatrix a = bmpa[3];
                        for (int i = chank.StartRow; i < chank.EndRow; i++)
                        {
                            byte* ptr = ptr0;
                            ptr0 += chank.FromStride;
                            for (ushort j = 0; j < w; j++)
                            {
                                b[i, j] = *ptr++;
                                g[i, j] = *ptr++;
                                r[i, j] = *ptr++;
                                a[i, j] = *ptr++;
                            }
                        }
                    }
                //}
                });
            }
            Source.Unlock();
            return bmpa;
        }
        public BitmapAccess ApplyConversion(FilterType access, int halfSize, int relativeLevel)
        {
            if (halfSize <= 0)
                return Clone();
            int byteLevel = byte.MaxValue * relativeLevel / 100; // relativeLlevel in %
            ByteMatrix[] original = CreateColorMatrixes();    // b, g, r, a
            int pairLength = Math.Min(3, original.Length);
            ByteMatrixPair[] bmpa = new ByteMatrixPair[pairLength];
            for (int i = 0; i < pairLength; i++)
            {
                bmpa[i] = new ByteMatrixPair();
                bmpa[i].original = original[i];
            }
            ByteMatrix transparency = original.Length == 4 ? original[3] : null;
            if (access == FilterType.MedianFilter)
            {   // Parallel inside MedianSmoothing
                for (int i = 0; i < pairLength; i++) { bmpa[i].transformed = bmpa[i].original.MedianSmoothing(halfSize, 500, (byte)byteLevel); }
            }
            else if (access == FilterType.WaveletFilter)
            {   // Parallel inside WaveletContrasting
                ByteMatrix filter = ByteMatrix.CreateWaveletFilter(halfSize);
                for (int i = 0; i < pairLength; i++) { bmpa[i].transformed = bmpa[i].original.WaveletContrasting(filter, 700, byteLevel, false); }
            }
            else if (access == FilterType.ConeAverage)
            {
                ByteMatrix filter = ByteMatrix.CreateConeFilter(halfSize);
                for (int i = 0; i < pairLength; i++) { bmpa[i].transformed = bmpa[i].original.ConeSmoothing(filter, 700); }
            }
            ByteMatrix[] newColors = new ByteMatrix[original.Length];
            for (int i = 0; i < pairLength; i++)
                newColors[i] = bmpa[i].transformed;
                //newColors[i] = bmpa[i].original;
            if (original.Length == 4)
                newColors[3] = transparency;
            return CreateFromColorMatrixes(newColors);
        }
        public WriteableBitmap CreateAdjustedPArgbBitmap(ColorTransform transform, ByteMatrix mask, ref bool transparencySet)
        {
            bool sameInputFormat = PixelFormat == PixelFormats.Pbgra32 || PixelFormat == PixelFormats.Bgra32;
            if ((transform == null || transform.IsIdentical) && sameInputFormat)
                return Source.Clone();
            bool alphaIgnored = (transform != null && transform.IsTransparentColorSet) || !sameInputFormat;
            WriteableBitmap bmn = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32, null);
            bmn.Lock();
            if (mask != null)
                mask.Set(Height, Width);
            bool flag = false;
            Chank[] chanks = Chank.CreateChanks(Height, 500, Source, bmn);
            int w = Width;
            unsafe
            {
                foreach(var chank in chanks)
                //Parallel.ForEach(chanks, (chank) =>
                {
                    byte a = 0, r, g, b;
                    byte* ptr0 = (byte*)chank.FromData;
                    byte * ptrn0 = (byte*)chank.ToData;
                    for (int i = chank.StartRow; i < chank.EndRow; i++)
                    {
                        byte* ptr = ptr0;
                        byte* ptrn = ptrn0;
                        for (ushort j = 0; j < w; j++)
                        {
                            if (Bytespp == 1)
                            {
                                r = b = g = *ptr++;
                            }
                            else if (Bytespp == 3)
                            {
                                b = *ptr++;
                                g = *ptr++;
                                r = *ptr++;
                            }
                            else
                            {
                                b = *ptr++;
                                g = *ptr++;
                                r = *ptr++;
                                a = *ptr++;
                            }
                            if (alphaIgnored)
                                a = byte.MaxValue;
                            transform?.Apply(ref a, ref r, ref g, ref b);
                            if (mask != null)
                            {
                                mask[i, j] = a;
                                if (a != 0) // mask had all 0: flag set true if it changed
                                    flag = true;
                            }
                            *ptrn++ = b;
                            *ptrn++ = g;
                            *ptrn++ = r;
                            *ptrn++ = a;
                        }
                        ptrn0 += bmn.BackBufferStride;
                        ptr0 += Source.BackBufferStride;
                    }
                }
                //});
            }
            bmn.Unlock();
            //TimeSpan t = DateTime.Now - to;
            //Console.WriteLine("CreateAdjustedPArgbBitmap: " + t.TotalMilliseconds);
            transparencySet = flag;
            return bmn;
        }
        public System.Drawing.Bitmap CreateBitmapImage()
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(Source));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);
                return new System.Drawing.Bitmap(bitmap);
            }
        }
        public string ToColorsString()
        {   // complete bitmap to string with all 4 bytes
            ByteMatrix[] bma = CreateColorMatrixes();    // b, g, r, a
            string[] ca = new string[] { "blue ", "green ", "red ", "alpha " };
            int i = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ToString());
            foreach (var bm in bma)
                sb.Append(ca[i++] + bm.ToString());
            return sb.ToString();
        }
        public string ToByteMatrixString(int ind)
        {   // bitmap byte=ind to string
            ByteMatrix[] bma = CreateColorMatrixes();    // b, g, r, a
            string[] ca = new string[] { "blue ", "green ", "red ", "alpha " };
            return ind>=0 && ind<4 ? ca[ind] + bma[ind].ToString() : "";
        }
        public override string ToString() { return PixelFormat.ToString() + ' ' + Width + 'x' + Height; }
        static public void DebugSave(string path, BitmapSource bs)
        {
            try
            {
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bs));
                    encoder.Save(fileStream);
                }
            }
            catch(Exception ex) {
                Debug.WriteLine("Saving "+ path+" failed: "+ex.Message); }
        }
        public void DebugSave(string path) { DebugSave(path, Source); }
    }
    public class ValueMartixBitmap : BitmapAccess
    {
        int hSize;              // src horizontal evaluation size
        int vSize;              // src vertical evaluation size
        int jump;               // src evaluation compression jump
        int bottomCut;          // cutoff of low brightness enhancement
        bool relative;          // gradient method
        int resolution;
        DataRange dataRange = new DataRange();
        int matrixOffset;       // size of not evaluated matrix edge 

        public ValueMartixBitmap(BitmapAccess src) : base(src.Source) { }
        public ValueMartix ScharrGradient(int resolution)
        {
            DataRange dataRange = new DataRange();
            int squareCoeff = 3;            // weight of square corners     mm m0 mp
            int crossCoeff = 10;            // weigth of cross points       0m 00 0p
            float coeff = crossCoeff + 2 * squareCoeff; // total weight     pm p0 pp
            byte r0m, g0m, b0m, l0m, r0p, g0p, b0p, l0p, rm0, gm0, bm0, lm0, rp0, gp0, bp0, lp0; // 1st - h; 2nd - w
            byte rmm, gmm, bmm, lmm, rmp, gmp, bmp, lmp, rpm, gpm, bpm, lpm, rpp, gpp, bpp, lpp;
            resolution = Math.Max(resolution, 1);
            int w = Width / resolution;
            int h = Height / resolution;
            float[,] gradN = new float[h, w];
            try
            {
                int hSize = resolution * Bytespp;
                int vSize = resolution * Stride;
                int jump = (resolution - 1) * Bytespp;
                unsafe
                {
                    byte* ptr = (byte*)DataPtr;
                    ptr += hSize + vSize;
                    for (int jj = 1; jj < h - 1; jj++)
                    {
                        byte* ptrmm = ptr - hSize - vSize;
                        byte* ptr0m = ptr - hSize;
                        byte* ptrpm = ptr - hSize + vSize;
                        byte* ptrm0 = ptr - vSize;
                        byte* ptrp0 = ptr + vSize;
                        byte* ptrmp = ptr + hSize - vSize;
                        byte* ptr0p = ptr + hSize;
                        byte* ptrpp = ptr + hSize + vSize;
                        for (int ii = 1; ii < w - 1; ii++)
                        {
                            float g;
                            if (Bytespp == 1)
                            {
                                lmm = *ptrmm++;
                                l0m = *ptr0m++;
                                lpm = *ptrpm++;
                                lm0 = *ptrm0++;
                                lp0 = *ptrp0++;
                                lmp = *ptrmp++;
                                l0p = *ptr0p++;
                                lpp = *ptrpp++;
                                int wl = l0p - l0m;
                                int hl = lp0 - lm0;
                                int wsl = lpp + lmp - lpm - lmm;
                                int hsl = lpp + lpm - lmp - lmm;
                                wl = crossCoeff * wl + squareCoeff * wsl;
                                hl = crossCoeff * hl + squareCoeff * hsl;
                                g = (float)Math.Sqrt(wl * wl + hl * hl);
                            }
                            else
                            {
                                bmm = *ptrmm++;
                                gmm = *ptrmm++;
                                rmm = *ptrmm++;
                                b0m = *ptr0m++;
                                g0m = *ptr0m++;
                                r0m = *ptr0m++;
                                bpm = *ptrpm++;
                                gpm = *ptrpm++;
                                rpm = *ptrpm++;
                                bm0 = *ptrm0++;
                                gm0 = *ptrm0++;
                                rm0 = *ptrm0++;
                                bp0 = *ptrp0++;
                                gp0 = *ptrp0++;
                                rp0 = *ptrp0++;
                                bmp = *ptrmp++;
                                gmp = *ptrmp++;
                                rmp = *ptrmp++;
                                b0p = *ptr0p++;
                                g0p = *ptr0p++;
                                r0p = *ptr0p++;
                                bpp = *ptrpp++;
                                gpp = *ptrpp++;
                                rpp = *ptrpp++;
                                int wb = b0p - b0m;
                                int hb = bp0 - bm0;
                                int wg = g0p - g0m;
                                int hg = gp0 - gm0;
                                int wr = r0p - r0m;
                                int hr = rp0 - rm0;
                                int wsb = bpp + bmp - bpm - bmm;
                                int hsb = bpp + bpm - bmp - bmm;
                                int wsg = gpp + gmp - gpm - gmm;
                                int hsg = gpp + gpm - gmp - gmm;
                                int wsr = rpp + rmp - rpm - rmm;
                                int hsr = rpp + rpm - rmp - rmm;
                                wb = crossCoeff * wb + squareCoeff * wsb;
                                hb = crossCoeff * hb + squareCoeff * hsb;
                                wg = crossCoeff * wg + squareCoeff * wsg;
                                hg = crossCoeff * hg + squareCoeff * hsg;
                                wr = crossCoeff * wr + squareCoeff * wsr;
                                hr = crossCoeff * hr + squareCoeff * hsr;
                                g = ((float)Math.Sqrt(wb * wb + hb * hb + wg * wg + hg * hg + wr * wr + hr * hr)) / coeff;
                            }
                            if (dataRange.max < g)
                                dataRange.max = g;
                            gradN[jj, ii] = g;
                            dataRange.average += g;
                            if (Bytespp == 4)
                            {
                                ptrmm++;
                                ptr0m++;
                                ptrpm++;
                                ptrm0++;
                                ptrp0++;
                                ptrmp++;
                                ptr0p++;
                                ptrpp++;
                            }
                            if (resolution > 1)
                            {
                                ptrmm += jump;
                                ptr0m += jump;
                                ptrpm += jump;
                                ptrm0 += jump;
                                ptrp0 += jump;
                                ptrmp += jump;
                                ptr0p += jump;
                                ptrpp += jump;
                            }
                        }
                        ptr += vSize;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
            dataRange.average = (float)dataRange.average / (w - 1) / (h - 1);
            return new ValueMartix(1, gradN, dataRange, resolution);
        }
        unsafe ValueMartix CreateValueMartix(FilterType type, int resolution_, bool compressed, int darkLevel)
        {
            resolution = Math.Max(resolution_, 1);
            int compression = Math.Max(compressed ? resolution : 1, 1);
            int w = Width / compression;
            int h = Height / compression;
            bottomCut = byte.MaxValue * darkLevel / 50;
            relative = bottomCut > 0;
            float[,] matrix = new float[h, w];
            fixed (float* amat = &matrix[0, 0])
            {
                dataRange.min = double.MaxValue;
                jump = (compression - 1) * Bytespp;
                matrixOffset = resolution / compression;
                hSize = resolution * Bytespp;
                vSize = resolution * Stride;
                int vStep = compressed ? vSize : Stride;
                Chank[] chanks = Chank.CreateChanks(h, 500, matrixOffset, DataPtr, vStep, (IntPtr)amat, w * sizeof(float));
                try
                {
                    foreach (var chank in chanks)
                    //Parallel.ForEach(chanks, (chank) =>
                    {
                        byte* ptr = (byte*)chank.FromData;
                        float* mptr = (float*)chank.ToData;
                        for (int i = chank.StartRow; i < chank.EndRow; i++)
                        {
                            if (type == FilterType.CrossAverage)
                                ProcessCrossAverage(ptr, mptr);
                            if (type == FilterType.CrossDifference)
                                ProcessCrossDifference(ptr, mptr);
                            ptr += vStep;
                            mptr += w;
                        }
                    //});
                    };
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
#endif
                }
            }
            dataRange.average = dataRange.average / (w - 2 * matrixOffset) / (h - 2 * matrixOffset);
            return new ValueMartix(matrixOffset, matrix, dataRange, compression);
        }
        unsafe void ProcessCrossAverage(byte* ptr, float* res)
        {
            IList<Color> palette = isIndexed ? Source.Palette.Colors : null;
            int r0m, g0m, b0m, r0p, g0p, b0p, rm0, gm0, bm0, rp0, gp0, bp0, b, g, r; // 1st - y, 2nd - x
            int val;
            Color c0p, c0m, cm0, cp0, c00;
            byte* ptr00 = ptr;
            byte* ptr0m = ptr - hSize;
            byte* ptrm0 = ptr - vSize;
            byte* ptrp0 = ptr + vSize;
            byte* ptr0p = ptr + hSize;
            for (int i = matrixOffset; i < Width - matrixOffset; i++)
            {
                if (Bytespp == 1)
                {
                    b = g = r = *ptr++;
                }
                else
                {
                    b = *ptr++;
                    g = *ptr++;
                    r = *ptr++;
                }

                if (isIndexed)
                {
                    c00 = palette[*ptr00++];
                    b = c00.B;
                    g = c00.G;
                    r = c00.R;
                    if (resolution > 0)
                    {
                        c0m = palette[*ptr0m++];
                        cm0 = palette[*ptrm0++];
                        cp0 = palette[*ptrp0++];
                        c0p = palette[*ptr0p++];
                        b = 2 * b + c0p.B + c0m.B + cp0.B + cm0.B;
                        g = 2 * g + c0p.G + c0m.G + cp0.G + cm0.G;
                        r = 2 * r + c0p.R + c0m.R + cp0.R + cm0.R;
                    }
                }
                else if (Bytespp == 1)
                {
                    r = g = b = *ptr00++;
                    if (resolution > 0)
                    {
                        b0m = *ptr0m++;
                        bm0 = *ptrm0++;
                        bp0 = *ptrp0++;
                        b0p = *ptr0p++;
                        r = g = b = 2 * b + bp0 + bm0 + b0p + b0m;
                    }
                }
                else
                {
                    b = *ptr00++;
                    g = *ptr00++;
                    r = *ptr00++;
                    if (resolution > 0)
                    {
                        b0m = *ptr0m++;
                        g0m = *ptr0m++;
                        r0m = *ptr0m++;
                        bm0 = *ptrm0++;
                        gm0 = *ptrm0++;
                        rm0 = *ptrm0++;
                        bp0 = *ptrp0++;
                        gp0 = *ptrp0++;
                        rp0 = *ptrp0++;
                        b0p = *ptr0p++;
                        g0p = *ptr0p++;
                        r0p = *ptr0p++;
                        b = 2 * b + bp0 + bm0 + b0p + b0m;
                        g = 2 * g + gp0 + gm0 + g0p + g0m;
                        r = 2 * r + rp0 + rm0 + r0p + r0m;
                    }
                }
                val = b + r + g;
                if (dataRange.max < val)
                    dataRange.max = val;
                if (dataRange.min > val)
                    dataRange.min = val;
                *res++ = val;
                dataRange.average += val;
                if (Bytespp == 4)
                {   // ignoring transparency
                    ptr00++;
                    ptr0m++;
                    ptrm0++;
                    ptrp0++;
                    ptr0p++;
                }
                if (jump > 0)
                {
                    ptr00 += jump;
                    ptr0m += jump;
                    ptrm0 += jump;
                    ptrp0 += jump;
                    ptr0p += jump;
                }
            }
        }
        unsafe void ProcessCrossDifference(byte* ptr, float* res)
        {
            IList<Color> palette = isIndexed ? Source.Palette.Colors : null;
            byte r00, g00, b00, r0m, g0m, b0m, r0p, g0p, b0p, rm0, gm0, bm0, rp0, gp0, bp0;
            int wb, hb, wg, hg, wr, hr; // 1st - y, 2nd - x
            double val;
            Color c00, c0p, c0m, cm0, cp0;
            res += matrixOffset;
            byte* ptr00 = ptr + matrixOffset * hSize;
            byte* ptr0m = ptr00 - hSize;
            byte* ptrm0 = ptr00 - vSize;
            byte* ptrp0 = ptr00 + vSize;
            byte* ptr0p = ptr00 + hSize;
            for (int i = matrixOffset; i < Source.PixelWidth - matrixOffset; i++)
            {
                if (isIndexed) 
                {
                    c00 = palette[*ptr00++];
                    c0m = palette[*ptr0m++];
                    cm0 = palette[*ptrm0++];
                    cp0 = palette[*ptrp0++];
                    c0p = palette[*ptr0p++];
                    b00 = c00.B;
                    g00 = c00.G;
                    r00 = c00.R;
                    b0m = c0m.B;
                    g0m = c0m.G;
                    r0m = c0m.R;
                    bm0 = cm0.B;
                    gm0 = cm0.G;
                    rm0 = cm0.R;
                    bp0 = cp0.B;
                    gp0 = cp0.G;
                    rp0 = cp0.R;
                    b0p = c0p.B;
                    g0p = c0p.G;
                    r0p = c0p.R;
                }
                else if (Bytespp == 1)
                {
                    r00 = g00 = b00 = *ptr00++;
                    r0m = g0m = b0m = *ptr0m++;
                    rm0 = gm0 = bm0 = *ptrm0++;
                    rp0 = gp0 = bp0 = *ptrp0++;
                    r0p = g0p = b0p = *ptr0p++;
                }
                else
                {
                    b00 = *ptr00++;
                    g00 = *ptr00++;
                    r00 = *ptr00++;
                    b0m = *ptr0m++;
                    g0m = *ptr0m++;
                    r0m = *ptr0m++;
                    bm0 = *ptrm0++;
                    gm0 = *ptrm0++;
                    rm0 = *ptrm0++;
                    bp0 = *ptrp0++;
                    gp0 = *ptrp0++;
                    rp0 = *ptrp0++;
                    b0p = *ptr0p++;
                    g0p = *ptr0p++;
                    r0p = *ptr0p++;
                }
                hb = Math.Max(Math.Abs(bp0 - b00), Math.Abs(b00 - bm0));
                hg = Math.Max(Math.Abs(gp0 - g00), Math.Abs(g00 - gm0));
                hr = Math.Max(Math.Abs(rp0 - r00), Math.Abs(r00 - rm0));
                wb = Math.Max(Math.Abs(b0p - b00), Math.Abs(b00 - b0m));
                wg = Math.Max(Math.Abs(g0p - g00), Math.Abs(g00 - g0m));
                wr = Math.Max(Math.Abs(r0p - r00), Math.Abs(r00 - r0m));
                val = Math.Max(Math.Max(Math.Sqrt(wb * wb + hb * hb), Math.Sqrt(wg * wg + hg * hg)), Math.Sqrt(wr * wr + hr * hr));
                if (relative)
                {
                    int br = b00 + bp0 + b0p + bm0 + b0m;
                    br = Math.Max(br, g00 + gp0 + g0p + gm0 + g0m);
                    br = Math.Max(br, r00 + rp0 + r0p + rm0 + r0m);
                    val *= byte.MaxValue / (br / 5.0 + bottomCut);
                    val = Math.Sqrt(val);
                }
                if (dataRange.max < val)
                    dataRange.max = val;
                if (dataRange.min > val)
                    dataRange.min = val;
                *res++ = (float)val;
                dataRange.average += val;
                if (Bytespp == 4)
                {   // ignoring transparency
                    ptr00++;
                    ptr0m++;
                    ptrm0++;
                    ptrp0++;
                    ptr0p++;
                }
                if (jump > 0)
                {
                    ptr00 += jump;
                    ptr0m += jump;
                    ptrm0 += jump;
                    ptrp0 += jump;
                    ptr0p += jump;
                }
            }
        }
    }
}

