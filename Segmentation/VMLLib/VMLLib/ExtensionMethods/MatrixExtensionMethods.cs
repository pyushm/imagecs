using System.Collections.Generic;

namespace VMLLib.ExtensionMethods
{
    public static class MatrixExtensionMethods
    {
        public static double[] GetRow(this double[,] matrix, int row)
        {
            if (matrix.GetLength(0) < row) throw new System.Exception("That row does not exist");

            var columns = matrix.GetLength(1);
            var array = new double[columns];
            for (int i = 0; i < columns; ++i)
                array[i] = matrix[row, i];
            return array;
        }
    }
}
