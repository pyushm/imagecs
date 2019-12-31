using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows;

namespace CodeEditor
{
    public enum EDecorationType// Enumeration of the available styles of text decoration
    {
        TextColor,
        Hilight,
        Underline,
        Strikethrough
    }
    public class Decoration : DependencyObject
    {   // this code adapted from web sample
        public static DependencyProperty DecorationTypeProperty = DependencyProperty.Register( // Default is TextColor
            "DecorationType", typeof(EDecorationType), typeof(Decoration), new PropertyMetadata(EDecorationType.TextColor));
        public static DependencyProperty BrushProperty = DependencyProperty.Register( // Brushed used for the decoration
            "Brush", typeof(Brush), typeof(Decoration), new PropertyMetadata(null));
        public EDecorationType DecorationType
        {
            get { return (EDecorationType)GetValue(DecorationTypeProperty); }
            set { SetValue(DecorationTypeProperty, value); }
        }
        public Brush Brush
        {
            get { return (Brush)GetValue(BrushProperty); }
            set { SetValue(BrushProperty, value); }
        }
        protected ItemsSearch parser = new ItemsSearch();
        public virtual List<Segment> Ranges(string text) { return parser.Ranges(text); }
        public virtual bool AreRangesSorted { get { return parser.AreRangesSorted; } }
        public List<string> Items { get { return parser == null ? null : parser.SearchItems; } set { if (parser != null) parser.SearchItems = value; } }
        public bool IsDirty { get; protected set; }
        public Decoration() { IsDirty = true; }
        public Decoration(Brush b) { IsDirty = true; SetValue(BrushProperty, b); }
        public Decoration(ItemsSearch p, Brush b) { IsDirty = true; parser = p; SetValue(BrushProperty, b); }
    }
    public class SegmentDecoration : Decoration
    {
        private List<Segment> segments = new List<Segment>();
        public SegmentDecoration(int start, int length, Brush brush) : base(brush) { segments = new List<Segment> { new Segment(start, length) }; }
        public SegmentDecoration(List<Segment> segs, Brush brush) : base(brush) { segments = segs; }
        public override List<Segment> Ranges(string Text) { return segments; }
        public override bool AreRangesSorted { get { return false; } }
    }
    public class ItemsDecoration : Decoration // decorates list of items  
    {
        public ItemsDecoration() { }
        public ItemsDecoration(string sample, Brush brush) : base(new ItemsSearch(sample), brush) { }
        public ItemsDecoration(List<string> strings, Brush brush) : base(new ItemsSearch(strings), brush) { }
        //public ItemsDecoration(List<string> strings, bool caseSensitive, Brush brush) : base(new ItemsSearch(strings, caseSensitive), brush) { }
    }
    public class StringDecoration : ItemsDecoration // finds strings
    {
        public StringDecoration() { }
        public StringDecoration(string sample, Brush brush) : base(sample, brush) { }
        public StringDecoration(List<string> strings, Brush brush) : base(strings, brush) { }
        //public StringDecoration(List<string> strings, bool caseSensitive, Brush brush) : base(strings, brush) { }
    }
    public class RegexDecoration : Decoration // Decoration based on a single regular expression string
    {
        public static DependencyProperty RegexStringProperty = DependencyProperty.Register(// Regular expression used to evaluate  a string
            "RegexString", typeof(String), typeof(RegexDecoration), new PropertyMetadata("", new PropertyChangedCallback(OnRegexStringChanged)));
        public string RegexString { get { return (String)GetValue(RegexStringProperty); } set { SetValue(RegexStringProperty, value); } }
        public RegexDecoration() { }
        public RegexDecoration(string regex, Brush brush) : base(brush) { RegexString = regex; }
        private static void OnRegexStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                ((RegexDecoration)d).IsDirty = true;
        }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            if (RegexString != "")
            {
                try
                {
                    Regex rx = new Regex(RegexString);
                    MatchCollection mc = rx.Matches(Text);
                    foreach (Match m in mc)
                    {
                        if (m.Length > 0)
                            pairs.Add(new Segment(m.Index, m.Length));
                    }
                }
                catch { }
            }
            IsDirty = false;
            return pairs;
        }
        public override bool AreRangesSorted { get { return true; } }
    }
    public class MultiRegexDecoration : Decoration
    {
        private List<string> mRegexStrings = new List<string>();
        public List<string> RegexStrings { get { return mRegexStrings; } set { mRegexStrings = value; } }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            foreach (string rString in mRegexStrings)
            {

                Regex rx = new Regex(rString);
                MatchCollection mc = rx.Matches(Text);
                foreach (Match m in mc)
                {
                    pairs.Add(new Segment(m.Index, m.Length));
                }
            }
            return pairs;
        }
        public override bool AreRangesSorted { get { return false; } }
    }
    public class DoubleRegexDecoration : Decoration
    {
        public static DependencyProperty OuterRegexStringProperty = DependencyProperty.Register("OuterRegexString", typeof(String), typeof(DoubleRegexDecoration),
        new PropertyMetadata("", new PropertyChangedCallback(OnRegexStringChanged)));
        public string OuterRegexString { get { return (string)GetValue(OuterRegexStringProperty); } set { SetValue(OuterRegexStringProperty, value); } }
        public static DependencyProperty InnerRegexStringProperty = DependencyProperty.Register("InnerRegexString", typeof(String), typeof(DoubleRegexDecoration),
        new PropertyMetadata("", new PropertyChangedCallback(DoubleRegexDecoration.OnRegexStringChanged)));
        public String InnerRegexString { get { return (string)GetValue(InnerRegexStringProperty); } set { SetValue(InnerRegexStringProperty, value); } }
        private static void OnRegexStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (e.NewValue != e.OldValue) ((DoubleRegexDecoration)d).IsDirty = true; }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            if (OuterRegexString != "" && InnerRegexString != "")
            {
                try
                {
                    Regex orx = new Regex(OuterRegexString);
                    Regex irx = new Regex(InnerRegexString);
                    MatchCollection omc = orx.Matches(Text);
                    foreach (Match om in omc)
                    {
                        if (om.Length > 0)
                        {
                            MatchCollection imc = irx.Matches(om.Value);
                            foreach (Match im in imc)
                            {
                                if (im.Length > 0)
                                    pairs.Add(new Segment(om.Index + im.Index, im.Length));
                            }
                        }
                    }
                }
                catch { }
            }
            IsDirty = false;
            return pairs;
        }
        public override bool AreRangesSorted { get { return true; } }
    }
    public class RegexMatchDecoration : Decoration // Decoration Based on a regular expression and a match 
    {
        public static DependencyProperty RegexStringProperty = DependencyProperty.Register("RegexString", typeof(String), typeof(RegexMatchDecoration),
            new PropertyMetadata("", new PropertyChangedCallback(RegexMatchDecoration.OnRegexStringChanged)));
        public string RegexString { get { return (String)GetValue(RegexStringProperty); } set { SetValue(RegexStringProperty, value); } }
        private static void OnRegexStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                ((RegexMatchDecoration)d).IsDirty = true;
        }
        public static DependencyProperty RegexMatchProperty = DependencyProperty.Register("RegexMatch", typeof(String), typeof(RegexMatchDecoration),
            new PropertyMetadata("selected", new PropertyChangedCallback(RegexMatchDecoration.OnRegexStringChanged)));
        public String RegexMatch// The Name of the group that to be selected, the default group is "selected"
        {
            get { return (String)GetValue(RegexMatchProperty); }
            set { SetValue(RegexMatchProperty, value); }
        }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            if (RegexString != "")
            {
                try
                {
                    Regex rx = new Regex(RegexString);
                    MatchCollection mc = rx.Matches(Text);
                    foreach (Match m in mc)
                    {
                        if (m.Length > 0)
                        {
                            pairs.Add(new Segment(m.Groups[RegexMatch].Index, m.Groups[RegexMatch].Length));
                        }
                    }
                }
                catch { }
            }
            IsDirty = false;
            return pairs;
        }
        public override bool AreRangesSorted { get { return true; } }
    }
}
