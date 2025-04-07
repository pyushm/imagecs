using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Solvers;
using Presentation;

namespace CodeEditor
{
    public class EquationDefinition
    {
        public string ModelSrc = "";    // model source text
        protected Variable[][] allVariables; // array of all variables by Group
        public Variable[] GVars(Group g) { return allVariables[(int)g]; }
        public string CArrayDeclaration(Group t) { return (t == Group.Const ? "static " : "") + "public double[] " + Variable.GroupName(t) + " = new double[" + allVariables[(int)t].Length + "];"; }
        public void Clear() { allVariables = null; VariableDefinition.Reset(); }
        public Variable GetDefinedVar(int i) { return i < GVars(Group.Var).Length ? GVars(Group.Var)[i] : GVars(Group.Expr)[i - GVars(Group.Var).Length]; } // variables + expressions
        public int DefinedVarCount { get { return GVars(Group.Var).Length + GVars(Group.Expr).Length; } }
        public EquationDefinition(Variable[][] vars) { allVariables = vars; }
        public EquationDefinition(SolverInterface model)
        {
            var vars = new List<Variable>[(int)Group.Length];
            for (int i = 0; i < (int)Group.Length; i++)
                vars[i] = new List<Variable>();
            int count = 0;
            foreach (PropertyInfo p in model.GetType().GetProperties())
            {
                object[] oa = p.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (oa.Length > 0)
                {
                    string cat = (oa[0] as CategoryAttribute).Category;
                    string[] sa = cat.Split(new char[] { ':' });
                    Group g = Variable.FindGroup(sa[0]);
                    if (g < Group.Length)
                    {
                        Variable v = new Variable(p.Name);
                        v.Type = g;
                        oa = p.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        string description = oa.Length>0 ? (oa[0] as DescriptionAttribute).Description : "";
                        if (sa.Length > 1)
                            v.Definition = new VariableDefinition(sa[1], description); // source, description
                        v.Index = vars[(int)g].Count;
                        vars[(int)g].Add(v);
                        count++;
                    }
                }
            }
            allVariables = new Variable[(int)Group.Length][];
            for (int i = 0; i < (int)Group.Length; i++)
                allVariables[i] = vars[i].ToArray();
        }
        //public Output.Panel[] CreateOutputAll(int nPanels)
        //{   // output all
        //    Output.Panel[] panels = new Output.Panel[nPanels];
        //    Output.VarGroup[] groups = new Output.VarGroup[nPanels * 2];
        //    for (int i = 0; i < panels.Length; i++)
        //        panels[i] = new Output.Panel();
        //    for (int i = 0; i < DefinedVarCount; i++)
        //    {
        //        var v = GetDefinedVar(i);
        //        int pind = i / nPanels;
        //        groups[2 * i] = panels[i].LeftSet;
        //        groups[2 * i + 1] = panels[i].RightSet;
        //        Output.VarGroup g = groups[i % groups.Length];
        //        g.AddVar(new Output.Var(v.Name, g));
        //    }
        //    return panels;
        //}
        public Output.Var[] OutputVars(Output.Var[] requestedOutput)
        {
            List<Output.Var> outList = new List<Output.Var>();
            foreach (var ov in requestedOutput)
            {
                if (ov.Name == "t")
                    outList.Add(ov);
                else
                    for (int i = 0; i < DefinedVarCount; i++)
                    {
                        var v = GetDefinedVar(i);
                        if (v.Name != ov.Name)
                            continue;
                        ov.SolverInd = v.Index + (v.Type == Group.Expr ? GVars(Group.Var).Length : 0);
                        outList.Add(ov);
                        break;
                    }
            }
            return outList.ToArray();
        }
    }
    public class Output
    {
        public class Var
        {
            public string Name { get; set; }
            public int SolverInd { get; set; }
            public VarGroup Set { get; set; }
            public int PanelIndex { get { return Set.Panel.Index; } }
            public Side Side { get { return Set.Side; } }
            public Var(string name, VarGroup g) { Name = name; SolverInd = -1; Set = g; }
            //public Var(string name, int ind, VarGroup g) { Name = name; SolverInd = ind; Set = g; }
        }
        public class VarGroup  // input of DisplaySet (variables with the same range)
        {
            public const char setSplitter = '\\';
            public const char varSplitter = '|';
            public Var[] Variables { get; private set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public Panel Panel { get; set; }
            public Side Side { get; set; }
            public VarGroup(string var, Panel p) { Variables = new Var[1]; Variables[0] = new Var(var, this); Side = Side.Bottom; Panel = p; }
            public VarGroup(string[] vars, double max, double min, Panel p) { initialize(vars); Min = min; Max = max; Side = Side.Left; Panel = p; }
            void initialize(string[] vars)
            {
                Variables = new Var[vars.Length];
                for (int i = 0; i < vars.Length; i++)
                    Variables[i] = new Var(vars[i], this);
            }
            public void AddVar(Var v)
            {
                Var[] vars = new Var[Variables.Length+1];
                Buffer.BlockCopy(Variables, 0, vars, 0, Variables.Length);
                vars[Variables.Length] = v;
                Variables = vars;
            }
            static public VarGroup FromString(string varstr, string rangestr, Panel p)
            {
                string[] vars = varstr.Split(varSplitter);
                for (int i = 0; i < vars.Length; i++)
                    vars[i] = vars[i].Trim();
                double max = 1;
                double min = 0;
                if (rangestr != null && rangestr.Length > 0)
                {
                    string[] range = rangestr.Split(varSplitter);
                    if (range.Length > 0)
                        if (!double.TryParse(range[0], out max))
                            return null;
                    if (range.Length > 1)
                        if (!double.TryParse(range[1], out min))
                            return null;
                }
                return new VarGroup(vars, max, min, p);
            }
            public override string ToString()
            {
                StringBuilder sb=new StringBuilder();
                foreach (var v in Variables)
                    sb.Append(v.Name + varSplitter);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(setSplitter);
                sb.Append(Max.ToString() + varSplitter + Min.ToString());
                return sb.ToString();
            }
        }
        public class Panel // input of DisplayArea (left and right DisplaySet)
        {   // Panel string: var1[| var2...][\max[|min][\varX]] or leftvar1[| leftvar2...]\leftmax[|leftmin]\rightvar1[| rightvar2...]\rightmax[| rightmin][\varX]
            // Panel array: Panel string[, Panel string[...]]
            public int Index { get; set; }  // panel index in OutputInterface
            public VarGroup BottomVar { get; set; }
            VarGroup lset;
            public VarGroup LeftSet { get { return lset; } set { lset = value; lset.Side = Side.Left; } }
            VarGroup rset;
            public VarGroup RightSet { get { return rset; } set { rset = value; rset.Side = Side.Right; } }
            //public Panel() { BottomVar = new VarGroup("t", this); }
            //public Panel(string[] vars, double max) { LeftSet = new VarGroup(vars, max, 0, this); }
            //public Panel(string[] vars, double max, string vx) { LeftSet = new VarGroup(vars, max, 0, this); BottomVar = new VarGroup(vx, this); }
            //public Panel(string[] vars, double max, double min) { LeftSet = new VarGroup(vars, max, min, this); }
            //public Panel(string[] vars, double max, double min, string vx) { LeftSet = new VarGroup(vars, max, min, this); BottomVar = new VarGroup(vx, this); }
            //public Panel(string[] v1, double x1, double n1, string[] v2, double x2, double n2, string vx) { LeftSet = new VarGroup(v1, x1, n1, this); RightSet = new VarGroup(v2, x2, n2, this); BottomVar = new VarGroup(vx, this); }
            public Panel(string panelstr, string eqVar = "t")
            {
                string[] sections = panelstr.Split(VarGroup.setSplitter);
                if (sections.Length == 1)
                    LeftSet = VarGroup.FromString(sections[0], null, this);
                else
                    LeftSet = VarGroup.FromString(sections[0], sections[1], this);
                if (sections.Length == 3)
                    eqVar = sections[2];
                else if (sections.Length > 3)
                    RightSet = VarGroup.FromString(sections[2], sections[3], this);
                if (sections.Length == 5)
                    eqVar = sections[4];
                BottomVar = new VarGroup(eqVar, this);
            }
            public override string ToString()
            {   return LeftSet.ToString() + (RightSet == null ? "" : VarGroup.setSplitter + RightSet.ToString()) +
                    VarGroup.setSplitter + BottomVar.Variables[0].Name; }
        }
        public List<Panel> Panels { get; private set; }
        public Output() { Panels = new List<Panel>(); }
        public void Add(Panel panel) { panel.Index = Panels.Count; Panels.Add(panel); }
        public void Add(IEnumerable<Panel> panels) { foreach (Panel p in panels) { p.Index = Panels.Count; Panels.Add(p); } }
        public Var[] AllVars
        {
            get
            {
                List<Var> all = new List<Var>();
                foreach (var p in Panels)
                {
                    all.Add(p.BottomVar.Variables[0]);
                    foreach (var v in p.LeftSet.Variables)
                        all.Add(v);
                    if (p.RightSet != null)
                        foreach (var v in p.RightSet.Variables)
                            all.Add(v);
                }
                return all.ToArray();
            }
        }
    }
    public class RunInfo
    {   // syntax: 'run: [finish[,start[,step[,nPoints]]]]
        public double Start;        // run start
        public double Finish;       // run end
        public double Step;         // calculation step
        public int NPoints;         // saved points
        public double Dt { get { return (Finish - Start) / NPoints; } }
        public RunInfo(double finish=10, double start=0, double step= 0, int np=500) 
        {
            Start = start; 
            Finish = finish; 
            NPoints = np; 
            Step = step == 0 ? Dt : step;
            if (Step * Dt < 0)
                Step = -Step;
        }
        public RunInfo(string[] inputs)
        {
            Start = 0;
            Finish = 10;
            NPoints = 500;
            Step = Dt; 
            if (inputs == null || inputs.Length == 0)
                return;
            int ninp = Math.Min(4, inputs.Length);
            int i = 0;
            double d;
            for (; i < ninp; i++)
            {
                if (!double.TryParse(inputs[i], out d))
                    break;
                else switch (i)
                    {
                        case 0: Finish = d; break;
                        case 1: Start = d; break;
                        case 2: Step = d; break;
                        case 3: NPoints = (int)d; break;
                    }
            }
            if (Step == 0 || Math.Abs(Step)> Math.Abs(Dt))
                Step = Dt;
            else if (Step * Dt < 0)
                Step = -Step;
        }
        public override string ToString() { return Finish.ToString() + ", " + Start.ToString() + ", " + Step.ToString() + ", " + NPoints.ToString(); }
    }
    public abstract class SolverInterface
    {
        public RunInfo Run { get; set; }
        public Output Output { get; private set; }
        Solvers.RungeKuttaSolver solver = null;
        protected Fnc.InitialValue[] ConstFunctions = new Fnc.InitialValue[0];
        protected Fnc.Assignment[] VarFunctions = new Fnc.Assignment[0];
        protected Fnc.Assignment[] ExprFunctions = new Fnc.Assignment[0];
        protected Fnc.Integrand[] FunctFunctions = new Fnc.Integrand[0];
        protected Fnc.InitialValue[] IniFunctions = new Fnc.InitialValue[0];
        protected Fnc.ArraySet SetConst = null;
        public double[] Variables { get { return solver.Variables; } }    // RK variable values
        public double[] Expressions { get { return solver.Expressions; } }// RK expression values
        void SetSolver(bool RK) { solver = new Solvers.RungeKuttaSolver(VarFunctions, ExprFunctions, FunctFunctions, SetConst, RK); }
        public void SetSolverOD(double t0, double[] cnst, double[] varIni) { SetSolver(false); solver.SetInitialValues(t0, varIni, cnst); }
        public void SetSolverRK(double t0, double[] cnst, double[] varIni) { SetSolver(true); solver.SetInitialValues(t0, varIni, cnst); }
        public SolverInterface() { Output = new Output(); }
        protected double[] getInitial(Fnc.InitialValue[] iniFunctions, double def)
        {
            int n = iniFunctions.Length;
            double[] vals = new double[n];
            for (int i = 0; i < n; i++)
            {
                vals[i] = iniFunctions[i] == null ? double.NaN : iniFunctions[i]();
                if (double.IsNaN(vals[i]))
                    vals[i] = def;
            }
            return vals;
        }
        public double[] GetConstantsFromModel(double def) { return getInitial(ConstFunctions, def); }
        public double[] GetInitialVarsFromModel(double def) { return getInitial(IniFunctions, def); }
        public double GetValue(int i) { return i < Variables.Length ? Variables[i] : Expressions[i - Variables.Length]; }
        public double TimeStep(double tstep) { return solver.TimeStep(tstep) ? solver.Time : double.NaN; }
        public double NextTimePoint(double tout, double tstep)
        {
            bool ok = true;
            while (tstep>0 ? solver.Time < tout : solver.Time > tout)
                if (!solver.TimeStep(tstep))
                {
                    ok = false;
                    break;
                }
            if (!ok)
            {
                if (solver.Warning.Length > 0)
                    Debug.WriteLine("********** " + solver.Warning);
                StringBuilder sb = new StringBuilder("Time=");
                sb.AppendLine(solver.Time.ToString("f3"));
                sb.Append("Expressions: ");
                foreach (double v in Expressions)
                    sb.AppendFormat("{0:0.000}, ", v);
                sb.AppendLine();
                sb.Append("Variables: ");
                foreach (double v in Variables)
                    sb.AppendFormat("{0:0.000}, ", v);
                throw new Exception("Time step failure");
                //Debug.WriteLine(sb.ToString());
            }
            return solver.Time;
        }
    }
}
