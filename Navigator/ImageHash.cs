using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    [Serializable]
    public struct LSHPixel
    {   // class represents Brightness, Saturation, Hue view of colored pixel
        static byte HLSmax = 252;
		static int HLSmaxD3=HLSmax/3;
		static int HLSmaxD3M2=2*HLSmax/3;
		static int HLSmaxD6=HLSmax/6;
		static int HLSmaxD12=HLSmax/12;	
        public float Brightness                 { get { return (float)Lum / byte.MaxValue; } }	// [0-1]
        public float Saturation                 { get { if (Lum == 0) return 0; return (float)Sat / byte.MaxValue; } }	// [0-Brightness]
		public float Tint						{ get { return (float)Hue/HLSmax; } } // [0-1]
        public byte Lum { get; private set; }   // luminocity [0, byte.MaxValue]
        public byte Sat { get; private set; }   // saturation [0, lum]
        public byte Hue { get; private set; }   // hue [0, HLSmax]
        public LSHPixel(byte r, byte g, byte b)
        {
            if (r == g && r == b)
            {
                Lum = r;
                Sat = 0;
                Hue = 0;
            }
            else if (b < g)
            {
                if (r < b)			// 2	r<b<g
                {
                    Lum = g;
                    Sat = (byte)(Lum - r);
                    Hue = (byte)(HLSmaxD3 - HLSmaxD6 * (b - r) / Sat);		// [HLSmaxD6, 2*HLSmaxD6]
                }
                else if (r < g)	// 3	b<=r<g
                {
                    Lum = g;
                    Sat = (byte)(Lum - b);
                    Hue = (byte)(HLSmaxD3 + (HLSmaxD6 * (r - b) + Sat / 2) / Sat);// [2*HLSmaxD6, 3*HLSmaxD6]
                }
                else			// 4	b<g<=r
                {
                    Lum = r;
                    Sat = (byte)(Lum - b);
                    Hue = (byte)(HLSmaxD3M2 - (HLSmaxD6 * (g - b) + Sat / 2) / Sat);// [3*HLSmaxD6, 4*HLSmaxD6]
                }
            }
            else
            {
                if (r > b)			// 5	g<=b<r
                {
                    Lum = r;
                    Sat = (byte)(Lum - g);
                    Hue = (byte)(HLSmaxD3M2 + (HLSmaxD6 * (b - g) + Sat / 2) / Sat);// [4*HLSmaxD6, 5*HLSmaxD6]
                }
                else if (r > g)	// 6	g<r<=b
                {
                    Lum = b;
                    Sat = (byte)(Lum - g);
                    Hue = (byte)(HLSmax - (HLSmaxD6 * (r - g) + Sat / 2) / Sat);	// [5*HLSmaxD6, 6*HLSmaxD6]
                }
                else			// 1	r<=g<=b
                {
                    Lum = b;
                    Sat = (byte)(Lum - r);
                    Hue = (byte)((HLSmaxD6 * (g - r) + Sat / 2) / Sat);			// [0, HLSmaxD6]
                }
            }
		}
        public void Normalize(int minB, int rangeB, int minS, int rangeS)
        {   // normalizes values to 0 - byte.MaxValue
            Lum = rangeB > 0 ? (byte)(byte.MaxValue * (Lum - minB) / rangeB) : (byte)0;
            Sat = rangeS > 0 ? (byte)(byte.MaxValue * (Sat - minB) / rangeS) : (byte)0;
            Hue = rangeB > 0 ? (byte)(byte.MaxValue * Hue / HLSmax) : (byte)0;
        }
    }
    [Serializable]
    public class ImageHash
    {   // compact representation of image properties
        const int sizeM = 8;        // size of compact image matrix (sizeM x sizeM)
        const int sizeH = 16;       // histogram size
        const int bitH = 4;         // histogram element bit size (histogram bit size = sizeH * bitH)
        const byte maxValueH = (1 << bitH) - 1; // max value of histogram element = 2^bitH-1
        const int hCoef = (byte.MaxValue + 1) / sizeH; // conversion of byte into sizeH
        public readonly DateTime CreateTime; // time when info was created
        public readonly ulong Lum;   // luminosity distribution of compact image 
        public readonly ulong Sat;   // saturation distribution of compact image 
        public readonly ulong Hue;   // hue distribution of compact image 
        public readonly ulong BWM;   // BW matrix of compact image 
        public readonly byte Aspect;
        public readonly byte AverageR;
        public readonly byte AverageG;
        public readonly byte AverageB;
        public bool IsEmpty         { get { return Aspect == 0; } }
        public ImageHash(ImageFileInfo loadInfo)
        {   // builds bim matrix and calculates averages and histograms
            byte[] bwHistogram = new byte[sizeH];
            byte[] hueHistogram = new byte[sizeH];
            byte[] satHistogram = new byte[sizeH];
            LSHPixel[,] lshm = new LSHPixel[sizeM, sizeM];
            BitmapAccess bmp = null;
            try { bmp = BitmapAccess.LoadImage(loadInfo.FSPath, loadInfo.IsEncrypted); }
            catch (Exception ex) { throw new Exception("Failed load " + loadInfo.FSPath + ": " + DataAccess.Warning + ": " + ex.Message); }
            if (bmp.Origin == BitmapOrigin.LoadingFailed)
                throw new Exception("Failed load " + loadInfo.FSPath + ": " + DataAccess.Warning);
            var src = bmp.Source;
            var ra = new int[sizeM, sizeM];   // red temp matrix
            var ga = new int[sizeM, sizeM];   // green temp matrix
            var ba = new int[sizeM, sizeM];   // blue temp matrix
            var ca = new int[sizeM, sizeM];   // count temp matrix
            BitmapPalette palette = src.Palette;
            byte minB = byte.MaxValue;  // min/max brightness
            byte maxB = 0;
            byte minS = byte.MaxValue;  // min/max color saturation
            byte maxS = 0;
            int width = src.PixelWidth;
            int height = src.PixelHeight;
            Aspect = (byte)(width >= height ? 255 - 128 * height / width : 128 * width / height);
            int nPixels = width * height;
            double boxX = width / (double)sizeM;
            double boxY = height / (double)sizeM;
            try
            {
                int pixelSize = bmp.Bytespp;
                src.Lock();
                byte r, g, b;
                unsafe
                {
                    for (int i = 0; i < height; i++)
                    {
                        byte* ptr = (byte*)bmp.DataPtr + i * bmp.Stride;
                        int binY = (int)(i / boxY);
                        for (int j = 0; j < width; j++)
                        {
                            int binX = (int)(j / boxX);
                            if (pixelSize == 1)
                            {
                                if (src.Format == PixelFormats.Indexed8)
                                {
                                    Color c = palette.Colors[*ptr];
                                    ptr++;
                                    r = c.R;
                                    g = c.G;
                                    b = c.B;
                                }
                                else
                                {
                                    b = g = r = *ptr;
                                    ptr++;
                                }
                            }
                            else
                            {
                                b = *ptr;
                                ptr++;
                                g = *ptr;
                                ptr++;
                                r = *ptr;
                                ptr++;
                            }
                            if (pixelSize == 4)
                                ptr++;
                            ra[binY, binX] += r;
                            ga[binY, binX] += g;
                            ba[binY, binX] += b;
                            ca[binY, binX]++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed create ImageInfo from " + loadInfo.FSPath + ": " + ex.Message);
                return;
            }
            finally
            {
                src.Unlock();
            }
            int avR = 0;
            int avG = 0;
            int avB = 0;
            for (int j = 0; j < sizeM; j++)
            {
                for (int i = 0; i < sizeM; i++)
                {
                    avR += ra[i, j];
                    avG += ga[i, j];
                    avB += ba[i, j];
                    int c = ca[i, j];
                    lshm[i, j] = new LSHPixel((byte)(ra[i, j] / c), (byte)(ga[i, j] / c), (byte)(ba[i, j] / c));
                    if (minB > lshm[i, j].Lum)
                        minB = lshm[i, j].Lum;
                    if (maxB < lshm[i, j].Lum)
                        maxB = lshm[i, j].Lum;
                    if (minS > lshm[i, j].Sat)
                        minS = lshm[i, j].Sat;
                    if (maxS < lshm[i, j].Sat)
                        maxS = lshm[i, j].Sat;
                }
            }
            AverageR = (byte)(avR / nPixels);
            AverageG = (byte)(avG / nPixels);
            AverageB = (byte)(avB / nPixels);
            int averagB = 0;
            int rangeB = maxB - minB;
            int rangeS = maxS - minS;
            for (int i = 0; i < sizeM; i++)
            {
                for (int j = 0; j < sizeM; j++)
                {
                    lshm[i, j].Normalize(minB, rangeB, minS, rangeS);
                    averagB += lshm[i, j].Lum;
                    bwHistogram[lshm[i, j].Lum / hCoef]++;
                    satHistogram[lshm[i, j].Sat / hCoef]++;
                    hueHistogram[lshm[i, j].Hue / hCoef]++;
                }
            }
            averagB /= sizeM * sizeM;
            Lum = Histogram(bwHistogram);
            Sat = Histogram(satHistogram);
            Hue = Histogram(hueHistogram);
            BWM = LumMatrix(lshm, averagB);
            CreateTime = DateTime.Now;
        }
        ulong Histogram(byte[] hist)
        {   // values truncated by maxValueH and appended sequentially
            ulong hc = 0;
            foreach (byte b in hist)
            {
                hc <<= bitH;
                hc |= b > maxValueH ? maxValueH : b;
            }
            return hc;
        }
        ulong LumMatrix(LSHPixel[,] mat, int averagB)
        {   // pixel luminosity normalized to BW (0,1) and appended sequentially
            ulong hc = 0;
            foreach (var b in mat)
            {
                hc <<= 1;
                if(b.Lum > averagB)
                    hc |= 0x1;
            }
            return hc;
        }
        string[] ToMatrixString(ulong p)
        {
            string[] sa = new string[8];
            for (int i = 0; i < 8; i++)
            {
                byte b = (byte)(p & 255);
                sa[i] = '|' + Convert.ToString(b, 2).PadLeft(8, '0').Replace('1', ' ') + '|';
                p >>= 8;
            }
            return sa; //show as 7 ... 0
        }
        public virtual string ToBWMString()
        {
            string[] sa = ToMatrixString(BWM);
            String s = "";
            for (int i = sa.Length - 1; i >= 0; i--)
                s += sa[i] + Environment.NewLine;
            return s;
        }
        public override string ToString()
        {
            return "A=" + Aspect.ToString("D3") + " R=" + AverageR.ToString("D3") + " G=" + AverageG.ToString("D3") + " B=" + AverageB.ToString("D3") +
                " Lum=" + Lum.ToString("X16") + " Sat=" + Sat.ToString("X16") + " Hue=" + Hue.ToString("X16") + " BWM=" + BWM.ToString("X16");
        }
        public class Pattern : ImageHash
        {
            const ulong up = 0xFF;
            const ulong down = 0xFF00000000000000;
            const ulong left = 0x0101010101010101;
            const ulong right = 0x8080808080808080;
            public readonly ulong BWM1;   // BW matrix of compact image 
            public readonly ulong BWM2;   // BW matrix of compact image 
            public readonly ulong BWM3;   // BW matrix of compact image 
            public readonly ulong BWM4;   // BW matrix of compact image 
            public Pattern(string imagePath) : base(new ImageFileInfo(new FileInfo(imagePath)))
            {
                BWM1 = BWM << sizeM;
                BWM1 |= up;
                BWM2 = BWM >> sizeM;
                BWM2 |= down;
                BWM3 = BWM << 1;
                BWM3 |= left;
                BWM4 = BWM >> 1;
                BWM4 |= right;
            }
            public int ExactPixelDif(ImageHash info)
            {
                int diff = 0;
                ulong d = info.BWM ^ BWM;
                while (d != 0)
                {
                    if ((d & 0x1) == 0x1)
                        diff++;
                    d >>= 1;
                }
                return diff;
            }
            public int ApprxPixelDif(ImageHash info)
            {
                {
                    int dif = Math.Min(BitCount(info.BWM ^ BWM), BitCount(info.BWM ^ BWM1));
                    dif = Math.Min(dif, BitCount(info.BWM ^ BWM2));
                    dif = Math.Min(dif, BitCount(info.BWM ^ BWM3));
                    return Math.Min(dif, BitCount(info.BWM ^ BWM4));
                }
            }
            int BitCount(ulong d)
            {
                int dif = 0;
                while (d != 0)
                {
                    if ((d & 0x1) == 0x1)
                        dif++;
                    d >>= 1;
                }
                return dif;
            }
            public int ExactLumDif(ImageHash inf) { return ExactHistogramDif(inf.Lum, Lum) / 4; }
            public int ExactSatDif(ImageHash inf) { return ExactHistogramDif(inf.Sat, Sat) / 4; }
            public int ExactHueDif(ImageHash inf) { return ExactHistogramDif(inf.Hue, Hue) * 2; }
            int ExactHistogramDif(ulong hist1, ulong hist2)
            {
                int sp = 0;
                for (int i = 0; i < sizeH; i++)
                {
                    sp += Math.Abs((int)(hist1 & maxValueH) - (int)(hist2 & maxValueH));
                    hist1 >>= bitH;
                    hist2 >>= bitH;
                }
                return sp;
            }
            public int ApprxLumDif(ImageHash inf) { return ApprxHistogramDif(inf.Lum, Lum) / 4; }
            public int ApprxSatDif(ImageHash inf) { return ApprxHistogramDif(inf.Sat, Sat) / 4; }
            public int ApprxHueDif(ImageHash inf) { return ApprxHistogramDif(inf.Hue, Hue) * 2; }
            int ApprxHistogramDif(ulong hist1, ulong hist2)
            {
                int sp = 0;
                ulong hist3 = hist2 >> bitH;
                ulong hist4 = hist2 << bitH;
                for (int i = 0; i < sizeH; i++)
                {
                    int ih = (int)(hist1 & maxValueH);
                    int d = Math.Min(Math.Abs(ih - (int)(hist2 & maxValueH)), Math.Abs(ih - (int)(hist3 & maxValueH)));
                    sp += Math.Min(d, Math.Abs(ih - (int)(hist4 & maxValueH)));
                    hist1 >>= bitH;
                    hist2 >>= bitH;
                    hist3 >>= bitH;
                    hist4 >>= bitH;
                }
                return sp;
            }
            public override string ToBWMString()
            {
                string[] sa = ToMatrixString(BWM);
                string[] sa1 = ToMatrixString(BWM1);
                string[] sa2 = ToMatrixString(BWM2);
                string[] sa3 = ToMatrixString(BWM3);
                string[] sa4 = ToMatrixString(BWM4);
                String s = "";
                for (int i = sa.Length - 1; i >= 0; i--)
                    s += sa[i] + sa1[i] + sa2[i] + sa3[i] + sa4[i] + Environment.NewLine;
                return s;
            }
            public string ToDifString(ImageHash info)
            {
                int dA = Math.Abs(info.Aspect - Aspect);
                int dR = Math.Abs(info.AverageR - AverageR);
                int dG = Math.Abs(info.AverageG - AverageG);
                int dB = Math.Abs(info.AverageB - AverageB);
                int adL = ApprxLumDif(info);
                int adS = ApprxSatDif(info);
                int adH = ApprxHueDif(info);
                int edL = ExactLumDif(info);
                int edS = ExactSatDif(info);
                int edH = ExactHueDif(info);
                int adP = ApprxPixelDif(info);
                int edP = ExactPixelDif(info);
                return dA.ToString() + '\t' + dR + '\t' + dG + '\t' + dB + '\t' + edL + '\t' + adL + '\t' + edS + '\t' + adS + '\t' + edH + '\t' + adH + '\t' + edP + '\t' + adP;
            }
        }
        public class Comparer : IComparer<ImageHash>
        {
            public readonly int MaxDifference; // all values normalized to byte.MaxValue. it is allowable measurement difference range
            Pattern pattern;
            bool ExactComparison { get; set; }
            public Pattern Pattern { get { return pattern; } }
            public Comparer(int errorLevel)
            {
                ExactComparison = true;
                MaxDifference = errorLevel;
            }
            public void SetPattern(string imagePath) { pattern = new Pattern(imagePath); }
            public int HashDifference(ImageHash info)
            {
                int partMax = MaxDifference / 4;
                int dif = Math.Abs(info.Aspect - pattern.Aspect);
                if (dif > partMax/2)
                    return int.MaxValue;
                dif += Math.Abs(info.AverageR - pattern.AverageR) + Math.Abs(info.AverageG - pattern.AverageG) + Math.Abs(info.AverageB - pattern.AverageB);
                if (dif > partMax)
                    return int.MaxValue;
                int el, es, eh, ep;
                if (ExactComparison)
                {
                    el = pattern.ExactLumDif(info);
                    if(el > partMax)
                        return int.MaxValue;
                    es = pattern.ExactSatDif(info);
                    if (es > partMax)
                        return int.MaxValue;
                    eh = pattern.ExactHueDif(info);
                    if (eh > partMax)
                        return int.MaxValue;
                    ep = pattern.ExactPixelDif(info);
                    if (ep > partMax)
                        return int.MaxValue;
                }
                else
                {
                    el = pattern.ApprxLumDif(info);
                    if (el > partMax)
                        return int.MaxValue;
                    es = pattern.ApprxSatDif(info);
                    if (es > partMax)
                        return int.MaxValue;
                    eh = pattern.ApprxHueDif(info);
                    if (eh > partMax)
                        return int.MaxValue;
                    ep = pattern.ApprxPixelDif(info);
                    if (ep > partMax)
                        return int.MaxValue;
                }
                return dif + el + es + eh + ep;
            }
            int IComparer<ImageHash>.Compare(ImageHash l1, ImageHash l2)
            {
                return 0;
            }
        }
    }
}
    