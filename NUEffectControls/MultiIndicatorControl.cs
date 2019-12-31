using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace ShaderEffects
{
    public enum ControlType
    {
        Brightness,
        Saturation,
        Hue,
        Transparency,
        ColorFilter,
        EdgeDetection,
        ViewPoint,
        Unknown
    }
    enum Mobility               // specifies ability to move within indicatorArea rectangle
    {
        Area,                   // unrestricted motion within range
        Border,                 // moves only on range border 
        Horizontal,             // horizontal slider
        Vertical,               // vertical slider
        HorizontalCommon,       // horizontal slider moving all control points (e.g. average position) - does not have value
    }
    public partial class MultiIndicatorControl : UserControl
    {
        protected class BackgroundElement   
        {
            Point[] points;     // poly points normalized to drawing rect {0-1, 0-1}
            Rect area;          // relative element area withing drawing rect {0-1, 0-1}
            Brush brush;        // element brush
            Pen pen;
            internal BackgroundElement(Rect area_, Pen pen_, Color color)
            {
                points = null;
                area = area_;
                pen = pen_;
                brush = new SolidColorBrush(color);
            }
            internal BackgroundElement(Rect area_, Pen pen_, Color color1, Color color2, double angle)
            {
                points=null;
                area = area_;
                pen = pen_;
                brush = new LinearGradientBrush(color1, color2, angle);
            }
            internal BackgroundElement(Point[] points_, Pen pen_, Color color)
            {
                points = points_;
                area = new Rect();
                pen = pen_;
                brush = new SolidColorBrush(color);
            }
            internal Color Color { set { brush = new SolidColorBrush(value); } }
            internal void Draw(DrawingContext g, Rect r)
            {
                if (points == null)
                {
                    Rect ar = new Rect(r.Left + r.Width * area.Left, r.Top + r.Height * area.Top, r.Width * area.Width, r.Height * area.Height);
                    g.DrawRectangle(brush, pen, ar);
                }
                else
                {
                    Point[] ap = new Point[points.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        ap[i].X = r.Left + r.Width * points[i].X;
                        ap[i].Y = r.Top + r.Height * points[i].Y;
                    }
                    PathSegment ps = new PolyLineSegment(ap, true); // true will draw poly line
                    Geometry geom = new PathGeometry(new PathFigure[] { new PathFigure(ap[0], new PathSegment[] { ps }, true) });
                    g.DrawGeometry(brush, pen, geom);
                }
            }
        }
        class Appearence   
        {
            static string RangeString(double val)
            {
                if ((int)val == val)
                    return ((int)val).ToString();
                if ((int)(10 * val) == 10 * val)
                    return val.ToString("f1");
                return val.ToString("f2");
            }
            internal int activeIndex = -1; // index of active control indicator
            Canvas frame;
            Label title;
            Thickness labelArea;    // border outside control area (in pixels) to position labels and other features
            Rect rect;              // area within control surface to draw indicators (in pixels)
            Rect indicatorArea;     // area within control surface to draw indicators reduced by markerSize (in pixels)
            double captureExpansion=2;
            ControlPointCollection controlPoints;
            double[] valueRangeBoxPos;
            TextBox[] valueRangeBoxes;
            bool changingRange = false;
            double markerSize;      // indicator marker size
            double markerCrossSize; // slider extend in the direction across direction of motion as a fraction of field size
            double rangeW = 1;
            double rangeH = 1;
            Brush markerBrush = new SolidColorBrush(Colors.White);
            Pen markerPen = new Pen(new SolidColorBrush(Colors.Black), 1);
            BackgroundElement[] backgroundElements;
            internal bool IsLayoutSet { get { return rect.Width > 0 && rect.Height > 0; } }
            internal Canvas Frame { get { return frame; } }
            internal Rect IndicatorArea { get { return indicatorArea; } }
            internal Appearence(ControlType type, Pen borderPen, Thickness labelArea_, double markerSize_, double markerCrossSize_)
            {
                ControlPoint[] indicators;
                labelArea = labelArea_;
                markerSize = markerSize_;
                markerCrossSize = markerCrossSize_;
                frame = new Canvas();
                title = new Label();
                title.Content = type.ToString();
                title.Height = labelArea.Top;
                frame.Children.Add(title);
                if (type == ControlType.Brightness)
                {
                    rangeH = 0.8;
                    Rect range = new Rect(0, 0, rangeW, rangeH);
                    Rect sliderRange = new Rect(0, rangeH, rangeW, 1-rangeH);
                    double x = rangeW / 2;
                    valueRangeBoxPos = new double[] { rangeH / 2 };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Border, range, new Point(x, 0), 0, "Bright corection position={1}, value={0}"),
                        new ControlPoint(Mobility.Area, range, new Point(x, rangeH / 2), 0, "Mid tone corection position={1}, value={0}"),
                        new ControlPoint(Mobility.Border, range, new Point(x, rangeH), 0, "Dark corection position={1}, value={0}"),
                        new ControlPoint(Mobility.HorizontalCommon, sliderRange, new Point(x, (1+rangeH)/2), -1, "Brightness adjustment") };
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(range, borderPen, Colors.White, Colors.Black, 90),
                        new BackgroundElement(sliderRange, null, SystemColors.ControlColor) };
                }
                else if (type == ControlType.Transparency)
                {
                    rangeH = 0.8;
                    Rect range = new Rect(0, 0, rangeW, rangeH);
                    Rect sliderRange = new Rect(0, rangeH, rangeW, 1 - rangeH);
                    valueRangeBoxPos = new double[] { rangeH / 2 };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Border, range, new Point(0, 0), 0, "Bright transparency position={1}, value={0}"),
                        new ControlPoint(Mobility.Border, range, new Point(0, rangeH), 0, "Dark transparency position={1}, value={0}"),
                        new ControlPoint(Mobility.HorizontalCommon, sliderRange, new Point(0, (1+rangeH)/2), -1, "Transparency adjustment") };
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(range, borderPen, Colors.White, Colors.Black, 90),
                        new BackgroundElement(sliderRange, null, SystemColors.ControlColor) };
                }
                else if (type == ControlType.Saturation)
                {
                    rangeH = 0.8;
                    double h = rangeH / 3;
                    double y = h / 2;
                    double x = rangeW / 2;
                    Rect rangeR = new Rect(0, 0, rangeW, h);
                    Rect rangeG = new Rect(0, h, rangeW, h);
                    Rect rangeB = new Rect(0, 2*h, rangeW, h);
                    Rect sliderRange = new Rect(0, rangeH, rangeW, 1 - rangeH);
                    valueRangeBoxPos = new double[] { rangeH / 2 };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Horizontal, rangeR, new Point(x, y), 0, "Red saturation value={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeG, new Point(x, y+h), 0, "Green saturation value={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeB, new Point(x, y+2*h), 0, "Blue saturation value={0}"),
                        new ControlPoint(Mobility.HorizontalCommon, sliderRange, new Point(x, (1+rangeH)/2), -1, "All colors saturation adjustment") };
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(rangeR, null, Colors.Gray, Colors.Red, 0),
                        new BackgroundElement(rangeG, null, Colors.Gray, Colors.Green, 0),
                        new BackgroundElement(rangeB, null, Colors.Gray, Colors.Blue, 0),
                        new BackgroundElement(new Rect(0, 0, rangeW, rangeH), borderPen, Colors.Transparent),
                        new BackgroundElement(sliderRange, null, SystemColors.ControlColor) };
                }
                else if (type == ControlType.Hue)
                {
                    rangeH = 0.8;
                    double h = rangeH / 3;
                    double w = rangeW / 2;
                    double y = h / 2;
                    double x = rangeW / 2;
                    Rect rangeR = new Rect(0, 0, rangeW, h);
                    Rect rangeG = new Rect(0, h, rangeW, h);
                    Rect rangeB = new Rect(0, 2 * h, rangeW, h);
                    Rect sliderRange = new Rect(0, rangeH, rangeW, 1 - rangeH);
                    valueRangeBoxPos = new double[] { rangeH / 2 };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Horizontal, rangeR, new Point(x, y), 0, "Red hue value={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeG, new Point(x, y+h), 0, "Green hue value={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeB, new Point(x, y+2*h), 0, "Blue hue value={0}"),
                        new ControlPoint(Mobility.HorizontalCommon, sliderRange, new Point(x, (1+rangeH)/2), -1, "All colors hue adjustment") };
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(new Rect(0, 0, w, h), null, Color.FromRgb(128, 0, 128), Colors.Red, 0),
                        new BackgroundElement(new Rect(w, 0, w, h), null, Colors.Red,Color.FromRgb(128, 128, 0), 0),
                        new BackgroundElement(new Rect(0, h, w, h), null, Color.FromRgb(128, 128, 0), Colors.Green, 0),
                        new BackgroundElement(new Rect(w, h, w, h), null, Colors.Green, Color.FromRgb(0, 128, 128), 0),
                        new BackgroundElement(new Rect(0, 2*h, w, h), null, Color.FromRgb(0, 128, 128), Colors.Blue, 0),
                        new BackgroundElement(new Rect(w, 2*h, w, h), null, Colors.Blue, Color.FromRgb(128, 0, 128), 0),
                        new BackgroundElement(new Rect(0, 0, rangeW, rangeH), borderPen, Colors.Transparent),
                        new BackgroundElement(sliderRange, null, SystemColors.ControlColor) };
                }
                else if (type == ControlType.ColorFilter)
                {
                    double h = rangeH / 4;
                    double y = h / 2;
                    double x = rangeW / 2;
                    Rect rangeSelect = new Rect(0, 0, rangeW, h);
                    Rect rangeRemove = new Rect(0, h, rangeW, h);
                    Rect rangeDark = new Rect(0, 2 * h, rangeW, h);
                    Rect rangeSize = new Rect(0, 3 * h, rangeW, h);
                    valueRangeBoxPos = new double[] { y, y + h, y + 2 * h, y + 3 * h };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Horizontal, rangeSelect, new Point(x, y), 0, "Select color range={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeRemove, new Point(x, y+h), 1, "Remove color range={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeDark, new Point(x, y+2*h), 2, "Dark areas similarity correction={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeSize, new Point(x, y+3*h), 3, "Color averaging size={0}") };
                    double d = h / 6;
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(rangeSelect, borderPen, Colors.Transparent),
                        new BackgroundElement(rangeRemove, borderPen, Colors.Transparent),
                        new BackgroundElement(rangeDark, borderPen, Colors.Gray, Colors.Black, 0),
                        new BackgroundElement(rangeSize, null, SystemColors.ControlColor),
                        new BackgroundElement(new Point[]{ new Point(0, 1-h/2), new Point(1, 1-h+d), new Point(1, 1-d) }, null, Colors.Gray) };
                }
                else if (type == ControlType.EdgeDetection)
                {
                    double h = rangeH / 4;
                    double y = h / 2;
                    double x = rangeW / 2;
                    Rect rangeBrightness = new Rect(0, 0, rangeW, h);
                    Rect rangeContrast = new Rect(0, h, rangeW, h);
                    Rect rangeMix = new Rect(0, 2 * h, rangeW, h);
                    Rect rangeSize = new Rect(0, 3 * h, rangeW, h);
                    valueRangeBoxPos = new double[] { y, y + h, y + 2 * h, y + 3 * h };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Horizontal, rangeBrightness, new Point(x, y), 0, "Edge brightness={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeContrast, new Point(x, y+h), 1, "Edge contrast={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeMix, new Point(x, y+2*h), 2, "Mix with original fraction={0}"),
                        new ControlPoint(Mobility.Horizontal, rangeSize, new Point(x, y+3*h), 3, "Edge size={0}") };
                    double d = h / 6;
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(rangeBrightness, borderPen, Colors.Transparent),
                        new BackgroundElement(rangeContrast, borderPen, Colors.Transparent),
                        new BackgroundElement(rangeMix, borderPen, Colors.Transparent),
                        new BackgroundElement(rangeSize, null, SystemColors.ControlColor),
                        new BackgroundElement(new Point[]{ new Point(0, 1-h/2), new Point(1, 1-h+d), new Point(1, 1-d) }, null, Colors.Gray) };
                }
                else if (type == ControlType.ViewPoint)
                {
                    rangeH = 0.8;
                    Rect range = new Rect(0, 0, rangeW, rangeH);
                    Rect sliderRange = new Rect(0, rangeH, rangeW, 1 - rangeH);
                    double x = rangeW / 2;
                    double y = (1 + rangeH) / 2;
                    valueRangeBoxPos = new double[] { rangeH / 2, y };
                    indicators = new ControlPoint[] { 
                        new ControlPoint(Mobility.Area, range, new Point(x, rangeH / 2), 0, "View point location x={0}, y={1}"),
                        new ControlPoint(Mobility.Horizontal, sliderRange, new Point(x, y), 1, "View point distance={0}") };
                    double d = (1 - rangeH) / 6;
                    backgroundElements = new BackgroundElement[] {
                        new BackgroundElement(range, borderPen, Colors.White),
                        new BackgroundElement(sliderRange, null, SystemColors.ControlColor),
                        new BackgroundElement(new Point[]{ new Point(0, (1+rangeH)/2), new Point(1, rangeH+d), new Point(1, 1-d) }, null, Colors.Gray) };
                }
                else
                {
                    valueRangeBoxPos = new double[0];
                    indicators = new ControlPoint[0]; 
                    backgroundElements = new BackgroundElement[0];
                }
                valueRangeBoxes = new TextBox[2*valueRangeBoxPos.Length];
                for (int i = 0; i < valueRangeBoxPos.Length; i++)
                {
                    valueRangeBoxes[2 * i] = CreateRangeTextBox("min" + i, labelArea.Left);
                    frame.Children.Add(valueRangeBoxes[2 * i]);
                    valueRangeBoxes[2 * i + 1] = CreateRangeTextBox("max" + i, labelArea.Right);
                    frame.Children.Add(valueRangeBoxes[2 * i + 1]);
                }
                controlPoints = new ControlPointCollection(indicators);
                Point[] valueRange=new Point[valueRangeBoxPos.Length];
                for(int i=0; i<valueRangeBoxPos.Length; i++)
                    valueRange[i]=new Point(0, 1);
                SetValueRanges(valueRange);
            }
            TextBox CreateRangeTextBox(string name, double w)
            {
                TextBox tb = new TextBox();
                tb.Height = labelArea.Top;
                tb.Width = w;
                tb.Name = name;
                tb.TextAlignment = TextAlignment.Center;
                tb.Background = new SolidColorBrush(Colors.Transparent);
                tb.BorderThickness = new Thickness(0);
                tb.LostKeyboardFocus+=new KeyboardFocusChangedEventHandler(RangeValueChanged);
                return tb;
            }
            void RangeValueChanged(object sender, KeyboardFocusChangedEventArgs e)
            {
                if (changingRange)
                    return;
                TextBox tb = (TextBox)sender;
                bool minChanged = tb.Name.Substring(0, 3) == "min";
                double oldVal;
                double minVal = double.MinValue;
                double maxVal = double.MaxValue;
                int index;
                try
                {
                    index = int.Parse(tb.Name.Substring(3));
                    if (minChanged)
                    {
                        oldVal = controlPoints.MinMax[index].X;
                        maxVal = controlPoints.MinMax[index].Y;
                    }
                    else
                    {
                        minVal = controlPoints.MinMax[index].X;
                        oldVal = controlPoints.MinMax[index].Y;
                    }
                }
                catch { return; }
                double value=0;
                bool restore = false;
                try 
                { 
                    value=double.Parse(tb.Text);
                    if (value < minVal || value > maxVal)
                        restore = true;
                }
                catch { restore=true; }
                if(restore)
                    value = oldVal;
                else
                {
                    Point[] rv = controlPoints.MinMax;
                    if (minChanged)
                        rv[index].X = value;
                    else
                        rv[index].Y = value;
                    controlPoints.MinMax=rv;
                }
                changingRange = true;
                tb.Text = RangeString(value);
                changingRange = false;
            }
            internal void SetValueRanges(Point[] minMax)
            {
                changingRange = true; 
                controlPoints.MinMax = minMax;
                for (int i = 0; i < minMax.Length; i++)
                {
                    valueRangeBoxes[2 * i].Text = RangeString(minMax[i].X);
                    valueRangeBoxes[2 * i + 1].Text = RangeString(minMax[i].Y);
                }
                changingRange = false;
            }
            internal void SetLayout(Size size)
            {
                Canvas.SetTop(title, 0);
                Canvas.SetLeft(title, size.Width / 2 - title.ActualWidth / 2);
                for (int i = 0; i < valueRangeBoxPos.Length; i++ )
                {
                    double y = indicatorArea.Top + indicatorArea.Height * valueRangeBoxPos[i];
                    Canvas.SetTop(valueRangeBoxes[2 * i], y - valueRangeBoxes[2 * i].Height / 2);
                    Canvas.SetTop(valueRangeBoxes[2 * i + 1], y - valueRangeBoxes[2 * i + 1].Height / 2);
                    Canvas.SetLeft(valueRangeBoxes[2*i], 0);
                    Canvas.SetLeft(valueRangeBoxes[2*i + 1], size.Width - labelArea.Right);
                }
                double w = size.Width - labelArea.Left - labelArea.Right;
                double h = size.Height - labelArea.Top - labelArea.Bottom;
                indicatorArea = rect = new Rect(labelArea.Left, labelArea.Top, w, h);
                indicatorArea.Inflate(-markerSize, -markerSize);
            }
            internal void SetIndicatorsToInitial(bool all) { controlPoints.SetToInitial(all); activeIndex = -1; }
            internal int SetIndicatorValues(Point mp) { controlPoints.SetIndicatorValues(mp, IndicatorArea, activeIndex); return activeIndex; }
            internal void Draw(DrawingContext g)
            {
                foreach (BackgroundElement be in backgroundElements)
                    be.Draw(g, indicatorArea);
                foreach (ControlPoint cp in controlPoints.Indicators)
                    DrawMarker(g, cp);
            }
            Rect MarkerRect(ControlPoint cp)
            {
                double w = markerSize;
                double h = markerSize;
                if (cp.Mobility == Mobility.HorizontalCommon || cp.Mobility == Mobility.Horizontal)
                    h = cp.RangeY * indicatorArea.Height * markerCrossSize;
                else if (cp.Mobility == Mobility.Vertical)
                    w = cp.RangeX * indicatorArea.Width * markerCrossSize;
                return new Rect(indicatorArea.Left + indicatorArea.Width * cp.Loc.X - w / 2, indicatorArea.Top + indicatorArea.Height * cp.Loc.Y - h / 2, w, h);
            }
            void DrawMarker(DrawingContext g, ControlPoint cp)
            {
                Rect mr=MarkerRect(cp);
                if (cp.Mobility == Mobility.HorizontalCommon || cp.Mobility == Mobility.Horizontal || cp.Mobility == Mobility.Vertical)
                    g.DrawRectangle(markerBrush, markerPen, mr);
                else
                    g.DrawEllipse(markerBrush, markerPen, mr.TopLeft+new Vector(mr.Width / 2, mr.Height / 2), mr.Width / 2, mr.Height / 2);
            }
            internal int IndicatorIndex(Point p)
            {
                for (int i = 0; i < controlPoints.Indicators.Length; i++)
                {
                    Rect mr = MarkerRect(controlPoints[i]);
                    mr.Inflate(captureExpansion, captureExpansion);
                    if (mr.Contains(p))
                        return i;
                }
                return -1;
            }
            internal void SetActiveIndicatorIndex(Point p) { activeIndex = IndicatorIndex(p); }
            internal ControlPoint ControlPoint(int ind) { return controlPoints[ind]; }
        }
        int col=0;
        int row;
        ControlType type;
        Appearence renderer;
        TextBlock tipText;
        //Point[] arrow = new Point[] { new Point(-3, -4), new Point(-7, 0), new Point(-3, 4), new Point(-3, 1), 
        //    new Point(3, 1), new Point(3, 4), new Point(7, 0), new Point(3, -4), new Point(3, -1), new Point(-3, -1)};
        public MultiIndicatorControl(ControlType type_, int row_)          // base for all multi-value controls
        {
            type = type_;
            row = row_;
            Pen borderPen = new Pen(new SolidColorBrush(Colors.Black), 2);
            renderer = new Appearence(type, borderPen, new Thickness(24, 22, 24, 0), 5, 0.5);
            this.MouseDown += new MouseButtonEventHandler(_MouseDown);
            this.MouseMove += new MouseEventHandler(_MouseMove);
            this.MouseUp += new MouseButtonEventHandler(_MouseUp);
            this.MouseLeave += new MouseEventHandler(_MouseLeave);
            Content = renderer.Frame;          
            ToolTip = CreateToolTip();
        }
        ToolTip CreateToolTip()     
        {
            ToolTip tooltip = new ToolTip();
            tooltip.Placement = PlacementMode.Right;
            tooltip.PlacementRectangle = new Rect(50, 0, 0, 0);
            tooltip.HorizontalOffset = 10;
            tooltip.VerticalOffset = 20;
            BulletDecorator bdec = new BulletDecorator();
            Ellipse littleEllipse = new Ellipse();
            littleEllipse.Height = 8;
            littleEllipse.Width = 12;
            littleEllipse.Fill = Brushes.Gold;
            bdec.Bullet = littleEllipse;
            tipText = new TextBlock();
            bdec.Child = tipText;
            tooltip.Content = bdec;
            return tooltip;
        }
        Size GetParentSize()        
        {
            Grid parent=base.Parent as Grid;
            if (parent == null)
                return new Size();
            int nc = parent.ColumnDefinitions.Count;
            int nr = parent.RowDefinitions.Count;
            double w;
            if (nc > 0)
            {
                col = col < 0 ? 0 : col >= nc ? nc - 1 : col;
                Grid.SetColumn(this, col);
                w = parent.ColumnDefinitions[col].ActualWidth;
            }
            else
                w = parent.ActualWidth;
            double h;
            if (nr > 0)
            {
                row = row < 0 ? 0 : row >= nr ? nr - 1 : row;
                Grid.SetRow(this, row);
                h = parent.RowDefinitions[row].ActualHeight;
            }
            else
                h = parent.ActualHeight;
            return new Size(w, h);
        }
        public void ResetLayout()   { renderer.SetLayout(GetParentSize()); }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!renderer.IsLayoutSet)
                ResetLayout();
            renderer.Draw(drawingContext);
        }
        void _MouseLeave(object s, EventArgs e) { renderer.SetIndicatorsToInitial(false); InvalidateVisual(); }
        void _MouseUp(object s, MouseEventArgs e) { renderer.SetIndicatorsToInitial(false); InvalidateVisual(); tipText.Visibility = Visibility.Hidden; }
        void _MouseMove(object o, MouseEventArgs e)
        {
            Point mp=e.GetPosition(this);
            int ind=renderer.SetIndicatorValues(mp);
            if (ind >= 0)
                InvalidateVisual();
            else
                ind=renderer.IndicatorIndex(mp);
            if (ind < 0)
                tipText.SetValue(TextBlock.TextProperty, type.ToString() + " control");
            else
                tipText.SetValue(TextBlock.TextProperty, renderer.ControlPoint(ind).ToString());
        }
        void _MouseDown(object s, MouseEventArgs e)
        {
            //controlPoints.activeIndex = renderer.ActiveIndicatorIndex(e.GetPosition(this));
            renderer.SetActiveIndicatorIndex(e.GetPosition(this));
            tipText.Visibility = Visibility.Visible;
        }
    }
}
