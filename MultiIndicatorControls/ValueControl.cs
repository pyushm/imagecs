﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CustomControls
{
    public class ValueControl : MultiIndicatorControl
    {   // allows selecting several individual values 
        protected override int PointDimension { get { return 1; } }
        public Color[] Colors { get; set; }     // control background
        public ValueControl()    { }
        //protected override Point[] IndicatorLocations(Point[] vals)
        //{
        //    Point[] iil = new Point[nPoints];
        //    for (int i = 0; i < nPoints; i++)
        //        iil[i] = new Point(SetX(leftOffset + border.Width * vals[i].X / 100), iArea[i].Y + iArea[i].Height / 2);
        //    return iil;
        //}
        public override float[] ControlPoints
        {
            set
            {
                Initialize(value.Length / PointDimension);
                SetValues(value);
                for (int i = 0; i < nPoints; i++)
                    iLoc[i].Y = iArea[i].Y + iArea[i].Height / 2;
                initialLoc = (Point[])iLoc.Clone();
            }
        }
        protected override void SetBrushes()// sets control background brushes
        {
            if (nPoints == 0)
                return;
            int m = (leftOffset + Width) / 2;
            Color c, cc;
            int gg = 170;
            Color g = Color.FromArgb(255, gg, gg, gg);
            int iah = (border.Height + nPoints / 2) / nPoints;
            iArea = new Rectangle[nPoints];
            iBrush=new Brush[nPoints];
            for (int i = 0; i < nPoints; i++)
            {
                int y = border.Y + i * iah+1;
                iArea[i] = new Rectangle(border.X, y, border.Width, iah);
                if (Colors != null && i < Colors.Length)
                {
                    c = MixColors(g, Colors[i], (offset+range+1)/2);
                    cc = MixColors(g, Colors[i], (offset+1)/2);
                    iBrush[i] = new LinearGradientBrush(new Point(leftOffset - 1, 0), new Point(Width, 0), c, cc);
                }
                else
                    iBrush[i] = new LinearGradientBrush(new Point(leftOffset - 1, 0), new Point(Width, 0), Color.White, Color.DarkGray);
            }
            Invalidate();
        }
        protected override void SetIndicatorValues(Point loc)      // set control values based on indicators' locations
        {
            int x = SetX(loc.X);
            if (activePoint < nPoints)
                iLoc[activePoint].X = x;
            else
            {
                int dv = x - sliderLoc.X;
                for (int i = 0; i < nPoints; i++)
                    iLoc[i].X = SetX(iLoc[i].X + dv);
            }
            ResetSlider();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs pe)
        {
            if (nPoints == 0)
                return;
            base.OnPaint(pe);
            DrawCommonElements(pe.Graphics);
            for (int i = 0; i < nPoints; i++)
                pe.Graphics.DrawLine(pen, iLoc[i].X, iArea[i].Top, iLoc[i].X, iArea[i].Bottom);
            for (int i = 0; i < nPoints; i++)
            {
                DrawIndicator(pe.Graphics, i);
                DrawValueLabel(pe.Graphics, i, iArea[i].Y);
            }
        }
    }
}
