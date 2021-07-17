namespace VMLLib.Math
{
    public class Statistics
    {
        public static double[] GenerateNormalDistribution(double avg, double amp, double sd, int numberOfSamples, double step)
        {
            double x = 0;
            double[] resultArray = new double[numberOfSamples];

            for(int i = 0; i < numberOfSamples; i++)
            {
                resultArray[i] = gauss(x, amp, avg, sd);
                x += step;
            }

            return resultArray;
        }

        private static double gauss(double x, double a, double b, double c)
        {
            var v1 = (x - b) / (2d * c * c);
            var v2 = -v1 * v1 / 2d;
            var v3 = a * System.Math.Exp(v2);

            return v3;
        }
    }
}
