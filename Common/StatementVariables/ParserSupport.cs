using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace CodeEditor
{
    public class Segment
    {
        public int Start;
        public int Length;
        public int End { get { return Start + Length; } }
        public Segment(int start, int length) { Start = start; Length = length; }
        public override string ToString() { return "range=" + Start + '-' + End; }
    }
    public class SegmentText : Segment
    {
        public int Line;
        public string Text;
        public SegmentText(int line, int start, string text) : base(start, 1) { Line = line; Text = text; }
        public SegmentText(int line, int start, int length, string text) : base(start, length) { Line = line; Text = text; }
        public override string ToString() { return "line=" + Line + " range=" + Start + '-' + End + " text=" + Text; }
    }
    public interface ISegmentSearch
    {
        List<Segment> Ranges(string source);
        bool AreRangesSorted { get; }
        List<string> SearchItems { get; set; }
    }
    public class ItemsSearch : ISegmentSearch // list of words  
    {
        protected List<string> items = new List<string>();
        public List<string> SearchItems { get { return items; } set { items = value; } }
        public bool ItemSet { get { return items.Count > 0 && items[0] != ""; } }
        public bool Item2Set { get { return items.Count > 1 && items[1] != ""; } }
        public bool CaseSensitive;
        public virtual bool AreRangesSorted { get { return false; } }
        public ItemsSearch() { CaseSensitive = true; }
        public ItemsSearch(string sample) { items.Add(sample); CaseSensitive = true; }
        public ItemsSearch(List<string> strings) { items.AddRange(strings); CaseSensitive = true; }
        public ItemsSearch(List<string> strings, bool caseSensitive) { items.AddRange(strings); CaseSensitive = caseSensitive; }
        public virtual List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            foreach (string word in items)
            {
                string rstring = @"(?i:\b" + word + @"\b)";
                if (CaseSensitive)
                    rstring = @"\b" + word + @"\b";
                Regex rx = new Regex(rstring);
                MatchCollection mc = rx.Matches(Text);
                foreach (Match m in mc)
                    pairs.Add(new Segment(m.Index, m.Length));
            }
            return pairs;
        }
    }
    public class StringSearch : ItemsSearch // finds strings search
    {
        public StringComparison StringComparison;
        public StringSearch(string sample) : base(sample) { StringComparison = StringComparison.CurrentCulture; }
        public StringSearch(List<string> strings) : base(strings) { StringComparison = StringComparison.CurrentCulture; }
        public StringSearch(List<string> strings, bool caseSensitive) : base(strings)
        { StringComparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase; }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            foreach (string word in items)
            {
                StringComparison sc = StringComparison;
                int index = Text.IndexOf(word, 0, StringComparison);
                while (index != -1)
                {
                    pairs.Add(new Segment(index, word.Length));
                    index = Text.IndexOf(word, index + word.Length, StringComparison);
                }
            }
            return pairs;
        }
    }
    public class RegexSearch : ItemsSearch // search based on a single regular expression string
    {
        public RegexSearch(string regex) : base(regex) { }
        public string RegexString { get { return ItemSet ? items[0] : ""; } set { items = new List<string> { value }; } }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            if (ItemSet)
            {
                try
                {
                    Regex rx = new Regex(items[0]);
                    MatchCollection mc = rx.Matches(Text);
                    foreach (Match m in mc)
                    {
                        if (m.Length > 0)
                            pairs.Add(new Segment(m.Index, m.Length));
                    }
                }
                catch { }
            }
            return pairs;
        }
        public override bool AreRangesSorted { get { return true; } }
    }
    public class MultiRegexSearch : ItemsSearch
    {
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            foreach (string rString in items)
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
    public class NestedRegexSearch : RegexSearch
    {
        public string InnerRegexString { get { return Item2Set ? items[1] : ""; } set { if (Item2Set) items[1] = value; else items.Add(value); } }
        public NestedRegexSearch(string regex, string innerRegex) : base(regex) { items.Add(innerRegex); }
        public override List<Segment> Ranges(string Text)
        {
            List<Segment> pairs = new List<Segment>();
            if (ItemSet && Item2Set)
            {
                try
                {
                    Regex orx = new Regex(RegexString);
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
            return pairs;
        }
    }
    public class GroupRegexSearch : RegexSearch // search based on a regular expression and a group name 
    {
        string group = "selected";
        public string Group { get { return group; } set { group = value; } }
        public GroupRegexSearch(string regex) : base(regex) { }
        public GroupRegexSearch(string regex, string name) : base(regex) { group = name; }
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
                            pairs.Add(new Segment(m.Groups[group].Index, m.Groups[group].Length));
                        }
                    }
                }
                catch { }
            }
            return pairs;
        }
    }
}
