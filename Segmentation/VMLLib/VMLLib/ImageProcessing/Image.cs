using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace VMLLib.ImageProcessing
{
    public static class Image
    {
        public static Bitmap ToGrayscale(Bitmap inputImage)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(inputImage.Width, inputImage.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(inputImage, new Rectangle(0, 0, inputImage.Width, inputImage.Height),
               0, 0, inputImage.Width, inputImage.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        public static Bitmap ToBlackAndWhite(Bitmap inputImage)
        {
            using (Graphics gr = Graphics.FromImage(inputImage)) // SourceImage is a Bitmap object
            {
                var gray_matrix = new float[][] {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0,      0,      0,      1, 0 },
                new float[] { 0,      0,      0,      0, 1 }
            };

                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(gray_matrix));
                ia.SetThreshold(0.8f); // Change this threshold as needed
                var rc = new Rectangle(0, 0, inputImage.Width, inputImage.Height);
                gr.DrawImage(inputImage, rc, 0, 0, inputImage.Width, inputImage.Height, GraphicsUnit.Pixel, ia);

                return inputImage;
            }
        }

        public static int[] GetHorizontalHistogram(Bitmap inputImage)
        {
            int[] values = new int[inputImage.Width];

            for(int i = 0; i < inputImage.Width; i++)
            {
                int totalBlackPixels = 0;
                for(int j = 0; j < inputImage.Height; j++)
                {
                    Color pixelColor = inputImage.GetPixel(i, j);
                    if (pixelColor.R != 255 && pixelColor.G != 255 && pixelColor.B != 255)
                        totalBlackPixels++;
                }
                values[i] = totalBlackPixels;
            }

            return values;
        }

        public static int[] GetVerticalHistogram(Bitmap inputImage)
        {
            int[] values = new int[inputImage.Height];

            for (int i = 0; i < inputImage.Height; i++)
            {
                int totalBlackPixels = 0;
                for (int j = 0; j < inputImage.Width; j++)
                {
                    Color pixelColor = inputImage.GetPixel(j, i);
                    if (pixelColor.R != 255 && pixelColor.G != 255 && pixelColor.B != 255)
                        totalBlackPixels++;
                }
                values[i] = totalBlackPixels;
            }

            return values;
        }
    }
}
