using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodeEditor
{
    public delegate void TextChanged();
    public partial class EditorWindow : Window // Interaction logic for ModelWindow.xaml
    {
        public Brush HighlightColor;// = Brushes.Wheat;
        public Brush ErrorColor;// = Brushes.Brown;
        public string Text { get { return codeBox.Text; } set { codeBox.Text = value; } }
        public event TextChanged TextChanged;
        public EditorWindow(CodeViewHelper shader)
        {
            InitializeComponent();
            HighlightColor = Brushes.Wheat;
            ErrorColor = Brushes.Brown;
            shader.Add(new Decoration(TextParser.QoutedString, Brushes.RosyBrown)); // quotedText
            shader.Add(new Decoration(TextParser.CComment, Brushes.Green)); // multiline comments
            shader.Add(new Decoration(TextParser.CppComment, Brushes.Green)); // single line comments

            codeBox.ViewHelper = shader;
            codeBox.FontFamily = new FontFamily("Consolas");
            codeBox.FontSize = 14d;
            codeBox.TextChanged += delegate (object sender, TextChangedEventArgs e) { if (TextChanged != null) TextChanged(); };
        }
        public void HilightErrors(List<SegmentLine> errors)
        {
            int[] lstart = codeBox.LineStart;
            if (lstart == null)
                return;
            List<Segment> errorSegments = new List<Segment>();
            foreach (var seg in errors)
            {
                //Debug.WriteLine(seg.ToString());
                if (seg.Line > 0 && seg.Line <= lstart.Length)
                {
                    Segment s = new Segment(seg.Start + lstart[seg.Line - 1] - 1, seg.Length);
                    //Debug.WriteLine(s.ToString());
                    errorSegments.Add(s);
                }
            }
            codeBox.ClearAddedDecorations();
            SegmentDecoration ed = new SegmentDecoration(errorSegments, ErrorColor);
            ed.DecorationType = EDecorationType.Underline;
            codeBox.Decorations.Add(ed);
            codeBox.InvalidateVisual();
        }
        void Window_Loaded(object sender, RoutedEventArgs e) { }
        private void RemoveVisibleInsertionPoint()
        {
            if (codeBox.VerticalOffset == 0)
            {
                string cachedText = codeBox.Text;
                codeBox.IsReadOnly = true;
                codeBox.Clear();
                UpdateLayout();
                codeBox.Text = cachedText;
            }
            else
            {
                double cachedVerticalOffset = codeBox.VerticalOffset;
                codeBox.IsReadOnly = true;
                codeBox.ScrollToEnd();
                codeBox.ScrollToHome();
                codeBox.ScrollToVerticalOffset(cachedVerticalOffset);
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void clearButtonClick(object sender, RoutedEventArgs e)
        {
            codeBox.ClearAddedDecorations();
            findBox.Text = null;
        }
        private void findButtonClick(object sender, RoutedEventArgs e)
        {
            codeBox.ClearAddedDecorations();
            if (findBox.Text == null || findBox.Text.Length == 0)
                return;
            StringDecoration ed = new StringDecoration(findBox.Text, HighlightColor);
            ed.DecorationType = EDecorationType.Hilight;
            codeBox.Decorations.Add(ed);
            codeBox.InvalidateVisual();
        }
    }
}
