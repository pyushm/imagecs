using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Text;

namespace ImageProcessor
{
    public class Convolution
    {
        public ByteMatrix original;
        public ByteMatrix filter;
        int offset;
        int total;
        int origWidth;
        int origHeight;
        int fltWidth;
        int fltHeight;
        int xOff;
        int yOff;
        public Convolution(ByteMatrix original_, ByteMatrix filter_)
        {
            original = original_;
            filter = filter_;
            total = 0;
            foreach (byte v in filter.Data)
                total += v;
            offset = filter.ValueOffset;
            origWidth = original.Width;
            origHeight = original.Height;
            fltWidth = filter.Width;
            fltHeight = filter.Height;
            xOff = fltWidth / 2;
            yOff = fltHeight / 2;
        }
        public int Apply(int j, int i)
        {
            int v = 0;
            int s = 0;
            for (int fi = 0; fi < fltWidth; fi++)
            {
                int ii = i - xOff + fi;
                if (ii >= 0 && ii < origWidth)
                {
                    if (offset != 0)
                    {
                        for (int fj = 0; fj < fltHeight; fj++)
                        {
                            int jj = j - yOff + fj;
                            if (jj >= 0 && jj < origHeight)
                            {
                                v += original[jj, ii] * filter[fi, fj];
                                s += original[jj, ii];
                            }
                        }
                    }
                    else
                    {
                        for (int fj = 0; fj < fltHeight; fj++)
                        {
                            int jj = j - yOff + fj;
                            if (jj >= 0 && jj < origHeight)
                                v += original[jj, ii] * filter[fi, fj];
                        }
                    }
                }
            }
            return offset==0 ? v / total - original[j, i] : v / total + offset * s / fltHeight / fltWidth;
        }
    }
    public struct ByteMatrixPair
    {
        public ByteMatrix original;
        public ByteMatrix transformed;
    }
    public class ByteMatrix
    {   // operation on byte[height, width]
        byte[,] bval = null;
        Convolution convolution = null;
        int xBase = 0;  // left-most side of matrix
        int yBase = 0;  // top-most side of matrix
        int offset = 0; // value offset: value = bval[,]+offset
        public int ValueOffset { get { return offset; } }
        public int RowOffset { get { return xBase; } }
        public int RowStart { get { return yBase; } }
        public int RowEnd { get { return yBase + Height; } }
        public int Width { get { return bval.GetLength(1); } }
        public int Height { get { return bval.GetLength(0); } }
        public byte[,] Data { get { return bval; } }
        public byte this[int i, int j] { get { return bval[i, j]; } set { bval[i, j] = value; } }
        public static ByteMatrix CreateConeFilter(double radius) // cone with max byte.MaxValue and base radius
        {
            double norm = Math.Max(radius + 0.5, 1);
            int l = Math.Max(0, (int)(norm - 0.2));    // cone base half size
            int size =2 * l + 1;
            ByteMatrix filter = new ByteMatrix(size, size);
            for (int i = 0; i <= l; i++)
            {
                for (int j = 0; j <= l; j++)
                {
                    double d = Math.Sqrt(i * i + j * j);
                    byte v = (byte)(byte.MaxValue * Math.Max(0, (1 - d / norm)));
                    filter[l + i, l + j] = filter[l - i, l + j] = filter[l + i, l - j] = filter[l - i, l - j] = v;
                }
            }
            return filter;
        }
        public static ByteMatrix CreateWaveletFilter(int l) // half size
        {
            l = Math.Max(1, Math.Min(3, l));
            int size = 2 * l + 1;
            ByteMatrix filter = new ByteMatrix(size, size, -1);
            byte v=1;
            for (int i = 0; i <= l; i++)
            {
                for (int j = 0; j <= l; j++)
                {
                    int k = i + j;
                    if (l == 1)
                        v = (byte)(k == 0 ? 5 : k == 1 ? 1 : 0);
                    else if (l == 2)
                        v = (byte)(k == 0 ? 5 : k == 1 ? 3 : k > 2 ? 0 : i == j ? 2 : 0);
                    else if (l == 3)
                        v = (byte)(k == 0 ? 5 : k == 1 ? 3 : k == 6 ? 1 : k == 5 ? 0 : k == 4 ? 0 : k == 2 ? (i == j ? 3 : 2) : (i == 0 || j == 0 ? 0 : 1));
                    filter[l + i, l + j] = filter[l - i, l + j] = filter[l + i, l - j] = filter[l - i, l - j] = v;
                }
            }
            return filter;
        }
        public ByteMatrix() { bval = new byte[0, 0]; }
        public ByteMatrix(byte[,] bval_) { bval = bval_ == null ? new byte[0, 0] : bval_; }
        public ByteMatrix(byte[,] bval_, int yMin, int xMin) { bval = bval_ == null ? new byte[0, 0] : bval_; yBase=yMin; xBase=xMin; }
        public ByteMatrix(int height, int width) { bval = new byte[height, width]; }
        public ByteMatrix(int height, int width, int offset_) { bval = new byte[height, width]; offset = offset_; }
        public void Set(int height, int width) { bval = new byte[height, width]; }
        public void SetConvolution(ByteMatrix filter) { convolution = new Convolution(this, filter); }
        public ByteMatrix ExpandByInterpolation(int compression)
        {
            if (compression < 2 || compression > 3)
                return this;
            int nh = Height;
            int nw = Width;
            if (nh <= 2 && nw <= 2)
                return this;
            ByteMatrix fi=new ByteMatrix((nh - 1) * compression + 1, (nw - 1) * compression + 1);
            for (int i = 0; i < nh - 1; i++)
            {
                int ii = i * compression;
                byte f00 = bval[i, 0];
                byte f10 = bval[i + 1, 0];
                for (int j = 0; j < nw - 1; j++)
                {
                    byte f01 = bval[i, j + 1];
                    byte f11 = bval[i + 1, j + 1];
                    int jj = j * compression;
                    if (compression == 2)
                    {
                        fi[ii, jj] = f00;
                        fi[ii + 1, jj] = (byte)((f00 + f10) / 2);
                        fi[ii, jj + 1] = (byte)((f00 + f01) / 2);
                        fi[ii + 1, jj + 1] = (byte)((f00 + f01 + f10 + f11) / 4);
                    }
                    else
                    {
                        fi[ii, jj] = f00;
                        fi[ii + 1, jj] = (byte)((2 * f00 + f10) / 3);
                        fi[ii + 2, jj] = (byte)((f00 + 2 * f10) / 3);
                        fi[ii, jj + 1] = (byte)((2 * f00 + f01) / 3);
                        fi[ii + 1, jj + 1] = (byte)((4 * f00 + 2 * (f01 + f10) + f11) / 9);
                        fi[ii + 2, jj + 1] = (byte)((4 * f10 + 2 * (f00 + f11) + f01) / 9);
                        fi[ii, jj + 2] = (byte)((f00 + 2 * f01) / 3);
                        fi[ii + 1, jj + 2] = (byte)((4 * f01 + 2 * (f00 + f11) + f10) / 9);
                        fi[ii + 2, jj + 2] = (byte)((4 * f11 + 2 * (f01 + f10) + f00) / 9);
                    }
                    f00 = f01;
                    f10 = f11;
                }
            }
            return fi;
        }
        public void FillVoids(byte val)
        {   // fills internal zero bytes with 'val'
            for(int ri=0; ri< Height; ri++)
            {
                int left = Width;
                for(int j=0; j< Width; j++)
                    if(bval[ri,j]>0)
                    {
                        left=j+1;
                        break;
                    }
                if (left == Width)
                    continue;
                int right = left;
                for(int j= Width-1; j> left; j--)
                    if(bval[ri, j] > 0)
                    {
                        right = j;
                        break;
                    }
                for (int j = left; j < right; j++)
                    if (bval[ri, j] == 0)
                        bval[ri, j] = val;
            }
        }
        public ByteMatrix SmoothingWithMidlevelCut(ByteMatrix filter, int sizePerChank)
        {
            FillVoids(byte.MaxValue);
            ByteMatrix ret = ConeSmoothing(filter, sizePerChank);
            unsafe
            {
                fixed(byte* begin = &ret.Data[0, 0])
                {
                    byte* end = begin + Width * Height;
                    byte* p = begin;
                    while (p < end)
                    {   // cut at the middle of transparency filter
                        *p = (byte)Math.Max(0, *p * 2 - byte.MaxValue);
                        p++;
                    }
                }
            }
            return ret;
        }
        public ByteMatrix ConeSmoothing(ByteMatrix filter, int sizePerChank)
        {
            return CreateByConvolution(filter, sizePerChank, byte.MaxValue, false); // no mixing with original
        }
        public ByteMatrix WaveletContrasting(ByteMatrix filter, int sizePerChank, int mixLevel, bool inverse)
        {
            return CreateByConvolution(filter, sizePerChank, 5*mixLevel, inverse);
        }
        ByteMatrix CreateByConvolution(ByteMatrix filter, int rowPerChank, int mixLevel, bool inverse)
        {   // convolution with 'filter' corrected by average 'offset'; mixed with original if 'mixLevel'>0; byte.MaxValue-convolution if 'inverce'
            if (filter.Height <= 1 && filter.Width <= 1)
                return this;
            convolution = new Convolution(this, filter);
            ByteMatrix filtered = new ByteMatrix(Height, Width);
            Chank[] chanks = Chank.CreateChanks(Height, rowPerChank, null, null);
            //foreach (var chank in chanks)
            Parallel.ForEach(chanks, (chank) =>
            {
                for (int j = chank.StartRow; j < chank.EndRow; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        int conv = convolution.Apply(j, i);
                        filtered[j, i] = (byte)Math.Max(0, Math.Min(byte.MaxValue, (mixLevel != 0 ? bval[j, i] + mixLevel * conv / byte.MaxValue :
                                                inverse ? byte.MaxValue - conv : conv)));
                    }
                }
            });
            //};
            return filtered;
        }
        void UpdateByConvolution(ByteMatrix transformed, Rectangle updateRect, ByteMatrix mask)
        {   // applying convolution with 'mask' intensity 
            if (convolution == null)
                return;
            ByteMatrix filtered = new ByteMatrix(Height, Width);
            int top = Math.Max(0, updateRect.Top);
            int bottom = Math.Max(Height, updateRect.Bottom);
            int left = Math.Max(0, updateRect.Left);
            int right = Math.Max(Width, updateRect.Right);
            for (int j = top; j < bottom; j++)
            {
                for (int i = left; i < right; i++)
                {
                    if (mask[j, i] > 0)
                    {
                        int conv = convolution.Apply(j, i);
                        filtered[j, i] = (byte)Math.Max(0, Math.Min(byte.MaxValue, bval[j, i] + mask[j, i] * conv / byte.MaxValue));
                    }
                }
            }
        }
        public ByteMatrix MedianSmoothing(int halfSize, int sizePerChank, byte cutLevel)
        {
            int apt_x = halfSize;
            int apt_y = halfSize;
            int lx = Width;
            int ly = Height;
            if (lx < apt_x || ly < apt_y)
                return new ByteMatrix(bval);
            ByteMatrix dst = new ByteMatrix(ly, lx);
            byte border= byte.MaxValue;
            for (int x = 0; x < lx; ++x)
                dst[0, x] = dst[ly - 1, x] = border;
            for (int y = 1; y < ly-1; ++y)
            {
                int less, num;
                int[] hst = new int[byte.MaxValue + 1];
                byte med = SetHistogram(hst, 0, y, apt_x, apt_y, out less, out num);
                dst[y, 0] = dst[y, lx - 1] = border;
                for (int x = 1; x < lx-1; ++x)
                {
                    if (x - apt_x - 1 >= 0)
                    { // removing left column
                        for (int yy = y - apt_y; yy <= y + apt_y; ++yy)
                        {
                            if (yy >= 0 && yy < ly)
                            {
                                --num;
                                int v = bval[yy, x - apt_x - 1];
                                --hst[v];
                                if (v < med) --less;
                            }
                        }
                    }
                    if (x + apt_x < lx)
                    { // adding right column
                        for (int yy = y - apt_y; yy <= y + apt_y; ++yy)
                        {
                            if (yy >= 0 && yy < ly)
                            {
                                ++num;
                                int v = bval[yy, x + apt_x];
                                ++hst[v];
                                if (v < med) ++less;
                            }
                        }
                    }
                    int num2 = (num - 1) / 2;
                    if (less > num2)
                    {
                        while (less > num2)
                        {
                            --med;
                            less -= hst[med];
                        }
                    }
                    else
                    {
                        while (less + (int)hst[med] <= num2)
                        {
                            less += hst[med];
                            ++med;
                        }
                    }
                    dst[y, x] = Math.Abs(bval[y, x] - med) > cutLevel ? med : bval[y, x];
                }
            }
            if (cutLevel > 0)
                return dst;
            ByteMatrix filter = ByteMatrix.CreateConeFilter((apt_x + apt_y) / 3.0);
            return dst.ConeSmoothing(filter, sizePerChank);
        }
        byte SetHistogram(int[] hst, int x0, int y0, int apt_x, int apt_y, out int less, out int num)
        {
            num = 0;
            for (int dy = -apt_y; dy <= apt_y; ++dy)
            {
                int y = y0 + dy;
                for (int dx = -apt_x; dx <= apt_x; ++dx)
                {
                    int x = x0 + dx;
                    if (x >= 0 && x < Width && y >= 0 && y < Height)
                    {
                        ++hst[bval[y, x]];
                        ++num;
                    }
                }
            }
            byte num2 = (byte)((num - 1) / 2);
            byte med_idx = 0;
            less = 0;
            while (less + hst[med_idx] <= num2)
            {
                less += hst[med_idx];
                ++med_idx;
            }
            return med_idx;
        }
        public override string ToString()
        {
            StringBuilder res = new StringBuilder("base=[" + xBase + ',' + yBase + ']' + Environment.NewLine);
            for (int j = 0; j < Height; j++)
            {
                byte[] row = new byte[Width];
                for (int i = 0; i < Width; i++)
                    row[i] = bval[j, i];
                res.Append(BitConverter.ToString(row));
                res.Append(Environment.NewLine);
            }
            return res.ToString();
        }
        public string ToBWString()
        {
            StringBuilder res = new StringBuilder();
            for (int j = 0; j < Height; j++)
            {
                char[] row = new char[Width];
                for (int i = 0; i < Width; i++)
                    row[i] = bval[j, i] == 0 ? ' ' : '*';
                res.Append(row);
                res.Append(Environment.NewLine);
            }
            return res.ToString();
        }
    }
}
