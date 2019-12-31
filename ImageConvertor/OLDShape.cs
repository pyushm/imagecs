//using System;
//using System.Drawing;
//using System.Collections.Generic;
//using System.Drawing.Drawing2D;
//using System.Collections;
//using System.IO;

//namespace ImageProcessor
//{
//    public enum EditMode
//    {
//        Click,      // points added at mouse click
//        Find,       // finds control points in the background image
//        Shape,      // shape control points move or delete
//        Geometry,   // adjust transform
//    }
//    [Serializable]
//    public class ShapeData                  // handles data to be storred
//    {
//        protected Color color;              // border color
//        protected int thickness;            // border thickness
//        protected float smoothness;         // control of conversion to drawing points
//        protected List<PointF> points;	        // list of PointF - drawing control points
//        internal ShapeData(int thickness_, float smoothness_, Color color_)
//        {
//            color = color_;
//            thickness = thickness_;
//            smoothness = smoothness_;
//            points = new List<PointF>();
//        }
//        internal ShapeData Clone(ShapeData sb)
//        {
//            ShapeData newsb = new ShapeData(sb.thickness, sb.smoothness, sb.color);
//            newsb.points.AddRange(sb.points);
//            return newsb;
//        }
//    }
//    public abstract class Shape : ShapeData // manipulates shape
//	{
//        Brush pointBrush = new SolidBrush(Color.White);
//        Brush activePointBrush = new SolidBrush(Color.Gray);
//        Pen controlPen = new Pen(Color.Black);
//        Matrix matrix = new Matrix();
//        EditMode editMode = EditMode.Click;
//        protected int captureSize = 4;      // control point capture size
//        protected int markerSize = 2;       // control point marker size
//        protected int activeIndex = -1;     // index of active control point
//        public EditMode EditMode            { get { return editMode; } set { editMode = value; } }
//        public Color Color                  { get { return color; } set { color = value; } }
//        public int Thickness                { get { return thickness; } set { thickness = value; } }
//        public float Smoothness             { get { return smoothness; } set { smoothness = value; } }
//        public int Count                    { get { return points.Count; } }
//        public PointF this[int i]           { get { return points[i]; } set { points[i] = value; } }
//        public void Clear()                 { points.Clear(); }
//        public void Undo()                  { if (points.Count > 0) points.RemoveAt(points.Count - 1); }
//        internal Shape(int t, float s) : base(t, s, Color.Black)
//        {
//        }
//        internal Shape(int t, Color c) : base(t, 0, c)
//        {
//        }
//        internal Shape(int t, float s, Color c) : base(t, s, c)
//        {
//        }
//        public void Add(PointF point)       { points.Add(point); }
//        public void Add(PointF[] addPoints) { points.AddRange(addPoints); }
//        public void Add(Shape shape) { points.AddRange(shape.points); }
//        public abstract void Draw(Graphics g);
//        protected void DrawControlPoint(Graphics g, PointF p, Brush fillBrush)
//        {
//            matrix = g.Transform;
//            Rectangle r = new Rectangle((int)p.X - markerSize, (int)p.Y - markerSize, 2 * markerSize, 2 * markerSize);
//            g.FillRectangle(fillBrush, r);
//            g.DrawRectangle(controlPen, r);
//        }
//        public void DrawControlPoints(Graphics g)
//        {
//            for (int i = 0; i < Count; i++)
//                DrawControlPoint(g, this[i], i==activeIndex ? activePointBrush : pointBrush);
//        }
//        bool IsInProximity(PointF p, PointF loc) { return Math.Abs(p.X - loc.X) < captureSize && Math.Abs(p.Y - loc.Y) < captureSize; }
//        public void SetActiveControl(PointF p) // sets active control point
//        {
//            PointF[] tps=points.ToArray();
//            matrix.TransformPoints(tps); // moves control points to drawing locations
//            for (int i = 0; i < Count; i++)
//                if (IsInProximity(p, tps[i]))
//                {
//                    activeIndex = i;
//                    return;
//                }
//            activeIndex = - 1;
//        }
//        public void UnsetActiveControl() // sets active control point
//        {
//            activeIndex = -1;
//        }
//        public void MoveActiveControl(PointF p)
//        {
//            if (activeIndex < 0)
//                return;
//            Matrix invertMatrix = matrix.Clone();
//            invertMatrix.Invert();
//            PointF[] tps=new PointF[]{p};
//            invertMatrix.TransformPoints(tps);
//            SetNewValues(tps[0]);
//        }
//        protected virtual void SetNewValues(PointF p) { points[activeIndex] = p;  } // overridden if point motion is restricted or other points have to move too
//        public class Collection : ArrayList 
//		{
//            public Shape Last               { get { if (Count > 0) return (Shape)this[Count - 1]; return null;  } }
//            public Shape AddShape(int thickness)
//			{
//                return AddShape(thickness, Color.Black);
//			}
//            public Shape AddShape(int thickness, Color color)
//            {
//                Shape shape=new Spline(thickness, color);
//                if(shape!=null)
//                    Add(shape);
//                return shape;
//            }
//            public void AddPoint(PointF p)  { if (Count > 0) Last.Add(p); }
//			public void Undo()				
//			{
//				if(Count==0)
//					return;
//				if(Last.Count>0)
//					Last.Undo();
//				else
//					RemoveAt(Count-1);
//			}
//		}
//	}
//    public class Spline : Shape             
//    {
//        public Spline(int thickness_) : base(thickness_, 0.3f) { }
//        public Spline(int thickness_, Color color_) : base(thickness_, 0.3f, color_) { }
//        public override void Draw(Graphics g)
//        {
//            Pen pen = new Pen(color, thickness);
//            PointF[] cs=CubicSpline();
//            if(cs.Length>3)
//                g.DrawBeziers(pen, cs);
//        }
//        PointF[] CubicSpline()				
//		{
//            if (points.Count == 0)
//				return new PointF[0];
//            PointF[] ba = new PointF[points.Count * 3 - 2];
//            if (points.Count == 1)
//                ba[0] = (PointF)points[0];
//            else if (points.Count == 2)
//			{
//                ba[0] = ba[1] = (PointF)points[0];
//                ba[2] = ba[3] = (PointF)points[1];
//			}
//			else
//			{
//                for (int i = 1; i < points.Count - 1; i++)
//				{
//                    PointF pm = (PointF)points[i - 1];
//                    PointF p0 = (PointF)points[i];
//                    PointF pp = (PointF)points[i + 1];
//					if(i==1)
//						ba[0]=ba[1]=pm;
//                    if (i == points.Count - 2)
//						ba[ba.Length-1]=ba[ba.Length-2]=pp;
//					int bi=i*3;
//					ba[bi]=p0;
//					ba[bi-1]=ControlPoint(p0, pp, pm);
//					ba[bi+1]=ControlPoint(p0, pm, pp);
//				}
//			}
//			return ba;
//		}
//		PointF ControlPoint(PointF p0, PointF p1, PointF p2)
//		{
//			PointF pn=new PointF(p0.X+smoothness*(p0.X-p1.X), p0.Y+smoothness*(p0.Y-p1.Y));
//			float a;
//			if((a=V(p2, p1, pn))!=0)
//				a=V(p2, p0, pn)/a;
//			return new PointF(p0.X+a*(p2.X-p1.X), p0.Y+a*(p2.Y-p1.Y));
//		}
//		static float V(PointF p0, PointF p1, PointF p2)
//		{
//			return (p2.Y-p0.Y)*(p1.X-p0.X)-(p1.Y-p0.Y)*(p2.X-p0.X);
//		}
//    }
//}
