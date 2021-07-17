using Math.Distance;

namespace KMeansProject
{
    public class EuclideanDistance : IDistance
    {
        public double Run(double[] array1, double[] array2)
        {
            double res = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                res += System.Math.Pow(array1[i] - array2[i], 2);
            }
            return System.Math.Sqrt(res);
        }
    }
}
