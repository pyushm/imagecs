using System;
using System.Linq;
using System.Collections.Generic;

namespace VMLLib.ExtensionMethods
{
    public static class VectorExtensionMethods
    {
        public static double[] Remap(this double[] valueArray, double from1, double to1, double from2, double to2)
        {
            List<double> result = new List<double>();

            foreach (double value in valueArray)
                result.Add((value - from1) / (to1 - from1) * (to2 - from2) + from2);

            return result.ToArray();
        }

        public static double[] NormalizeElements(this double[] array, double min, double max)
        {
            double arrayMinElement = array.Min();
            double arrayMaxElement = array.Max();

            double[] normalizedArray = new double[array.Length];
            for(int i = 0; i < array.Length; i++)
            {
                normalizedArray[i] = ((max - min) * (array[i] - arrayMinElement)) / (arrayMaxElement - arrayMinElement) + min;
            }

            return normalizedArray;
        }

        public static double[] MultiplyElements(this double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length) throw new Exception("Multiplication not possible!");

            double[] result = new double[array1.Length];
            for(int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] * array2[i];
            }
            return result;
        }

        public static double[] SubtractElements(this double[] array1, double[] array2, bool useAbsValues)
        {
            if (array1.Length != array2.Length) throw new Exception("Subtraction not possible!");

            double[] result = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = useAbsValues ? System.Math.Abs(array1[i] - array2[i]) :  array1[i] - array2[i];
            }
            return result;
        }

        public static double[] AddElements(this double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length) throw new Exception("Addition not possible!");

            double[] result = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
            return result;
        }

        public static double[] Pow(this double[] array)
        {
            double[] result = new double[array.Length];
            for(int i=0;i<array.Length;i++)
            {
                result[i] = System.Math.Pow(array[i], 2);
            }
            return result;
        }
        
        public static double[] DivideElements(this double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length) throw new Exception("Multiplication not possible!");

            double[] result = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] / array2[i];
            }
            return result;
        }

        public static void PrintArray(this double[] valueArray)
        {
            Console.WriteLine(String.Format("Array: {0}", string.Join(", ", valueArray)));
        }
        
    }
}
