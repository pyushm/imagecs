using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.Windows.Media;

namespace ImageProcessor
{
    class ColorPoint
    {
        public ColorPoint(int X, int Y, Color Clr) { this.X = X; this.Y = Y; this.Clr = Clr; }
        public int X { get; set; }
        public int Y { get; set; }
        public Color Clr { get; set; }
        public double Distance2(ColorPoint p) { return (p.X - X) * (p.X - X) + (p.Y - Y) * (p.Y - Y); }
        public double ClrDistance2(ColorPoint p)
        {
            return (p.Clr.R - Clr.R) * (p.Clr.R - Clr.R) + (p.Clr.G - Clr.G) * (p.Clr.G - Clr.G) + (p.Clr.B - Clr.B) * (p.Clr.B - Clr.B);
        }
    }
    class Segment : BitmapAccess
    {   // collection of cluster points
        public Segment(int w, int h, List<ColorPoint> Centroids, ColorPoint Center) : base(w, h) { this.Centroids = Centroids; this.Center = Center; }
        public List<ColorPoint> Centroids { get; set; }// Centroids List Property
        public ColorPoint Center { get; set; }// Central Super-Pixel Property
        public void Save(string path) { SaveToFile(path, true, false); }
    }
    class ImageSegmentation
    {
        private const int gap = 5;
        private const int clrGap = 50;
        HashSet<Segment> Clusters = new HashSet<Segment>();
        public ImageSegmentation() { }
        private readonly System.Random rand = new System.Random();
        BitmapAccess src; 
        BitmapAccess result;
        public void Init()
        {
            // Initialize the array of super-pixels by creating List<RGBPoint> class object
            // Generate an initial array of super-pixels of the original source image stored in the FrameBuffer bitmap object
            List<ColorPoint> Centroids=Generate(gap* gap, clrGap* clrGap);
            // Compute the value of the centeral super-pixel coordinates and assign it to the Mean local variable
            ColorPoint Mean = GetMean(Centroids);
            // Append an initial cluster being initialized to the array of clusters
            Clusters.Add(new Segment(src.Width, src.Height, Centroids, Mean));
        }
        public List<ColorPoint> Generate(int gap2, int clrGap2)
        {
            List<ColorPoint> centroids = new List<ColorPoint>();
            // W * H/gap2 is the maximum possible number of random super-pixel being generated
            for (int IterCount = 0; IterCount < src.Width * src.Height/ gap2; IterCount++)
            {
                int Rand_X = rand.Next(0, src.Width);
                int Rand_Y = rand.Next(0, src.Height);
                ColorPoint RandPoint = new ColorPoint(Rand_X, Rand_Y, src.GetPixel(Rand_X, Rand_Y));
                bool isNew = true;
                foreach (var p in centroids)
                    if(RandPoint.Distance2(p)<gap2 || RandPoint.ClrDistance2(p)<clrGap2)
                    { isNew = false; break; }
                if (isNew)
                    centroids.Add(RandPoint);   // If new point is sufficiently far in color and distance, append RandPoint to array of super-pixels
            }
            return centroids;
        }
        public ColorPoint GetMean(List<ColorPoint> Centroids)
        {
            int Mean_X = 0, Mean_Y = 0;
            for (int Index = 0; Index < Centroids.Count(); Index++)
            {
                Mean_X += Centroids[Index].X;
                Mean_Y += Centroids[Index].Y;
            }
            Mean_X /= Centroids.Count();
            Mean_Y /= Centroids.Count();
            return new ColorPoint(Mean_X, Mean_Y, src.GetPixel(Mean_X, Mean_Y));
        }
        public void Add(BitmapAccess FrameImage, List<ColorPoint> Centroids, ColorPoint Center) { Clusters.Add(new Segment(FrameImage, Centroids, Center)); }
        public Segment this[int Index] { get { return Clusters.ElementAt(Index); } }
        public IEnumerator<Segment> GetEnumerator() { return Clusters.GetEnumerator(); }
        public void Compute(string InputFile, string OutputFile)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            DirectoryInfo dir_info = new DirectoryInfo("Clusters");
            if (dir_info.Exists == false) dir_info.Create();
            ImageFileInfo imageFileInfo = new ImageFileInfo(new FileInfo(InputFile));
            src = BitmapAccess.LoadImage(InputFile, imageFileInfo.IsEncrypted);
            Init(); // Initialize the array of clusters by generating an initial cluster containing src image associated with the array of super-pixels
            BitmapAccess result = new BitmapAccess(src.Width, src.Height);// Initialize the bitmap object used to store the resulting segmented image
            int FrameIndex = 0;
            // Iterate throught the array of clusters until we've process all clusters being generated
            for (int Index = 0; Index < Clusters.Count(); Index++)
            {
                var seg = Clusters.ElementAt(Index);
                // For each particular cluster from the array, obtain the values of bitmap object and
                // the List<RGBPoint> object which is the array of centroid super-pixels
                List<ColorPoint> Centroids =seg.Centroids.ToList();
                BitmapAccess FrameBuffer = new BitmapAccess(seg.Source);

                // Save the image containg the segmented area associated with the current cluster to the
                // specific file, which name has the following format, for example "D:\Clusters\Cluster_N.jpg"
                seg.Save("Clusters\\Cluster_" + FrameIndex + ".jpg");

                // Iterating through the array of centroid super pixels and for each super-pixels
                // perform a linear search to find all those pixel in the current image which distance
                // does not exceed the value of specific boundary parameter.
                for (int Cnt = 0; Cnt < Centroids.Count(); Cnt++)
                {
                    // Obtain the value of Width and Height of the image for the current cluster
                    int Width = FrameBuffer.Width;
                    int Height = FrameBuffer.Height;

                    // Create a bitmap object to store an image for the newly built cluster
                    BitmapAccess TargetFrame = new BitmapAccess(FrameBuffer.Width, FrameBuffer.Height);

                    // Iterate through each element of the matrix of pixels for the current image
                    for (int Row = 0; Row < FrameBuffer.Width; Row++)
                    {
                         for (int Col = 0; Col < Height; Col++)
                         {
                            var pix = new ColorPoint(Row, Col, FrameBuffer.GetPixel(Row, Col));
                              // For each pixel in this matrix, compute the value of color offset of the current centroid super-pixel
                              double OffsetClr = pix.ClrDistance2(new ColorPoint(Centroids[Cnt].X, Centroids[Cnt].Y, Centroids[Cnt].Clr));

                              //Perform a check if the color offset value does not exceed the value of boundary parameter
                              if (OffsetClr <= clrGap* clrGap) // Copy the current pixel to the target image for the newly created cluster
                                  TargetFrame.SetPixel(Row, Col, Centroids[Cnt].Clr);
                              // in the target bitmap for the newly built cluster
                              else // Otherwise, set the color of the current pixel to "white"
                                  TargetFrame.SetPixel(Row, Col, Color.FromArgb(255, 255, 255, 255));
                         }
                    }
                    // Create an array of centroid super-pixels and append 
                    // it the value of current centroid super-pixel retrieved
                    List<ColorPoint> TargetCnts = new List<ColorPoint>();
                    TargetCnts.Add(Centroids[0]);

                    // Compute the "mean" value for the newly created cluster
                    ColorPoint Mean = Clusters.GetMean(TargetFrame, TargetCnts);

                    // Perform a check if the "mean" point coordinates of the newly created cluster are
                    // not equal to the coordinates of the current centroid super-pixel (e.g. the centroid
                    // super-pixel has been moved). If so, append a newly built cluster to the array of clusters
                    if (Mean.X != Clusters[Index].Center.X && Mean.Y != Clusters[Index].Center.Y)
                         Clusters.Add(TargetFrame, TargetCnts, Mean);

                     FrameIndex++;
                }
            }
            // Iterate through the array of clusters previously obtained
            for (int Index = 0; Index < Clusters.Count(); Index++)
            {
                // For each cluster retrieve a specific image containing the segmented area
                BitmapAccess FrameOut = new BitmapAccess(Clusters[Index].m_Bitmap);

                 FrameOut.LockBits();

                 FrameOut.Save("temp_" + Index + ".jpg");

                 // Obtain the dimensions of that image
                 int Width = FrameOut.Width, Height = FrameOut.Height;
                 // Iterate through the matrix of pixels for the current image and for each
                 // pixel perform a check if its color is not equal to "white" (R;G;B) => (255;255;255).
                 // If not, copy the pixel data to the target matrix of pixels for the resulting segmented image
                 for (int Row = 0; Row < Width; Row++)
                 {
                      for (int Col = 0; Col < Height; Col++)
                      {
                           if (FrameOut.GetPixel(Row, Col) != Color.FromArgb(255, 255, 255))
                           {
                               result.SetPixel(Row, Col, FrameOut.GetPixel(Row, Col));
                           }
                      }
                 }

                 FrameOut.UnlockBits();
            }
            // Save the segmented image to file with name which is the value of OutputFile variable
            result.Save(OutputFile);

            watch.Stop(); // Stop the execution timer
            // Obtain the value of executing time in milliseconds
            var elapsedMs = watch.ElapsedMilliseconds;

            // Create timespan from the elapsed milliseconds value
            TimeSpan ts = TimeSpan.FromMilliseconds(elapsedMs);
            // Print the message "Done" and the formatted execution time
            Console.WriteLine("***Done***\n" + ts.ToString(@"hh\:mm\:ss"));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Image Segmentation Utility v.1.0a by Arthur V. Ratz, CPOL @ 2017\n");

            Console.Write("Input file name: ");
            string InpFile = Console.ReadLine();

            Console.Write("Output file name: ");
            string OutFile = Console.ReadLine();

            ImageSegmentation ImageSeg = new ImageSegmentation();
            ImageSeg.Compute(InpFile, OutFile);

            Console.ReadKey();
        }
    }
}
