using System;
using System.Collections.Generic;
using System.Threading;

using Math.Distance;

namespace KMeansProject
{
    public delegate void OnUpdateProgress(object sender, KMeansEventArgs eventArgs);
    public class KMeans
    {
        private double[][] _dataset;

        private IDistance _distance;
        private int _k;
        private List<Centroid> centroidList;
        public Centroid[] Centroids
        {
            get { return centroidList.ToArray(); }
        }

        public event OnUpdateProgress UpdateProgress;
        protected virtual void OnUpdateProgress(KMeansEventArgs eventArgs)
        {
            if (UpdateProgress != null)
                UpdateProgress(this, eventArgs);
            Thread.Sleep(1500);
        }

        public KMeans(int k, IDistance distance)
        {
            _k = k;
            _distance = distance;
        }

        public int Classify(double[] input)
        {
            int closestIndex = -1;
            double minDistance = Double.MaxValue;
            for (int k = 0; k < centroidList.Count; k++)
            {
                double distance = _distance.Run(centroidList[k].Array, input);
                if (distance < minDistance)
                {
                    closestIndex = k;
                    minDistance = distance;
                }
            }
            return closestIndex;
        }

        public double[] GetClosestCentroid(double[] input)
        {
            double[] closestCentroid = null;
            double minDistance = Double.MaxValue;
            for (int k = 0; k < centroidList.Count; k++)
            {
                double distance = _distance.Run(centroidList[k].Array, input);
                if (distance < minDistance)
                {
                    closestCentroid = centroidList[k].Array;
                    minDistance = distance;
                }
            }

            //Closest point to that centroid
            double[] closestPoint = null;
            minDistance = Double.MaxValue;
            for(int i = 0; i < _dataset.GetLength(0); i++)
            {
                double distance = _distance.Run(closestCentroid, _dataset[i]);
                if (distance < minDistance)
                {
                    closestPoint = _dataset[i];
                    minDistance = distance;
                }
            }

            return closestPoint;
        }

        public Centroid[] Run(double[][] dataSet)
        {
            _dataset = dataSet;
             centroidList = new List<Centroid>();

            for (int i = 0; i < _k; i++)
            {
                Centroid centroid = new Centroid(dataSet);
                centroidList.Add(centroid);
            }

            OnUpdateProgress(new KMeansEventArgs(centroidList, dataSet));

            while (true)
            {
                foreach (Centroid centroid in centroidList)
                    centroid.Reset();

                for (int i = 0; i < dataSet.GetLength(0); i++)
                {
                    double[] point = dataSet[i];
                    int closestIndex = -1;
                    double minDistance = Double.MaxValue;
                    for (int k = 0; k < centroidList.Count; k++)
                    {
                        double distance = _distance.Run(centroidList[k].Array, point);
                        if (distance < minDistance)
                        {
                            closestIndex = k;
                            minDistance = distance;
                        }
                    }
                    centroidList[closestIndex].addPoint(point);
                }

                foreach (Centroid centroid in centroidList)
                    centroid.MoveCentroid();

                OnUpdateProgress(new KMeansEventArgs(centroidList, null));

                bool hasChanged = false;
                foreach (Centroid centroid in centroidList)
                    if (centroid.HasChanged())
                    {
                        hasChanged = true;
                        break;
                    }
                if (!hasChanged)
                    break;
            }

            return centroidList.ToArray();
        }
    }
}
