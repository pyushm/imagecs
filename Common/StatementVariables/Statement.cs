using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEditor
{
    public class Statement
    {
        public bool IsDerivative = false;
        public string LHS { get; set; }
        public string RHS { get; set; }
        public string Description { get; set; }
        public bool IsAssignment { get; set; }
        public string Code { get { return IsAssignment ? LHS + " = " + RHS : RHS.Length > 0 ? RHS : LHS; } }
        public Statement(bool derivative, string var, string rhs, bool eq, string ct) { IsDerivative = derivative; LHS = var; RHS = rhs; IsAssignment = eq; Description = ct.Length > 0 ? ct : ""; }
        public Statement(string var, string rhs) { LHS = var; RHS = rhs; IsAssignment = true; Description = ""; }
    }
}
