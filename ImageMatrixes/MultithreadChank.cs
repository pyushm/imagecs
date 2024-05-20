using System;
using System.Windows.Media.Imaging;


namespace ImageProcessor
{
    public struct Chank
    {   // chank represents set of data rows from StartRow to EndRow 
        public int StartRow { get; private set; }
        public int EndRow { get; private set; }
        public IntPtr FromData { get; private set; }    // start of from row data
        public IntPtr ToData { get; private set; }      // start of to row data
        public int FromStride { get; private set; }
        public int ToStride { get; private set; }
        public static Chank[] CreateChanks(int height, int rowsPerChank, WriteableBitmap from, WriteableBitmap to)
        {
            IntPtr fromBuffer = from != null ? from.BackBuffer : IntPtr.Zero;
            IntPtr toBuffer = to != null ? to.BackBuffer : IntPtr.Zero;
            int fromStride = from != null ? from.BackBufferStride : 0;
            int toStride = to != null ? to.BackBufferStride : 0;
            return CreateChanks(height, rowsPerChank, 0, fromBuffer, fromStride, toBuffer, toStride);
        }
        public static Chank[] CreateChanks(int height, int rowsPerChank, int margin, IntPtr fromBuffer, int fromStride, IntPtr toBuffer, int toStride)
        {
            int count = Math.Min(height / rowsPerChank + 1, 8);
            ushort rows = (ushort)((height - 1) / count + 1);
            Chank[] chanks = new Chank[count];
            ushort[] rowStart = new ushort[count + 1];
            for (ushort i = 0; i <= count; i++)
                rowStart[i] = (ushort)(i == 0 ? margin : i == count ? height - margin : i * rows);
            for (ushort i = 0; i < count; i++)
            {
                chanks[i].FromStride = fromStride;
                chanks[i].ToStride = toStride;
                chanks[i].StartRow = rowStart[i];
                chanks[i].EndRow = rowStart[i + 1];
                chanks[i].FromData = fromBuffer + rowStart[i] * fromStride;
                chanks[i].ToData = toBuffer + rowStart[i] * toStride;
            }
            return chanks;
        }
        public override string ToString()
        {
            return "StartRow=" + StartRow + " EndRow=" + EndRow + " FromData=" + FromData + " ToData=" + ToData;
        }
    }
}
