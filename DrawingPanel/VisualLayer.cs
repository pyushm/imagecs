using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using ShaderEffects;

namespace ImageProcessor
{
    public enum ToolMode
    {
        None,       // no tool - basic geometry transform
        Distortion,
        FreeSelection,
        RectSelection,
        StrokeEdit,
        InfoImage,
        Crop,
        Color,
        Morph,
    }
    public class VisualLayer : UIElement
    {   // collection of visual items comprizing one UIElement (similar to DrawingGroup, but working with DependencyProperties)
        protected VisualCollection children;
        protected VisualLayerType type;       // bitmap derivative 
        protected ColorTransform colorTransform = new ColorTransform();
        public MatrixControl MatrixControl { get; private set; }
        public List<MorthPoint> morthPoints = new List<MorthPoint>();
        public virtual void SetEffectParameters(double weight, double middle, double resolution) { }
        public virtual EffectType DerivativeType { get { return EffectType.None; } }
        bool deleting = false;      // marked for delayed deletion (do not display in layer list)
        public string Name          { get; set; }
        public bool IsImage         { get { return this as BitmapLayer != null; } }
        public IntSize LayoutSize   { get; protected set; } // image size used by layout system 
        public VisualLayerType Type { get { return type; } }
        public bool Deleted         { get { return deleting; } set { deleting = value; } }
        public ColorTransform ColorTransform { get { return colorTransform; } set { colorTransform.CopyFrom(value); } }
        public VisualLayer(string name_)
        {
            Name = name_;
            children = new VisualCollection(this);
        }
        public int AddVisual(Visual v) { return children.Add(v); }
        public void RemoveVisual(Visual v) { children.Remove(v); }
        public void Clear() { children.Clear(); }
        public void SwitchSideSize() { LayoutSize = new IntSize(LayoutSize.Height, LayoutSize.Width); }
        protected override int VisualChildrenCount { get { return children.Count; } }
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= VisualChildrenCount)
                throw new ArgumentOutOfRangeException();
            return children[index];
        }
        protected override Size MeasureCore(Size s) { return LayoutSize.Size; }
        internal void SetStoredMatrixContro(VisualLayerData stored) { MatrixControl = stored.MatrixControl; }
        public VisualLayerData CreateVisualLayerData(byte[] data) { return new VisualLayerData(Type, Name, LayoutSize, MatrixControl, data); }
        public byte[] SerializeImage(BitmapEncoder bitmapEncoder)
        {
            byte[] data;
            using (MemoryStream dataStream = new MemoryStream())
            {
                if (Type == VisualLayerType.Bitmap)
                {
                    BitmapLayer bl = this as BitmapLayer;
                    RenderTargetBitmap rtb = new RenderTargetBitmap(LayoutSize.Width, LayoutSize.Height, 96, 96, PixelFormats.Default);
                    RenderToBitmap(rtb);
                    bitmapEncoder.Frames.Add(BitmapFrame.Create(rtb));
                    bitmapEncoder.Save(dataStream);
                }
                if (Type == VisualLayerType.Drawing)
                {
                    DrawingLayer sl = this as DrawingLayer;
                    if (sl != null)
                    {
                        List<FlexiblePolygon> polygons = sl.Polygons;
                        StorePointCollection[] sda = new StorePointCollection[polygons.Count];
                        for (int i = 0; i < sda.Length; i++)
                            sda[i] = new StorePointCollection(polygons[i]);
                        BinaryFormatter f = new BinaryFormatter();
                        f.Serialize(dataStream, sda);
                    }
                }
                data = dataStream.ToArray();
            }
            return data;
        }
        public void RenderToBitmap(RenderTargetBitmap rtb)
        {
            for (int i = 0; i < VisualChildrenCount; i++)
                rtb.Render(GetVisualChild(i));
        }
        public virtual bool TransformColor() { return false; }
        public void SetScale(double s) { MatrixControl.RenderScale = s; }
        public void InitializeTransforms(VisualLayer renderSrc)
        {
            MatrixControl = renderSrc.MatrixControl.Clone();
            RenderTransform = renderSrc.RenderTransform.Clone();
        }
        public void InitializeTransforms(double w, double h, double scale, MatrixControl matrixControl)
        {
            if (scale < 0)      // auto scale limited by -scale
                scale = Math.Min(Math.Min(w / LayoutSize.Width, h / LayoutSize.Height), -scale);
            double offsetX = w / 2 - LayoutSize.Width * scale / 2;
            double offsetY = h / 2 - LayoutSize.Height * scale / 2;
            RenderTransform = new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY);
            if (matrixControl == null)
            {
                MatrixControl = DerivativeType == EffectType.ViewPoint ? new DistortionControl() : new MatrixControl();
                MatrixControl.Center = new Point(w / 2, h / 2);
                MatrixControl.RenderScale = scale;
            }
            else
            {
                //MatrixControl.MoveCenter(new Vector(w / 2 - MatrixControl.Center.X, h / 2 - MatrixControl.Center.Y));
                UpdateRenderTransform();
                Matrix m = RenderTransform.Value;
                m.Translate(MatrixControl.Center.X - w / 2, MatrixControl.Center.Y - h / 2);
                RenderTransform = new MatrixTransform(m);
            }
        }
        public void UpdateRenderTransform() // RenderTransform built from MatrixControl and keeps MatrixControl.Center at the same location
        {
            Matrix rm = RenderTransform.Value;
            rm.Invert();
            Point ip = rm.Transform(MatrixControl.Center);
            Matrix nrt = MatrixControl.GeometryMatrix;
            nrt.Scale(MatrixControl.RenderScale, MatrixControl.RenderScale);
            Point tp = nrt.Transform(ip);
            nrt.Translate(MatrixControl.Center.X - tp.X, MatrixControl.Center.Y - tp.Y);
            RenderTransform = new MatrixTransform(nrt);
        }
        public void SetResizedRenderTransform(Vector centerShift, double scaleCoef, double dAngle)
        {
            Translate(centerShift);
            MatrixControl.RenderScale *= scaleCoef;
            MatrixControl.RotateAngle(dAngle);
            UpdateRenderTransform();
        }
        public void Flip()
        {
            MatrixControl.FlipX();
            UpdateRenderTransform();
        }
        public void Translate(Vector v)
        {
            MatrixControl.MoveCenter(v);
            Matrix m = RenderTransform.Value;
            m.Translate(v.X, v.Y);
            RenderTransform = new MatrixTransform(m);
        }
        public MorthPoint GetLastMorthPoint() { int nmp = morthPoints.Count; return nmp > 0 ? morthPoints[nmp - 1] : null; }
        public void AddMorthPoint(MorthPoint mp) { morthPoints.Add(mp); }
        public override string ToString() { return string.Format("mode={0} name={1} size={2}x{3}", type, Name, LayoutSize.Width, LayoutSize.Height); }
        public string ToTransformString() { return "MatrixControl: " + MatrixControl.ToString() + " RenderTransform: " + RenderTransform.Value.ToString(); }
    }
    public class DrawingLayer : VisualLayer
    {
        List<FlexiblePolygon> polygons;
        Brush editBrush = null;
        public List<FlexiblePolygon> Polygons { get { return polygons; } }
        public DrawingLayer(string name, IntSize s, List<FlexiblePolygon> polygons_) : base(name)
        {
            type = VisualLayerType.Drawing;
            LayoutSize = s;
            polygons = polygons_;
            //new List<FlexiblePolygon>(strokes.Length);
            //foreach (var spc in strokes)
            //{
            //    StrokePointDecoder decoder = new StrokePointDecoder(spc);
            //    polygons.Add(new FlexiblePolygon(decoder, LayoutSize));
            //}
            AddStrokes(polygons);
        }
        public DrawingLayer(string name, IntSize s) : base(name)
        {
            type = VisualLayerType.Drawing;
            LayoutSize = s;
            polygons = new List<FlexiblePolygon>();
        }
        public void AddStrokes(List<FlexiblePolygon> strokes)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            foreach (var ss in strokes)
                ss.Draw(drawingContext, editBrush, ss.Pen);
            drawingContext.Close();
            children.Add(drawingVisual);
        }
    }
    public class BitmapLayer : VisualLayer  // layers of this type shown on the layer list
    {
        protected BitmapAccess image = null;         // current image
        public virtual BitmapAccess Image { get { return image; } }
        public BitmapLayer(string name) : base(name) { }    // for derivation
        public BitmapLayer(string name, BitmapAccess ba) : base(name) { SetImage(ba, -1); }
        public BitmapLayer(string name, BitmapAccess ba, int edge) : base(name) { SetImage(ba, edge); }
        public void UpdateImage() { image = new BitmapAccess(Image.Source.Clone(), Image.Origin); }
        public void SetImage(BitmapAccess ba, int transparentEdge)
        {
            if (ba == null)
                return;
            type = VisualLayerType.Bitmap;
            image = ba;
            LayoutSize = new IntSize(image.Width, image.Height);
            if (transparentEdge >= 0)
            {
                ByteMatrix transparencyMask = ba.TransparencyMask();
                if (transparencyMask != null)
                {
                    ByteMatrix filter = ByteMatrix.CreateConeFilter(transparentEdge);
                    transparencyMask = transparencyMask.SmoothingWithMidlevelCut(filter, 120);
                    //Debug.WriteLine("transparencyMask *******" + Environment.NewLine + transparencyMask.ToString());
                    image.UpdateTransparency(transparencyMask); // works only for Pbgra32
                }
                Color borderColor = image.BorderColor();
                if (borderColor != ColorTransform.ColorNull)
                {   //apply transparent edge
                    bool borderSet = false;
                    colorTransform = new ColorTransform(borderColor);
                    transparencyMask = new ByteMatrix(0, 0);
                    image = new BitmapAccess(image.AdjustedPArgbImage(colorTransform, transparencyMask, ref borderSet), image.Origin);
                    if (borderSet)
                    {
                        ByteMatrix filter = ByteMatrix.CreateConeFilter(transparentEdge);
                        transparencyMask = transparencyMask.SmoothingWithMidlevelCut(filter, 120);
                        //Debug.WriteLine("transparencyMask *******" + Environment.NewLine + transparencyMask.ToString());
                        image.UpdateTransparency(transparencyMask);
                    }
                }
            }
            Effect = new ColorAdjustmentEffect();
            //Debug.WriteLine("SetImage");
            //Debug.WriteLine(image.ToString());
            //Debug.WriteLineIf(image.Width < 100 && image.Height < 100, image.ToString());
            RedrawImage();
        }
        public void RedrawImage()
        {
            //image = image.Clone(); ?? 
            children.Clear();
            children.Add(CreateImageDrawing());
            //Debug.WriteLine("RedrawImage: RT=" + RenderTransform.Value.ToString());
        }
        public override void SetEffectParameters(double strength, double level, double size)
        {
            ParametricEffect cae = Effect as ParametricEffect;
            if (cae != null)
                cae.SetParameters(colorTransform, strength, level, size);
            Debug.WriteLine("SetEffectParameters: image=" + image.Path);
        }
        protected DrawingVisual CreateImageDrawing()
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            try
            {
                drawingContext.DrawImage(image.Source, new Rect(0, 0, image.Width, image.Height));
                drawingContext.Close();
            }
            catch { }
            return drawingVisual;
        }
    }
    public class BitmapDerivativeLayer : BitmapLayer
    {
        BitmapLayer derivative;
        ParametricEffect derivativeEffect;
        public override BitmapAccess Image { get { return derivative.Image; } }
        public override EffectType DerivativeType { get { return derivativeEffect.Type; } }
        public BitmapDerivativeLayer(string name, BitmapAccess ba, ParametricEffect effect, int edge) : base(name)
        {
            try
            {
                type = VisualLayerType.Derivative;
                LayoutSize = new IntSize(ba.Width, ba.Height);
                derivative = new BitmapLayer("", ba, edge);
                derivative.Effect = derivativeEffect = effect;
                effect.SetImageSize(LayoutSize);
                derivative.SetEffectParameters(0, 0, 1);
                children.Add(derivative);
                Effect = new ColorAdjustmentEffect();
                SetEffectParameters(0, 0, 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public override void SetEffectParameters(double strength, double level, double size)
        {
            ParametricEffect cae = Effect as ParametricEffect;
            if (cae != null)
                cae.SetParameters(colorTransform, strength, level, size);
            derivativeEffect.SetParameters(colorTransform, strength, level, size);
        }
    }
}
