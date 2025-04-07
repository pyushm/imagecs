using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

namespace CodeEditor
{
    public class ListException : Exception
    {
        public List<string> Messages { get; private set; }
        public ListException(List<string> errors) { Messages = errors; }
    }
    public enum Group   // variable groups
    {
        Var,        // variable used in time evolution equations
        Expr,       // defines value on which variable may depend
        Funct,      // function with blind variable used in integration
        Ini,        // expression defining intial value of variable
        Const,      // constant value (only initial defined) 
        Length      // always last: number of groups
    }
    public class Variable
    {
        public static ConvertionSyntax Syntax { get; set; }
        public static string GroupName(Group g)
        {
            char t = ' ';
            switch(g)
            {
                case Group.Const:   t = '\u0109'; break;
                case Group.Expr:    t = '\u0113'; break;
                case Group.Var:     t = '\u0233'; break;
                case Group.Ini:     t = '\u0438'; break;
                case Group.Funct:   t = '_';      break;
            }
            return new string(new char[] {t});
        }
        public static Group FindGroup(string gname)
        {
            foreach (Group g in Enum.GetValues(typeof(Group)))
                if (g.ToString() == gname)
                    return g;
            return Group.Length;
        }
        public object Tag = null;
        public VariableDefinition Definition { get; set; } // assignment statement
        public Variable InitialValue = null;
        public bool Defined { get { return Definition != null; } }
        public bool Assigned { get { return Defined && Definition.Assigned; } }
        public string Name { get; private set; }
        public string FunctionText { get { return "return " + CreateModelRHS() + ';'; } }
        public string Source { get { return Defined ? Definition.Source.Trim() : ""; } }
        public string RHS { get { return Defined ? Definition.RHS : ""; } }
        public string Description { get { return Defined ? Definition.Description : ""; } }
        public int Index { get; set; }                          // sequence number in 'type' group (-1 means 'not set')
        public bool IsDerivative { get { return Defined && Definition.IsDerivative; } }
        public Group Type { get; set; } = Group.Const;          // evolution type
        public Variable[] Dependency { get; set; }              // variable that has to be defined before 'var'
        public bool IsConst { get { return Type == Group.Const; } }
        public bool IsExpression { get { return Type == Group.Expr; } }
        public bool IsEquation { get { return Type == Group.Var; } }
        public string CArrayVar { get { return string.Format("{0}[{1}]", GroupName(Type), Index); } }
        string COutputArrayVariable { get { return string.Format("{0}[{1}]", IsEquation ? "Variables" : IsExpression ? "Expressions" : GroupName(Type), Index); } }
        public string CDeclaration { get { return Attributes + Environment.NewLine + "public double " + Name + 
                    " { get { return " + COutputArrayVariable + "; } set { " + COutputArrayVariable + " = value; } }"; } }
        public string MethodName { get { return (Type == Group.Ini ? Variable.GroupName(Group.Ini) : Variable.GroupName(Group.Funct)) + Name; } }
        string Attributes { get { return (Description.Length > 0 ? "[Description(\"" + Description + "\"), Category(\"" : "[Category(\"") +
                                    Type.ToString() + ": " + Source + "\")]"; } }
        public Variable(string name_) { Name = name_; Index = -1; }
        public int DependencyCount(List<Variable> sorted)
        {
            if (Dependency == null)
                return 0;
            int dc = 0;
            foreach (var v in Dependency)
            {
                if (v.Type == Group.Expr && !sorted.Contains(v))
                    dc++;
            }
            return dc;
        }
        public string CreateModelRHS()
        {   // replaces original variable names with model CS array name
            string eq = CSSyntax.AddCShMathPrefix(RHS.Trim());
            if (Dependency != null)
            {
                List<string> errors = new List<string>();
                errors.Add("Variable dependencies");
                foreach (var v in Dependency)
                {
                    try { eq = Regex.Replace(eq, @"\b" + v.Name + @"\b", v.CArrayVar); }
                    catch { errors.Add("'" + Name + "' depends on '" + v.Name +"' ?"); }
                }
                if (errors.Count > 1)
                    throw new ListException(errors);
            }
            eq = Regex.Replace(eq, @"\b" + Syntax.EquationVar + @"\b", ConvertionSyntax.Independent);
            int ib = eq.IndexOf(ConvertionSyntax.IntegralFnc); // begin of Integral
            if (ib >= 0)
            {   // expression has to be ... Integral(func, min, max) ...
                ib = eq.IndexOf(Group.Funct.ToString(), ib + ConvertionSyntax.IntegralFnc.Length) + Group.Funct.ToString().Length;
                ib = eq.IndexOf('[', ib);          // end of Integrand
                StringBuilder sb = new StringBuilder(eq.Substring(0, ib));
                int ie = eq.IndexOf(']', ib);
                sb.Append(eq.Substring(ib + 1, ie - ib - 1));
                sb.Append(", t, " + Group.Expr.ToString() + ", " + Group.Var.ToString() + eq.Substring(ie + 1));
                eq = sb.ToString();
            }
            return eq.Length == 0 ? "double.NaN" : eq.Trim();
        }
        public Variable[] ValidateDependencyOrder()
        {
            if (Dependency == null)
                return new Variable[0];
            List<Variable> err = new List<Variable>(Dependency.Length);
            foreach (Variable dv in Dependency)
                if (dv.Type == Group.Expr && dv.Index >= Index)
                    err.Add(dv);
            return err.ToArray();
        }
        public bool DependOn(Variable v)
        {
            if(Type == Group.Var || Dependency==null)
                return false;
            foreach (var p in Dependency)
                if (v.Name == p.Name)
                    return true;
            return false;
        }
        public string ToDependencyString()
        {
            string ret = Type.ToString()+' '+ Name;
            if (Dependency != null && Dependency.Length > 0)
            {
                ret += " <= ";
                foreach (Variable v in Dependency)
                    ret += v.Name+", ";
            }
            return ret;
            //return RHS == null ? ret : ret + " [" + RHS + ']';
        }
    }
    public class VariableList : Dictionary<string, Variable>
    {
        static List<string> warnings = new List<string>();
        public static Variable[][] CreateVariables(List<VariableDefinition> definitions)
        {
            VariableList dictionary = new VariableList();           // list of model variables
            foreach (var a in definitions)
                dictionary.AddAssignedVariables(a);
            return dictionary.CreateTypeLists();
        }
        List<Variable>[] typeVariables = new List<Variable>[Enum.GetValues(typeof(Group)).Length];
        public string[] Warnings { get { return warnings.ToArray(); } }
        public Variable FindOrAdd(string vname)
        {
            Variable v;
            if (!TryGetValue(vname, out v))
            {
                v = new Variable(vname); // default: show
                Add(vname, v);
            }
            return v;
        }
        public void AddAssignedVariables(VariableDefinition definition)
        {
            Variable old = null;
            Variable v = FindOrAdd(definition.Name);
            if (v.Defined)
            {
                if (v.Assigned && definition.Assigned)
                {   // 2 assignments is needed to define initial value of variable
                    old = v;
                    v = new Variable(definition.Name);
                    if ((old.Type != Group.Var && !definition.IsDerivative) || (old.Type == Group.Var && definition.IsDerivative))
                    {
                        warnings.Add("Variable " + definition.Name + " already assigned: new assignement ignored");
                        return;
                    }
                    else if (definition.Description.Length > 0)
                        old.Definition.Description = definition.Description;
                }
                else
                {
                    if (v.Assigned) // one more definition is a description only - no assignement
                    {
                        v.Definition.Description = definition.Description;
                        return;
                    }
                    else
                        definition.Description = v.Definition.Description;
                }
            }
            v.Definition = definition;
            if (v.Name.StartsWith(ConvertionSyntax.Integrand))
                v.Type = Group.Funct;
            string[] rhsVars = definition.GetUsedVars();
            if (rhsVars == null)
                    return;
            int fncInd = -1;
            bool useSpecVar = false;
            List<Variable> dep = new List<Variable>(rhsVars.Length);
            for (int i = 0; i < rhsVars.Length; i++)
            {
                string s = rhsVars[i];
                if (Variable.Syntax.IsSpecVar(s))
                    useSpecVar = true;
                else if (Variable.Syntax.IsFunction(s))
                    fncInd = i;
                else
                    dep.Add(FindOrAdd(s));
            }
            if (dep.Count > 0)
                v.Dependency = dep.ToArray();
            if (v.Type != Group.Funct)
                v.Type = definition.IsDerivative ? Group.Var :
                    v.Dependency == null && !useSpecVar ? Group.Const : Group.Expr;
            if (v.Type == Group.Expr && v.DependOn(v))
                warnings.Add("Variable " + v.Name + " depends on itself");
            if (v.Type == Group.Var && old == null)
            {   // set undefined initial value if it is not available
                Variable ini = new Variable(v.Name);
                ini.Type = Group.Ini;
                v.InitialValue = ini;
            }
            if (old != null && v != null)
            {   // 2 assignments with the same name: variable and initial
                Variable var = old.Type == Group.Var ? old : v.Type == Group.Var ? v : null;
                Variable ini = old.Type == Group.Var ? v : v.Type == Group.Var ? old : null;
                if (var != null)
                {
                    ini.Type = Group.Ini;
                    var.InitialValue = ini;
                }
                else
                    warnings.Add("Variable " + v.Name + " has 2 inconsistent assignments");
            }
        }
        public Variable[][] CreateTypeLists()
        {
            for (int i = 0; i < typeVariables.Length; i++)
                typeVariables[i] = new List<Variable>();
            int[] gcount = new int[typeVariables.Length]; // variable count in each group
            var all = Values.ToList();  // all variables in dictionary
            List<string> dependVars = new List<string>();
            foreach( var v in all)
            {
                if(v.Dependency==null)
                    continue;
                foreach (var dv in v.Dependency)
                    if (!dependVars.Contains(dv.Name))
                        dependVars.Add(dv.Name);
            }
            foreach (var v in all)
            {
                if (!dependVars.Contains(v.Name) && v.Type==Group.Const) // unused constants ignored
                    continue;
                int t = (int)v.Type;
                v.Index = gcount[t]++;
                typeVariables[t].Add(v);
            }
            Debug.WriteLine("Unsorted list " + all.Count);
            List<Variable> ev = typeVariables[(int)Group.Expr];
            List<Variable> evSorted = new List<Variable>(ev.Count);
            List<Variable> added = new List<Variable>();
            do
            {
                added.Clear();
                foreach (var v in ev)
                    if (v.DependencyCount(evSorted) == 0)
                    {
                        evSorted.Add(v);
                        added.Add(v);
                    }
                foreach (var v in added)
                    ev.Remove(v);
            } while (added.Count > 0);
            if (ev.Count > 0)
            {
                string er = "Circular dependency: Expression variables: ";
                foreach (var v in ev)
                    er += v.Name + ", ";
                warnings.Add(er);
            }
            for (int i = 0; i < evSorted.Count; i++)
                evSorted[i].Index = i;
            //DebugOutVariableList(evSorted, "Sorted");
            string[] err = ValidateDependencyOrder(evSorted);
            if (err.Length > 0)
            {
                DebugOutErrors(err, "Sorted errors");
                foreach (string s in err)
                    warnings.Add(s);
            }
            typeVariables[(int)Group.Expr] = evSorted;
            foreach (var v in typeVariables[(int)Group.Var])
            {
                Variable ini = v.InitialValue;
                if (ini != null)
                {
                    ini.Index = v.Index;
                    typeVariables[(int)Group.Ini].Add(ini);
                }
            }
            Variable[][] allv = new Variable[typeVariables.Length][];
            for(int i=0; i<allv.Length; i++)
                allv[i] = typeVariables[i].ToArray();
            return allv;
        }
        string[] ValidateDependencyOrder(List<Variable> vlist)
        {
            List<string> sl = new List<string>(); 
            foreach (Variable v in vlist)
            {
                Variable[] el = v.ValidateDependencyOrder();
                if (el.Length > 0)
                {
                    string s = v.CArrayVar + " before";
                    foreach (var ev in el)
                        s += ' ' + ev.Name;
                    sl.Add(s);
                }
            }
            return sl.ToArray();
        }
        void DebugOutErrors(string[] err, string title)
        {
            Debug.WriteLine(title + ' ' + err.Length);
            foreach (string s in err)
                Debug.WriteLine(s);
        }
        void DebugOutVariableList(List<Variable> vlist, string title)
        {
            Debug.WriteLine(title+' ' + vlist.Count);
            foreach (Variable v in vlist)
                Debug.WriteLine(v.ToDependencyString());
        }
    }
}