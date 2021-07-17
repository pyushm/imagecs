using System;
using System.Collections.Generic;
using System.IO;

namespace VMLLib.FileIO
{
    public class CSVFile
    {
        public static List<double[]> ImportCSVFileAsList(string CSVFilePath, bool skipHeader, char splitChar, params int[] importColumns)
        {
            List<double[]> resultList = new List<double[]>();

            string[] lines = File.ReadAllLines(CSVFilePath);

            foreach (string line in lines)
            {
                if (skipHeader)
                {
                    skipHeader = false;
                    continue;
                }

                string[] lineData = line.Split(splitChar);

                List<double> doubleLineData = new List<double>();
                foreach (int colIndex in importColumns)
                {
                    if (colIndex > lineData.Length) throw new IndexOutOfRangeException();

                    double value = Double.Parse(lineData[colIndex]);
                    doubleLineData.Add(value);
                }

                resultList.Add(doubleLineData.ToArray());
            }
            return resultList;
        }
        public static double[][] ImportCSVFile(string CSVFilePath,bool skipHeader,char splitChar, params int[] importColumns)
        {
            List<double[]> resultList = new List<double[]>();

            string[] lines = File.ReadAllLines(CSVFilePath);

            foreach(string line in lines)
            {
                if (skipHeader)
                {
                    skipHeader = false;
                    continue;
                }

                string[] lineData = line.Split(splitChar);

                List<double> doubleLineData = new List<double>();
                foreach(int colIndex in importColumns)
                {
                    if (colIndex > lineData.Length) throw new IndexOutOfRangeException();

                    double value = Double.Parse(lineData[colIndex]);
                    doubleLineData.Add(value);
                }

                resultList.Add(doubleLineData.ToArray());
            }
            return resultList.ToArray();
        }
    }
}
