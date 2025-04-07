using System;
using System.Diagnostics;
using System.Text;

namespace Solvers
{
    public class Row
    {
        double[] v;
        public static implicit operator double[](Row sm) => sm.v;
        public Row(double[] d) { v = d; }
        public Row(SquareMatrix sm, int i) { v = sm.GetRow(i); }
        public double ScalaProduct(Row r)
        {
            double sp = 0;
            for (int i = 0; i < r.v.Length && i < v.Length; ++i)
                sp += r.v[i] * v[i];
            return sp;
        }
        public Row MultiplyBy(double c)
        {
            double[] r = new double[v.Length];
            for (int i = 0; i < v.Length; ++i)
                r[i] += c * v[i];
            return new Row(r);
        }
        public string ToString(string title = "", string fmt = "f4")
        {
            StringBuilder sb = new StringBuilder(title);
            if (v != null)
                for (int i = 0; i < v.Length; i++) sb.Append('\t' + v[i].ToString(fmt).PadLeft(8));
            return sb.ToString();
        }
    }
    public class SquareMatrix
    {
        class LUPDecomposion : SquareMatrix
        {
            int[] perm; // row permutations; 
            int toggle; // toggle is +1 or -1 (even or odd)
            public LUPDecomposion(SquareMatrix src)
                : base(src.Clone().m)// Doolittle LUP decomposition with partial pivoting
            {
                perm = new int[dim]; // set up row permutation
                for (int i = 0; i < dim; ++i)
                    perm[i] = i;
                toggle = 1; // toggle tracks row swaps. +1 - > even, -1 - > odd. used by MatrixDeterminant
                for (int j = 0; j < dim - 1; ++j)// main j column loop
                {
                    double colMax = Math.Abs(m[j, j]);
                    int pRow = j;
                    for (int i = j + 1; i < dim; ++i)
                    {
                        if (Math.Abs(m[i, j]) > colMax)
                        {
                            colMax = Math.Abs(m[i, j]);
                            pRow = i;
                        }
                    }
                    if (pRow != j) // if largest value not on pivot, swap rows
                    {
                        SwapRows(pRow, j);
                        int tmp = perm[pRow]; // swap perm info
                        perm[pRow] = perm[j];
                        perm[j] = tmp;
                        toggle = -toggle; // adjust the row-swap toggle
                    }
                    // if there is a 0 on the diagonal, find a good row
                    // from i = j+1 down that doesn't have 0 in column j, and swap that good row with row j
                    if (m[j, j] == 0.0)
                    {
                        int goodRow = -1;
                        for (int row = j + 1; row < dim; ++row)
                        {
                            if (m[row, j] != 0.0)
                                goodRow = row;
                        }
                        if (goodRow == -1)
                            throw new Exception("Cannot use Doolittle's method");
                        SwapRows(goodRow, j);
                        int tmp = perm[goodRow]; // and swap perm info
                        perm[goodRow] = perm[j];
                        perm[j] = tmp;
                        toggle = -toggle; // adjust the row-swap toggle
                    }
                    for (int i = j + 1; i < dim; ++i)
                    {
                        m[i, j] /= m[j, j];
                        for (int k = j + 1; k < dim; ++k)
                            m[i, k] -= m[i, j] * m[j, k];
                    }
                }
            }
            public SquareMatrix Inverse()
            {
                SquareMatrix result = Clone();
                double[] b = new double[dim];
                for (int i = 0; i < dim; ++i)
                {
                    for (int j = 0; j < dim; ++j)
                        b[j] = i == perm[j] ? 1.0 : 0.0;
                    double[] x = LUPLinearSystemSolve(b);
                    for (int j = 0; j < dim; ++j)
                        result[j, i] = x[j];
                }
                return result;
            }
            public double Determinant()
            {
                double result = toggle;
                for (int i = 0; i < dim; ++i)
                    result *= m[i, i];
                return result;
            }
            public double[] LinearSystemResult(double[] rhs)
            {
                double[] b = new double[dim];
                for (int i = 0; i < dim; ++i)
                    b[i] = rhs[perm[i]];// permute b according to perm[] into bp
                return LUPLinearSystemSolve(b);
            }
            double[] LUPLinearSystemSolve(double[] b)
            { 
                double[] x = new double[dim];
                b.CopyTo(x, 0);
                for (int i = 1; i < dim; ++i)
                {
                    double sum = x[i];
                    for (int j = 0; j < i; ++j)
                        sum -= m[i, j] * x[j];
                    x[i] = sum;
                }
                x[dim - 1] /= m[dim - 1, dim - 1];
                for (int i = dim - 2; i >= 0; --i)
                {
                    double sum = x[i];
                    for (int j = i + 1; j < dim; ++j)
                        sum -= m[i, j] * x[j];
                    x[i] = sum / m[i, i];
                }
                return x;
            }
        }
        double[,] m;
        int dim;
        public int Dim => dim;
        public static implicit operator double[,](SquareMatrix sm) => sm.m; 
        public SquareMatrix(int size)
        {
            m = new double[size, size]; // row, col
            dim = size;
        }
        public SquareMatrix(double[,] mat)
        {
            dim = mat.GetLength(0);
            if (dim != mat.GetLength(1))
                throw new Exception("Non-square matrix");
            m = mat;
        }
        public SquareMatrix(double[] v)
        {
            dim = v.Length;
            m =new double[dim, dim];
            for (int i = 0; i < dim; ++i)
                for (int j = 0; j < dim; ++j)
                    m[i, j] = i == j ? v[i] : 0;
        }
        public double this[int i, int j] { get { return m[i, j]; } set { m[i, j] = value; } }
        public static SquareMatrix RandomMatrix(int size, double range, int seed) // return a matrix with random values within +-range
        {
            Random ran = new Random(seed);
            SquareMatrix m = new SquareMatrix(size);
            for (int i = 0; i < size; ++i)
                for (int j = 0; j < size; ++j)
                    m[i, j] = (2 * ran.NextDouble() - 1) * range;
            return m;
        }
        public static SquareMatrix IdentityMatrix(int n)// return an n x n Identity matrix
        {
            SquareMatrix m = new SquareMatrix(n);
            for (int i = 0; i < n; ++i)
                m[i, i] = 1.0;
            return m;
        }
        //public static void TestLinearSolver()
        //{
        //    int dim = 4;
        //    int seed = DateTime.Now.Millisecond;
        //    for (int j = 0; j < 10; j++)
        //    {
        //        seed += j;
        //        Solvers.SquareMatrix m = Solvers.SquareMatrix.RandomMatrix(dim, 100, seed);
        //        double[] rhs = new double[dim];
        //        Random ran = new Random(seed * seed);
        //        for (int i = 0; i < dim; i++)
        //            rhs[i] = (2 * ran.NextDouble() - 1) * 100;
        //        Solvers.SquareMatrix.Decomposion dm = m.LUPDecomposion();
        //        double[] res = dm.LinearSystemResult(rhs);
        //        double d = m.ValidateSolution(rhs, res);
        //        Solvers.SquareMatrix im = dm.Inverse();
        //        Solvers.SquareMatrix prod = im.MultiplyBy(m);
        //        Debug.WriteLine("j=" + j + " seed=" + seed + " diff=" + d.ToString() + " trace-dim=" + (prod.Trace()-dim).ToString());
        //    }
        //}
        //public LUPDecomposion LUPDecompose() { return new LUPDecomposion(this); }
        public double Trace() 
        {
            double trace = 0;
            for(int i=0; i<dim;i++)
                trace+=m[i,i];
            return trace;
        }
        public string ToString(string title = "", string fmt = "f4")
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dim; ++i)
            {
                sb.Append(new Row(this, i).ToString(string.IsNullOrEmpty(title) ? "" : title+i, fmt));
                if (i != dim - 1)
                    sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        public bool Equals(SquareMatrix matrix, double epsilon)
        {
            if (dim != matrix.dim)
                return false;
            for (int i = 0; i < dim; ++i)
                for (int j = 0; j < dim; ++j)
                    if (Math.Abs(m[i, j] - matrix[i, j]) > epsilon)
                        return false;
            return true;
        }
        public SquareMatrix MultiplyBy(SquareMatrix mb)
        {
            if (dim != mb.dim)
                throw new Exception("Non-conformable matrices in MatrixProduct");
            SquareMatrix sm = new SquareMatrix(dim);
            for (int i = 0; i < dim; ++i) // each row of A
                for (int j = 0; j < dim; ++j) // each col of B
                    for (int k = 0; k < dim; ++k) // could use k < bRows
                        sm[i, j] += m[i, k] * mb[k, j];
            return sm;
        }
        public double[] MultiplyBy(double[] vector) // column vector (yielding an n x 1 column vector)
        {
            if (dim != vector.Length)
                throw new Exception("Non-conformable matrix and vector");
            double[] result = new double[dim];
            for (int i = 0; i < dim; ++i)
                for (int j = 0; j < dim; ++j)
                    result[i] += m[i, j] * vector[j];
            return result;
        }
        public double[] LinearSystemSolve(double[] rhs)// Solves M*res = rhs
        {
            if (rhs.Length != dim)
                throw new Exception("Matrix dim [" + dim + "] != rhs dim [" + rhs.Length + ']');
            LUPDecomposion lum = new LUPDecomposion(this);
            return lum.LinearSystemResult(rhs);
        }
        public double ValidateSolution(double[] rhs, double[] res)
        {
            double[] product = MultiplyBy(res);
            double diff = 0;
            for (int j = 0; j < dim; ++j)
                diff += Math.Abs(product[j] - rhs[j]);
            return diff;
        }
        public SquareMatrix Clone()
        {
            SquareMatrix result = new SquareMatrix(dim);
            Buffer.BlockCopy(m, 0, result.m, 0, dim * dim * sizeof(double));
            return result;
        }
        public void SwapRows(int r1, int r2)
        {
            double[] row=new double[dim];
            int rowBytes = dim * sizeof(double);
            Buffer.BlockCopy(m, rowBytes * r1, row, 0, rowBytes);
            Buffer.BlockCopy(m, rowBytes * r2, m, rowBytes * r1, rowBytes);
            Buffer.BlockCopy(row, 0, m, rowBytes * r2, rowBytes);
        }
        public Row GetRow(int ri)
        {
            if (ri < 0 || ri >= dim)
                throw new Exception("Parameter 'shortDim' has to be in the range [0:" + (dim - 1) + ']');
            double[] row = new double[dim];
            int rowBytes = dim * sizeof(double);
            Buffer.BlockCopy(m, rowBytes * ri, row, 0, rowBytes);
            return new Row(row);
        }
        public Row[] GetRows()
        {
            Row[] rows = new Row[dim];
            int rowBytes = dim * sizeof(double);
            for (int i = 0; i < dim; i++)
            {
                var v = new double[dim];
                Buffer.BlockCopy(m, rowBytes * i, v, 0, rowBytes);
                rows[i]=new Row(v);
            }
            return rows;
        }
        public SquareMatrix SubMatrix(int shortDim)
        {
            if (shortDim < 0 || shortDim > dim)
                throw new Exception("Parameter 'shortDim' has to be in the range [0:" + dim + ']');
            double[,] a = new double[shortDim, shortDim];
            for (int i = 0; i < shortDim; i++)
                for (int j = 0; j < shortDim; j++) a[i, j] = m[i, j];
            return new SquareMatrix(a);
        }
    }
}
