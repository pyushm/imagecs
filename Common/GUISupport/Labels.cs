using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Presentation
{
    public class TextHelper
    {
        public static TextHelper CaptionHelper = new TextHelper(14);
        FontFamily fontFamily;
        GlyphTypeface glyphTypeface;
        public char SubscriptSeparator { get; set; } = '_';
        public FontFamily FontFamily
        {
            get { return fontFamily; }
            set
            {
                fontFamily = value;
                Typeface typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                    throw new InvalidOperationException("No glyphtypeface found");
            }
        }
        public double FontSize { get; set; } = 14;
        public double TextHeight { get { return FontSize + 5; } }
        public TextHelper(double size = 14, string font = "Consolas") { FontFamily = new FontFamily(font); }
        public double TextWidth(string l)
        {
            double labelWidth = 0;
            for (int n = 0; n < l.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[l[n]];
                labelWidth += glyphTypeface.AdvanceWidths[glyphIndex] * FontSize;
            }
            return labelWidth;
        }
    }
    public enum LabelState
    {
        Hide,   // no show
        Show,   // show
        Details // show with details
    }
    public class Caption : TextBlock   // basic text on canvas 
    {
        static double subscriptCoeff = 0.7;
        TextHelper helper = TextHelper.CaptionHelper;
        internal TextHelper Helper { get { return helper; } set { helper = value; FontFamily = helper.FontFamily; } }
        protected string text;      // permanent text
        protected string subscript; // text subscript
        protected string value;     // value string
        protected bool accent;
        public Caption(string src, double x = 0, double y = 0, Brush br = null)
        {
            SetText(src);
            Update();
            SetPosition(x, y);
            Foreground = br ?? Foreground;
        }
        void SetText(string src)
        {
            if (src == null)
                return;
            string[] ta = src.Split(new char[] { Helper.SubscriptSeparator }, StringSplitOptions.None);
            text = ta.Length > 0 ? ta[0] : "";
            subscript = ta.Length > 1 ? ta[1] : "";
        }
        public void SetPosition(double x, double y, string src = null) { Canvas.SetLeft(this, x); Canvas.SetTop(this, y); SetText(src); }
        public void Update(bool? accent_ = null)
        {
            accent = accent_ ?? accent;
            Inlines.Clear();
            Run tr = new Run(text);
            tr.FontSize = helper.FontSize;
            if (accent)
                Inlines.Add(new Bold(tr));
            else
                Inlines.Add(tr);
            if (subscript.Length > 0)
            {
                Run sr = new Run(subscript);
                sr.FontSize = helper.FontSize * subscriptCoeff;
                sr.BaselineAlignment = BaselineAlignment.Bottom;
                if (accent)
                    Inlines.Add(new Bold(sr));
                else
                    Inlines.Add(sr);
            }
            if (value != null && value.Length > 0)
            {
                Run vr = new Run('=' + value);
                vr.FontSize = helper.FontSize;
                Inlines.Add(vr);
            }
        }
        public double TextWidth { get { return (helper.TextWidth(text) + helper.TextWidth(subscript) * subscriptCoeff); } }
        public void UpdateValue(double v) { value = string.Format("{0:0.000}", v); Update(); }
    }
    public class ScaleLabel : TextBlock // rounded editable numeric label 
    {
        Point position;
        PlotArea area;
        TextBox editBox = null;
        public double Value { get; private set; }
        public ScaleLabel(Point pos, double val, PlotArea da)
        {
            area = da;
            position = pos;
            FontSize = area.TextHelper.FontSize;
            UpdateValue(val);
            MouseLeftButtonDown += click;
        }
        void UpdateValue(double val)
        {
            string text = area.Properties.Label(val);
            Value = val;
            Text = text;
            Canvas.SetLeft(this, position.X < 0 ? position.X - area.TextHelper.TextWidth(text) : position.X);
            Canvas.SetTop(this, position.Y);
            area.Remove(editBox);
        }
        void click(object o, MouseButtonEventArgs a)
        {
            editBox = new TextBox();
            Canvas.SetLeft(editBox, Canvas.GetLeft(this) - 5);
            Canvas.SetTop(editBox, Canvas.GetTop(this));
            editBox.Width = 26;
            editBox.Loaded += delegate (object sender, RoutedEventArgs e) { editBox.Focus(); };
            editBox.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                    UpdateValue(double.Parse(editBox.Text));
                area.RedrawNotify?.Invoke();
            };
            area.Add(editBox);
        }
    }
    public class NumericLabel : TextBlock // numeric label on canvas with mid-text position specified (e.g. X tick marks)
    {
        public string Format { get; set; } = "f";
        public Point TextCenter { get; set; }
        public NumericLabel(Point pos, string f)
        {
            TextCenter = pos;
            Format = f;
        }
        public void SetValue(double v)
        {
            Text = v.ToString(Format);
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Arrange(new Rect(DesiredSize));
            Canvas.SetLeft(this, TextCenter.X - ActualWidth / 2);
            Canvas.SetTop(this, TextCenter.Y - ActualHeight / 2);
        }
    }
    public class LinkedLabel : Caption // text label having 3 states; controls visibility of linked UI elements
    {
        internal LabelState State { get; private set; }
        internal UIElement LinkedUI { get { return linkedUI; } set { linkedUI = value; UpdateVisibility(); } }   // controlled element 
        internal UIElement SuplementUI { get { return suplementUI; } set { suplementUI = value; UpdateVisibility(); } }  // suplementary controlled element 
        UIElement linkedUI;
        UIElement suplementUI;
        public LinkedLabel(PlotCurve pc, string src) : base(src)
        {
            Update();
            MouseLeftButtonDown += delegate (object o, MouseButtonEventArgs maa) { IncrementState(); };
            State = LabelState.Show;
        }
        internal void IncrementState()
        {
            int s = ((int)State + 1) % Enum.GetValues(typeof(LabelState)).Length; // rotate states
            State = (LabelState)s;
            if (SuplementUI == null && State == LabelState.Details)
                State = LabelState.Hide;
            Update(State == LabelState.Details);
            UpdateVisibility();
        }
        void UpdateVisibility()
        {
            Opacity = State == LabelState.Hide ? 0.3 : 1;
            UpdateAttached(LinkedUI, State == LabelState.Hide);
            UpdateAttached(SuplementUI, State != LabelState.Details);
        }
        void UpdateAttached(UIElement uie, bool hide)
        {
            if (uie == null)
                return;
            uie.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            uie.InvalidateVisual();
        }
    }
    public class VerticalLineMarker    // line + caption
    {
        Line vline = new Line();
        Caption caption;
        PlotArea pa;
        public VerticalLineMarker(PlotArea area, Color color, string name, double thickness, double beginY, double markerLength)
        {
            pa = area;
            vline.Y1 = beginY;
            vline.Y2 = vline.Y1 + markerLength;
            Brush br = new SolidColorBrush(color);
            vline.Stroke = br;
            vline.StrokeThickness = thickness;
            this.pa.Children.Add(vline);
            caption = new Caption(name, 0, vline.Y2 - this.pa.TextHelper.TextHeight / 2, br);
            vline.Visibility = caption.Visibility = Visibility.Hidden;
            this.pa.Children.Add(caption);
        }
        public void UpdatePosition(double x)
        {
            if (!pa.XRange.IsInRange(x))
                vline.Visibility = caption.Visibility = Visibility.Hidden;
            else
            {
                vline.Visibility = caption.Visibility = Visibility.Visible;
                vline.X1 = vline.X2 = pa.ToPlot(x);
                caption.UpdateValue(x);
                Canvas.SetLeft(caption, vline.X2);
            }
        }
    }
    public class RangeMarker    // box + caption
    {
        Caption caption;
        PlotArea pa;
        double y;
        Line l1 = new Line();
        Line l2 = new Line();
        Line l3 = new Line();
        public RangeMarker(PlotArea area, Color color, string name, double thickness)
        {
            double off = area.Height / 2;
            pa = area;
            l1.Y1 = l2.Y1 = area.Height;
            l3.Y1 = l3.Y2 = l1.Y2 = l2.Y2 = area.Height - area.TextHelper.TextHeight;
            Brush br = new SolidColorBrush(color);
            l1.Stroke = l2.Stroke = l3.Stroke = br;
            l1.StrokeThickness = l2.StrokeThickness = l3.StrokeThickness = thickness;
            area.Children.Add(l1);
            area.Children.Add(l2);
            area.Children.Add(l3);
            caption = new Caption(name, 0, 0, br);
            y = area.Height - area.TextHelper.TextHeight;
            l1.Visibility = l2.Visibility = l3.Visibility = caption.Visibility = Visibility.Hidden;
            area.Children.Add(caption);
        }
        public void UpdatePosition(double left, double right)
        {
            if (left >= right)
                l1.Visibility = l2.Visibility = l3.Visibility = caption.Visibility = Visibility.Hidden;
            else
            {
                l1.Visibility = l2.Visibility = l3.Visibility = caption.Visibility = Visibility.Visible;
                l1.X1 = l1.X2 = l3.X1 = pa.ToPlot(left);
                l2.X1 = l2.X2 = l3.X2 = pa.ToPlot(right);
                caption.SetPosition((l1.X1 + l2.X1 - caption.TextWidth) / 2, y);
            }
        }
    }
    public class PointMarker
    {
        public enum Type
        {
            Circle,
            Rect,
        }
        CurveSet cs;
        Shape el;
        public Point Values
        {
            set
            {
                var c = cs.ToPlot(value.X, value.Y);
                Canvas.SetLeft(el, c.X - el.Width / 2);
                Canvas.SetBottom(el, cs.PlotArea.Height - c.Y - el.Height/2);
            }
        }
        public PointMarker(CurveSet ps, Brush stroke, Brush fill, Type t, double size=4, double thickness=0.8)
        {
            cs = ps;
            el = t == Type.Circle ? (Shape)new Ellipse() : t == Type.Rect ? new Rectangle() : null;
            if (el == null)
                return;
            el.Width = el.Height = size;
            el.Stroke = stroke;
            el.StrokeThickness = thickness;
            el.Fill = fill;
            ps.PlotArea.Children.Add(el);
        }
    }
    public class CircleMarker : PointMarker
    {
        public CircleMarker(PlotCurve pc, double size = 4) : base(pc.Set, pc.Brush, new SolidColorBrush(Colors.White), Type.Circle, size, size / 5) { }
        public CircleMarker(PlotCurve pc, Brush color, double size = 4) : base(pc.Set, pc.Brush, color, Type.Circle, size, size / 5) { }
    }
    public class RectMarker : PointMarker
    {
        public RectMarker(PlotCurve pc, double size = 4) : base(pc.Set, pc.Brush, new SolidColorBrush(Colors.White), Type.Rect, size, size / 5) { }
        public RectMarker(PlotCurve pc, Brush color, double size = 4) : base(pc.Set, pc.Brush, color, Type.Rect, size, size / 5) { }
    }
}
