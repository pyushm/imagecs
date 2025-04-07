using System;
using System.Reflection;
using System.Diagnostics;

namespace Solvers
{
    [Serializable]
    public class Progonka
    {
        public enum Boundary
        {
            NoFlux,     // 0 derivative (LBV=0)
            Derivative,
            Value,
            Equation
        }
        protected double[] a, b, c, d;
        protected double dx;
        protected double LBV;
        protected double RBV;
        protected static int N;
        Boundary leftType;
        Boundary rightType;
        int requiredBoundaryValues;
        public Progonka(double delta, int count, Boundary left, Boundary right) { Set(delta, count, left, right); }
        public Progonka(double delta, int count, Boundary right) { Set(delta, count, Boundary.NoFlux, right); }
        void Set(double delta, int count, Boundary left, Boundary right)
        {   // 
            if(left == Boundary.Equation)
                throw new Exception("Equation at left boundary not implemented");
            dx = delta;
            N = right == Boundary.Equation ? count + 1 : count;
            leftType = left;
            rightType = right;
            requiredBoundaryValues = rightType == Boundary.Equation ? 3 : 2;
            if (leftType != Boundary.NoFlux)
            {
                LBV = 0;
                requiredBoundaryValues--;
            }
            if (rightType != Boundary.NoFlux)
            {
                RBV = 0;
                requiredBoundaryValues--;
            }
            a = new double[N];
            b = new double[N];
            c = new double[N];
            d = new double[N];
        }
        public double Yleft(double[] y) { return leftType == Boundary.Equation ? y[0] : leftType == Boundary.Value ? 2 * LBV - y[0] : y[0] - dx * LBV; } // y[-1]
        public double Yright(double[] y) { return rightType == Boundary.Equation ? y[N - 1] : rightType == Boundary.Value ? 2 * RBV - y[N - 1] : y[N - 1] + dx * RBV; } // y[N]
        public double[] CartesianTimeStep(// time step of the equation ∂y/∂t=(1/C) ∂/∂x(D ∂y/∂x)+RHS
            double dt,                  // time step to get new values
            double[] y,                 // old value {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] C,                 // mertic coefficient {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] D,                 // diffusion coefficient {0:N} defined at x=x0+dx*i
            double[] RHS,               // right-hand-side {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] BV)     // left and right boundary conditions (value or derivative depending on leftType, rightType)
        {
            SetCartesianMatrix(dt, y, C, D, RHS, BV);
            return GetNewValues();
        }
        public double[] CylindricalTimeStep(// time step of the equation ∂y/∂t=(1/rC) ∂/∂r(rD ∂y/∂r)+RHS
            double dt,                  // time step to get new values
            double[] y,                 // old value {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] C,                 // mertic coefficient {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] D,                 // diffusion coefficient {0:N} defined at x=x0+dx*i
            double[] RHS,               // right-hand-side {0:N-1} defined at x=x0+dx*(i+0.5)
            double RBV)                 // right boundary condition (value or derivative depending on rightType)
        {
            SetCylindricalMatrix(dt, y, C, D, RHS, RBV);
            return GetNewValues();
        }
        public double[] PsiTimeStep(    // time step of the equation ∂y/∂t=B/C  ∂/∂x (D/B  ∂y/∂x)+RHS
            double dt,                  // time step to get new values
            double[] y,                 // old value {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] C,                 // mertic coefficient {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] D,                 // diffusion coefficient {0:N} defined at x=x0+dx*i
            double[] RHS,               // right-hand-side {0:N-1} defined at x=x0+dx*(i+0.5)
            double[] B,                 // magnetic {0:N} defined at x=x0+dx*i
            double RBV)                 // right boundary condition (value or derivative depending on rightType)
        {
            SetPsiMatrix(dt, y, C, D, RHS, B, RBV);
            return GetNewValues();
        }
        double[] GetNewValues()
        {   // a_i*Y_{i - 1} + b_i*Y_i + c_i*Y_{i + 1} = d_i 
            for (int i = 1; i < N; i++)
            {
                double m = a[i] / b[i - 1];
                b[i] -= m * c[i - 1];
                d[i] -= m * d[i - 1];
            }
            double[] Y = new double[N+1];
            Y[N - 1] = d[N - 1] / b[N - 1];
            for (int i = N - 2; i >= 0; i--)
                Y[i] = ((d[i] - c[i] * Y[i + 1])) / b[i];
            Y[N] = Yright(Y);
            return Y;
        }
        void SetCartesianMatrix(double dt, double[] y, double[] C, double[] D, double[] RHS, double[] bv)
        {
            if (requiredBoundaryValues != bv.Length)
                throw new Exception(requiredBoundaryValues + " boundary values required; got " + bv.Length);
            int bvInd = 0;
            if (leftType != Boundary.NoFlux) 
                LBV = bv[bvInd++];
            RBV = bv[bvInd++];  // for Boundary.Equation RBV is ∆_R
            double tau = rightType == Boundary.Equation ? bv[bvInd++] : 0;
            double s = dt / dx / dx;
            c[0] = -s * D[1] / C[0];
            b[0] = 1 - c[0] + s * ((leftType == Boundary.Value ? 2 : 0) * D[0]) / C[0];
            d[0] = y[0] + RHS[0] * dt + s * (D[0] * (leftType == Boundary.Value ? 2 : -dx) * LBV) / C[0];
            for (int i = 1; i < N - 1; i++)
            {
                a[i] = -s * D[i] / C[i];
                c[i] = -s * D[i + 1] / C[i];
                b[i] = 1 - a[i] - c[i];
                d[i] = y[i] + RHS[i] * dt;
            }
            a[N - 1] = -s * D[N - 1] / C[N - 1] * (rightType == Boundary.Equation ? dx / RBV : 1);
            b[N - 1] = 1 - a[N - 1] + (rightType == Boundary.Equation ? dt / tau : s * D[N] / C[N - 1] * (rightType == Boundary.Value ? 2 : 0));
            d[N - 1] = y[N - 1] + RHS[N - 1] * dt + (rightType == Boundary.Equation ? 0 : s * (D[N] * RBV * (rightType == Boundary.Value ? 2 : dx)) / C[N - 1]);
        }
        void SetCylindricalMatrix(double dt, double[] y, double[] C, double[] D, double[] RHS, double rbv)
        {
            RBV = rbv;
            double s = dt / dx / dx;
            for (int i = 0; i < N - 1; i++)
            {
                double g = s / ((i + 0.5) * C[i]);
                a[i] = -i * D[i] * g;
                c[i] = -(i + 1) * D[i + 1] * g;
                b[i] = 1 - a[i] - c[i];
                d[i] = y[i] + RHS[i] * dt;
            }
            double gN = s / (N - 0.5) * C[N - 1];
            a[N - 1] = -(N - 1) * D[N - 1] * gN;
            b[N - 1] = 1 - a[N - 1] + N * (rightType == Boundary.Value ? 2 : 0) * D[N] * gN;
            d[N - 1] = y[N - 1] + RHS[N - 1] * dt + N * D[N] * RBV * (rightType == Boundary.Value ? 2 : dx) * gN;
        }
        void SetPsiMatrix(double dt, double[] y, double[] C, double[] D, double[] RHS, double[] B, double rbv)
        {
            RBV = rbv;
            double s = dt / dx / dx;
            c[0] = -s * D[1] / (2 * C[0]);
            b[0] = 1 - c[0];
            d[0] = y[0] + RHS[0] * dt;
            for (int i = 1; i < N - 1; i++)
            {
                double g = s * (B[i] + B[i + 1]) / (2 * C[i]);
                a[i] = -g * D[i] / B[i];
                c[i] = -g * D[i + 1] / B[i + 1];
                b[i] = 1 - a[i] - c[i];
                d[i] = y[i] + RHS[i] * dt;
            }
            double gN = s * (B[N - 1] + B[N]) / (2 * C[N - 1]);
            a[N - 1] = -gN * D[N - 1] / B[N - 1];
            b[N - 1] = 1 - a[N - 1] + gN * (rightType == Boundary.Value ? 2 : 0) * D[N] / B[N];
            d[N - 1] = y[N - 1] + RHS[N - 1] * dt + gN * (D[N] * RBV * (rightType == Boundary.Value ? 2 : dx)) / B[N];
        }
    }
    public class ProgonkaTest : Progonka
    {
        double[] C;
        double[] D;
        double[] RHS;
        double[] B;
        public ProgonkaTest(double delta, int count, Boundary rb) : base(delta, count, rb) {  }
        public void SetTest(double src, double rbv)
        {  // C=D=1, uniform source
            RBV = rbv;
            C = new double[N];
            D = new double[N + 1];
            B = new double[N + 1];
            RHS = new double[N];
            for (int i = 0; i < N; i++)
            {
                C[i] = 1;
                D[i] = 1;
                B[i] = Math.Sqrt(i);
                RHS[i] = src;
            }
            D[N] = 1;
            B[N] = Math.Sqrt(N);
        }
        public double[] CartesianTimeStep(double dt, double[] y, double tau) { return CartesianTimeStep(dt, y, C, D, RHS, new double[] { RBV, tau }); }
        public double[] CartesianTimeStep(double dt, double[] y) { return CartesianTimeStep(dt, y, C, D, RHS, new double[] { RBV }); }
        public double[] CylindricalTimeStep(double dt, double[] y) { return CylindricalTimeStep(dt, y, C, D, RHS, RBV); }
        public double[] PsiTimeStep(double dt, double[] y) { return PsiTimeStep(dt, y, C, D, RHS, B, RBV); }
    }
}