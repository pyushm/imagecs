using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CodeEditor
{
    public class VariableDefinition // assigment statements: "[operator] 'variable' 'assignment' 'function of variables'"
    {
        static int areaIndex = 0;   // area counter 
        static int areasCount = 6;  // number of show areas
        static int AreaIndex { get { if (areaIndex >= areasCount) areaIndex = 0; return areaIndex++; } } // default show area index
        public static void Reset() { areaIndex = 0; }
        public string source;                               // source statement
        public string Name { get; private set; }            // variable assigned by statement
        public string RHS { get; private set; }             // RHS of assignment statement
        public bool IsDerivative { get; private set; }      // based on operator index (no operator: -1)
        public string AssignementType { get { return IsDerivative ? Variable.Syntax.DerivativeOperator : ""; } }
        public string Description { get; set; }             // variable description
        public bool Assigned { get { return RHS.Length != 0; } }
        public string Source { get { return source != null ? source : (AssignementType.Length == 0 ? Name : AssignementType + '(' + Name + ')') + (Assigned ? "" : CSSyntax.AssignmentSeparator + RHS); } }
        public VariableDefinition(string src, string desc)
        {   // from unassigned variavle 
            RHS = "";
            Description = desc;
            source = src;
        }
        public VariableDefinition(Variable v)
        {   // from unassigned variavle 
            IsDerivative = v.Type == Group.Var;
            Name = v.Name;
            RHS = "";
            Description = "";
        }
        public VariableDefinition(Statement statement)
        {   // from parsing of model source text 
            IsDerivative = statement.IsDerivative;
            Name = statement.LHS;
            RHS = statement.RHS;
            Description = statement.Description;
        }
        public VariableDefinition(bool derivative, string var, string rhs, string show)
        {   // from storred model
            IsDerivative = derivative;
            Name = var;
            RHS = rhs.Trim();
            Description = "";
        }
        public void CopyFrom(VariableDefinition va)
        {   // any changes
            IsDerivative = va.IsDerivative;
            Name = va.Name;
            RHS = va.RHS;
            Description = va.Description;
        }
        public string[] GetUsedVars()
        {
            if (RHS == null || RHS.Length == 0)
                return null;
            List<string> usedVars = new List<string>();
            List<Segment> eqVarList = new List<Segment>();
            Segment[] segs = Variable.Syntax.SplitSeparators(RHS);   // finds all symbol segments
            if (segs.Length > 0)
            {
                for (int i = 0; i < segs.Length; i++)
                {
                    string s = RHS.Substring(segs[i].Start, segs[i].Length);
                    if (!Variable.Syntax.IsKnown(s) && !usedVars.Contains(s))
                        usedVars.Add(s);
                }
            }
            return usedVars.ToArray();
        }
    }
}
