using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ImageProcessor
{
    public class PointAnimationUsingPathExample : Page
    {
        public PointAnimationUsingPathExample()
        {
            NameScope.SetNameScope(this, new NameScope());// Create a NameScope for the page to use Storyboards.
            EllipseGeometry animatedEllipseGeometry = new EllipseGeometry(new Point(10, 100), 15, 15);// Create the EllipseGeometry to animate.
            RegisterName("AnimatedEllipseGeometry", animatedEllipseGeometry);// Register the EllipseGeometry's name with the page to be targeted by a storyboard.
            Path ellipsePath = new Path();// Create a Path element to display the geometry.
            ellipsePath.Data = animatedEllipseGeometry;
            ellipsePath.Fill = Brushes.Blue;
            ellipsePath.Margin = new Thickness(15);
            Canvas mainPanel = new Canvas();// Create a Canvas to contain ellipsePath and add it to the page.
            mainPanel.Width = 400;
            mainPanel.Height = 400;
            mainPanel.Children.Add(ellipsePath);
            Content = mainPanel;
            PathGeometry animationPath = new PathGeometry();// Create the animation path.
            PathFigure pFigure = new PathFigure();
            pFigure.StartPoint = new Point(10, 100);
            PolyBezierSegment pBezierSegment = new PolyBezierSegment();
            pBezierSegment.Points.Add(new Point(35, 0));
            pBezierSegment.Points.Add(new Point(135, 0));
            pBezierSegment.Points.Add(new Point(160, 100));
            pBezierSegment.Points.Add(new Point(180, 190));
            pBezierSegment.Points.Add(new Point(285, 200));
            pBezierSegment.Points.Add(new Point(310, 100));
            pFigure.Segments.Add(pBezierSegment);
            animationPath.Figures.Add(pFigure);
            animationPath.Freeze();// Freeze the PathGeometry for performance benefits.
            // Create a PointAnimationgUsingPath to move EllipseGeometry along the animation path.
            PointAnimationUsingPath centerPointAnimation = new PointAnimationUsingPath();
            centerPointAnimation.PathGeometry = animationPath;
            centerPointAnimation.Duration = TimeSpan.FromSeconds(5);
            centerPointAnimation.RepeatBehavior = RepeatBehavior.Forever;
            // Set the animation to target the Center property of the EllipseGeometry named "AnimatedEllipseGeometry".
            Storyboard.SetTargetName(centerPointAnimation, "AnimatedEllipseGeometry");
            Storyboard.SetTargetProperty(centerPointAnimation, new PropertyPath(EllipseGeometry.CenterProperty));
            // Create a Storyboard to contain and apply the animation.
            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            pathAnimationStoryboard.AutoReverse = true;
            pathAnimationStoryboard.Children.Add(centerPointAnimation);
            ellipsePath.Loaded += delegate (object sender, RoutedEventArgs e)// Start the Storyboard when ellipsePath is loaded.
            {
                pathAnimationStoryboard.Begin(this);// Start the storyboard.
            };
        }
    }
}