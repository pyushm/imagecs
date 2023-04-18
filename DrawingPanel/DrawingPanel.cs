using System;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ShaderEffects;

namespace ImageProcessor
{
    public interface IPanelHolder
    {
        ToolMode ToolMode { get; }
        void CropRectangleUpdated();
        void GeometryTransformUpdated();
        void FocusControl();
        double SelectedSensitivity();
        void ActiveLayerUpdated(int i);
    }
    public class DrawingPanel : Canvas
    {
        PolygonBuilder builder = new PolygonBuilder();
        FlexiblePolygonEditor strokeEditor;
        Brush toolBrush = null;
        Pen toolPen = null;
        Point getBackgroundCenter() { return BackgroundLayer?.MatrixControl != null ? BackgroundLayer.MatrixControl.Center : new Point(); }
        double getBackgroundScale() { return BackgroundLayer?.MatrixControl != null ? BackgroundLayer.MatrixControl.RenderScale : 1; }
        double getBackgroundAngle() { return BackgroundLayer?.MatrixControl != null ? BackgroundLayer.MatrixControl.Angle : 0; }
        Polygon collectedPolygon = null; // temporary selection: mouse path in image pixels
        bool strokeEdit = false;
        IPanelHolder panelHolder;
        string lastImageFile = "lastImageFile";
        Size previousHostSize;
        //string loadedFilePath = "";
        //bool newSelection = true;
        bool mouseDown;                 // true between mouseDown and MouseUp events
        MouseOperation mouseAction;     // support of mouse control manipulations
        VisualLayer tools = new VisualLayer("tools");
        MorphControl morphControl = null;
        public string FrameSizeString
        {
            get
            {
                string s = backgroundLayoutSize.ToString();
                return CropRectangle != null ? s + " selection: " + CropRectangle.ToString() : s;
            }
        }
        IntSize backgroundLayoutSize         { get { return BackgroundLayer == null ? new IntSize((int)Width, (int)Height) : BackgroundLayer.LayoutSize; } }
        public bool XYmirroredFrame     { get; set; }
        public Point SavedPosition      { get; private set; } // position saved from last operation
        public CropRect CropRectangle   { get; private set; } // rectangle area to save
        public FlexiblePolygon Selection{ get; private set; }
        public Matrix ToCanvas          { get; private set; }
        public Matrix FromCanvas        { get; private set; }
        public int LayerCount           { get; private set; } // count of BitmapLayers: first 'layerCount' Children are BitmapLayers
        public bool IsActiveLayerVisible{ get { return ActiveLayer != null && ActiveLayer.Visibility == Visibility.Visible; } }
        public int activeLayerIndex = -1;
        public VisualLayer ActiveLayer  { get; private set; }
        bool MenuMode                   { get { return ContextMenu != null && ContextMenu.IsOpen; } }
        public VisualLayer BackgroundLayer { get; private set; }
        public bool HasDrawing          { get { for (int i = 0; i < LayerCount; i++) if (GetLayer(i) is DrawingLayer) return true; return false; } }
        #region Layer list manipulation
        public VisualLayer GetLayer(int li) { if (li < 0 || li >= Children.Count) return null; return Children[li] as VisualLayer; }
        public bool UpdateActiveLayer(int li) 
        {
            if (activeLayerIndex == li)
                return true;
            ActiveLayer = GetLayer(li); 
            bool ok = ActiveLayer != null; 
            if (ok) 
            { 
                activeLayerIndex = li; 
                UpdateToolDrawing(); 
            } 
            return ok; 
        }
        public int RemoveActiveLayer()
        {
            if (ActiveLayer != null)
            {
                Children.Remove(ActiveLayer);
                LayerCount--;
            }
            return ClosestVisibleIndex(activeLayerIndex);
        }
        public int ClosestVisibleIndex(int current)  // closest visible layer or -1
        {
            for (int i = current; i < 2 * LayerCount; i++)
            {
                int j = i + 1;
                if (j < LayerCount && Children[j].Visibility == Visibility.Visible)
                    return j;
                j = 2 * current - i - 1;
                if (j >= 0 && Children[j].Visibility == Visibility.Visible)
                    return j;
            }
            return -1;
        }
        public bool MoveLayerBack(int ind)
        {
            if (LayerCount < 2 || ind <= 0 || ind >= LayerCount)
                return false;
            UIElement moved = Children[ind];
            Children.RemoveAt(ind);
            Children.Insert(ind - 1, moved);
            return true;
        }
        public bool MoveLayerOnTop(int ind)
        {
            if (LayerCount < 2 || ind < 0 || ind >= LayerCount - 1)
                return false;
            UIElement moved = Children[ind];
            Children.RemoveAt(ind);
            Children.Insert(LayerCount - 1, moved);
            return true;
        }
        string UniqueName(string inName)
        {
            foreach (var l in Children)
            {
                VisualLayer vl = l as VisualLayer;
                if (vl != null && string.Compare(inName, vl.Name) == 0)
                {
                    inName = inName + '|';
                    return UniqueName(inName);
                }
            }
            return inName;
        }
        public void RenameActiveLayer(string name) { if (ActiveLayer != null) ActiveLayer.Name = UniqueName(name); }
        #endregion
        public DrawingPanel(IPanelHolder tool)
        {
            XYmirroredFrame = false;
            CreateToolBrushes(Colors.Black, Colors.Yellow, Colors.Transparent);
            Background = Brushes.Transparent;
            FlexiblePolygon.Smoother = new PolygonSmoother(0.4, 10);
            strokeEditor = new FlexiblePolygonEditor(this);
            Children.Add(tools);
            Margin = new Thickness(50);
            Background = SystemColors.ControlLightBrush;//DashBrush.Make(5, Colors.Red, Colors.LightYellow)
            panelHolder = tool;
        }
        void CreateToolBrushes(Color core, Color dash2nd, Color markerCenter)
        {
            int d = 10;  // cheker size
            RadialGradientBrush rgb = new RadialGradientBrush();
            rgb.GradientOrigin = new Point(0.5, 0.5);
            rgb.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
            rgb.GradientStops.Add(new GradientStop(core, 1.0));
            //rgb.Opacity = 0.5;
            toolBrush = rgb;
            DrawingBrush db = new DrawingBrush();
            db.TileMode = TileMode.Tile;
            db.Viewport = new Rect(0, 0, d, d);
            db.ViewportUnits = BrushMappingMode.Absolute;
            DrawingGroup dg = new DrawingGroup();
            SolidColorBrush bcore = new SolidColorBrush(core);
            SolidColorBrush bdash2nd = new SolidColorBrush(dash2nd);
            db.Drawing = dg;
            dg.Children.Add(new GeometryDrawing(bdash2nd, null, new RectangleGeometry(new Rect(0, 0, d, d))));
            dg.Children.Add(new GeometryDrawing(bdash2nd, null, new RectangleGeometry(new Rect(d, d, d, d))));
            dg.Children.Add(new GeometryDrawing(bcore, null, new RectangleGeometry(new Rect(d, 0, d, d))));
            dg.Children.Add(new GeometryDrawing(bcore, null, new RectangleGeometry(new Rect(0, d, d, d))));
            db.Opacity = 1;
            toolPen = new Pen(db, 0.5);
        }
        public bool Resize(bool firstTime, double scale, float hostW, float hostH)
        {
            if (BackgroundLayer == null)
                return false;
            bool matrixControlSet = BackgroundLayer.MatrixControl != null;
            Debug.Assert(firstTime || matrixControlSet);
            if (!firstTime && !matrixControlSet) // !init assumes resize event: all MatrixControl has to be set and be changing according to resize
                return false;                     // init creates new MatrixControls if !matrixControlSet, otherwize (loaded from file) only shifts center
            Vector centerShift = new Vector((hostW - previousHostSize.Width) / 2.0, (hostH - previousHostSize.Height) / 2.0);
            double scaleCoef = 1;
            previousHostSize = new Size(hostW, hostH);
            double dAngle = 0;
            if (scale == 0)    // fit to size up to max scale
            {
                scale = Math.Min(Math.Min((double)hostW / BackgroundLayer.LayoutSize.Width, (double)hostH / backgroundLayoutSize.Height), 2);
                if (scale == 0)
                    return false;
                scaleCoef = scale / getBackgroundScale();
            }
            Width = Math.Max(hostW, backgroundLayoutSize.Width * scale);
            Height = Math.Max(hostH, backgroundLayoutSize.Height * scale);
            Point center = getBackgroundCenter();
            Transform prev = BackgroundLayer.RenderTransform;

            double prevAngle = getBackgroundAngle();
            //Debug.WriteLine("---------->>> " + (firstTime ? "NEW IMAGE " : "RESIZE EXISTING ") + BackgroundLayer.Name+ " Layers=" + LayerCount + " Children=" + Children.Count + " " + hostW + "x" + hostH);
            if (!firstTime)
            {
                BackgroundLayer.Translate(centerShift);
                BackgroundLayer.MatrixControl.RenderScale *= scaleCoef;
                BackgroundLayer.UpdateRenderTransform();
                dAngle = prevAngle - getBackgroundAngle();
                //Debug.WriteLine("RESIZE Background " + BackgroundLayer.ToTransformString());
            }
            else if (matrixControlSet)
            {
                BackgroundLayer.InitializeTransforms(hostW, hostH, scale, BackgroundLayer.MatrixControl);
                Debug.WriteLine("APPLY EXISTING MatrixControl to Background " + BackgroundLayer.ToTransformString());
            }
            else
            {
                scaleCoef = scale / getBackgroundScale();
                BackgroundLayer.InitializeTransforms(hostW, hostH, scale);
                //Debug.WriteLine("APPLY NEW MatrixControl to Background " + BackgroundLayer.ToTransformString());
            }
            Matrix current = BackgroundLayer.RenderTransform.Value;
            for (int i = 1; i < LayerCount; i++)
            {
                VisualLayer vl = Children[i] as VisualLayer;
                if (vl == null)
                    continue;
                if (!firstTime)
                {
                    //string mc = vl.MatrixControl.ToString();
                    Point oldC = vl.MatrixControl.Center;
                    Matrix rm = prev.Value;
                    rm.Invert();
                    Point ip = rm.Transform(oldC);
                    Point tp = current.Transform(ip);
                    vl.SetResizedRenderTransform(tp - oldC, scaleCoef, dAngle);
                    //Debug.WriteLine("RESIZE Layer " + i + " {" +vl.ToString() + "} " + vl.ToTransformString());
                }
                else if (matrixControlSet && vl.MatrixControl != null)
                {
                    vl.InitializeTransforms(hostW, hostH, scale, vl.MatrixControl);
                    //Debug.WriteLine("APPLY EXISTING MatrixControl to Layer " + i + " {" + vl.ToString() + "} " + vl.ToTransformString());
                }
                else
                {
                    Debug.Assert(vl.MatrixControl == null && !matrixControlSet);
                    vl.InitializeTransforms(hostW, hostH, scale);
                    //Debug.WriteLine("APPLY NEW MatrixControl to Layer " + i + " {" + vl.ToString() + "} " + vl.ToTransformString());
                }
            }
            CropRectangle?.SetToDrawingTransform(BackgroundLayer.MatrixControl.RenderScale, Width, Height);
            //Debug.WriteLine("Resize: Crop set to " + CropRectangle?.ToString());
            double offsetX = Width / 2 - backgroundLayoutSize.Width / 2 * BackgroundLayer.MatrixControl.RenderScale;
            double offsetY = Height / 2 - backgroundLayoutSize.Height / 2 * BackgroundLayer.MatrixControl.RenderScale;
            MatrixTransform drawing = new MatrixTransform(BackgroundLayer.MatrixControl.RenderScale, 0, 0, BackgroundLayer.MatrixControl.RenderScale, offsetX, offsetY);
            //Debug.WriteLine("Transform set: offsetX=" + offsetX.ToString() + " offsetY=" + offsetY.ToString() + " scale=" + BackgroundLayer.MatrixControl.RenderScale.ToString());
            Matrix m = ToCanvas = drawing.Matrix;
            m.Invert();
            FromCanvas = m;
            //Debug.WriteLine("FromCanvas: " + m.ToString());
            if (Selection != null)
                Selection.ToDrawing = new MatrixTransform(ToCanvas);
            UpdateToolDrawing();
            Point c0 = ToCanvas.Transform(new Point(0, 0));
            Point cx = ToCanvas.Transform(new Point(backgroundLayoutSize.Width, backgroundLayoutSize.Height));
            Debug.WriteLine("ImageSize=" + backgroundLayoutSize.ToString() + " c0=" + c0.ToString() + "cm=" + cx.ToString() + " 0->" + FromCanvas.Transform(c0).ToString() + " x->" + FromCanvas.Transform(cx).ToString()); 
            return true;
        }
        public int AddVisualLayer(VisualLayer vl)
        {
            if (vl == null)
                return LayerCount;
            vl.Name = UniqueName(vl.Name);
            Children.Insert(LayerCount, vl);
            return LayerCount++;
        }
        public int AddVisualLayer(VisualLayer vl, double scale, bool selection = true)
        {
            if (vl == null)
                return 0;
            vl.InitializeTransforms(Width, Height, scale);
            vl.FromSelection = selection;
            return AddVisualLayer(vl);
        }
        public int AddVisualLayer(VisualLayer vl, VisualLayer renderSrc, Vector shift)
        {
            if (renderSrc == null)
                vl?.InitializeTransforms(Width, Height, -1);
            else
                vl?.InitializeTransforms(renderSrc);
            if (shift.LengthSquared != 0)
            {
                shift = renderSrc.RenderTransform.Value.Transform(shift);
                vl?.Translate(shift);
            }
            return AddVisualLayer(vl);
        }
        public int AddVisualLayer(VisualLayer vl, VisualLayer renderSrc) { return AddVisualLayer(vl, renderSrc, new Vector()); }
        public int ReplaceVisuallLayer(VisualLayer vl, VisualLayer renderSrc) 
        {   // returns index of incerted layer
            if (renderSrc == null || vl == null)
                return -1;
            int ind = Children.IndexOf(renderSrc);
            if (ind < 0)
                return -2;
            vl.InitializeTransforms(renderSrc);
            vl.Name = renderSrc.Name + "#";
            Children.RemoveAt(ind);
            Children.Insert(ind, vl);
            if(ind == 0 ) { BackgroundLayer = vl; }
            ActiveLayer = vl;
            return ind;
        }
        public int AddStrokeLayer(string name)
        {
            name = UniqueName(name);
            VisualLayer vl = new DrawingLayer(name, backgroundLayoutSize);
            vl.InitializeTransforms(Width, Height, -1);
            Children.Insert(LayerCount, vl);
            return LayerCount++;
        }
        public void DeleteSelection()
        {
            if (Selection != null && ActiveLayer.IsImage)
            {
                Polygon lp = Selection.Contour();
                BitmapLayer bl = ActiveLayer as BitmapLayer;
                bl.SetImage(bl.Image.DeleteSelection(lp.IntRect(0), lp.Poly), 0);
            }
        }
        public void SetClipboardFromSelection()
        {
            BitmapAccess clip = GetSelected(0);
            //clip.DebugSave("test.png");
            //Debug.WriteLine(clip.ToColorString());
            if (clip != null)
                Clipboard.SetData(DataFormats.Bitmap, clip.Source);
        }
        public BitmapAccess GetSelected(int offset)
        {
            BitmapLayer bl = ActiveLayer as BitmapLayer;
            if (bl == null)
                return null;
            if (Selection == null)
                return bl.Image;
            Polygon lp = Selection.Contour();
            Int32Rect rect = lp.IntRect(offset);
            SavedPosition = new Point(rect.X, rect.Y);
            // border ==0 set image outside selection to white (compatible with MSPaint selection)
            // border !=0 leaves image outside selection for processing with average or contrastiong (only A set to 0)
            return bl.Image.SetSelectionBitmap(rect, lp.Poly, offset == 0);
        }
        public void InitializeToolDrawing() { InitializeToolDrawing(backgroundLayoutSize); } // scalable selection rectangle
        public void InitializeToolDrawing(IntSize size)
        {
            CropRectangle = null;
            strokeEdit = panelHolder.ToolMode == ToolMode.ContourEdit;
            if (BackgroundLayer == null)
                return;
            if (panelHolder.ToolMode == ToolMode.Crop)
            {
                if (XYmirroredFrame)
                    size = size.XYmirrored;
                CropRectangle = new CropRect(size, backgroundLayoutSize.Width / 2.0, backgroundLayoutSize.Height / 2.0);
                CropRectangle.SetToDrawingTransform(BackgroundLayer.MatrixControl.RenderScale, Width, Height);
                //Debug.WriteLine("Tool: Frame=" + backgroundLayoutSize.ToString() + "Crop set to " + CropRectangle.ToString());
            }
            else if (panelHolder.ToolMode == ToolMode.InfoImage)
            {
                CropRectangle = new CropRect(size, Width / 2, Height / 2);
                CropRectangle.ToDrawing = null;
                if (ActiveLayer != null)
                    ActiveLayer.MatrixControl.Center = CropRectangle.Center;
            }
            Selection = null;
            bool selectionMode = panelHolder.ToolMode == ToolMode.RectSelection || panelHolder.ToolMode == ToolMode.FreeSelection;
            collectedPolygon = new Polygon(size);
            strokeEditor.BackgroundImage = selectionMode ? (GetLayer(0) as BitmapLayer)?.Image : null;
            //Debug.WriteLine(layerTool.ToolMode.ToString()+ (pathPolygon==null ? " null Polygon" : " real Polygon") + (CropRectangle == null ? " null Rect" : " real Rect"));
            UpdateToolDrawing();
        }
        public void UpdateToolDrawing()
        {
            if (ActiveLayer == null)
                return;
            tools.Clear();
            if (panelHolder.ToolMode == ToolMode.Distortion3D)
                tools.AddVisual(ActiveLayer.MatrixControl?.ToVisual(toolBrush, toolPen));
            else if (panelHolder.ToolMode == ToolMode.Morph)
                tools.AddVisual(morphControl.ToVisual(toolBrush, toolPen));
            if (CropRectangle != null)
                tools.AddVisual(CropRectangle.ToVisual(toolBrush, toolPen));
            if (collectedPolygon != null)
            {
                collectedPolygon.ToDrawing = (MatrixTransform)ActiveLayer.RenderTransform;
                tools.AddVisual(collectedPolygon.ToVisual(new SolidColorBrush(), toolPen));
            }
            if (Selection != null)
            {
                Selection.ToDrawing = (MatrixTransform)ActiveLayer.RenderTransform;
                tools.AddVisual(Selection.ToVisual(new SolidColorBrush(), toolPen));
            }
        }
        #region Load/Save
        public void FadeAway(double seconds)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(seconds));
            EventHandler[] completedHandler = new EventHandler[LayerCount];
            for (int i = LayerCount - 1; i >= 0; i--)
            {
                VisualLayer oldContent = GetLayer(i);
                if (oldContent != null)
                {
                    oldContent.IsHitTestVisible = false;
                    oldContent.Deleted = true;
                    completedHandler[i] = delegate (object sender, EventArgs e)
                    {
                        Children.Remove(oldContent);
                        if (oldContent is IDisposable)
                            (oldContent as IDisposable).Dispose();
                    };
                    OpacityAnimation(oldContent, 1, 0, duration, completedHandler[i]); // gradually decrease opacity
                }
            }
            LayerCount = 0;
        }
        void OpacityAnimation(UIElement element, double begin, double end, Duration duration, EventHandler completedEventHandler)
        {
            element.Opacity = begin;
            DoubleAnimation animation = new DoubleAnimation(begin, end, duration);
            if (completedEventHandler != null)
                animation.Completed += completedEventHandler;
            element.BeginAnimation(OpacityProperty, animation);
        }
        public string LoadFile(ImageFileInfo loadInfo, double replaceDuration)
        {
            XYmirroredFrame = false;
            CropRectangle = null;
            if (replaceDuration > 0 && LayerCount > 0)
                FadeAway(replaceDuration);
            VisualLayer vl;
            try
            {
                if (loadInfo.IsImage)
                {
                    BitmapAccess ba = BitmapAccess.LoadImage(loadInfo.FSPath, loadInfo.IsEncrypted);
                    if (DataAccess.Warning.Length > 0)
                    //{
                    //    loadedFilePath = "";
                        return DataAccess.Warning;
                    //}
                    //else
                    //    loadedFilePath = loadInfo.FSPath;
                    vl = new BitmapLayer(loadInfo.RealName, ba);
                    //Debug.WriteLine("LoadFile: RT=" + vl.RenderTransform.Value.ToString());
                    AddVisualLayer(vl);
                }
                else if (loadInfo.IsMultiLayer)
                {
                    VisualLayerData[] vlda = VisualLayerData.LayersFromFile(loadInfo.FSPath, loadInfo.IsEncrypted);
                    for (int ind = 0; ind < vlda.Length; ind++)
                    {
                        if (vlda[ind].IsThumbnail || vlda[ind].Data == null)
                            continue;
                        if (vlda[ind].Data.Length == 0)
                            return DataAccess.Warning;
                        try
                        {
                            vl = vlda[ind].IsBitmap ? (VisualLayer)new BitmapLayer(vlda[ind].Name, vlda[ind].GetImageAccess()) :
                                 vlda[ind].IsDrawing ? new DrawingLayer(vlda[ind].Name, vlda[ind].PixelSize, vlda[ind].GetStrokes()) : null;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("  >>Restore Exception: " + ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                            vl = null;
                        }
                        vl?.SetStoredMatrixContro(vlda[ind].MatrixControl);
                        AddVisualLayer(vl);
                    }
                }
                else if (loadInfo.IsEncryptedVideo)
                {
                    BitmapAccess ba = BitmapAccess.LoadImage("mediaImage.png", false);
                    AddVisualLayer(new BitmapLayer("Video", ba));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return ex.Message;
            }
            tools.Clear();
            ActiveLayer = BackgroundLayer = LayerCount > 0 ? Children[0] as VisualLayer : null;
            //Debug.WriteLine(BackgroundLayer.Name + (replaceDuration < 0.6 ? "  View" : "  Edit"));
            if (BackgroundLayer != null)
            {
                var mp = BackgroundLayer.GetLastMorthPoint();
                if (mp == null)
                {
                    double x = BackgroundLayer.LayoutSize.Width / 2;
                    double y = BackgroundLayer.LayoutSize.Height / 2;
                    mp = new MorthPoint(new Point(x, y), Math.Min(x, y) / 10);
                }
                morphControl = new MorphControl(mp);
            }
            return BackgroundLayer == null ? "BackgroundLayer failure" : "";
        }
        public string SaveLayers(string fileName, BitmapEncoder bitmapEncoder)
        {
            try
            {
                VisualLayer[] vla = new VisualLayer[LayerCount];
                for (int i = 0; i < LayerCount; i++)
                    vla[i] = Children[i] as VisualLayer;
                FileInfo fi = new FileInfo(fileName);
                if (fi.Exists && ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                    fi.Attributes = fi.Attributes ^ FileAttributes.ReadOnly;
                VisualLayerData[] vlda = new VisualLayerData[vla.Length + 1];
                for (int i = 0; i < vla.Length; i++)
                {
                    VisualLayer vl = vla[i] as VisualLayer;
                    byte[] ba = vl.SerializeImage(bitmapEncoder);
                    ba = DataAccess.WriteBytes(ba, true);
                    vlda[i] = vl.CreateVisualLayerData(ba);
                }
                vlda[vla.Length] = CreateThumbnailData(bitmapEncoder);
                BinaryFormatter f = new BinaryFormatter();
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Write)) { f.Serialize(fs, vlda); }
                RenderTransform = Transform.Identity;
                UpdateLayout(); 
                return "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return ex.Message;
            }
        }
        VisualLayerData CreateThumbnailData(BitmapEncoder bitmapEncoder)
        {
            int ms = 200;
            byte[] ba = SerializeRendering(ms, bitmapEncoder);
            ba = DataAccess.WriteBytes(ba, true);
            return new VisualLayerData(VisualLayerType.Tool, "", new IntSize(ms, ms), new MatrixControl(), ba);
        }
        public byte[] SerializeRendering(int maxSize, BitmapEncoder bitmapEncoder)
        {
            if (CropRectangle == null)
            {
                CropRectangle = new CropRect(backgroundLayoutSize, backgroundLayoutSize.Width / 2.0, backgroundLayoutSize.Height / 2.0);
                CropRectangle.ToDrawing = new MatrixTransform(ToCanvas);
            }
            IntSize saveSize = ScaleToSize(CropRectangle, maxSize);
            int w = saveSize.Width;
            int h = saveSize.Height;
            double scale = Math.Max(w / CropRectangle.Rect.Width, h / CropRectangle.Rect.Height);
            Matrix t;
            if (CropRectangle.Scaled)
            {
                t = FromCanvas;
                t.Scale(scale, scale);
                double offsetX = -CropRectangle.Rect.Left * scale;
                double offsetY = -CropRectangle.Rect.Top * scale;
                t.Translate(offsetX, offsetY);
                //Debug.WriteLine("SerializeImage Scaled: Crop=" + CropRectangle.ToString() + " saveSize=" + saveSize.ToString() + Environment.NewLine + t.ToString());
            }
            else
            {
                t = new Matrix(scale, 0, 0, scale, -CropRectangle.Rect.Left, -CropRectangle.Rect.Top);
                //Debug.WriteLine("SerializeImage NOT Scaled: " + t.ToString());
            }
            Point c0 = ToCanvas.Transform(new Point(0, 0));
            Point cx = ToCanvas.Transform(new Point(backgroundLayoutSize.Width, backgroundLayoutSize.Height));
            Debug.WriteLine("Image=" + backgroundLayoutSize.ToString() + " c0=" + c0.ToString() + "cm=" + cx.ToString() + " 0->" + FromCanvas.Transform(c0).ToString() + " x->" + FromCanvas.Transform(cx).ToString());
            Debug.WriteLine("save=" + saveSize.ToString() + " c0=" + c0.ToString() + "cm=" + cx.ToString() + " 0->" + t.Transform(c0).ToString() + " x->" + t.Transform(cx).ToString());
            RenderTransform = new MatrixTransform(t);
            tools.Clear();
            for (int i = 0; i < Children.Count; i++)
            {
                var bdl = GetLayer(i) as BitmapDerivativeLayer;
                if (bdl != null && bdl.DerivativeType == EffectType.ViewPoint) // any clip is a ViewPoint derivative; 0.0001 needed to activate drawing in case effect never activated
                    bdl.SetEffectParameters(panelHolder.SelectedSensitivity(), bdl.MatrixControl.ViewDistortion.X+0.0001, bdl.MatrixControl.ViewDistortion.Y + 0.0001);
            }
            UpdateLayout();
            RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(this);
            var enc = bitmapEncoder as JpegBitmapEncoder;
            if (enc != null) // ~87 for 3 Mpixels, ~93 fpr 1 Mpixel, 99 for 0
                enc.QualityLevel = Math.Min((int)(77 + 55 / (w * h / 1.0e6 + 2.5)), 100);
            bitmapEncoder.Frames.Add(BitmapFrame.Create(rtb));
            MemoryStream ms = new MemoryStream();
            bitmapEncoder.Save(ms);
            return ms.ToArray();
        }
        public string SaveSingleImage(string fileName, int maxSize, BitmapEncoder bitmapEncoder, bool encrypt)
        {
            byte[] data = SerializeRendering(maxSize, bitmapEncoder);
            FileInfo fi = new FileInfo(fileName);
            bool originalSaved = true;
            if (fi.Exists)
            {
                try
                {
                    if (File.Exists(lastImageFile))
                    {
                        File.SetAttributes(lastImageFile, FileAttributes.Normal);
                        File.Delete(lastImageFile);
                    }
                    fi.MoveTo(lastImageFile);
                    File.SetAttributes(lastImageFile, FileAttributes.Normal);
                }
                catch
                {
                    originalSaved = false;
                    return "Original not saved to lastImageFile: '" + fileName + "' not saved";
                }
            }
            if (originalSaved && !DataAccess.WriteFile(fileName, data, encrypt))
            {
                fi = new FileInfo(lastImageFile);
                if (!fi.Exists || fileName == null)
                    return "";
                fi.MoveTo(fileName);
                return DataAccess.Warning;
            }
            RenderTransform = Transform.Identity;
            UpdateLayout();
            return "";
        }
        IntSize ScaleToSize(CropRect rect, int maxSize)
        {
            if (rect == null || LayerCount == 0)
                return new IntSize();
            Rect saveRect = rect.Rect;
            double scale = 1; // BackgroundLayer.MatrixControl.RenderScale;
            if (maxSize > 0)
            {   // cutRectangle is scaled the same way as image; for cutting perposes image should not be scaled
                if (scale < saveRect.Width / maxSize)
                    scale = saveRect.Width / maxSize;
                if (scale < saveRect.Height / maxSize)
                    scale = saveRect.Height / maxSize;
            }
            int w = (int)((saveRect.Width + 0.5) / scale);
            int h = (int)((saveRect.Height + 0.5) / scale);
            if (maxSize > 0 && (w > 1200 || h > 1200))
            {   // resizing to standard dimentions multiple of 8
                if (w > h)
                {
                    w = (w + 4) / 8 * 8;
                    h = (int)(w * (double)saveRect.Height / saveRect.Width);
                }
                else
                {
                    h = (h + 4) / 8 * 8;
                    w = (int)(h * (double)saveRect.Width / saveRect.Height);
                }
            }
            return new IntSize((w + 1) / 2 * 2, (h + 1) / 2 * 2); // both dimentions multiple of 2 to avoid saving qulity deterioration
        }
        #endregion
        Cursor ActionCursor
        {
            get
            {
                switch (mouseAction)
                {
                    case MouseOperation.Bottom:
                    case MouseOperation.Left:
                    case MouseOperation.Top:
                    case MouseOperation.Right: return Cursors.Hand;
                    case MouseOperation.Rotate: return Cursors.No;
                    case MouseOperation.Scale: return Cursors.SizeAll;
                    case MouseOperation.OpCenter:
                    case MouseOperation.VortexBL:
                    case MouseOperation.VortexBR:
                    case MouseOperation.VortexTL:
                    case MouseOperation.VortexTR: return Cursors.Cross;
                    case MouseOperation.Stroke: return Cursors.Pen;
                }
                return Cursors.Arrow;
            }
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (MenuMode)
                return;
            panelHolder.FocusControl();
            mouseDown = true;
            Point position = e.GetPosition(this);   // position in canvas coordinates
            //Debug.WriteLine("OnMouseDown " + "strokeEdit="+strokeEdit.ToString());
            if (strokeEdit)
            {
                DrawingLayer sl = ActiveLayer as DrawingLayer;
                FlexiblePolygon[] ss = sl != null ? sl.Polygons.ToArray() : new FlexiblePolygon[] { Selection };
                mouseAction = strokeEditor.MouseDown(e, ss, position);
                if (mouseAction == MouseOperation.OpCenter)
                {
                    Selection.Clear();
                    strokeEdit = false;
                    UpdateToolDrawing();
                }
                //Debug.WriteLine("newSelection " + newSelection.ToString() + " mouseAction " + mouseAction.ToString());
            }
            else
            {
                mouseAction = MouseOperation.None;
                if (LayerCount == 1)   // crop and info creation and selection applies only to single layer image 
                {
                    if ((panelHolder.ToolMode == ToolMode.RectSelection || panelHolder.ToolMode == ToolMode.FreeSelection) && collectedPolygon != null)
                    {
                        collectedPolygon.Clear();
                        collectedPolygon.Add(collectedPolygon.FromDrawing.Transform(position));
                        mouseAction = MouseAction.OperationFromMouse;
                        if (mouseAction == MouseOperation.Move)
                            mouseAction = MouseOperation.Add;
                    }
                    if (panelHolder.ToolMode == ToolMode.Crop && CropRectangle != null)
                        mouseAction = CropRectangle.OperationFromLine(CropRectangle.FromDrawing.Transform(position));
                    else if (panelHolder.ToolMode == ToolMode.InfoImage)
                        mouseAction = MouseAction.OperationFromMouse;
                }
                else
                {
                    if (ActiveLayer != BackgroundLayer && IsActiveLayerVisible)    // operation on BackgroundLayer in multy-layer images cause confusion
                    {
                        mouseAction = panelHolder.ToolMode == ToolMode.Distortion3D ? ActiveLayer.MatrixControl.OperationFromPoint(position) :
                            panelHolder.ToolMode == ToolMode.Morph ? morphControl.OperationFromPoint(position) :
                            panelHolder.ToolMode == ToolMode.None ? MouseAction.OperationFromMouse :
                            MouseOperation.None;
                    }
                    if (mouseAction == MouseOperation.Move || mouseAction == MouseOperation.None) // if higher than active layer clicked, set clicked layer as active
                    {
                        int selected = -1;
                        for (int i = 1; i < LayerCount; i++)    // only layer not intersecting with othes may be seledted by clicking (allowed)
                        {
                            var vli = Children[i] as VisualLayer;
                            if (vli != null && vli.HitTest(position))
                            {
                                bool allowed = true;
                                for (int j = 1; j < LayerCount; j++)
                                {
                                    var vlj = Children[j] as VisualLayer;
                                    if (j != i && vli != null && vli.IntersectsWith(vlj))
                                    {
                                        allowed = false;
                                        break;
                                    }
                                }
                                selected = allowed ? i : -1;
                                break;
                            }
                        }
                        if (selected != -1 && selected != activeLayerIndex)  // if hit other layer => new active layer
                        {
                            panelHolder.ActiveLayerUpdated(selected);
                            mouseAction = MouseOperation.None;
                        }
                    }
                }
            }
            Mouse.OverrideCursor = ActionCursor;
            //Debug.WriteLine("DP down " + mouseAction.ToString());
            SavedPosition = position;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (MenuMode || !mouseDown || mouseAction == MouseOperation.None)
                return;
            Point position = e.GetPosition(this);   // position in canvas coordinates
            Vector lastShift = position - SavedPosition;
            if (lastShift.Length < 2)
                return;
            lastShift *= panelHolder.SelectedSensitivity();
            if (strokeEdit && mouseAction == MouseOperation.Stroke)
            {
               mouseAction = strokeEditor.MouseMove(e, lastShift) ? MouseOperation.Stroke : MouseOperation.None;
                //Debug.WriteLine("strokeEdit move " + mouseAction.ToString());
            }
            else if (mouseAction == MouseOperation.Add && collectedPolygon != null && panelHolder.ToolMode == ToolMode.FreeSelection)
                collectedPolygon.Add(collectedPolygon.FromDrawing.Transform(position));
            else if (mouseAction == MouseOperation.Add && collectedPolygon != null && panelHolder.ToolMode == ToolMode.RectSelection)
                collectedPolygon.SetRectangle(collectedPolygon.FromDrawing.Transform(position));
            else if (MouseAction.IsLineOperation(mouseAction) && CropRectangle != null && CropRectangle.Update(mouseAction, CropRectangle.FromDrawing.Transform(position)))
                panelHolder.CropRectangleUpdated();
            else if (IsActiveLayerVisible)
            {
                if (mouseAction == MouseOperation.OpCenter)
                    ActiveLayer.MatrixControl.MoveCenter(lastShift);
                if (mouseAction == MouseOperation.ViewPoint && ActiveLayer.DerivativeType == EffectType.ViewPoint)
                {
                    DistortionControl dc = ActiveLayer.MatrixControl as DistortionControl;
                    if (dc != null)
                    {
                        Vector vd = dc.SetViewDistortion(position);
                        ActiveLayer.SetEffectParameters(panelHolder.SelectedSensitivity(), vd.X, vd.Y);
                    }
                }
                if (mouseAction == MouseOperation.Morph && ActiveLayer.DerivativeType == EffectType.Morph)
                {
                    DistortionControl dc = ActiveLayer.MatrixControl as DistortionControl;
                    if (dc != null)
                    {
                        Vector vd = dc.SetViewDistortion(position);
                        ActiveLayer.SetEffectParameters(panelHolder.SelectedSensitivity(), vd.X, vd.Y);
                    }
                }
                else if (mouseAction == MouseOperation.Move)
                {
                    Matrix t = ActiveLayer.RenderTransform.Value;
                    t.Translate(lastShift.X, lastShift.Y);
                    ActiveLayer.RenderTransform = new MatrixTransform(t);
                    if (panelHolder.ToolMode != ToolMode.InfoImage)
                        ActiveLayer.MatrixControl.MoveCenter(lastShift);
                    if (ActiveLayer == BackgroundLayer)
                        for (int i = 1; i < LayerCount; i++)
                            (Children[i] as VisualLayer)?.Translate(lastShift);
                }
                else if (mouseAction == MouseOperation.Scale)
                {
                    double sc = ActiveLayer.MatrixControl.ScaleFromPoints(position, SavedPosition);
                    ActiveLayer.MatrixControl.RenderScale *= 1 + (sc - 1) * panelHolder.SelectedSensitivity();
                    if (ActiveLayer == BackgroundLayer)
                        for (int i = 1; i < LayerCount; i++)
                            (Children[i] as VisualLayer)?.MatrixControl.ScaleAt(sc, BackgroundLayer.MatrixControl.Center);
                }
                else if (mouseAction == MouseOperation.Rotate)
                {
                    double rad = ActiveLayer.MatrixControl.RadiansFromPoints(position, SavedPosition) * panelHolder.SelectedSensitivity();
                    ActiveLayer.MatrixControl.RotateAngle(rad);
                    if (ActiveLayer == BackgroundLayer)
                        for (int i = 1; i < LayerCount; i++)
                            (Children[i] as VisualLayer)?.MatrixControl.RotateAt(rad, BackgroundLayer.MatrixControl.Center);
                }
                else
                {
                    //Debug.WriteLine("active b " + ActiveLayer.MatrixControl.ToString());
                    //Debug.WriteLine(mouseAction.ToString() + ' ' + lastShift.X.ToString("f6") + ' ' + lastShift.Y.ToString("f6"));
                    double inc = ActiveLayer.MatrixControl.SetShearAndAspect(mouseAction, lastShift);
                    //Debug.WriteLine("active a " + ActiveLayer.MatrixControl.ToString());
                    if (ActiveLayer == BackgroundLayer)
                        for (int i = 1; i < LayerCount; i++)
                        {
                            //Debug.WriteLine(i.ToString() + " layerB " + (Children[i] as VisualLayer)?.MatrixControl.ToString());
                            //Debug.WriteLine(mouseAction.ToString() + ' ' + lastShift.X.ToString("f6") + ' ' + lastShift.Y.ToString("f6"));
                            (Children[i] as VisualLayer)?.MatrixControl.ApplyShearAndAspectIncrement(mouseAction, inc);
                            //Debug.WriteLine(i.ToString() + " layerA " + (Children[i] as VisualLayer)?.MatrixControl.ToString());
                        }
                }
                ActiveLayer.UpdateRenderTransform();
                if (ActiveLayer == BackgroundLayer)
                    for (int i = 1; i < LayerCount; i++)
                        (Children[i] as VisualLayer)?.UpdateRenderTransform();
                panelHolder.GeometryTransformUpdated();
            }
            //Debug.WriteLine("DP move " + mouseAction.ToString());
            UpdateToolDrawing();
            SavedPosition = position;
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            ActionEnd(SavedPosition);
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this);   // position in canvas coordinates
            if (MenuMode)
                return;
            if (strokeEdit && mouseAction == MouseOperation.Stroke)
            {
                strokeEditor.MouseUp(e, position);
                mouseAction = MouseOperation.None;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
            else
                ActionEnd(position);
        }
        protected void ActionEnd(Point position)
        {
            if (MenuMode || !mouseDown)
                return;
            mouseDown = false;
            Mouse.OverrideCursor = Cursors.Arrow;
            if (mouseAction == MouseOperation.Add && collectedPolygon != null)
            {
                if (!collectedPolygon.IsEmpty)
                {
                    bool rect = panelHolder.ToolMode == ToolMode.RectSelection;
                    if (rect)
                    {
                        collectedPolygon.SetRectangle(collectedPolygon.FromDrawing.Transform(position));
                        Selection = FlexiblePolygon.CreateFromRect(collectedPolygon);
                    }
                    else
                    {
                        collectedPolygon.Close(collectedPolygon.FromDrawing.Transform(position));
                        double len = Math.Sqrt(collectedPolygon.TotalLength() / getBackgroundScale()) + 1;
                        //Debug.WriteLine("tot len=" + collectedPolygon.TotalLength().ToString("f1") + " scale=" + getBackgroundScale().ToString("f1") + " avr len=" + len.ToString("f1")+' '+ size.ToString());
                        Polygon src = builder.CreateSmoothedPolygon(collectedPolygon, len);
                        Selection = new FlexiblePolygon(src);
                    }
                    double l = Selection.TotalLength() / Selection.Count;
                    Int32Rect ir = Selection.IntRect(0);
                    if (!ir.IsEmpty)
                    {
                        double dn = l * l / (ir.Width + ir.Height);
                        //Debug.WriteLine("before " + flexPolygon.ToPointString());
                        int changed = strokeEditor.StickToEdge(Selection, dn);
                        //Debug.WriteLine("after " + flexPolygon.ToPointString());
                        //Debug.WriteLine("len=" + len.ToString("f1") + " l=" + l.ToString("f1") + " dn=" + dn.ToString("f1") + " changed=" + changed);
                        //Debug.WriteLine("strokeEdit SET");
                        strokeEdit = true;
                    }
                }
                collectedPolygon.Clear();
            }
            UpdateToolDrawing();
        }
    }
}
