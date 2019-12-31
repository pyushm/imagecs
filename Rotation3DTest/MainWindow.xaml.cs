using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using ImageProcessor;

namespace Rotation3DTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            double distortionX = -0.3;
            double distortionY = 0;
            //var src1 = BitmapAccess.LoadImage(@"D:\OldC\maui201910\DSC_0003.JPG", false);
            var src1 =new BitmapImage(new Uri(@"D:\OldC\maui201910\DSC_0003.JPG"));
            var ivp1 = CreateProjection(src1, distortionX, distortionY);
            g1.Children.Add(ivp1);
            ivp1.SetValue(Grid.ColumnProperty, 0); // default
            ivp1.SetValue(Grid.RowProperty, 0);
            //var src2 = BitmapAccess.LoadImage(@"D:\OldC\maui201910\DSC_0003v.JPG", false);
            var src2 = new BitmapImage(new Uri(@"D:\OldC\maui201910\DSC_0003v.JPG"));
            var ivp2 = CreateProjection(src2, distortionX, distortionY);
            g1.Children.Add(ivp2);
            ivp2.SetValue(Grid.ColumnProperty, 1); // default
            ivp2.SetValue(Grid.RowProperty, 0);
        }
        Viewport3D CreateProjection(BitmapSource imageSource, double distortionX, double distortionY)
        //Bitmap3DLayer CreateProjection(BitmapAccess ba, double distortionX, double distortionY)
        {
            //Bitmap3DLayer ivp = new Bitmap3DLayer("test", ba, 0);
            Viewport3D ivp = new Viewport3D();
            double cameraDistance = 1;
            double viewHalfSize = 1;
            //double h = ba.Height;
            //double w = ba.Width;
            double h = imageSource.PixelHeight;
            double w = imageSource.PixelWidth;
            double xs = h < w ? 0 : h / w / 2 - 0.5;
            double ys = h < w ? w / h / 2 - 0.5 : 0;
            distortionY /= 1 + ys;
            distortionX /= 1 + xs;
            double distortion = Math.Sqrt(distortionX * distortionX + distortionY * distortionY);
            Rect viewRect = new Rect(-viewHalfSize, -viewHalfSize, 2 * viewHalfSize, 2 * viewHalfSize);
            double angleOfView = 360 * Math.Atan(viewHalfSize / cameraDistance) / Math.PI; // smaller angle - less you see in the port
            Vector3D rotationAxis = new Vector3D(distortionY / distortion, distortionX / distortion, 0);

            ivp.Camera = new PerspectiveCamera(new Point3D(0, 0, cameraDistance), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), angleOfView);
            double rotationAngle = 180 * Math.Atan(cameraDistance * distortion * 0.5 / viewHalfSize) / Math.PI;
            var r3d = new RotateTransform3D() { Rotation = new AxisAngleRotation3D(rotationAxis, rotationAngle) };
            var viewPosition = new Point3DCollection(new Point3D[] { new Point3D(viewRect.Left, viewRect.Bottom, 0), new Point3D(viewRect.Left, viewRect.Top, 0),
                                                                     new Point3D(viewRect.Right, viewRect.Top, 0), new Point3D(viewRect.Right, viewRect.Bottom, 0) });
            var texturePosition = new PointCollection(new Point[] { new Point(-xs, -ys), new Point(-xs, 1 + ys), new Point(1 + xs, 1 + ys), new Point(1 + xs, -ys) });
            var mg3d = new MeshGeometry3D() { Positions = viewPosition, TextureCoordinates = texturePosition, TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 } };
            var im = new DiffuseMaterial();
            im.SetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty, true);
            var i3d = new Viewport2DVisual3D() { Geometry = mg3d, Transform = r3d, Material = im, Visual = new Image() { Source = imageSource } };
            ivp.Children.Add(i3d);
            ivp.Children.Add(new ModelVisual3D() { Content = new AmbientLight(Colors.White) });
            ivp.RenderTransform = new MatrixTransform();
            ivp.LayoutTransform=new MatrixTransform();
            //children.Add(CreateImageDrawing());
            return ivp;
        }
    }
}
