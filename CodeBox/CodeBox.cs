using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;

namespace CodeEditor
{   // this code adapted from web sample
    class CodeBoxRenderInfo
    {
        public FormattedText BoxText { get; set; }
        public FormattedText LineNumbers { get; set; }
        public Point RenderPoint { get; set; }

        public Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> PreparedDecorations { get; set; }
        public Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> BasePreparedDecorations { get; set; }

    }
    public partial class CodeBox : TextBox //  A control to view or edit styled text<kssksk> 
    {
        private System.Windows.Threading.DispatcherTimer renderTimer;// Timer used to redo failed renders
        private CodeBoxRenderInfo renderinfo = new CodeBoxRenderInfo();// Used to cached the render in case of invalid textbox properties.
        bool mScrollingEventEnabled;// Has the scroll event on the scrollviewer been enabled.
        public List<Decoration> Decorations = new List<Decoration>(); // added decorations
        public static DependencyProperty ViewHelperProperty = DependencyProperty.Register("CodeViewHelper", typeof(CodeViewHelper), typeof(CodeBox),
            new FrameworkPropertyMetadata(new CodeViewHelper(), FrameworkPropertyMetadataOptions.AffectsRender));
        public CodeViewHelper ViewHelper // CodeViewHelper used for the CodeBox
        {
            get { return (CodeViewHelper)GetValue(ViewHelperProperty); }
            set { SetValue(ViewHelperProperty, value); }
        }
        public CodeBox()
        {
            TextChanged += new TextChangedEventHandler(txtTest_TextChanged);
            Background = new SolidColorBrush(Colors.Transparent);   // ignors base rendering
            Foreground = new SolidColorBrush(Colors.Transparent);
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            TextWrapping = TextWrapping.Wrap;
            renderTimer = new System.Windows.Threading.DispatcherTimer();
            renderTimer.IsEnabled = false;
            renderTimer.Tick += new EventHandler(renderTimer_Tick);
            renderTimer.Interval = TimeSpan.FromMilliseconds(50);
            InitializeComponent();
            AcceptsReturn = true;
        }
        void renderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.IsEnabled = false;
            this.InvalidateVisual();
        }
        public static DependencyProperty BaseForegroundProperty = DependencyProperty.Register("BaseForeground", typeof(Brush), typeof(CodeBox),
           new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Black), FrameworkPropertyMetadataOptions.AffectsRender));
        [Bindable(true)]
        public Brush BaseForeground
        {
            get { return (Brush)GetValue(BaseForegroundProperty); }
            set { SetValue(BaseForegroundProperty, value); }
        }
        public static DependencyProperty CodeBoxBackgroundProperty = DependencyProperty.Register("CodeBoxBackground", typeof(Brush), typeof(CodeBox),
           new FrameworkPropertyMetadata(new SolidColorBrush(Colors.White), FrameworkPropertyMetadataOptions.AffectsRender));
        [Bindable(true)]
        public Brush CodeBoxBackground
        {
            get { return (Brush)GetValue(CodeBoxBackgroundProperty); }
            set { SetValue(CodeBoxBackgroundProperty, value); }
        }
        #region LineNumber Properties
        public int[] LineStart { get { return VisibleLineStartCharcterPositions(); } }
        public static DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register("ShowLineNumbers", typeof(bool), typeof(CodeBox),
           new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        [Category("LineNumbers")]
        public bool ShowLineNumbers
        {
            get { return (bool)GetValue(ShowLineNumbersProperty); }
            set { SetValue(ShowLineNumbersProperty, value); }
        }
        public static DependencyProperty LineNumberForegroundProperty = DependencyProperty.Register("LineNumberForeground", typeof(Brush), typeof(CodeBox),
           new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Gray), FrameworkPropertyMetadataOptions.AffectsRender));
        [Category("LineNumbers")]
        public Brush LineNumberForeground
        {
            get { return (Brush)GetValue(LineNumberForegroundProperty); }
            set { SetValue(LineNumberForegroundProperty, value); }
        }
        public static DependencyProperty LineNumberMarginWidthProperty = DependencyProperty.Register("LineNumberMarginWidth", typeof(double), typeof(CodeBox),
           new FrameworkPropertyMetadata(15.0, FrameworkPropertyMetadataOptions.AffectsRender));
        [Category("LineNumbers")]
        public double LineNumberMarginWidth
        {
            get { return (Double)GetValue(LineNumberMarginWidthProperty); }
            set { SetValue(LineNumberMarginWidthProperty, value); }
        }
        public static DependencyProperty StartingLineNumberProperty = DependencyProperty.Register("StartingLineNumber", typeof(int), typeof(CodeBox),
           new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));
        [Category("LineNumbers")]
        public int StartingLineNumber
        {
            get { return (int)GetValue(StartingLineNumberProperty); }
            set { SetValue(StartingLineNumberProperty, value); }
        }
        #endregion
        public void ClearAddedDecorations()
        {
            Decorations.Clear();
            InvalidateVisual();
        }
        void txtTest_TextChanged(object sender, TextChangedEventArgs e) { InvalidateVisual(); }
        FormattedText formattedText;
        int previousFirstChar = -1;
        #region OnRender
        protected override void OnRender(DrawingContext drawingContext)// Overrides render and divides into the designer and nondesigner cases.
        {
            EnsureScrolling();
            base.OnRender(drawingContext);
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                OnRenderDesigner(drawingContext);
            }
            else
            {
                if (this.LineCount == 0)
                {
                    ReRenderLastRuntimeRender(drawingContext);
                    renderTimer.IsEnabled = true;
                }
                else
                {
                    OnRenderRuntime(drawingContext);
                }
            }
        }
        protected void OnRenderRuntime(DrawingContext drawingContext)//The main render code <param name="drawingContext"></param>
        {
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));//restrict drawing to textbox
            drawingContext.DrawRectangle(CodeBoxBackground, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));//Draw Background
            if (this.Text == "") return;

            int firstLine = GetFirstVisibleLineIndex();// GetFirstLine();
            int firstChar = (firstLine == 0) ? 0 : GetCharacterIndexFromLineIndex(firstLine);// GetFirstChar();
            string visibleText = VisibleText;
            if (visibleText == null) return;

            Double leftMargin = 4.0 + this.BorderThickness.Left;
            Double topMargin = 2.0 + this.BorderThickness.Top;

            formattedText = new FormattedText(
                   this.VisibleText,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface(this.FontFamily.Source),
                    this.FontSize,
                    BaseForeground);  //Text that matches the textbox's
            formattedText.Trimming = TextTrimming.None;

            ApplyTextWrapping(formattedText);

            Segment visiblePair = new Segment(firstChar, visibleText.Length);
            Point renderPoint = GetRenderPoint(firstChar);

            //Generates the prepared decorations for the BaseDecorations
            Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> basePreparedDecorations
                = GeneratePreparedDecorations(visiblePair, ViewHelper);
            //Displays the prepared decorations for the BaseDecorations
            DisplayPreparedDecorations(drawingContext, basePreparedDecorations, renderPoint);

            //Generates the prepared decorations for the Decorations
            Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> preparedDecorations
                = GeneratePreparedDecorations(visiblePair, Decorations);
            //Displays the prepared decorations for the Decorations
            DisplayPreparedDecorations(drawingContext, preparedDecorations, renderPoint);

            ColorText(firstChar, ViewHelper);//Colors According to Scheme
            ColorText(firstChar, Decorations);//Colors Acording to Decorations
            drawingContext.DrawText(formattedText, renderPoint);

            if (ShowLineNumbers && this.LineNumberMarginWidth > 0) //Are line numbers being used
            { //Even if we gey this far it is still possible for the line numbers to fail
                if (this.GetLastVisibleLineIndex() != -1)
                {
                    FormattedText lineNumbers = GenerateLineNumbers();
                    drawingContext.DrawText(lineNumbers, new Point(3, renderPoint.Y));
                    renderinfo.LineNumbers = lineNumbers;
                }
                else
                {
                    drawingContext.DrawText(renderinfo.LineNumbers, new Point(3, renderPoint.Y));
                }
            }

            //Cache information for possible rerender
            renderinfo.BoxText = formattedText;
            renderinfo.BasePreparedDecorations = basePreparedDecorations;
            renderinfo.PreparedDecorations = preparedDecorations;
        }
        protected void OnRenderDesigner(DrawingContext drawingContext)// Render logic for the designer <param name="drawingContext"></param>
        {

            int firstChar = 0;

            Double leftMargin = 4.0 + this.BorderThickness.Left;
            Double topMargin = 2.0 + this.BorderThickness.Top;


            string visibleText = VisibleText;

            formattedText = new FormattedText(
                   this.Text,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface(this.FontFamily.Source),
                    this.FontSize,
                    BaseForeground);  //Text that matches the textbox's
            formattedText.Trimming = TextTrimming.None;

            string lineNumberString = "1\n2\n3\n";
            FormattedText lineNumbers = new FormattedText(
                  lineNumberString,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface(this.FontFamily.Source),
                    this.FontSize,
                    LineNumberForeground);


            previousFirstChar = firstChar;
            Segment visiblePair = new Segment(firstChar, Text.Length);

            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));//restrict text to textbox
            Point renderPoint = new Point(this.LineNumberMarginWidth + leftMargin, topMargin);

            drawingContext.DrawRectangle(CodeBoxBackground, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            Dictionary<Decoration, List<Geometry>> hilightgeometryDictionary = PrepareGeometries(visiblePair, formattedText, Decorations, EDecorationType.Hilight, HilightGeometryMaker);
            DisplayGeometry(drawingContext, hilightgeometryDictionary, renderPoint);

            Dictionary<Decoration, List<Geometry>> strikethroughGeometryDictionary = PrepareGeometries(visiblePair, formattedText, Decorations, EDecorationType.Strikethrough, StrikethroughGeometryMaker);
            DisplayGeometry(drawingContext, strikethroughGeometryDictionary, renderPoint);

            Dictionary<Decoration, List<Geometry>> underlineGeometryDictionary = PrepareGeometries(visiblePair, formattedText, Decorations, EDecorationType.Underline, UnderlineGeometryMaker);
            DisplayGeometry(drawingContext, underlineGeometryDictionary, renderPoint);
            ColorText(firstChar, Decorations);
            if (!ShowLineNumbers)
            {
                drawingContext.DrawText(lineNumbers, new Point(3, renderPoint.Y));
            }
            drawingContext.DrawText(formattedText, renderPoint);

        }
        protected void ReRenderLastRuntimeRender(DrawingContext drawingContext)// Performs the last successful render again.
        {
            drawingContext.DrawText(renderinfo.BoxText, renderinfo.RenderPoint);
            DisplayPreparedDecorations(drawingContext, renderinfo.PreparedDecorations, renderinfo.RenderPoint);
            DisplayPreparedDecorations(drawingContext, renderinfo.BasePreparedDecorations, renderinfo.RenderPoint);
            if (this.LineNumberMarginWidth > 0) //Are line numbers being used
            {
                drawingContext.DrawText(renderinfo.LineNumbers, new Point(3, renderinfo.RenderPoint.Y));
            }
        }
        private void ColorText(int firstChar, List<Decoration> decorations)// Performs the EDecorationType.TextColor decorations in the formattted text
        {
            if (decorations != null)
            {
                foreach (Decoration dec in decorations)
                {
                    if (dec.DecorationType == EDecorationType.TextColor)
                    {
                        List<Segment> ranges = dec.Ranges(this.Text);
                        foreach (Segment p in ranges)
                        {
                            if (p.End > firstChar && p.Start < firstChar + formattedText.Text.Length)
                            {
                                int adjustedStart = Math.Max(p.Start - firstChar, 0);
                                int adjustedLength = Math.Min(p.Length + Math.Min(p.Start - firstChar, 0), formattedText.Text.Length - adjustedStart);
                                formattedText.SetForegroundBrush(dec.Brush, adjustedStart, adjustedLength);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyTextWrapping(FormattedText formattedText)
        {
            switch (this.TextWrapping)
            {
                case TextWrapping.NoWrap:
                    break;
                case TextWrapping.Wrap:
                    formattedText.MaxTextWidth = this.ViewportWidth; //Used with Wrap only
                    break;
                case TextWrapping.WrapWithOverflow:
                    formattedText.SetMaxTextWidths(VisibleLineWidthsIncludingTrailingWhitespace());
                    break;
            }

        }
        /// <param name="visiblePair">The pair representing the first character of the Visible text with respect to the whole text</param>
        /// <param name="renderPoint">The Point representing the offset from (0,0) for rendering</param>
        /// <param name="decorations">The List of Decorations</param>
        private void DisplayDecorations(DrawingContext drawingContext, Segment visiblePair, Point renderPoint, List<Decoration> decorations)// Displays the Decorations for a List of Decorations
        {
            Dictionary<Decoration, List<Geometry>> hilightgeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Hilight, HilightGeometryMaker);
            DisplayGeometry(drawingContext, hilightgeometryDictionary, renderPoint);
            Dictionary<Decoration, List<Geometry>> strikethroughGeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Strikethrough, StrikethroughGeometryMaker);
            DisplayGeometry(drawingContext, strikethroughGeometryDictionary, renderPoint);
            Dictionary<Decoration, List<Geometry>> underlineGeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Underline, UnderlineGeometryMaker);
            DisplayGeometry(drawingContext, underlineGeometryDictionary, renderPoint);
        }
        /// <param name="visiblePair">The pair representing the first character of the Visible text with respect to the whole text</param>
        /// <param name="decorations">The List of Decorations</param>
        private Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> GeneratePreparedDecorations(Segment visiblePair, List<Decoration> decorations)
        {// The first part of the split version of Display decorations.
            Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> preparedDecorations = new Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>>();
            Dictionary<Decoration, List<Geometry>> hilightgeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Hilight, HilightGeometryMaker);
            preparedDecorations.Add(EDecorationType.Hilight, hilightgeometryDictionary);
            Dictionary<Decoration, List<Geometry>> strikethroughGeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Strikethrough, StrikethroughGeometryMaker);
            preparedDecorations.Add(EDecorationType.Strikethrough, strikethroughGeometryDictionary);
            Dictionary<Decoration, List<Geometry>> underlineGeometryDictionary = PrepareGeometries(visiblePair, formattedText, decorations, EDecorationType.Underline, UnderlineGeometryMaker);
            preparedDecorations.Add(EDecorationType.Underline, underlineGeometryDictionary);
            return preparedDecorations;
        }
        /// <param name="drawingContext">The drawing Context from the OnRender</param>
        /// <param name="preparedDecorations">The previously prepared decorations</param>
        /// <param name="renderPoint">The Point representing the offset from (0,0) for rendering</param>
        private void DisplayPreparedDecorations(DrawingContext drawingContext, Dictionary<EDecorationType, Dictionary<Decoration, List<Geometry>>> preparedDecorations, Point renderPoint)
        {// The second half of the  DisplayDecorations.
            DisplayGeometry(drawingContext, preparedDecorations[EDecorationType.Hilight], renderPoint);
            DisplayGeometry(drawingContext, preparedDecorations[EDecorationType.Strikethrough], renderPoint);
            DisplayGeometry(drawingContext, preparedDecorations[EDecorationType.Underline], renderPoint);
        }
        #endregion
        private Point GetRenderPoint(int firstChar)// The first visible character
        {// Gets the Renderpoint, the top left corner of the first character displayed. Note that this can  have negative vslues when the textbox is scrolling.
            try
            {
                Rect cRect = GetRectFromCharacterIndex(firstChar);
                Point renderPoint = new Point(cRect.Left, cRect.Top);
                if (!Double.IsInfinity(cRect.Top))
                {
                    renderinfo.RenderPoint = renderPoint;
                }
                else
                {
                    this.renderTimer.IsEnabled = true;
                }
                return renderinfo.RenderPoint;
            }
            catch
            {
                this.renderTimer.IsEnabled = true;
                return renderinfo.RenderPoint;
            }
        }
        private void DisplayGeometry(DrawingContext drawingContext, Dictionary<Decoration, List<Geometry>> geometryDictionary, Point renderPoint)
        {
            foreach (Decoration dec in geometryDictionary.Keys)
            {
                List<Geometry> GeomList = geometryDictionary[dec];
                foreach (Geometry g in GeomList)
                {
                    g.Transform = new System.Windows.Media.TranslateTransform(renderPoint.X, renderPoint.Y);
                    drawingContext.DrawGeometry(dec.Brush, null, g);
                }
            }
        }
        private Dictionary<Decoration, List<Geometry>> PrepareGeometries(Segment pair, FormattedText visibleFormattedText, List<Decoration> decorations, EDecorationType decorationType, GeometryMaker gMaker)
        {
            Dictionary<Decoration, List<Geometry>> geometryDictionary = new Dictionary<Decoration, List<Geometry>>();
            foreach (Decoration dec in decorations)
            {
                List<Geometry> geomList = new List<Geometry>();
                if (dec.DecorationType == decorationType)
                {
                    List<Segment> ranges = dec.Ranges(this.Text);
                    foreach (Segment p in ranges)
                    {
                        if (p.End > pair.Start && p.Start < pair.Start + VisibleText.Length)
                        {
                            int adjustedStart = Math.Max(p.Start - pair.Start, 0);
                            int adjustedLength = Math.Min(p.Length + Math.Min(p.Start - pair.Start, 0), pair.Length - adjustedStart);
                            Geometry geom = gMaker(visibleFormattedText, new Segment(adjustedStart, adjustedLength));
                            geomList.Add(geom);
                        }
                    }
                }
                geometryDictionary.Add(dec, geomList);
            }
            return geometryDictionary;
        }
        // <param name="text">The FormattedText used for the decoration</param>
        // <param name="p">The pair defining the begining character and the length of the character range</param>
        private delegate Geometry GeometryMaker(FormattedText text, Segment p);//Delegate used with the PrepareGeomeries method.
        private Geometry HilightGeometryMaker(FormattedText text, Segment p)// Creates the Geometry for the Hilight decoration, used with the GeometryMakerDelegate.
        {
            return text.BuildHighlightGeometry(new Point(0, 0), p.Start, p.Length);
        }
        private Geometry UnderlineGeometryMaker(FormattedText text, Segment p)// Creates the Geometry for the Underline decoration, used with the GeometryMakerDelegate.
        {
            Geometry geom = text.BuildHighlightGeometry(new Point(0, 0), p.Start, p.Length);
            if (geom != null)
            {
                StackedRectangleGeometryHelper srgh = new StackedRectangleGeometryHelper(geom);
                return srgh.BottomEdgeRectangleGeometry();
            }
            else
            {
                return null;
            }
        }
        private Geometry StrikethroughGeometryMaker(FormattedText text, Segment p)// Creates the Geometry for the Strikethrough decoration, used with the GeometryMakerDelegate.
        {
            Geometry geom = text.BuildHighlightGeometry(new Point(0, 0), p.Start, p.Length);
            if (geom != null)
            {
                StackedRectangleGeometryHelper srgh = new StackedRectangleGeometryHelper(geom);
                return srgh.CenterLineRectangleGeometry();
            }
            else
            {
                return null;
            }
        }
        private void EnsureScrolling()// Makes sure that the scrolling event is being listended to.
        {
            if (!mScrollingEventEnabled)
            {
                try
                {
                    DependencyObject dp = VisualTreeHelper.GetChild(this, 0);
                    dp = VisualTreeHelper.GetChild(dp, 0);
                    ScrollViewer sv = VisualTreeHelper.GetChild(dp, 0) as ScrollViewer;
                    sv.ScrollChanged += new ScrollChangedEventHandler(ScrollChanged);
                    mScrollingEventEnabled = true;
                }
                catch { }
            }
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.InvalidateVisual();
        }
        private string VisibleText// Gets the Text that is visible in the textbox. Please note that it depends on GetFirstVisibleLineIndex
        {
            get
            {
                if (this.Text == "") { return ""; }
                string visibleText = "";
                try
                {
                    int textLength = Text.Length;
                    int firstLine = GetFirstVisibleLineIndex();
                    int lastLine = GetLastVisibleLineIndex();

                    int lineCount = this.LineCount;
                    int firstChar = (firstLine == 0) ? 0 : GetCharacterIndexFromLineIndex(firstLine);

                    int lastChar = lastLine < 0 ? 0 : GetCharacterIndexFromLineIndex(lastLine) + GetLineLength(lastLine) - 1;
                    int length = lastChar - firstChar + 1;
                    int maxlenght = textLength - firstChar;
                    string text = Text.Substring(firstChar, Math.Min(maxlenght, length));
                    if (text != null)
                    {
                        visibleText = text;
                    }
                }
                catch
                {
                    Debug.WriteLine("GetVisibleText failure");
                }
                return visibleText;
            }
        }
        private Double[] VisibleLineWidthsIncludingTrailingWhitespace()// Returns the line widths for use with the wrap with overflow.
        {

            int firstLine = this.GetFirstVisibleLineIndex();
            int lastLine = Math.Max(this.GetLastVisibleLineIndex(), firstLine);
            Double[] lineWidths = new Double[lastLine - firstLine + 1];
            if (lineWidths.Length == 1)
            {
                lineWidths[0] = MeasureString(this.Text);
            }
            else
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    string lineString = this.GetLineText(i);
                    lineWidths[i - firstLine] = MeasureString(lineString);
                }
            }
            return lineWidths;
        }
        private double MeasureString(string str)// Returns the width of the string in the font and fontsize of the textbox including the trailing white space.
        {
            formattedText = new FormattedText(VisibleText, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface(this.FontFamily.Source),
                    FontSize, BaseForeground, VisualTreeHelper.GetDpi(this).PixelsPerDip) { Trimming = TextTrimming.None };  //Text that matches the textbox's
            if (str == "")
            {
                return formattedText.WidthIncludingTrailingWhitespace;
            }
            else if (str.Substring(0, 1) == "\t")
            {
                return formattedText.WidthIncludingTrailingWhitespace;
            }
            else
            {
                return formattedText.WidthIncludingTrailingWhitespace;
            }
        }
        #region line number calculations
        private FormattedText GenerateLineNumbers()// Generates the formated text used to display the line numbers. 
        {
            switch (this.TextWrapping)
            {
                case TextWrapping.NoWrap:
                    return LineNumberWithoutWrap();
                case TextWrapping.Wrap:
                    return LineNumberWithWrap();
                case TextWrapping.WrapWithOverflow:
                    return LineNumberWithWrap();
            }
            return null;
        }
        private FormattedText LineNumberWithoutWrap()// Generates FormattedText for line numbers when TextWrapping = None
        {
            int firstLine = GetFirstVisibleLineIndex();
            int lastLine = GetLastVisibleLineIndex();
            StringBuilder sb = new StringBuilder();
            for (int i = firstLine; i <= lastLine; i++)
            {
                sb.Append((i + StartingLineNumber) + "\n");
            }
            string lineNumberString = sb.ToString();
            FormattedText lineNumbers = new FormattedText(
                  lineNumberString,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface(this.FontFamily.Source),
                    this.FontSize,
                    LineNumberForeground);
            return lineNumbers;
        }
        private FormattedText LineNumberWithWrap()// Generates FormattedText for line numbers when TextWrapping = Wrap or WrapWithOverflow
        {
            try
            {
                int[] linePos = MinLineStartCharcterPositions();
                int[] lineStart = VisibleLineStartCharcterPositions();
                if (lineStart != null)
                {
                    string lineNumberString = LineNumbers(lineStart, linePos);
                    FormattedText lineText = new FormattedText(
                          lineNumberString,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(this.FontFamily.Source),
                            this.FontSize,
                            LineNumberForeground);

                    renderinfo.LineNumbers = lineText;
                    return lineText;
                }
                return renderinfo.LineNumbers;
            }
            catch
            {
                return renderinfo.LineNumbers;
            }
        }
        private int[] MinLineStartCharcterPositions()  // Returns the character positions that start lines as determined only by the characters.
        {
            int totalChars = this.Text.Length;
            char[] boxChars = this.Text.ToCharArray();
            char newlineChar = Convert.ToChar("\n");
            char returnChar = Convert.ToChar("\r");
            char formfeed = Convert.ToChar("\f");
            char vertQuote = Convert.ToChar("\v");

            List<int> breakChars = new List<int>() { 0 };

            //This looks a bit exotic but keep in mind that \r\n or \r or \n or \f or \v all will signify a new line to the textbox.
            if (boxChars.Length > 1)
            {
                for (int i = 2; i < boxChars.Length; i++)
                {
                    if (boxChars[i - 1] == returnChar && boxChars[i - 2] == newlineChar)
                        breakChars.Add(i);
                    if (boxChars[i - 1] == newlineChar && boxChars[i] != returnChar)
                        breakChars.Add(i);
                    if (boxChars[i - 1] == formfeed || boxChars[i - 1] == vertQuote)
                        breakChars.Add(i);
                }
            }
            int[] MinPositions = new int[breakChars.Count];
            breakChars.CopyTo(MinPositions);
            return MinPositions;
        }
        private int[] VisibleLineStartCharcterPositions()// Returns character positions that textbox declares to begin visible lines.
        {
            int firstLine = GetFirstVisibleLineIndex();
            int lastLine = GetLastVisibleLineIndex();
            if (lastLine != -1)
            {
                int lineCount = lastLine - firstLine + 1;
                int[] startingPositions = new int[lineCount];
                for (int i = firstLine; i <= lastLine; i++)
                {
                    int startPos = GetCharacterIndexFromLineIndex(i);
                    startingPositions[i - firstLine] = startPos;
                }
                return startingPositions;
            }
            return null;
        }

        /// Create the String of line numbers. Uses merge algorithm http://en.wikipedia.org/wiki/Merge_algorithm
        /// <param name="listA">The List of the first characters of the visible lines. This is affected by box width.</param>
        /// <param name="listB">The List of First Characters of the Lines determined by characters rather than  box width.</param>
        private string LineNumbers(int[] listA, int[] listB)
        {
            StringBuilder sb = new StringBuilder();
            int a = 0;
            int b = 0;
            List<int> matches = new List<int>();
            List<int> skipped = new List<int>();
            while (a < listA.Length && b < listB.Length)
            {
                if (listA[a] == listB[b])
                {
                    matches.Add(b);
                    a++;
                    b++;
                }
                else if (listA[a] < listB[b])
                {
                    matches.Add(-1);
                    a++;
                }
                else
                {
                    skipped.Add(b);
                    b++;
                }
            }
            while (a < listA.Length)
                a++;
            while (b < listB.Length)
                b++;

            //There will be missing lien numbers where the lines are blank. The skipped lines are returned.         
            for (int i = (skipped.Count - 1); i >= 0; i--)// in reverse because ther could be more than one sequential blank line.
            {
                int index = matches.IndexOf(skipped[i] + 1) - 1;// position directly before the index in the matches array of one greaer than the missing elements. 
                if (index > -1)
                    matches[index] = skipped[i];
            }
            //Adjusts the line numbers so that line 0 has the value of StartingLineNumber
            for (int i = 0; i < matches.Count; i++)
                if (matches[i] != -1) matches[i] += this.StartingLineNumber;
            StringBuilder sb2 = new StringBuilder();
            foreach (int i in matches)
            {
                if (i == -1)
                    sb2.Append("\n");
                else
                    sb2.Append(i + "\n");
            }
            return sb2.ToString();
        }
        #endregion
    }
}

