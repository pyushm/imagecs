using System.Linq;
using Math.Distance;

namespace VMLLib.Math.Distance
{
    public class PearsonDistance : IDistance
    {
        public double Run(double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length) throw new System.Exception("Vector dimesnion must be of same size!");

            double sum1 = array1.Sum();
            double sum2 = array2.Sum();

            double sum1Sq = 0.0;
            double sum2Sq = 0.0;
            double pSum = 0.0;
            for(int i = 0; i < array1.Length; i++)
            {
                sum1Sq += System.Math.Pow(array1[i], 2);
                sum2Sq += System.Math.Pow(array2[i], 2);

                pSum += array1[i] * array2[i];
            }

            double num = pSum - (sum1 * sum2 / (double)array1.Length);
            double den = System.Math.Sqrt((sum1Sq - System.Math.Pow(sum1, 2) / (double)array1.Length) * (sum2Sq - System.Math.Pow(sum2, 2) / (double)array2.Length));
            if (den == 0) return 0;

            return 1.0 - num / den;
        }
    }
}
