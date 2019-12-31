using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ImageProcessor
{
    public sealed class VisualLayerBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return typeName.IndexOf("VisualLayerType") != -1 ? typeof(VisualLayerType) :
                   typeName.IndexOf("VisualLayerData") != -1 ? typeof(VisualLayerData) :
                   typeName.IndexOf("StorePointCollection") != -1 ? typeof(StorePointCollection) :
                   typeName.IndexOf("StorePoint") != -1 ? typeof(StorePoint) : null;
        }
    }
    public enum VisualLayerType
    {   // add only at the end. overvise stored data will be lost
        Bitmap,       // contains bitmap
        Drawing,      // contains strokes
        Derivative,   // bitmap image derived from source image (transform applies to derivation)
        Tool
    }
    [Serializable]
    public class VisualLayerData
    {   // changing fields of this class will make storred data unreadable (cleate a new class instead)
        public static VisualLayerData[] LayersFromFile(string fullPath, bool encrypted)
        {
            FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryFormatter f = new BinaryFormatter();
            f.Binder = new VisualLayerBinder();
            object o = f.Deserialize(fs);
            VisualLayerData[] vlda = (VisualLayerData[])o;
            for (int ind = 0; ind < vlda.Length; ind++)
            {
                if (vlda[ind].Data == null)
                    continue;
                byte[] dec = DataAccess.ReadBytes(vlda[ind].Data, encrypted);
                vlda[ind].SetData(dec);
            }
            return vlda;
        }
        public static byte[] LoadThumbnail(string fullPath, bool encrypted)
        {
            FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryFormatter f = new BinaryFormatter();
            f.Binder = new VisualLayerBinder();
            object o = f.Deserialize(fs);
            VisualLayerData[] vlda = (VisualLayerData[])o;
            int ind = -1;
            int indb = -1;
            for (int i = 0; i < vlda.Length; i++)
            {
                if (vlda[i].IsThumbnail)
                    ind = i;
                if (vlda[i].IsBitmap && indb<0)
                    indb = i;
            }
            if (ind < 0)
                ind = indb;
            if (ind < 0)
                return null;
            if (vlda[ind].Data == null)
                return null;
            byte[] dec = DataAccess.ReadBytes(vlda[ind].Data, encrypted);
            return dec;
        }
        VisualLayerType mode;
        Size size;
        Matrix matrix;
        byte[] data;
        string name;
        public bool IsThumbnail { get { return mode == VisualLayerType.Tool; } }
        public bool IsBitmap { get { return mode == VisualLayerType.Bitmap; } }
        public bool IsDrawing { get { return mode == VisualLayerType.Drawing; } }
        public string Name { get { return name!=null && name.Length>0 ? name : mode.ToString(); } }
        public byte[] Data { get { return data; } }
        public IntSize PixelSize { get { return new IntSize((int)size.Width, (int)size.Height); } }
        public VisualLayerData(VisualLayerType t, string n, IntSize sz, MatrixControl m, byte[] d)
        {
            size = sz.Size;
            name = n;
            mode = t;
            matrix = new Matrix(m.RenderScale * m.Flip, m.Angle, m.Aspect, m.Shear, m.Center.X, m.Center.Y);
            data = d;
        }
        public void SetData(byte[] d) { data = d; }
        public BitmapAccess GetImage() { return mode == VisualLayerType.Bitmap ? new BitmapAccess(new MemoryStream(data), 0) : null; }
        public List<FlexiblePolygon> GetStrokes()
        {
            if (mode != VisualLayerType.Drawing)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new VisualLayerBinder();
            StorePointCollection[] strokeData = (StorePointCollection[])bf.Deserialize(new MemoryStream(data));
            List<FlexiblePolygon> polygons = new List<FlexiblePolygon>(strokeData.Length);
            foreach (var spc in strokeData)
            {
                StorePointDecoder decoder = new StorePointDecoder(spc);
                polygons.Add(new FlexiblePolygon(decoder, PixelSize));
            }
            return polygons;
        }
        public MatrixControl MatrixControl { get { return new MatrixControl(Math.Sign(matrix.M11), Math.Abs(matrix.M11), matrix.M12, matrix.M21, matrix.M22, new Point(matrix.OffsetX, matrix.OffsetY)); } }
        public override string ToString()
        {
            object[] oa = new object[] { mode, (int)size.Width, (int)size.Height, name, data.Length };
            return string.Format("mode={0} name={3} dataLength={4} size={1}x{2}", oa);
        }
    }
}
