using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using Solvers;

namespace CodeEditor
{
    public abstract class FuncionGroup  // array of functions with the same signature
    {
        public string Name { get; private set; }   // group name
        public string Signature { get; private set; }   // format string to create function text
        public abstract Type Type { get; }
        public abstract void CreateFunctionArray(int c);        // support of creation by compiler
        public abstract void SetFunction(int i, object f);  // support of creation by compiler
        public FuncionGroup(string n, string f) { Name = n; Signature = f; }
    }
    public class FncGroup<T> : FuncionGroup // array of functions with the same signature
    {
        public override Type Type { get { return typeof(T); } }   // function type
        public override void CreateFunctionArray(int c) { Functions = new T[c]; }
        public override void SetFunction(int i, object f) { Functions[i] = (T)f; }
        public T[] Functions { get; set; }
        public FncGroup(string n, string s) : base(n, s) { Functions = new T[0]; }
    }
    public class CodeGenerator
    {
        List<string> warnings = new List<string>();
        protected string[] gHeaders;
        protected FuncionGroup[] gInfo;
        public FncGroup<Fnc.Assignment> ExprGroup { get; protected set; }
        public FncGroup<Fnc.Assignment> VarGroup { get; protected set; }
        public FncGroup<Fnc.InitialValue> ConstGroup { get; protected set; }
        public FncGroup<Fnc.InitialValue> IniGroup { get; protected set; }
        public FncGroup<Fnc.Integrand> FunctGroup { get; protected set; }
        public Fnc.ArraySet SetConst { get; protected set; }
        public EquationDefinition EquationDefinition { get; protected set; }
        public Fnc.IntegralSetter IntegralSetter { get; protected set; }
        public ConstructorInfo ODSolverConstructor { get; protected set; }
        public string[] Warnings { get { return warnings.ToArray(); } }
        public CodeGenerator(EquationDefinition model = null)
        {
            EquationDefinition = model;
            gInfo = new FuncionGroup[(int)Group.Length];
            string Assignment = "public static double {0}(double t, double[] " + Variable.GroupName(Group.Expr) + ", double[] " +
                Variable.GroupName(Group.Var) + ") {{ {1} }}";  // name, body
            gInfo[(int)Group.Expr] = ExprGroup = new FncGroup<Fnc.Assignment>(Variable.GroupName(Group.Expr), Assignment);
            gInfo[(int)Group.Var] = VarGroup = new FncGroup<Fnc.Assignment>(Variable.GroupName(Group.Var), Assignment);
            string InitialValue = "public static double {0}() {{ {1} }}"; // name, body
            gInfo[(int)Group.Const] = ConstGroup = new FncGroup<Fnc.InitialValue>(Variable.GroupName(Group.Const), InitialValue);
            gInfo[(int)Group.Ini] = IniGroup = new FncGroup<Fnc.InitialValue>(Variable.GroupName(Group.Ini), InitialValue);
            string Integrand = "public static double {0}(double blind, double t, double[] " + Variable.GroupName(Group.Expr) + 
                ", double[] " + Variable.GroupName(Group.Var) + ") {{ {1} }}";
            gInfo[(int)Group.Funct] = FunctGroup = new FncGroup<Fnc.Integrand>(Variable.GroupName(Group.Funct), Integrand);
            gHeaders = new string[(int)Group.Length];
            gHeaders[(int)Group.Expr] = "    ExprFunctions = new Fnc.Assignment[] {";
            gHeaders[(int)Group.Var] = "    VarFunctions = new Fnc.Assignment[] {";
            gHeaders[(int)Group.Const] = "    ConstFunctions = new Fnc.InitialValue[] {";
            gHeaders[(int)Group.Ini] = "    IniFunctions = new Fnc.InitialValue[] {";
            gHeaders[(int)Group.Funct] = "    FunctFunctions = new Fnc.Integrand[] {";
        }
        string MethodBody(Group g, Variable v) { return string.Format(gInfo[(int)g].Signature, v.MethodName, v.FunctionText); } 
        string CreateGroupDeclarationsAndFunctions(Group g)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// " + g.ToString());
            if (g != Group.Ini)
                sb.AppendLine(EquationDefinition.CArrayDeclaration(g));
            foreach (var v in EquationDefinition.GVars(g))
            {
                if (g != Group.Ini)
                    sb.AppendLine(v.CDeclaration);
                sb.AppendLine(MethodBody(g, v));
            }
            return sb.ToString();
        }
        string CreateConstructor(string[] runStrings, string[] plotStrings, string eqVar = "t")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static void ConstSet(double[] v) { " + Variable.GroupName(Group.Const) + " = new double[v.Length]; Buffer.BlockCopy(v, 0, " +
                Variable.GroupName(Group.Const) + ", 0, v.Length * sizeof(double)); }");
            sb.AppendLine("public ODSolver()");
            sb.AppendLine("{");
            sb.AppendLine("    SetConst = ConstSet;");
            sb.AppendLine(CreateGroupFunctionArrays(Group.Const));
            sb.AppendLine(CreateGroupFunctionArrays(Group.Expr));
            sb.AppendLine(CreateGroupFunctionArrays(Group.Var));
            sb.AppendLine(CreateGroupFunctionArrays(Group.Ini));
            RunInfo rs = new RunInfo(runStrings);
            sb.AppendLine("    Run = new RunInfo(" + rs.ToString() + ");");
            if (plotStrings != null)
            {
                foreach (var s in plotStrings)
                {
                    try
                    {
                        Output.Panel p = new Output.Panel(s, eqVar);
                        sb.AppendLine("    Output.Add(new Output.Panel(@\"" + p.ToString() + "\"));");
                    }
                    catch (Exception)
                    {
                        warnings.Add("Output error '" + s);
                    }
                }
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
        string CreateGroupFunctionArrays(Group g)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(gHeaders[(int)g]);
            for (int i = 0; i < EquationDefinition.GVars(g).Length; i++)
            {
                if(i > 0)
                    sb.Append(", ");
                sb.Append(EquationDefinition.GVars(g)[i].MethodName);
            }
            sb.Append("};");
            return sb.ToString();
        }
        public void SetGroupFunctions(Group g, object[] functions) 
        {
            var gi = gInfo[(int)g];
            int n = g == Group.Ini ? EquationDefinition.GVars(Group.Var).Length : EquationDefinition.GVars(g).Length;
            gi.CreateFunctionArray(n);
            foreach (var v in EquationDefinition.GVars(g))
                gi.SetFunction(v.Index, functions[v.Index]);
        }
        public List<SegmentText> CompileCSharpCode(string cText) // return errors
        {
            List<SegmentText> errors = new List<SegmentText>();
            CSharpCodeProvider provider = new CSharpCodeProvider();
            string cd = Directory.GetCurrentDirectory();
            var refs = new string[] {
                Path.Combine(cd, "../../Common/bin/Solvers.dll"),
                Path.Combine(cd, "../../Common/bin/CodeGeneratorODE.dll"),
                "System.dll" };
            CompilerResults results = provider.CompileAssemblyFromSource(new CompilerParameters(refs), cText);
            if (results.Errors.HasErrors)
            {
                foreach (CompilerError error in results.Errors)
                    errors.Add(new SegmentText(error.Line, error.Column, error.ErrorText));
            }
            else
            {
                Type mf = results.CompiledAssembly.GetType("ODEquation.ODSolver");
                //for (Group g = 0; g < Group.Length; g++)
                //{
                //    var gi = gInfo[(int)g];
                //    int n = g == Group.Ini ? EquationDefinition.GVars(Group.Var).Length : EquationDefinition.GVars(g).Length;
                //    gi.CreateFunctionArray(n);
                //    foreach (var a in EquationDefinition.GVars(g))
                //        gi.SetFunction(a.Index, Delegate.CreateDelegate(gi.Type, mf.GetMethod(a.MethodName)));
                //}
                //SetConst = (Fnc.ArraySet)Delegate.CreateDelegate(typeof(Fnc.ArraySet), mf.GetMethod("SetConst"));
                //IntegralSetter = (Fnc.IntegralSetter)Delegate.CreateDelegate(typeof(Fnc.IntegralSetter), mf.GetMethod("SetIntegral"));
                ODSolverConstructor = mf.GetConstructor(new Type[0]);
            }
            return errors;
        }
        public string CreateCSharpCode(string src, List<Statement> statements, StatementParser parser, string eqVar = "t")
        {
            warnings.Clear();
            List<VariableDefinition> definitions = new List<VariableDefinition>();
            foreach (var statement in statements)
                definitions.Add(new VariableDefinition(statement));
            EquationDefinition = new EquationDefinition(VariableList.CreateVariables(definitions)); EquationDefinition.ModelSrc = src;
            StringBuilder code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using Solvers;");
            code.AppendLine("using System.ComponentModel;");
            code.AppendLine("using CodeEditor;");
            code.AppendLine("namespace ODEquation {");
            code.AppendLine("public class ODSolver : SolverInterface { ");
            try
            {
                code.AppendLine(CreateGroupDeclarationsAndFunctions(Group.Const));
                code.AppendLine(CreateGroupDeclarationsAndFunctions(Group.Expr));
                code.AppendLine(CreateGroupDeclarationsAndFunctions(Group.Var));
                code.AppendLine(CreateGroupDeclarationsAndFunctions(Group.Ini));
                code.AppendLine(CreateConstructor(parser.GetVariables(StatementParser.RunInfoKey), parser.GetVariables(StatementParser.OutputKey), eqVar));
            }
            catch (ListException le) { warnings.AddRange(le.Messages); }
            catch (Exception ex) { warnings.Add(ex.Message); }
            code.AppendLine("} }");
            return code.ToString();
        }
    }
}