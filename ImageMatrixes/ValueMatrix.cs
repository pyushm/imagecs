using System;

namespace ImageProcessor
{
    //using System.Diagnostics;
    //static string[] MethodName()
    //{
    //    StackTrace curTrace = new StackTrace();
    //    StackFrame[] curFrame = curTrace.GetFrames();
    //    string[] mn = new string[curFrame.Length];
    //    for (int i = 0; i < curFrame.Length; i++)
    //        mn[i] = curFrame[i].GetMethod().Name;
    //    return mn;
    //}
    public struct DataRange
    {
        public double min;        // data min
        public double average;    // data average
        public double max;        // data max
        public override string ToString() { return "Range=["+ min.ToString("f1")+'-'+ max.ToString("f1")+ "] average="+ average.ToString("f1"); }
    }
    public class ValueMartix
    {
        float[,] matrix;            //[h,w]
        int compression;            // compression with respect to original
        DataRange dataRange;
        public int Width            { get { return matrix.GetLength(1); } }
        public int Height           { get { return matrix.GetLength(0); } }
        public float[,] Matrix      { get { return matrix; } }
        public DataRange DataRange  { get { return dataRange; } }
        public int Compression      { get { return compression; } }
        public ValueMartix(int offset, float[,] matrix_, DataRange dataRange_, int compression_)
        {
            matrix = matrix_;
            dataRange = dataRange_;
            compression = compression_;
            int w = Width;
            int h = Height;
            if (offset > 0)
            {
                for (int i = offset; i < w - offset; i++)
                {
                    for (int j = 0; j < offset; j++)
                        matrix[j, i] = matrix[offset, i];
                    for (int j = h - offset; j < h; j++)
                        matrix[j, i] = matrix[h - offset - 1, i];
                }
                for (int j = 0; j < h; j++)
                {
                    for (int i = 0; i < offset; i++)
                        matrix[j, i] = matrix[j, offset];
                    for (int i = w - offset; i < w; i++)
                        matrix[j, i] = matrix[j, w - offset - 1];
                }
            }
        }
        public ByteMatrix Contrast(float floor, float factor, InterpolationFunction func)
        {
            ByteMatrix bm = new ByteMatrix(Height, Width);
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                    bm.Data[j, i] = (byte)(func.Apply(1 - (Matrix[j, i] - floor) / factor) * byte.MaxValue);
            }
            //Console.WriteLine("******"+ func.ToString());
            //for (float x= floor- factor; x< floor + 2*factor; x+=0.5f)
            //    Console.WriteLine("x=" + x.ToString("f2") + " t=" + (1-(x - floor) / factor).ToString("f2") + " b=" + func.Apply(1-(x - floor) / factor).ToString("f2"));
            return bm.ExpandByInterpolation(Compression);
        }

    }
}
