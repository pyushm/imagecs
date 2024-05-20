using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;

namespace ImageProcessor
{
    public class FlexiblePolygonEditor
    {   // 
        delegate void ClickHandler(object o, RoutedEventArgs e);
        DrawingPanel panel;             // parent drawing panel
        BitmapAccess img;
        int threshold = 3;              // max dif has to be > thereshold*average to change point
        internal void SetEdgeDetector(BitmapAccess ba) { img = ba; }
        FlexiblePolygon activeStroke = null;
        public BitmapAccess BackgroundImage { set { SetEdgeDetector(value); } }
        public FlexiblePolygonEditor(DrawingPanel panel_) { panel = panel_; }
        MenuItem CreateMenuItem(string title, ClickHandler handler)
        {
            MenuItem mi = new MenuItem();
            mi.Name = title.Replace(' ', '_');
            mi.Header = title;
            mi.Click += new RoutedEventHandler(handler);
            return mi;
        }
        internal MouseOperation MouseDown(MouseButtonEventArgs e, FlexiblePolygon[] ss, Point canvasPoint)
        {
            if (ss.Length == 0)
                return MouseOperation.None;
            FlexiblePolygon newActiveStroke = null;
            if (ss.Length == 1)
                newActiveStroke = activeStroke = ss[0];
            if (activeStroke.HitControlTest(canvasPoint))
                return MouseOperation.OpCenter;
            int sind = activeStroke != null ? activeStroke.HitContourTest(canvasPoint) : -1;  // segment index from activeStroke
            if (sind < 0 && ss.Length > 1) // if active stroke not hit and need to check other strokes
            {
                foreach (var fp in ss)
                {
                    sind = fp.HitContourTest(canvasPoint);
                    if (sind >= 0)
                    {   // 'fp' hit @ sind
                        newActiveStroke = fp;
                        break;
                    }
                }
            }
            //Debug.WriteLine("segment " + sind + " hit");
            if (sind < 0)   // nothing hit
                return MouseOperation.None; 
            if (activeStroke != newActiveStroke)
            {
                if (activeStroke != null) // unselect previous active stroke
                    activeStroke.RemoveProperty(PointProperties.Selected);
                activeStroke = newActiveStroke;
            }
            int pind = activeStroke.HitPointTest(canvasPoint); // pind>=0 => point 'pind' hit
            //Debug.WriteLine("point " + pind+" hit");
            if (e.ClickCount == 2)
            {
                if (pind >= 0)
                    activeStroke.InverseProperty(pind, PointProperties.Sharp);
                else
                    activeStroke.AddPoint(sind + 1, activeStroke.FromDrawing.Transform(canvasPoint));
                return MouseOperation.None;
            }
            if (e.ChangedButton != MouseButton.Left || pind < 0)
                return MouseOperation.None;
            activeStroke.RemoveProperty(PointProperties.Selected);
            activeStroke.AddProperty(pind, PointProperties.Selected);   // single left click on point marks point selected
            //Debug.WriteLine("move point "+ss.ToPropertiesString());
            return MouseOperation.Stroke;  // moving point starts   
        }
        internal bool MouseMove(MouseEventArgs e, Vector canvasShift)
        {
            if (panel.Selection != null)
            {
                Vector shift = panel.Selection.FromDrawing.Value.Transform(canvasShift);
                if (panel.Selection.MoveSelectedPoints(shift))
                    return false; // stop moving if point merged
            }
            //foreach (FlexiblePolygon ss in panel.Polygons)
            //{ // edit contour drawing
            //    Vector shift = ss.FromDrawing.Value.Transform(canvasShift);
            //    if (ss.MoveSelectedPoints(shift))
            //        editOperation = MouseOperations.Basic;
            //}
            return true;
        }
        internal void MouseUp(MouseButtonEventArgs e, Point mp)
        {
            ////foreach (FlexiblePolygon ss in panel.Polygons)
            //{ // edit contour drawing
            //    if (ss == activeStroke && singlePointInd >= 0)
            //    {
            //        ss.RemoveProperty(singlePointInd, PointProperties.Selected);
            //        singlePointInd = -1;
            //    }
            //}
            activeStroke.RemoveProperty(PointProperties.Selected);
            //Debug.WriteLine("SE up " + editOperation.ToString());
        }
        public int TryStickToImageEdge(FlexiblePolygon fp, int gradSearchRange)
        {
            if (fp.IsEmpty)
                return 0;
            int changed = 0;
            for (int i = 0; i < fp.Count; i++)
                if (!fp.HasProperty(i, PointProperties.Sharp))
                {
                    Point prev = i == 0 ? fp[fp.Count - 2] : fp[i - 1];
                    Point next = i == fp.Count-1 ? fp[1] : fp[i + 1];
                    Point p = fp[i];
                    if (SetsToMaxGradLocation(ref p, gradSearchRange, next - prev))
                    {
                        changed++;
                        fp[i] = p;
                    }
                }
            return changed;
        }
        bool SetsToMaxGradLocation(ref Point p, int range, Vector edgeDirection)
        {   // finds max grad on the line [p-n, p+n] within range
            if (img == null)
                return false;
            edgeDirection.Normalize();
            Vector n = new Vector(edgeDirection.Y, -edgeDirection.X);
            Point sp = p - n * range;                  // search point
            byte[][] data = new byte[2 * range + 1][];
            for (int i = 0; i <= 2 * range; i++)
            {
                data[i] = img.GetBytes((int)(sp.X + 0.5), (int)(sp.Y + 0.5));
                sp += n;
            }
            int maxDif = 0;
            int difTot = 0;
            int nc = data[0].Length;
            int maxInd = 0;
            for (int i = 1; i < 2 * range; i++)
            {
                int dif = Math.Abs(data[i - 1][0] - data[i + 1][0]);
                if (nc >= 3)
                    dif += Math.Abs(data[i - 1][1] - data[i + 1][1]) + Math.Abs(data[i - 1][2] - data[i + 1][2]);
                difTot += dif;
                if (maxDif < dif)
                {
                    maxDif = dif;
                    maxInd = i;
                }
            }
            bool changed = maxDif > 50 && maxDif * (2 * range - 1) > threshold * difTot;
            //Debug.WriteLine("changed="+ changed+ " maxDif =" + maxDif+ " av="+ difTot/ (2 * range - 1) + " shift="+(maxInd - range));
            if (changed)
                p += (maxInd - range) * n;
            return changed;
        }
    }
}