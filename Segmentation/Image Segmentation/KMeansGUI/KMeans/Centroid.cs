using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KMeansProject
{
    public class Centroid
    {
        public double[] Array { get; private set; }
        public Color CentroidColor { get; private set; }
        public void DrawMe(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillEllipse( new SolidBrush(CentroidColor), (float)Array[0], (float)Array[1], 15, 15);
            foreach(double[] point in _closestPointsList)
                g.DrawEllipse(new Pen(CentroidColor, 2.0f), (float)point[0], (float)point[1], 10, 10);
        }
        private List<double[]> _oldPointsList;
        private List<double[]> _closestPointsList;
        public void addPoint(double[] closestArray)
        {
            _closestPointsList.Add(closestArray);
        }
        private static Random random = new Random();
        public Centroid(double[][] dataSet, Color color)
        {
            CentroidColor = color;
            List<Tuple<double, double>> minMaxPoints = Misc.GetMinMaxPoints(dataSet);
            Array = new double[minMaxPoints.Count];
            int i = 0;
            foreach (Tuple<double, double> tuple in minMaxPoints)
            {
                double minimum = tuple.Item1;
                double maximum = tuple.Item2;
                double element = random.NextDouble() * (maximum - minimum) + minimum;
                Array[i] = element;
                i++;
            }       
            _oldPointsList = new List<double[]>();
            _closestPointsList = new List<double[]>();
        }
        public void MoveCentroid()
        {
            List<double> resultVector = new List<double>();
            if (_closestPointsList.Count == 0) 
                return;
            for(int j = 0; j < _closestPointsList[0].GetLength(0); j++)
            {
                double sum = 0.0;
                for(int i = 0; i < _closestPointsList.Count; i++)
                    sum += _closestPointsList[i][j];
                sum /= _closestPointsList.Count;
                resultVector.Add(sum);
            }
            Array = resultVector.ToArray();
        }
        public bool HasChanged()
        {
            bool result = true;

            if (_oldPointsList.Count != _closestPointsList.Count) return true;
            if (_oldPointsList.Count == 0 || _closestPointsList.Count == 0) return false;

            for(int i=0; i < _closestPointsList.Count; i++)
            {
                double[] oldPoit = _oldPointsList[i];
                double[] currentPoint = _closestPointsList[i];

                for(int j=0;j<oldPoit.Length;j++)
                    if (oldPoit[j] != currentPoint[j])
                    {
                        result = false;
                        break;
                    }
            }

            return !result;
        }
        public void Reset()
        {
            _oldPointsList = Misc.Clone(_closestPointsList);
            _closestPointsList.Clear();
        }
        public override string ToString() { return String.Join(",", Array); }
    }
}
