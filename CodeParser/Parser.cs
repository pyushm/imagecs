using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor
{   // this code adapted from web sample
    public class Segment
    {
        public int Start;
        public int Length;
        public int End { get { return Start + Length; } }
        public Segment(int start, int length) { Start = start; Length = length; }
        public override string ToString() { return "range=" + Start + '-' + End; }
    }
    public class SegmentLine : Segment
    {
        public int Line;
        public SegmentLine(int line, int start, int length) : base(start, length) { Line = line; }
        public override string ToString() { return "line=" + Line + " range=" + Start + '-' + End; }
    }
    public interface ISegmentSearch
    {
        List<Segment> Ranges(string source);
        bool AreRangesSorted { get; }
        List<string> SearchItems { get; set; }
    }
    public class TextParser
    {
        public static ItemsSearch QoutedString { get { return new RegexSearch("(?s:\".*?\")"); } }
        public static ItemsSearch CComment { get { return new RegexSearch(@"(?s:/\*.*?\*/)"); } }
        public static ItemsSearch CppComment { get { return new RegexSearch("//.*"); } }
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
        string group="selected";
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
    public class AssignmentParser
    {   // hadlles collection of assigmnet statements: "[operator] 'variable' 'assignment' 'function of variables'"
        public class Syntax             // describes assigmnet statement
        {   
            public List<string> KnownVariables = new List<string>();    // known variables: do not touch
            public List<string> KnownRegex = new List<string>();        // known strings (matchin regex): do not touch
            public string SeparatorString = "=()+-*/ ";                 // variable splitters 
            public List<string> Operators = new List<string>();         // type of operation
            public string Assignment = "=";                             // splitter between variable and function
            public string ConstantName;                                 // name of constant array in C# implementation
            public string ExpressionName;                               // name of expression array in C# implementation
            public string VariableName;                                 // name of function array in C# implementation
            public List<string> ReplacementRegex = new List<string>();  // regex fixing default omissions in original model; e.g. 2t -> 2*t
            public bool IsKnown(string s)
            {
                if (KnownVariables.Contains(s))
                    return true;
                foreach (var rxs in KnownRegex)
                {
                    Regex rx = new Regex(rxs);
                    if (rx.IsMatch(s))
                        return true;
                }
                return false;
            }
            public int OperatorIndex(string s)
            {
                for (int i = 0; i < Operators.Count; i++)
                    if (s.IndexOf(Operators[i]) >= 0)
                        return i;
                return -1;
            }
        }
        public class Statement          // parses assignment statement
        {   
            public static Syntax Syntax;
            string source;              // statement input
            string corrected;           // statement with fixed syntax
            string assigned;            // variable assigned by statement
            string expression;          // expression defining assigned variable
            string[] usedVariables;     // variables used in expression
            string warning;             // parsing warnings
            int operatorIndex;          // based on operator index (no operator: -1)
            public int OperatorIndex { get { return operatorIndex; } }
            public string Warning { get { return warning; } }
            public string Source { get { return source; } }
            public string Assigned { get { return assigned; } }
            public string Expression { get { return expression; } }
            public string[] UsedVariables { get { return usedVariables; } }
            public Statement(string l) { source = l; operatorIndex = -1; }
            public void Parse()
            {
                FixMissingMultiplication();
                int ieq = corrected.IndexOf(Syntax.Assignment);
                Debug.Assert(ieq >= 0);
                string lhs = corrected.Substring(0, ieq);
                operatorIndex = Syntax.OperatorIndex(lhs);
                if (operatorIndex >= 0)
                {   // remove operator
                    string op = Syntax.Operators[operatorIndex];
                    lhs = lhs.Substring(lhs.IndexOf(op) + op.Length);
                }
                string[] lhsVar = lhs.Split(Syntax.SeparatorString.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lhsVar.Length != 1)
                {
                    warning = "LHS has to be a variable";
                    return;
                }
                else
                    assigned = lhsVar[0];
                int iRHS = ieq + 1;
                expression = corrected.Substring(iRHS);
                Segment[] segs = SplitSeparators(expression);   // finds all symbol segments
                if (segs.Length > 0)
                {
                    List<string> vars = new List<string>();
                    for (int i = 0; i < segs.Length; i++)
                    {
                        string s = corrected.Substring(segs[i].Start + iRHS, segs[i].Length);
                        if (!Syntax.IsKnown(s) && !vars.Contains(s))
                            vars.Add(s);
                    }
                    usedVariables = vars.ToArray();              // array of 
                }
            }
            void FixMissingMultiplication()
            {
                corrected = source;
                foreach (var p in Syntax.ReplacementRegex)
                    corrected = Regex.Replace(corrected, p, "${1}*${2}");
            }
            Segment[] SplitSeparators(string s)
            {
                List<int> sPos = new List<int>();
                for (int i = 0; i < s.Length; i++)
                {
                    if (Syntax.SeparatorString.IndexOf(s[i]) >= 0)
                        sPos.Add(i);
                }
                List<Segment> segs = new List<Segment>();
                int start = -1;
                for (int i = 0; i < sPos.Count; i++)
                {
                    int sl = sPos[i] - start - 1;
                    if (sl > 0)
                        segs.Add(new Segment(start + 1, sl));
                    start = sPos[i];
                }
                int sle = s.Length - start - 1;
                if (sle > 0)
                    segs.Add(new Segment(start + 1, sle));
                return segs.ToArray();
            }
        }

        List<string> input;
        List<Statement> expressions = new List<Statement>();
        List<Statement> equations = new List<Statement>();
        public Statement[] Equations { get { return equations.ToArray(); } }
        public Statement[] Expressions { get { return expressions.ToArray(); } }
        public AssignmentParser(Syntax syntax, List<string> inp)
        {
            Statement.Syntax = syntax;
            input = inp;
        }
        public void CollectStatements()
        {
            foreach (var s in input)
            {
                if (s.IndexOf(Statement.Syntax.Assignment) < 0)
                    continue;
                Statement asm = new Statement(s);
                asm.Parse();
                if(asm.OperatorIndex < 0)
                    expressions.Add(asm);
                else
                    equations.Add(asm);
            }
        }
    }
}
