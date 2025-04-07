using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace CodeEditor
{
    public static class CSSyntax
    {
        public static char AssignmentSeparator { get { return '='; } }              // splitter between variable and assignment statement
        public static string SeparatorString { get { return "=<>()+-*/^ ,?:"; } }   // variable splitters 
        public static string MathPrefix { get { return "Math."; } }
        public static ItemsSearch QoutedString { get { return new RegexSearch("(?s:\".*?\")"); } }
        public static ItemsSearch CComment { get { return new RegexSearch(@"(?s:/\*.*?\*/)"); } }
        public static ItemsSearch CppComment { get { return new RegexSearch("//.*"); } }
        public static List<string> MathFunc = new List<string>();           // functions defined in Math class
        public static string[,] ElementaryFuncSynonims
        {
            get
            {
                return new string[,] {
                    { @"\babs\b",   "Abs" },
                    { @"\bacos\b",  "Acos" },
                    { @"\basin\b",  "Asin" },
                    { @"\batan\b",  "Atan" },
                    { @"\batan2\b", "Atan2" },
                    { @"\bcos\b",   "Cos" },
                    { @"\bcosh\b",  "Cosh" },
                    { @"\bexp\b",   "Exp" },
                    { @"\blog\b",   "Log" },
                    { @"\bln\b",    "Log" },
                    { @"\blog10\b", "Log10" },
                    { @"\bmax\b",   "Max" },
                    { @"\bmin\b",   "Min" },
                    { @"\bpow\b",   "Pow" },
                    { @"\bsign\b",  "Sign" },
                    { @"\bsin\b",   "Sin" },
                    { @"\bsin\b",   "Sinh" },
                    { @"\bsqrt\b",  "Sqrt" },
                    { @"\u221A",    " Sqrt" },  //(space needed to eliminate possible merge with previous symbol)
                    { @"\btan\b",   "Tan" },
                    { @"\btg\b",    "Tan" },
                    { @"\btanh\b",  "Tanh" },
                    };
            }
        }
        public static List<string> ElementaryFunctions { get; private set; }
        static CSSyntax()
        {
            ElementaryFunctions = new List<string>();
            for (int i = 0; i < ElementaryFuncSynonims.GetLength(0); i++)
            {
                string ef = ElementaryFuncSynonims[i, 1].Trim();
                if (!ElementaryFunctions.Contains(ef))
                    ElementaryFunctions.Add(ef);
            }
            MathFunc.AddRange(ElementaryFunctions);
            MathFunc.Add("PI");
        }
        public static bool IsKnownExpression(string s)
        {
            return Regex.Match(s, @"^[0-9,\.]+$").Success;
        }
        public static string AddCShMathPrefix(string src)
        {
            foreach (var mf in MathFunc)
                src = Regex.Replace(src, @"\b" + mf + @"\b", MathPrefix + mf);
            return src;
        }
    }
    public class ConvertionSyntax
    {
        public const string Independent = "t";
        public string EquationVar { get; private set; }
        public string DerivativeOperator { get { return "d/(d" + EquationVar + ')'; } }// type of operation
        public static string IntegrandVar { get { return "IntegrandVar"; } }
        public static string IntegralFnc { get { return "IntegralFnc"; } }
        public static string Integrand { get { return "Integrand"; } }          // separator of integral bounds from integrand - MEDIUM SHADE
        public static string MinRange { get { return "MinI"; } }                // min integral bound
        public static string MaxRange { get { return "MaxI"; } }                // max integral bound
        public List<string> Functions = new List<string>();                     // list of functions defined in Func class: other function used as first argument
        public ConvertionSyntax(string equationVar, string[] externalFunctions)
        {
            Functions.Add(IntegralFnc);
            Functions.AddRange(CSSyntax.ElementaryFunctions);
            if (externalFunctions != null)  //"BVal", "CVal", "BDer", "CDer"
                Functions.AddRange(externalFunctions);
            EquationVar = equationVar;
            Variable.Syntax = this;
        }
        public bool IsDerivativeOperator(ref string lhs)
        {
            if (!lhs.StartsWith(DerivativeOperator))
                return false;
            lhs = lhs.Substring(DerivativeOperator.Length);
            return true;
        }
        public bool IsSpecVar(string s) { return s== EquationVar || s== IntegrandVar; }
        public bool IsFunction(string s) { return Functions.Contains(s); }
        public bool IsKnown(string s) { return IsFunction(s) || CSSyntax.IsKnownExpression(s); }
        public Segment[] SplitSeparators(string s)
        {
            List<int> sPos = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                if (CSSyntax.SeparatorString.IndexOf(s[i]) >= 0)
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
}
