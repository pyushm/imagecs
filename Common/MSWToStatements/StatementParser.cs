using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace CodeEditor
{
    public class StatementParser                    // converts MSWord txt statement into Model language
    {
        static public string RunInfoKey { get { return "run:"; } }
        static public string OutputKey { get { return "plot:"; } }
        static public string[] SpecialLineKeys = new string[] { RunInfoKey, OutputKey, "variable:", "functions:" };
        List<string> tokens = new List<string>();
        List<string> tokenNames = new List<string>();
        Dictionary<string, string[]> SpecialLines;
        string errors = "";
        string rhs;
        int internalCount = 0;          // counter of statements with added supplement variables
        public string[] GetVariables(string key, string[] ext = null) 
        {
            string[] vars;
            if(SpecialLines.TryGetValue(key, out vars))
                return vars;
            if (ext == null)
                return new string[0];
            List<string> ls = new List<string>();
            foreach (var f in ext)
                if (f.Trim().Length != 0)
                    ls.Add(f.Trim());
            return ls.ToArray();
        }
        public string GetVariable(string key, string ext) 
        { 
            string[] l = GetVariables(key, new string[] { ext }); 
            return l.Length>0 && l[0].Length > 0 ? l[0] : ext; 
        }
        public Statement[] Supplements { get; private set; }    // supplemental assignement of statement (e.g. min/max integration range)
        public void ResetSupplements() { Supplements = null; }
        public string Errors { get { return errors; } }
        Statement ParseMSWSource(string text, bool modelEq, bool replaceBrakets, ConvertionSyntax convertion) 
        {
            ResetSupplements();
            string orig = text;
            try
            {
                text = text.Trim();
                if (text.Length == 0)
                    return null;
                int ict = text.IndexOf(MSWSyntax.Comment);
                string cmnt = ict < 0 ? "" : text.Substring(ict+1);
                if (ict >= 0)
                    text = text.Substring(0, ict);
                text = MSWSyntax.ApplyHumanAssumptions(text, replaceBrakets, convertion);
                text = MSWSyntax.MangleSubscript(text);
                errors = "";
                int ieq = text.IndexOf(MSWSyntax.AssignmentSeparator);
                string lhs = ieq >= 0 ? text.Substring(0, ieq) : text;
                bool derivative = convertion.IsDerivativeOperator(ref lhs);
                string[] lhsVar = lhs.Split(CSSyntax.SeparatorString.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (modelEq && (lhsVar.Length != 1 || lhsVar[0].Length == 0 || char.IsDigit(lhsVar[0][0])))
                {
                    if(ieq>0)
                        errors += " LHS is not " + (lhsVar.Length < 1 ? "defined" : "a variable");
                    return null;
                }
                string var = lhsVar.Length == 1 ? lhsVar[0] : "";
                int iRHS = text.LastIndexOf(MSWSyntax.AssignmentSeparator) + 1;
                if (!modelEq || iRHS > 0)
                {
                    rhs = text.Substring(iRHS);
                    int integralPos = rhs.IndexOf(MSWSyntax.IntegralSymbol);
                    if (integralPos >= 0)
                        rhs = rhs.Substring(0, integralPos) + CreateIntegralStatement(rhs.Substring(integralPos));
                    rhs = Tokenize(rhs);
                }
                else
                    rhs = "";
                return new Statement(derivative, var, rhs, ieq >= 0, cmnt);
            }
            catch(Exception ex)
            {
                errors += "converted from:" + Environment.NewLine + orig + Environment.NewLine + "to:" + Environment.NewLine + text + Environment.NewLine + ex.Message;
                return null;
            }
        }
        public string ReplaceSubscriptWithIndex(Statement res)
        {
            VariableDefinition va = new VariableDefinition(res);
            string lhs = res.LHS;
            string sub=null;
            string super=null;
            lhs = MSWSyntax.SplitSubSuper(res.LHS, ref sub, ref super);
            if (sub != null)
                lhs += '[' + sub + ']';
            string[] uv = va.GetUsedVars();
            string eq = res.RHS;
            if (uv != null)
            {
                Array.Sort(uv);
                for (int i = uv.Length-1; i >= 0; i--)
                {   // longest matches first
                    string vn = MSWSyntax.SplitSubSuper(uv[i], ref sub, ref super);
                    if (sub != null)
                        eq = Regex.Replace(eq, @"\b" + uv[i] + @"\b", vn + '[' + sub + ']');
                }
            }
            return lhs + " = " + eq + ';';
        }
        public string[] ReadSource(string equationsSource)
        {
            SpecialLines = new Dictionary<string, string[]>();
            List<string> statementLines = new List<string>();
            using (StringReader sr = new StringReader(equationsSource))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    bool isStatement = true;
                    foreach (var sk in SpecialLineKeys)
                    {
                        if (s.StartsWith(sk, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!SpecialLines.ContainsKey(sk))
                            {
                                int pos = s.IndexOf(sk, StringComparison.OrdinalIgnoreCase);
                                string input = s.Substring(pos + sk.Length);
                                string[] inputs = input.Split(new char[] { ',' });
                                for (int i = 0; i < inputs.Length; i++)
                                    inputs[i] = inputs[i].Trim();
                                if(inputs[0].Length > 0)
                                    SpecialLines.Add(sk, inputs);
                            }
                            isStatement = false;
                        }
                    }
                    if(isStatement)
                        statementLines.Add(s);
                }
            }
            return statementLines.ToArray();
        }
        public List<Statement> ProcessEquationLines(string[] equationLines, bool modelEq, bool replaceBrakets, ConvertionSyntax convertion)
        {
            StringBuilder sb = new StringBuilder();
            string ss = "";
            List<Statement> statements = new List<Statement>();
            try
            {
                foreach (string s in equationLines)
                {
                    ss = s;
                    Statement res = ParseMSWSource(s, modelEq, replaceBrakets, convertion);
                    if (res == null)
                        continue;
                    if (Supplements != null)
                    {
                        foreach (var sup in Supplements)
                            statements.Add(sup);
                    }
                    statements.Add(res);
                }
                errors += sb.ToString();
                return statements;
            }
            catch (Exception ex)
            {
                errors += Environment.NewLine + sb.ToString() + Environment.NewLine + ex.Message + Environment.NewLine + "line: " + ss;
                return statements;
            }
        }
        string CreateIntegralStatement(string s)
        {
            string varInd = internalCount.ToString();
            Supplements = new Statement[3];
            string[] integralVars = new string[4];  // lowBound, highBound, integralFunc, var
            int idiv = s.IndexOf(MSWSyntax.IntegralSeparator);
            MSWSyntax.SplitSubSuper(s.Substring(0, idiv), ref integralVars[0], ref integralVars[1]);
            string integrand = s.Substring(idiv + 1);
            int dpos = integrand.LastIndexOf('d');
            string[] diff = Regex.Split(integrand, "([d])");
            if (dpos < 2 || dpos > integrand.Length-2)
            {
                integralVars[2] = integralVars[3] = "";
                errors += "Integration expression undefined - has to be <integrand>d<var>";
            }
            else
            {
                integralVars[2] = integrand.Substring(0, dpos - 1);
                integralVars[3] = integrand.Substring(dpos + 1);
            }
            string lowBound = ConvertionSyntax.MinRange + varInd;
            string highBound = ConvertionSyntax.MaxRange + varInd;
            string integrandFunc = ConvertionSyntax.Integrand + varInd;
            Supplements[0] = new Statement(lowBound, integralVars[0]);
            Supplements[1] = new Statement(highBound, integralVars[1]);
            integralVars[2] = Regex.Replace(integralVars[2], @"\b" + integralVars[3] + @"\b", ConvertionSyntax.IntegrandVar);
            Supplements[2] = new Statement(integrandFunc, Tokenize(integralVars[2]));
            internalCount++;
            return ConvertionSyntax.IntegralFnc + '(' + integrandFunc + ", " + lowBound + ", " + highBound + ')';
        }
        string Tokenize(string rhs)
        {
            tokens.Clear();
            tokens.Add(rhs);
            tokenNames.Clear();
            tokenNames.Add("xpr0");
            string pattern = "([()])";
            string[] split_eqn = Regex.Split(rhs, pattern);
            int nvar = 1;
            for (int i = 0; i < split_eqn.Length; i++)
            {
                //Debug.WriteLine(var);
                if (split_eqn[i] == "(")
                {
                    string name = String.Format("xpr{0}", nvar);
                    string var = GetVariable(split_eqn, i);
                    //Debug.WriteLine(var);
                    tokens.Add(var);
                    tokenNames.Add(name);
                    nvar++;
                }
            }
            for (int i = 0; i < tokens.Count; i++)
            {
                for (int j = i + 1; j < tokens.Count; j++)
                    tokens[i] = Regex.Replace(tokens[i], Regex.Escape(tokens[j]), tokenNames[j]);
            }
            for (int i = 0; i < tokens.Count; i++)
            {
                if (!tokens[i].Contains("^"))
                    continue;
                string[] split_token = Regex.Split(tokens[i], @"([\^\*/\+\-\(\)])");
                for (int j = 0; j < split_token.Length; j++)
                {
                    if (split_token[j] != "^")
                        continue;
                    string old_pwr = split_token[j - 1] + "^" + split_token[j + 1];
                    string new_pwr = "Pow(" + split_token[j - 1] + ", " + split_token[j + 1] + ")";
                    //Debug.WriteLine("'"+old_pwr+"' replaced with: '"+new_pwr+"' in " + Tokens[i]);
                    tokens[i] = Regex.Replace(tokens[i], Regex.Escape(old_pwr), new_pwr);
                    //Debug.WriteLine(Tokens[i]);
                }
            }
            string new_corrected = tokens[0];
            for (int i = 1; i < tokens.Count; i++)
                new_corrected = Regex.Replace(new_corrected, tokenNames[i], tokens[i]);
            return new_corrected;
        }
        string GetVariable(string[] s, int i)
        {
            int li = 1;
            int ri = 0;
            int j = i + 1;
            string newvar = s[i];
            while (li > ri)
            {
                if (s[j] == "(") li++;
                if (s[j] == ")") ri++;
                newvar = newvar + s[j];
                j++;
                if (j >= s.Length)
                {
                    errors += "Unclosed parenthesis: " + rhs;
                    break;
                }
            }
            return newvar;
        }
    }
}