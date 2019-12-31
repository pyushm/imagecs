using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageProcessor
{
    public class FlexiblePolygonEditor
    {   // 
        delegate void ClickHandler(object o, RoutedEventArgs e);
        DrawingPanel panel;             // parent drawing panel
        BitmapAccess img;
        int threshold = 3;              // max dif has to be > thereshold*average to change point
        int range = 5;                  // search range
        internal void SetEdgeDetector(BitmapAccess ba) { img = ba; }
        FlexiblePolygon activeStroke = null;
        int activePointInd = -1;
        int singlePointInd = -1;        // index of single moved point
        MouseOperations editOperation;
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
        bool ProcessStrokes(MouseButtonEventArgs e, FlexiblePolygon[] ss, Point canvasPoint)
        {
            FlexiblePolygon newActiveStroke = null;
            int ind = -1;
            if (ss.Length == 0)
                return false;
            if (ss.Length == 1)
                activeStroke = newActiveStroke = ss[0];
            if (activeStroke != null)
            {
                Point np = activeStroke.FromDrawing.Transform(canvasPoint);
                ind = activeStroke.HitPointTest(canvasPoint); // i>=0 => point i hit
                if (ind < 0 && e.ClickCount == 2)
                {
                    ind = activeStroke.HitContourTest(canvasPoint);
                    if (ind >= 0)
                        activeStroke.AddPoint(ind + 1, np);
                    return true;
                }
                if (ind < 0)
                    return false;
                if (e.ClickCount == 2)
                {
                    activeStroke.InverseProperty(ind, PointProperties.Sharp);
                    return true;
                }
            }
            else
            {
                foreach (var fp in ss)
                {
                    ind = fp.HitContourTest(canvasPoint);
                    if (ind >= 0)
                    {
                        newActiveStroke = fp;
                        break;
                    }
                }
            }
            if (activeStroke != newActiveStroke && activeStroke != null) // unselect previous active stroke
                activeStroke.RemoveProperty(PointProperties.Selected);
            activeStroke = newActiveStroke;
            if (activeStroke == null)
                return false;
            //activeStroke.AddProperty(PointProperties.Show);
            activePointInd = ind;
            if (e.ChangedButton == MouseButton.Left)
            {   // single left click on point adds point to selected and starts moving selected points  
                activeStroke.AddProperty(ind, PointProperties.Selected);
                singlePointInd = ind;
                editOperation = MouseOperations.Move;
                Mouse.OverrideCursor = Cursors.Pen;
                //Debug.WriteLine("move point "+ss.ToPropertiesString());
            }
            return true;
        }
        internal MouseOperations MouseDown(MouseButtonEventArgs e, FlexiblePolygon[] ss, Point canvasPoint)
        {
            editOperation = MouseOperations.None;
            activePointInd = -1;
            ProcessStrokes(e, ss, canvasPoint);
            //Debug.WriteLine("SE down " + editOperation.ToString());
            return editOperation == MouseOperations.None ? MouseOperations.None : MouseOperations.Stroke;
        }
        internal void MouseMove(MouseEventArgs e, Vector canvasShift)
        {
            if (editOperation == MouseOperations.Move)
            {
                if(panel.Selection != null)
                {
                    Vector shift = panel.Selection.FromDrawing.Value.Transform(canvasShift);
                    if (panel.Selection.MoveSelectedPoints(shift))
                        editOperation = MouseOperations.None;
                }
                //foreach (FlexiblePolygon ss in panel.Polygons)
                //{
                //    Vector shift = ss.FromDrawing.Value.Transform(canvasShift);
                //    if (ss.MoveSelectedPoints(shift))
                //        editOperation = MouseOperations.None;
                //}
            }
            //Debug.WriteLine("SE move " + editOperation.ToString());
        }
        internal void MouseUp(MouseButtonEventArgs e, Point mp)
        {
            if (editOperation == MouseOperations.Move)
            {   // ends operation
                if (panel.Selection != null && singlePointInd >= 0)
                {
                    panel.Selection.RemoveProperty(singlePointInd, PointProperties.Selected);
                    singlePointInd = -1;
                }
                //foreach (FlexiblePolygon ss in panel.Polygons)
                //{
                //    if (ss == activeStroke && singlePointInd >= 0)
                //    {
                //        ss.RemoveProperty(singlePointInd, PointProperties.Selected);
                //        singlePointInd = -1;
                //    }
                //}
                editOperation = MouseOperations.None;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
            //Debug.WriteLine("SE up " + editOperation.ToString());
        }
        public int StickToEdge(FlexiblePolygon fp, double del)
        {
            if (fp.IsEmpty)
                return 0;
            int changed = 0;
            range = (int)(del+0.5);
            for (int i = 0; i < fp.Count; i++)
                if (!fp.HasProperty(i, PointProperties.Sharp))
                {
                    Point prev = i == 0 ? fp[fp.Count - 2] : fp[i - 1];
                    Point next = i == fp.Count-1 ? fp[1] : fp[i + 1];
                    Point p = fp[i];
                    if (EdgeCorrection(ref p, next - prev))
                    {
                        changed++;
                        fp[i] = p;
                    }
                }
            return changed;
        }
        bool EdgeCorrection(ref Point p, Vector edgeDirection)
        {   // finds max grad on the line [p-n, p+n] within range
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