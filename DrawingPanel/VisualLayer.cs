﻿using System;
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
    {   // user set mouse controll modes; Basic is default and does not have associated graphic tool
        Default,        // basic transforms (shift, scale, rotate)
        Distortion3D,   // handles scale, rotate, aspect, shear, ViewDistortion with parallelogram 
        FreeSelection,  // creates FlexiblePolygon from free selection
        RectSelection,  // creates FlexiblePolygon  from rectangle
        ContourEdit,    // editing FlexiblePolygon drawing
        InfoImage,      // choice of info images cut rectangles
        Crop,           // cut part of image
        Morph,          // image morph
    }
    public class VisualLayer : UIElement
    {   // collection of visual items comprizing one UIElement (similar to DrawingGroup, but working with DependencyProperties)
        protected VisualCollection children;
        protected ColorTransform colorTransform = new ColorTransform();
        public MatrixControl MatrixControl { get; private set; } 
        public List<MorthPoint> morthPoints = new List<MorthPoint>();
        public virtual void SetEffectParameters(double weight, double middle, double resolution) { }
        public virtual EffectType DerivativeType { get { return EffectType.None; } }
        public bool FromSelection { get; set; } = false;
        public string Name { get; set; }
        public bool IsImageLayer    { get { return this as BitmapLayer != null; } }
        public IntSize LayoutSize   { get; protected set; } // image size used by layout system 
        public VisualLayerType Type { get; protected set; }
        public bool Deleted         { get; set; }   // marked for delayed deletion (do not display in layer list)
        public Rect RenderRect      { get { var diag = new Point[] { new Point(0, 0), new Point(LayoutSize.Width, LayoutSize.Height) }; RenderTransform.Value.Transform(diag); return new Rect(diag[0], diag[1]); } }
        public bool IntersectsWith(VisualLayer vl) { return RenderRect.IntersectsWith(vl.RenderRect); }
        public ColorTransform ColorTransform { get { return colorTransform; } set { colorTransform.CopyFrom(value); } }
        public VisualLayer(string name_)
        {
            Name = name_;
            children = new VisualCollection(this);
        }
        public int AddVisual(Visual v) { return children.Add(v); }
        public void RemoveVisual(Visual v) { children.Remove(v); }
        public bool HitTest(Point pt) 
        {
            Matrix rm = RenderTransform.Value;
            rm.Invert();
            Point tp = rm.Transform(pt);
            return tp.X>0 && tp.X< LayoutSize.Width && tp.Y>0 && tp.Y< LayoutSize.Height ? true : false;
        }
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
        internal void SetStoredMatrixContro(MatrixControl stored) { MatrixControl = stored; }
        public VisualLayerData CreateVisualLayerData(byte[] data) { return new VisualLayerData(Type, Name, LayoutSize, MatrixControl, data); }
        public byte[] SerializeImage(bool exact)
        {
            using (MemoryStream dataStream = new MemoryStream())
            {
                if (Type == VisualLayerType.Bitmap || Type == VisualLayerType.Derivative)
                {
                    BitmapLayer bl = this as BitmapLayer;
                    RenderTargetBitmap rtb = new RenderTargetBitmap(LayoutSize.Width, LayoutSize.Height, 96, 96, PixelFormats.Default);
                    RenderToBitmap(rtb);
                    return BitmapAccess.SerializeFrame(BitmapFrame.Create(rtb), exact);
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
                        return dataStream.ToArray();
                    }
                }
            }
            return null;
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
        public void InitializeTransforms(double w, double h, double scale, MatrixControl matrixControl = null)
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
            else    // RenderTransform built from MatrixControl with account for MatrixControl.Center shift
            {
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
        public override string ToString() { return string.Format("mode={0} name={1} children={4} size={2}x{3} ", Type, Name, LayoutSize.Width, LayoutSize.Height, VisualChildrenCount); }
        public string ToTransformString() { return "MatrixControl: " + MatrixControl?.ToString() + " RenderTransform: " + RenderTransform?.Value.ToString(); }
    }
    public class DrawingLayer : VisualLayer
    {
        List<FlexiblePolygon> polygons;
        Brush editBrush = null;
        public List<FlexiblePolygon> Polygons { get { return polygons; } }
        public DrawingLayer(string name, IntSize s, List<FlexiblePolygon> polygons_) : base(name)
        {
            Type = VisualLayerType.Drawing;
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
            Type = VisualLayerType.Drawing;
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
        public void UpdateImage() { image = new BitmapAccess(Image.Source.Clone()); }
        public void SetImage(BitmapAccess ba, int transparentEdge)
        {
            if (ba == null)
                return;
            Type = VisualLayerType.Bitmap;
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
                if (borderColor == ColorTransform.ColorNull)
                    borderColor=Color.FromArgb(255,255,255,255);
                {   //apply transparent edge
                    bool borderSet = false;
                    colorTransform = new ColorTransform(borderColor);
                    transparencyMask = new ByteMatrix(0, 0);
                    image = new BitmapAccess(image.CreateAdjustedPArgbBitmap(colorTransform, transparencyMask, ref borderSet));
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
            children.Clear();
            children.Add(CreateImageDrawing());
            //Debug.WriteLine("RedrawImage: RT=" + RenderTransform.Value.ToString());
        }
        public override void SetEffectParameters(double strength, double level, double size)
        {
            ParametricEffect cae = Effect as ParametricEffect;
            cae?.SetParameters(colorTransform, strength, level, size);
            //Debug.WriteLine("SetEffectParameters: image=" + image.Path);
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
        public string ToEffectString() { return Effect.ToString(); }
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
                Type = VisualLayerType.Derivative;
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
        public string ToDerivativeEffectString() { return derivativeEffect.ToString(); }
    }
}
