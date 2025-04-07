using System;
using System.Reflection;
using System.Diagnostics;

namespace Solvers
{
    public partial class RungeKuttaSolver
    {
        public static double NewIntegral(Fnc.Integrand fun, double t, double[] expressions, double[] variables, double from, double to)
        {   // example of alternative integral
            int n = 300;
            double db = (to - from) / n;
            double b = from;
            double s = fun(b, t, expressions, variables) / 2;
            for (int i = 1; i < n; i++)
            {
                b += db;
                s += fun(b, t, expressions, variables);
            }
            s += fun(to, t, expressions, variables) / 2;
            return s* db;
        }
        int nex;                // number of expressions
        int neq;                // number of variables
        int exbytes;            // number of bytes in all expressions
        double t;               // current process time
        double[] expressions;   // parameters fully defined by functions - no initial values needed
        double[] variables;     // variables defined by evolution equations - initial values needed
        Fnc.Assignment[] expressionsFnc;
        Fnc.Assignment[] variableDerivatives;
        Fnc.ArraySet constantAssignments;
        Fnc.Integrand[] integrandsFnc; // blind functions
        double[] deltaVar;      // last step variable delta 
        string warning="";
        public string Warning { get { return warning; } }
        public double Time { get { return t; } }
        public double[] Expressions { get { return expressions; } }
        public double[] Variables { get { return variables; } }
        public double[] DeltaVar { get { return deltaVar; } }
        public bool RK = true;
        public RungeKuttaSolver(Fnc.Assignment[] vars, Fnc.ArraySet cSet) { InitializeSolver(vars, new Fnc.Assignment[0], new Fnc.Integrand[0], cSet); }
        public RungeKuttaSolver(Fnc.Assignment[] vars, Fnc.Assignment[] expr, Fnc.ArraySet cSet) { InitializeSolver(vars, expr, new Fnc.Integrand[0], cSet); }
        public RungeKuttaSolver(Fnc.Assignment[] vars, Fnc.Assignment[] expr, Fnc.Integrand[] integrands, Fnc.ArraySet cSet, bool rk) { InitializeSolver(vars, expr, integrands, cSet); RK = rk; }
        void InitializeSolver(Fnc.Assignment[] vars, Fnc.Assignment[] express, Fnc.Integrand[] integ, Fnc.ArraySet constSet)
        {
            nex = express.Length;
            neq = vars.Length;
            expressionsFnc = express;
            constantAssignments = constSet;
            variableDerivatives = vars;
            integrandsFnc = integ;
            exbytes = nex * sizeof(double);
            expressions = new double[nex];
            variables = new double[neq];
            deltaVar = new double[neq];
        }
        public void SetInitialValues(double t_, double[] vals, double[] cnst) // time, variables and expressions
        {
            t = t_;
            constantAssignments(cnst);
            string ex = "";
            Debug.Assert(vals.Length == neq);
            Buffer.BlockCopy(vals, 0, variables, 0, neq * sizeof(double));
            for (int i = 0; i < nex; i++)
            {
                expressions[i] = (expressionsFnc[i])(t, expressions, variables);
                if (double.IsNaN(expressions[i]))
                    ex += "Expression " + (i + variables.Length) + " failed at initialization" + Environment.NewLine;
            }
            if (ex.Length > 0)
                throw new Exception(ex);
        }
        void updateExpressions(double t, double[] expr, double[] val)
        {
            for (int i = 0; i < nex; i++)
                expr[i] = expressionsFnc[i](t, expr, val);
        }
        double[] setVariableDerivatives(double t, double[] expr, double[] val)
        {
            double[] res = new double[neq];
            for (int i = 0; i < neq; i++)
                res[i] = variableDerivatives[i](t, expressions, variables);
            return res;
        }
        double[] setVariables(double dt, double[] derv)
        {
            double[] res = new double[neq];
            for (int i = 0; i < neq; i++)
                res[i] = variables[i] + dt * derv[i];
            return res;
        }
        public double[] InterpolatedVariables(double dold, double dnew) // interpolated to 0 between dold & dnew
        {
            double[] interpolated = new double[neq];
            double d = dold - dnew;
            double c = d == 0 ? 1 : dnew/d;
            for (int i = 0; i < neq; i++)
                interpolated[i] = variables[i] + deltaVar[i] * c;
            return interpolated;
        }
        public bool TimeStep(double step)
        {
            try
            {
                if (RK)
                    TimeAdvance4(step);
                else
                    TimeAdvance1(step);
                t += step;
            }
            catch(Exception ex)
            {
                warning = ex.Message;
                return false;
            }
            return true;
        }
        void TimeAdvance4(double step)
        {
            double dt = step/2;
            double[] der1 = setVariableDerivatives(t, expressions, variables);
            double[] var = setVariables(dt, der1);
            updateExpressions(t + dt, expressions, var);
            double[] der2 = setVariableDerivatives(t + dt, expressions, var);
            var = setVariables(dt, der2);
            updateExpressions(t + dt, expressions, var);
            double[] der3 = setVariableDerivatives(t + dt, expressions, var);
            dt = step;
            var = setVariables(dt, der3);
            updateExpressions(t + dt, expressions, var);
            double[] der4 = setVariableDerivatives(t + dt, expressions, var);
            for (int i = 0; i < neq; i++)
            {
                deltaVar[i] = (der1[i] + 2 * der2[i] + 2 * der3[i] + der4[i]) / 6;
                variables[i] += dt * deltaVar[i];
            }
            updateExpressions(t + dt, expressions, variables);
        }
        void TimeAdvance1(double step)
        {
            for (int i = 0; i < neq; i++)
                deltaVar[i] = step * variableDerivatives[i](t, expressions, variables);
            for (int i = 0; i < neq; i++)
                variables[i] += deltaVar[i];
            updateExpressions(t, expressions, variables);
        }
    }
}