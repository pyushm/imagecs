using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections;
using System.Windows;
using System.Collections.Concurrent;
using System.IO;
using System.Drawing.Imaging;

namespace KMC
{
    class KMCPoint<T>
    {
        // KMCPoint constructor
        public KMCPoint(T X, T Y, Color Clr) { this.X = X; this.Y = Y; this.Clr = Clr; }
        // X coordinate property
        public T X { get { return m_X; } set { m_X = value; } }
        // Y coordinate property
        public T Y { get { return m_Y; } set { m_Y = value; } }
        // Colorref property
        public Color Clr { get { return m_Color; } set { m_Color = value; } }

        private T m_X; // X coord
        private T m_Y; // Y coord
        private Color m_Color; // Colorref
    }
    class KMCFrame
    {
        // KMCFrame Constructor
        public KMCFrame(LockedBitmap Frame, List<KMCPoint<int>> Centroids, KMCPoint<int> Center)
        {
            this.Frame = Frame;
            this.m_Centroids = Centroids; this.Center = Center;
        }
        // Bitmap Frame Property
        public LockedBitmap Frame
        {
            get { return m_Frame; }
            set { m_Frame = value;  }
        }
        // Centroids List Property
        public List<KMCPoint<int>> Centroids
        {
            get { return m_Centroids; }
            set { m_Centroids = value; }
        }
        // Central Super-Pixel Property
        public KMCPoint<Int32> Center
        {
            get { return m_Center; }
            set { m_Center = value; }
        }
        // Bitmap Frame Object
        private LockedBitmap m_Frame = null;
        // Central Super-Pixel Point Object
        private KMCPoint<Int32> m_Center;
        // Array of Super-Pixel Objects (i.e. Centroids)
        private List<KMCPoint<int>> m_Centroids = null;
    }

    class KMCClusters : IEnumerable<KMCFrame>
    {
        private readonly System.Random rand = new System.Random();
        private static HashSet<KMCFrame> m_Clusters = new HashSet<KMCFrame>();
        public void Init(string Filename, Int32 Distance, Int32 Offset)
        {
            // Declare a bitmap object to load and use the original image to be segmented
            LockedBitmap FrameBuffer = new LockedBitmap(Filename);

            // Initialize the array of super-pixels by creating List<KMCPoint<int>> class object
            List<KMCPoint<int>> Centroids = new List<KMCPoint<int>>();
            // Generate an initial array of super-pixels of the original source image
            // stored in the FrameBuffer bitmap object
            this.Generate(ref Centroids, FrameBuffer, Distance, Offset);

            // Compute the value of the centeral super-pixel coordinates and assign it
            // to the Mean local variable
            KMCPoint<int> Mean = this.GetMean(FrameBuffer, Centroids);

            // Append an initial cluster being initialized to the array of clusters
            m_Clusters.Add(new KMCFrame(FrameBuffer, Centroids, Mean));
        }
        public void Generate(ref List<KMCPoint<int>> Centroids, LockedBitmap ImageFrame, Int32 Distance, Int32 Offset)
        {
            // Compute the number of iterations performed by the main loop equal to image W * H
            // The following value is the maximum possible number of random super-pixel being generated
            Int32 Size = ImageFrame.Width * ImageFrame.Height;
            ImageFrame.LockBits();
            // Performing Size - iterations of the following loop to generate a specific amount of super-pixels
            for (Int32 IterCount = 0; IterCount < Size; IterCount++)
            {
                // Obtain a random value of X - coordinate of the current super-pixel
                Int32 Rand_X = rand.Next(0, ImageFrame.Width);
                // Obtain a random value of Y - coordinate of the current super-pixel
                Int32 Rand_Y = rand.Next(0, ImageFrame.Height);

                // Create and instantinate a point object by using the values of
                // Rand_X, Rand_Y and Colorref parameters. The value of colorref is
                // retrieved by using the GetPixel method for the current bitmap object
                KMCPoint<int> RandPoint = new KMCPoint<int>(Rand_X,
                              Rand_Y, ImageFrame.GetPixel(Rand_X, Rand_Y));

                // Performing a validity check if none of those super-pixel previously
                // selected don't exceed the distance and color offset boundary to the 
                // currently generated super-pixel with coordinates Rand_X and Rand_Y and
                // specific color stored as a parameter value of Clr variable
                if (!this.IsValidColor(Centroids, RandPoint, Offset) &&
                    !this.IsValidDistance(Centroids, RandPoint, Distance))
                {
                     // If not, check if the super-pixel with the following coordinates and color
                     // already exists in the array of centroid super-pixels being generated.
                     if (!Centroids.Contains(RandPoint))
                     {
                         // If not, append the object RandPoint to the array of super-pixel objects
                         Centroids.Add(RandPoint);
                     }
                }
            }

            ImageFrame.UnlockBits();
        }
        private bool IsValidDistance(List<KMCPoint<int>> Points, KMCPoint<int> Target, Int32 Distance)
        {
            Int32 Index = -1; bool Exists = false;
            // Iterate through the array of super-pixels until we've found the super-pixel which
            // distance to the target super-pixel is less than or equals to the specified boundary
            while (++Index < Points.Count() && !Exists)
                // For each super-pixel from the array we compute the value of distance and
                // perform a check if the following value is less than or equals to 
                // the value of specific boundary parameter.
                Exists = ((Math.Abs(Target.X - Points.ElementAt(Index).X) <= Distance) ||
                          (Math.Abs(Target.Y - Points.ElementAt(Index).Y) <= Distance)) ? true : false;

            return Exists;
        }
        private bool IsValidColor(List<KMCPoint<int>> Points, KMCPoint<int> Target, Int32 Offset)
        {
            Int32 Index = -1; bool Exists = false;
            // Iterate through the array of super-pixels until we've found the super-pixel which
            // color offset to the target super-pixel is less than or equals to the specified boundary
            while (++Index < Points.Count() && !Exists)
                // For each super-pixel from the array we compute the value of color offset and
                // perform a check if the following value is less than or equals to 
                // the value of specific boundary parameter.
                Exists = (Math.Sqrt(Math.Pow(Math.Abs(Points[Index].Clr.R - Target.Clr.R), 2) +
                                    Math.Pow(Math.Abs(Points[Index].Clr.G - Target.Clr.G), 2) +
                                    Math.Pow(Math.Abs(Points[Index].Clr.B - Target.Clr.B), 2))) <= Offset ? true : false;

            return Exists;
        }
        public KMCPoint<int> GetMean(LockedBitmap FrameBuffer, List<KMCPoint<int>> Centroids)
        {
            // Declaring two variables to assign the value of the "mean" of 
            // the sets of coordinates (X;Y) of each super-pixel
            double Mean_X = 0, Mean_Y = 0;
            // Iterating through the array of super-pixels and for each
            // super-pixel retrieve its X and Y coordinates and divide it
            // by the overall amount of super-pixels. After that, sum up
            // each value with the values of Mean_X and Mean_Y variables
            for (Int32 Index = 0; Index < Centroids.Count(); Index++)
            {
                Mean_X += Centroids[Index].X / (double)Centroids.Count();
                Mean_Y += Centroids[Index].Y / (double)Centroids.Count();
            }

            // Convert the values of Mean_X and Mean_Y to Int32 datatype
            Int32 X = Convert.ToInt32(Mean_X);
            Int32 Y = Convert.ToInt32(Mean_Y);

            FrameBuffer.LockBits();
            Color Clr = FrameBuffer.GetPixel(X, Y);
            FrameBuffer.UnlockBits();

            // Constructing KMCPoint<int> object and return its value
            return new KMCPoint<int>(X, Y, Clr);
        }
        public void Add(LockedBitmap FrameImage, List<KMCPoint<int>> Centroids, KMCPoint<int> Center)
        {
            m_Clusters.Add(new KMCFrame(FrameImage, Centroids, Center));
        }

        public KMCFrame this[Int32 Index]
        {
            get { return m_Clusters.ElementAt(Index); }
        }

        public IEnumerator<KMCFrame> GetEnumerator()
        {
            return m_Clusters.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class LockedBitmap
    {
        public LockedBitmap(string filename)
        {
            if (m_Bitmap == null)
            {
                m_Bitmap = new Bitmap(filename);
                m_rect = new Rectangle(new Point(0, 0), m_Bitmap.Size);
            }
        }
        public LockedBitmap(Int32 Width, Int32 Height)
        {
            if (m_Bitmap == null)
            {
                m_Bitmap = new Bitmap(Width, Height);
                m_rect = new Rectangle(new Point(0, 0), m_Bitmap.Size);
            }
        }
        public LockedBitmap(Bitmap bitmap)
        {
            if (m_Bitmap == null)
            {
                m_Bitmap = new Bitmap(bitmap);
                m_rect = new Rectangle(new Point(0, 0), m_Bitmap.Size);
            }
        }
        public static implicit operator LockedBitmap(Bitmap bitmap)
        {
            return new LockedBitmap(bitmap);
        }
        public void LockBits()
        {
            m_BitmapInfo = m_Bitmap.LockBits(m_rect, System.Drawing.Imaging.
                ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

            m_BitmapPtr = m_BitmapInfo.Scan0;
            m_Pixels = new byte[Math.Abs(m_BitmapInfo.Stride) * m_Bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(m_BitmapPtr, m_Pixels,
                0, Math.Abs(m_BitmapInfo.Stride) * m_Bitmap.Height);
        }
        public void UnlockBits()
        {
            m_BitmapPtr = m_BitmapInfo.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(m_Pixels, 0,
                m_BitmapPtr, Math.Abs(m_BitmapInfo.Stride) * m_Bitmap.Height);

            // Unlock the bits.
            m_Bitmap.UnlockBits(m_BitmapInfo);
        }
        public Color GetPixel(Int32 Row, Int32 Col)
        {
            Int32 Channel = System.Drawing.Bitmap.GetPixelFormatSize(m_BitmapInfo.PixelFormat);
            Int32 Pixel = (Row + Col * m_Bitmap.Width) * (Channel / 8);

            Int32 Red = 0, Green = 0, Blue = 0, Alpha = 0;

            if (Channel == 32)
            {
                Blue = m_Pixels[Pixel];
                Green = m_Pixels[Pixel + 1];
                Red = m_Pixels[Pixel + 2];
                Alpha = m_Pixels[Pixel + 3];
            }

            else if (Channel == 24)
            {
                Blue = m_Pixels[Pixel];
                Green = m_Pixels[Pixel + 1];
                Red = m_Pixels[Pixel + 2];
            }

            else if (Channel == 16)
            {
                Blue = m_Pixels[Pixel];
                Green = m_Pixels[Pixel + 1];
            }

            else if (Channel == 8)
            {
                Blue = m_Pixels[Pixel];
            }

            return (Channel != 8) ? Color.FromArgb(Red, Green, Blue) : Color.FromArgb(Blue, Blue, Blue);
        }
        public void SetPixel(Int32 Row, Int32 Col, Color Clr)
        {
            Int32 Channel = System.Drawing.Bitmap.GetPixelFormatSize(m_BitmapInfo.PixelFormat);
            Int32 Pixel = (Row + Col * m_Bitmap.Width) * (Channel / 8);

            if (Channel == 32)
            {
                m_Pixels[Pixel] = Clr.B;
                m_Pixels[Pixel + 1] = Clr.G;
                m_Pixels[Pixel + 2] = Clr.R;
                m_Pixels[Pixel + 3] = Clr.A;
            }

            else if (Channel == 24)
            {
                m_Pixels[Pixel] = Clr.B;
                m_Pixels[Pixel + 1] = Clr.G;
                m_Pixels[Pixel + 2] = Clr.R;
            }

            else if (Channel == 16)
            {
                m_Pixels[Pixel] = Clr.B;
                m_Pixels[Pixel + 1] = Clr.G;
            }

            else if (Channel == 8)
            {
                m_Pixels[Pixel] = Clr.B;
            }
        }

        public Int32 Width { get { return m_Bitmap.Width; } }
        public Int32 Height { get { return m_Bitmap.Height; } }

        public void Save(string filename)
        {
            m_Bitmap.Save(filename);
        }

        public Bitmap m_Bitmap = null;

        private Rectangle m_rect;
        private IntPtr m_BitmapPtr;
        private byte[] m_Pixels = null;
        private BitmapData m_BitmapInfo = null;
    }
    class ImageSegmentation
    {
        private const Int32 m_Distance = 5;
        private const Int32 m_OffsetClr = 50;

        private static KMCClusters m_Clusters = new KMCClusters();
        public ImageSegmentation() { }
        public void Compute(string InputFile, string OutputFile)
        {
            // Initialize the code execution timer
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Initialize the directory info reference object
            DirectoryInfo dir_info = new DirectoryInfo("Clusters");
            // Check if the directory with name "Clusters" is created.
            // If not, create the directory with name "Clusters"
            if (dir_info.Exists == false) dir_info.Create();

            // Initialize the array of clusters by generating an initial cluster
            // containing the original source image associated with the array of super-pixels
            m_Clusters.Init(InputFile, m_Distance, m_OffsetClr);

            // Initialize the bitmap object used to store the resulting segmented image
            LockedBitmap ResultBitmap = new LockedBitmap(m_Clusters[0].Frame.Width, m_Clusters[0].Frame.Height);

            Int32 FrameIndex = 0;
            // Iterate throught the array of clusters until we've process all clusters being generated
            for (Int32 Index = 0; Index < m_Clusters.Count(); Index++)
            {
                // For each particular cluster from the array, obtain the values of bitmap object and
                // the List<KMCPoint<int>> object which is the array of centroid super-pixels
                List<KMCPoint<int>> Centroids = m_Clusters[Index].Centroids.ToList();
                LockedBitmap FrameBuffer = new LockedBitmap(m_Clusters[Index].Frame.m_Bitmap);

                // Save the image containg the segmented area associated with the current cluster to the
                // specific file, which name has the following format, for example "D:\Clusters\Cluster_N.jpg"
                FrameBuffer.Save("Clusters\\Cluster_" + FrameIndex + ".jpg");

                FrameBuffer.LockBits();

                // Iterating through the array of centroid super pixels and for each super-pixels
                // perform a linear search to find all those pixel in the current image which distance
                // does not exceed the value of specific boundary parameter.
                for (Int32 Cnt = 0; Cnt < Centroids.Count(); Cnt++)
                {
                    // Obtain the value of Width and Height of the image for the current cluster
                    Int32 Width = FrameBuffer.Width;
                    Int32 Height = FrameBuffer.Height;

                    // Create a bitmap object to store an image for the newly built cluster
                    LockedBitmap TargetFrame = new LockedBitmap(FrameBuffer.Width, FrameBuffer.Height);

                    TargetFrame.LockBits();

                    // Iterate through each element of the matrix of pixels for the current image
                    for (Int32 Row = 0; Row < FrameBuffer.Width; Row++)
                    {
                         for (Int32 Col = 0; Col < Height; Col++)
                         {
                              // For each pixel in this matrix, compute the value of color offset of the current centroid super-pixel
                              double OffsetClr = GetEuclClr(new KMCPoint<int>(Row, Col, FrameBuffer.GetPixel(Row, Col)),
                                                            new KMCPoint<int>(Centroids[Cnt].X, Centroids[Cnt].Y, Centroids[Cnt].Clr));

                              //Perform a check if the color offset value does not exceed the value of boundary parameter
                              if (OffsetClr <= 50)
                              {
                                  // Copy the current pixel to the target image for the newly created cluster
                                  TargetFrame.SetPixel(Row, Col, Centroids[Cnt].Clr);
                              }

                              // Otherwise, set the color of the current pixel to "white" (R;G;B) => (255;255;255)
                              // in the target bitmap for the newly built cluster
                              else TargetFrame.SetPixel(Row, Col, Color.FromArgb(255, 255, 255));
                         }
                    }

                    TargetFrame.UnlockBits();

                    // Create an array of centroid super-pixels and append 
                    // it the value of current centroid super-pixel retrieved
                    List<KMCPoint<int>> TargetCnts = new List<KMCPoint<int>>();
                    TargetCnts.Add(Centroids[0]);

                    // Compute the "mean" value for the newly created cluster
                    KMCPoint<int> Mean = m_Clusters.GetMean(TargetFrame, TargetCnts);

                    // Perform a check if the "mean" point coordinates of the newly created cluster are
                    // not equal to the coordinates of the current centroid super-pixel (e.g. the centroid
                    // super-pixel has been moved). If so, append a newly built cluster to the array of clusters
                    if (Mean.X != m_Clusters[Index].Center.X && Mean.Y != m_Clusters[Index].Center.Y)
                         m_Clusters.Add(TargetFrame, TargetCnts, Mean);

                     FrameIndex++;
                }

                FrameBuffer.UnlockBits();
            }

            ResultBitmap.LockBits();

            // Iterate through the array of clusters previously obtained
            for (Int32 Index = 0; Index < m_Clusters.Count(); Index++)
            {
                 // For each cluster retrieve a specific image containing the segmented area
                 LockedBitmap FrameOut = new LockedBitmap(m_Clusters[Index].Frame.m_Bitmap);

                 FrameOut.LockBits();

                 FrameOut.Save("temp_" + Index + ".jpg");

                 // Obtain the dimensions of that image
                 int Width = FrameOut.Width, Height = FrameOut.Height;
                 // Iterate through the matrix of pixels for the current image and for each
                 // pixel perform a check if its color is not equal to "white" (R;G;B) => (255;255;255).
                 // If not, copy the pixel data to the target matrix of pixels for the resulting segmented image
                 for (Int32 Row = 0; Row < Width; Row++)
                 {
                      for (Int32 Col = 0; Col < Height; Col++)
                      {
                           if (FrameOut.GetPixel(Row, Col) != Color.FromArgb(255, 255, 255))
                           {
                               ResultBitmap.SetPixel(Row, Col, FrameOut.GetPixel(Row, Col));
                           }
                      }
                 }

                 FrameOut.UnlockBits();
            }

            ResultBitmap.UnlockBits();

            // Save the segmented image to file with name which is the value of OutputFile variable
            ResultBitmap.Save(OutputFile);

            watch.Stop(); // Stop the execution timer
            // Obtain the value of executing time in milliseconds
            var elapsedMs = watch.ElapsedMilliseconds;

            // Create timespan from the elapsed milliseconds value
            TimeSpan ts = TimeSpan.FromMilliseconds(elapsedMs);
            // Print the message "Done" and the formatted execution time
            Console.WriteLine("***Done***\n" + ts.ToString(@"hh\:mm\:ss"));
        }
        public double GetEuclD(KMCPoint<int> Point1, KMCPoint<int> Point2)
        {
            // Compute the Euclidian distance between two pixel in the 2D-space
            return Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) + 
                             Math.Pow(Point1.Y - Point2.Y, 2));
        }
        public double GetEuclClr(KMCPoint<int> Point1, KMCPoint<int> Point2)
        {
            // Compute the Euclidian distance between two colors in the 3D-space
            return Math.Sqrt(Math.Pow(Math.Abs(Point1.Clr.R - Point2.Clr.R), 2) +
                             Math.Pow(Math.Abs(Point1.Clr.G - Point2.Clr.G), 2) +
                             Math.Pow(Math.Abs(Point1.Clr.B - Point2.Clr.B), 2));
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
