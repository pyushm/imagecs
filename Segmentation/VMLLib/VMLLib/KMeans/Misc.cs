using System;
using System.Collections.Generic;
using System.Drawing;

namespace KMeansProject
{
    public static class Misc
    {
        static Misc()
        {
        
        }

        public static List<double[]> Clone(List<double[]> array)
        {
            List<double[]> resultList = new List<double[]>();
            foreach (double[] tempArray in array)
            {
                double[] newArray = new double[tempArray.Length];
                for (int i = 0; i < tempArray.Length; i++)
                    newArray[i] = tempArray[i];
                resultList.Add(newArray);
            }
            return resultList;
        }

        public static List<Tuple<double, double>> GetMinMaxPoints(double[][] dataset)
        {
            List<Tuple<double, double>> result = new List<Tuple<double, double>>();

            for (int j = 0; j < dataset[0].GetLength(0); j++)
            {
                double min = Double.MaxValue;
                double max = Double.MinValue;
                for (int i = 0; i < dataset.Length; i++)
                {
                    double element = dataset[i][j];
                    if (element < min)
                        min = element;
                    if (element > max)
                        max = element;
                }
                result.Add(new Tuple<double, double>(min, max));
            }
            return result;
        }

        public static double GenerateRandomDouble(Random random, double minimum, double maximum)
        {
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}
