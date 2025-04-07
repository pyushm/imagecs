using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace CodeEditor
{
    public static class MSWSyntax          // convertion syntax from MSWord txt into statement language
    {
        public const char AssignmentSeparator = '=';         // splitter between variable and assignment statement
        public const char Comment = '\u2013';                 // splitter between code and comment
        public const char SubscriptSeparator = '_';
        public const char SuperscriptSeparator = '^';
        public const char MangleBegin = '(';
        public const char MangleEnd = ')';
        public const char FunctionIndicator = '\u2208';
        public const char IntegralSeparator = '\u2592'; // separator of integral bounds from integrand - MEDIUM SHADE
        public const char IntegralSymbol = '\u222b'; // separator of integral bounds from integrand - MEDIUM SHADE
        static string[,] InitialSubstitutions { get { return new string[,] {// basic replacement of MSWord txt (applied before showing imported text)
                    { "×",        "*" },           // Cross OPERATOR 
                    { "\u2219",   "*" },           // BULLET OPERATOR 
                    { "\u2044",   "/" },           // FRACTION SLASH 
                    { "\u2061",    "" },           // remove FUNCTION APPLICATION
                    { "\u3016",   " " },           // remove LEFT WHITE LENTICULAR BRACKET (space needed to eliminate possible merge with previous symbol)
                    { "\u3017",    "" },           // RIGHT WHITE LENTICULAR BRACKET;
                    { "_*",   "_star" },           // * as subscript
                    { "\u22A5","perp" },           // PERP -> 
                    { "\u2225","parl" },           // PARALLEL -> 
                    { "\u03C0"," PI " },
                    { "{",        "(" },           // figure braces are the same
                    { "}",        ")" },
                    { "\u2329",   "(" },           // ANGLE BRACKETs are the same
                    { "\u232a",   ")" },           // 
                    { " [",       "[" },           // open braket is a part of indexed variable
                    { "]",       "] " },           // close braket is a separator
                    }; } }
        static char[] operations = new char[] { '\u221A', '/' };
        //" sqrt"
        static char[,] BraketsReplacement { get { return new char[,] {// converting [ ] brakets to round braces
                    { '[',        '(' },  
                    { ']',        ')' }, 
                    }; } }
        static char[,] IndexMangle { get { return new char[,] {// array index mangling
                    { '+',   '\u2295' },        
                    { '-',   '\u2296' },        
                    { '*',   '\u2297' }
                    }; } }
        static string[] MissingMultiplicationAtBrakets { get { return new string[] {            // regex fixing * omissions
                    @"(\b[0-9][0-9]*\.?[0-9]*)\s*([a-zA-Z\p{IsGreekandCoptic}])",               // '2x', '2α' but not 'a2a'
                    @"([0-9a-zA-Z\p{IsGreekandCoptic}])\s+([0-9a-zA-Z\p{IsGreekandCoptic}])",   // '2 2', '2 a', 'a 2', 'α 2', 'a α', 'α a'
                    @"([0-9a-zA-Z\p{IsGreekandCoptic}])\s+([0-9a-zA-Z\p{IsGreekandCoptic}])",   // '2 2', '2 a', 'a 2', 'α 2', 'a α', 'α a'
                    @"([0-9a-zA-Z\p{IsGreekandCoptic}])\s*([\(\u222b])",                        // '2(', 'x(', 'α(', '2∫', 'x∫', 'α∫'
                    @"([\)\]])\s*([0-9a-zA-Z\p{IsGreekandCoptic}\(])",                          // ')2', ')x', ')(', ')α', ']x', ']2', '](', ']α'
                    }; } }
        public static string CorrectEquationSource(string input)
        {
            StringBuilder lines = new StringBuilder();
            using (StringReader sr = new StringReader(input))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    if (line.Length > 0)
                        lines.AppendLine(ApplyInitialSubstitutions(line));
            }
            return lines.ToString();
        }
        static string ApplyInitialSubstitutions(string text)
        {
            int i = text.Length - 1;
            if (text[i] == ')')
            {   // remove equation numbering like (N) at the end
                while (i > 0 && char.IsDigit(text[--i]))
                    continue;
                text = i > 0 && text[i] != '(' ? text : text.Substring(0, i - 1);
            }
            // basic substitutions
            for (i = 0; i < InitialSubstitutions.GetLength(0); i++)
                text = text.Replace(InitialSubstitutions[i, 0], InitialSubstitutions[i, 1]);
            // grouping all terms abter '/' or '√'
            int iop = -1;
            while ((iop = text.IndexOfAny(operations, iop + 1)) >= 0)
            {
                int next = iop + 1;
                if (next < text.Length && text[next] == '(')
                    continue;       // b/(2*ρ), b/(2 ρ), d⁄( √( ab)) c
                int keep = text.IndexOfAny(new char[] { ' ', ')' }, iop);   // b/2ρ c  or  b/2ρ)c
                //int ibr = text.IndexOf('(', iop);
                //if (ibr < isp)
                //{
                //    ibr = text.IndexOf(')', iop);
                //    if (ibr >= 0)
                //        isp = text.IndexOf(' ', ibr);
                //}
                if (keep < 0)                        // b/2ρ
                    keep = text.Length;
                text = text.Substring(0, iop) + (text[iop] == operations[0] ? " sqrt(" : "/(") + text.Substring(iop + 1, keep - iop - 1) + ')'
                    + (keep < text.Length ? text.Substring(keep, text.Length - keep) : "");
            }
            return text;
        }
        public static string MangleSubscript(string src)
        {
            StringBuilder sb = new StringBuilder();
            bool mangle = false;
            int subscriptSeparatorInd = -2;
            for (int i = 0; i < src.Length; i++)
            {
                if (!mangle)
                {
                    if (i != subscriptSeparatorInd + 1)
                    {
                        sb.Append(src[i]);
                        if (src[i] == SubscriptSeparator)
                            subscriptSeparatorInd = i;
                    }
                    else if (src[i] == MangleBegin)
                        mangle = true;
                    else
                        sb.Append(src[i]);
                }
                else if (src[i] == MangleEnd)
                    mangle = false;
                else                // inside index magnling
                {
                    char c = src[i];
                    for (int j = 0; j < IndexMangle.GetLength(0); j++)
                        if (c == IndexMangle[j, 0])
                            c = IndexMangle[j, 1];
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        public static string UnMangleSubscript(string src)
        {
            if (src == null)
                return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                for (int j = 0; j < IndexMangle.GetLength(0); j++)
                    if (c == IndexMangle[j, 1])
                        c = IndexMangle[j, 0];
                sb.Append(c);
            }
            return sb.ToString();
        }
        public static string SplitSubSuper(string src, ref string sub, ref string super)
        {
            int l = src.IndexOf(SubscriptSeparator) + 1;
            int u = src.IndexOf(SuperscriptSeparator, l);
            super = u < 0 ? null : src.Substring(u + 1);
            sub = l == 0 ? null : u < 0 ? src.Substring(l) : src.Substring(l, u - l);
            sub = UnMangleSubscript(sub);
            return l > 0 ? src.Substring(0, l - 1) : u < 0 ? src : src.Substring(0, l - 1);
        }
        internal static string ApplyHumanAssumptions(string text, bool replaceBrakets, ConvertionSyntax convertion) // replases brakets and functions to standard, adds missing ultiplications
        {
            if (replaceBrakets)
                for (int i = 0; i < BraketsReplacement.GetLength(0); i++)
                    text = text.Replace(BraketsReplacement[i, 0], BraketsReplacement[i, 1]);
            for (int i = 0; i < CSSyntax.ElementaryFuncSynonims.GetLength(0); i++)  // replacement with standartized Elementary Functions
                text = Regex.Replace(text, CSSyntax.ElementaryFuncSynonims[i, 0], CSSyntax.ElementaryFuncSynonims[i, 1], RegexOptions.IgnoreCase);
            for (int i = 0; i < convertion.Functions.Count; i++)                    // adds FunctionIndicator to prevent '*' between function name and argument '('
                text = Regex.Replace(text, convertion.Functions[i], convertion.Functions[i] + FunctionIndicator, RegexOptions.IgnoreCase);
            foreach (var p in MissingMultiplicationAtBrakets)                       // adding '*' in human default assumptions
                text = Regex.Replace(text, p, "${1}*${2}");
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)                                                // removes FunctionIndicator
                if (c != FunctionIndicator)
                    sb.Append(c);
            return sb.ToString();
        }
    }
}