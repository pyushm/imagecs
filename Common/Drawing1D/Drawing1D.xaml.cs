using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;

namespace Presentation
{
    public partial class Drawing1D : Window // window with single plot
    {
        public bool IsClosed = false;
        PlotArea plotArea;
        Thickness frame;
        CurveSet output;
        public Range XRange { get { return plotArea.XRange; } set { plotArea.XRange = value; } }
        public int SetXValues(float[] xvs, bool extend = false) => plotArea.SetXValues(xvs, extend);
        public int NCurves => output.NCurves;
        public float[] XValues => plotArea.XValues;
        public Drawing1D(string title, Thickness border, double font = 16)
        {
            try
            {
                InitializeComponent();
                Title = title;
                frame = border;
                plotArea = new PlotArea(plotCanvas, frame, font);
                output = plotArea.LeftSet;
                plotArea.AddGridLines(Side.Horizontal(), Side.Vertical(10));
                plotArea.RedrawNotify = Redraw;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                using (StringReader sr = new StringReader(ex.Message))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                        sb.AppendLine(s);
                }
                MessageBox.Show(sb.ToString());
            }
        }
        public void SetYRange(double yMin, double yMax, bool round = true, bool include0 = true)
        {
            output.SetRange(yMin, yMax, round, include0);
            plotArea.SetYTicksToRange(Side.Left);
        }
        public bool AddData(float[] val, string name) 
        {
            if (val == null || val.Length != plotArea.XValues.Length)
                return false;
            try
            {
                PlotCurve c = new PlotCurve(name, output, val);
                c.Draw();
            }
            catch (Exception ex) { Debug.WriteLine(ex.StackTrace); }
            return true;
        }
        public void Redraw() => plotCanvas.InvalidateVisual();
        void WindowSizeChanged(object sender, SizeChangedEventArgs e) 
        {
            var plotRect = new Rect(frame.Left, frame.Top, Width - frame.Left - frame.Right, Height - frame.Top - frame.Bottom);
            plotArea.Resize(plotRect);
            plotArea.RedrawFrame();
            foreach (var c in output.Curves)
                c.Draw();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }
    }
}
