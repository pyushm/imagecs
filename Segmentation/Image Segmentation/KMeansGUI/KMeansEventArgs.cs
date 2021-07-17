using KMeansProject;
using System.Collections.Generic;

namespace KMeansGUI
{
    public class KMeansEventArgs
    {
        private List<Centroid> _centroidList;
        public List<Centroid> CentroidList
        {
            get { return _centroidList; }
        }
        
        private double[][] _dataset;
        public double[][] Dataset
        {
            get { return _dataset; }
        }

        public KMeansEventArgs(List<Centroid> centroidList,double[][] dataset)
        {
            _centroidList = centroidList;
            _dataset = dataset;
        }
    }
}
