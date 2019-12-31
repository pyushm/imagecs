using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpGL.SceneGraph;
using SharpGL;
using System.Diagnostics;

namespace SharpGLWPFApplicationTest
{
    public struct EdgePoint
    {
        public readonly float x;    // x base position
        public readonly float y;    // y base position
        public readonly float dx;   // x normal
        public readonly float dy;   // y normal
        public EdgePoint(double x_, double y_, double dx_, double dy_) { x = (float)x_; y = (float)y_; dx = (float)dx_; dy = (float)dy_; }
    }
    public struct EdgePointSmoothing
    {
        EdgePoint edgePoint;
        float centerCoef;               // rounding center fraction: 0.5+dc/w
        public readonly float R;        // rounding radius
        public readonly float EdgeWidth;
        public readonly float dXnorm;
        public readonly float dYnorm;
        public readonly double α;
        public readonly float TopCenterX;
        public readonly float TopCenterY;
        public readonly float BottomCenterX;
        public readonly float BottomCenterY;
        public EdgePointSmoothing(EdgePoint ep, double height, double roundingFraction)
        {
            edgePoint = ep;
            centerCoef = (float)(0.5 + roundingFraction);
            double w2=edgePoint.dx * edgePoint.dx + edgePoint.dy * edgePoint.dy;
            EdgeWidth = (float)Math.Sqrt(w2);
            dXnorm = edgePoint.dx / EdgeWidth;
            dYnorm = edgePoint.dy / EdgeWidth;
            R = (float)(roundingFraction * EdgeWidth * (EdgeWidth + Math.Sqrt(w2 + height * height)) / height);
            α = Math.Atan2(height, EdgeWidth);
            TopCenterX = (float)(edgePoint.x - centerCoef * edgePoint.dx);
            TopCenterY = (float)(edgePoint.y - centerCoef * edgePoint.dy);
            BottomCenterX = (float)(edgePoint.x + centerCoef * edgePoint.dx);
            BottomCenterY = (float)(edgePoint.y + centerCoef * edgePoint.dy);
        }
    }
    public class TrapezoidSmoothing
    {
        float h;    // height of trapezoid
        float f;    // rounding fraction: length of extension / edge width: dc/w
        int extra;  // extra rounding points
        public int nSmooth { get { return 2 * extra + 4; } }
        public int Length { get { return edges.Count; } }
        List<EdgePointSmoothing[]> edges = new List<EdgePointSmoothing[]>();
        public TrapezoidSmoothing(float height, float roundingFraction, int extra_) { h = height; f = roundingFraction; extra = extra_; }
        public void AddEdge(EdgePoint[] edgeinput)
        {
            EdgePointSmoothing[] se = new EdgePointSmoothing[edgeinput.Length];
            for (int i = 0; i < edgeinput.Length; i++)
                se[i] = new EdgePointSmoothing(edgeinput[i], h, f);
            edges.Add(se);
        }
        public float[][][] CreateInterpolationContour(EdgePointSmoothing[] edge, int ind) //[vertex/normal][i-contour][xyz]
        {
            if (ind < 0 || ind >= nSmooth)
                return null;
            bool top = ind > extra + 1;
            float[][][] edgeContour = new float[2][][];    // verteces and normals
            edgeContour[0] = new float[edge.Length][];
            edgeContour[1] = new float[edge.Length][];
            for (int i = 0; i < edge.Length; i++)
            {
                double dα = edge[i].α / (extra + 1);
                int αind = top ? nSmooth - 1 - ind : ind;
                float c = (float)Math.Cos(dα * αind);
                float s = (float)Math.Sin(dα * αind);
                edgeContour[1][i] = new float[3];   // normals
                edgeContour[1][i][0] = s * edge[i].dXnorm;
                edgeContour[1][i][1] = s * edge[i].dYnorm;
                edgeContour[1][i][2] = c;
                edgeContour[0][i] = new float[3];   // vertexes
                if (top)
                {
                    edgeContour[0][i][0] = edge[i].TopCenterX + edge[i].R * edgeContour[1][i][0];
                    edgeContour[0][i][1] = edge[i].TopCenterY + edge[i].R * edgeContour[1][i][1];
                    edgeContour[0][i][2] = h - edge[i].R * (1 - edgeContour[1][i][2]);
                }
                else
                {
                    edgeContour[0][i][0] = edge[i].BottomCenterX - edge[i].R * edgeContour[1][i][0];
                    edgeContour[0][i][1] = edge[i].BottomCenterY - edge[i].R * edgeContour[1][i][1];
                    edgeContour[0][i][2] = edge[i].R * (1 - edgeContour[1][i][2]);
                }
            }
            return edgeContour;
        }
        public float[][] TopTriangleFan(int shape) { return TriangleFan(CreateInterpolationContour(edges[shape], nSmooth-1)[0], new float[] { 0, 0, h }, new float[] { 0, 0, 1 }); }
        public float[][] BottomTriangleFan(int shape) { return TriangleFan(CreateInterpolationContour(edges[shape], 0)[0], new float[] { 0, 0, 0 }, new float[] { 0, 0, -1 }); }
        float[][] TriangleFan(float[][] v, float[] c, float[] n)
        {
            List<float> q = new List<float>(v.Length + 2);
            List<float> l = new List<float>(v.Length + 2);
            q.AddRange(c);
            l.AddRange(n);
            for (int i = 0; i < v.Length; i++)
            {
                q.AddRange(v[i]);
                l.AddRange(n);
            }
            q.AddRange(v[0]);
            l.AddRange(n);
            float[][] res = new float[2][];
            res[0] = q.ToArray();
            res[1] = l.ToArray();
            return res;
        }
        public float[][] TopQuads()
        {
            float[][] res = new float[2][];
            res[0] = Quads(CreateInterpolationContour(edges[0], nSmooth - 1)[0], CreateInterpolationContour(edges[1], nSmooth - 1)[0]);
            List<float> l = new List<float>(res[0].Length);
            for (int i = 0; i < res[0].Length/3; i++)
                l.AddRange(new float[] { 0, 0, 1 });
            res[1] = l.ToArray();
            return res;
        }
        public float[][] BottomQuads()
        {
            float[][] res = new float[2][];
            res[0] = Quads(CreateInterpolationContour(edges[0], 0)[0], CreateInterpolationContour(edges[1], 0)[0]);
            List<float> l = new List<float>(res[0].Length);
            for (int i = 0; i < res[0].Length / 3; i++)
                l.AddRange(new float[] { 0, 0, -1 });
            res[1] = l.ToArray();
            return res;
        }
        public float[][][] EdgeQuadsArray(int shapeInd)
        {
            float[][][] eqa = new float[nSmooth-1][][];
            float[][][] prev = CreateInterpolationContour(edges[shapeInd], 0);
            for(int i=1;i<nSmooth;i++)
            {
                eqa[i - 1] = new float[2][];
                float[][][] curr = CreateInterpolationContour(edges[shapeInd], i);
                eqa[i - 1][0] = Quads(prev[0], curr[0]);
                eqa[i - 1][1] = Quads(prev[1], curr[1]);
                prev = curr;
            }
            return eqa;
        }
        float[] Quads(float[][] v1, float[][] v2)
        {
            List<float> q = new List<float>(2 * v1.Length + 2);
            for (int i = 0; i < v1.Length; i++)
            {
                q.AddRange(v1[i]);
                q.AddRange(v2[i]);
            }
            q.AddRange(v1[0]);
            q.AddRange(v2[0]);
            return q.ToArray();
        }
    }
    public partial class MainWindow : Window
    {
        OpenGL gl;
        TrapezoidSmoothing trapezoid = new TrapezoidSmoothing(1.5f, 0.4f, 3);
        public MainWindow()
        {
            InitializeComponent();
            gl = openGLControl.OpenGL;
            trapezoid.AddEdge(CreateShape(2.5, 150, 4, true));
            trapezoid.AddEdge(CreateShape(1, 150, 4, false));
        }
        public EdgePoint[] CreateShape(double r, int n, int m, bool mat)// radius, countour length, shape symmery, material or hole
        {
            List<EdgePoint> edge = new List<EdgePoint>();
            double w = 0.6; 
            double fr = 0.1;    // fraction of m-harmonic radius
            double fn = Math.PI * 2 / n;
            double fm = m * fn;
            Random ran = new Random();
            for (int i = 0; i < n; i++)
            {
                double fin = i * fn;
                double fim = i * fm;
                double c = Math.Cos(fin);
                double s = Math.Sin(fin);
                double cm = Math.Cos(fim);
                double sm = Math.Sin(fim);
                double lr = r * (1 + fr * cm + 0.05 * ran.NextDouble());
                double x = lr * c;
                double y = lr * s;
                double dx = r*(fn*s-fr*fm*sm*c);
                double dy = r*(fn*c+fr*fm*sm*s);
                double lw = w + 0.3 * w * cm;
                double d = Math.Sqrt(dx * dx + dy * dy)/lw;
                dx /= mat ? d : -d;
                dy /= mat ? d : -d;
                edge.Add(new EdgePoint(x, y, dy, dx));
                //Debug.WriteLine((x + dy / 2).ToString("f3") + '\t' + (y + dx / 2).ToString("f3") + '\t' + (x - dy / 2).ToString("f3") + '\t' + (y - dx / 2).ToString("f3"));
            }
            return edge.ToArray();
        }
        private void openGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            setLight();
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            gl.ShadeModel(OpenGL.GL_SMOOTH);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);//  Clear the color and depth buffer.
            //gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity(); //  Load the identity matrix.
            gl.Rotate(rotation, 0.0f, 0.4f, 1.0f);//  Rotate around the Y axis.
            gl.Color(0.5f, 0.5f, 0.6f, 1f);
            gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
            gl.EnableClientState(OpenGL.GL_NORMAL_ARRAY);
            for (int i = 0; i < trapezoid.Length; i++)
            {
                float[][][] edgeQuads = trapezoid.EdgeQuadsArray(i);
                foreach (var quads in edgeQuads)
                    DrawSurface(OpenGL.GL_QUAD_STRIP, quads[0], quads[1]);
            }
            if (trapezoid.Length == 1)
            {
                float[][] flat = trapezoid.TopTriangleFan(0);
                DrawSurface(OpenGL.GL_TRIANGLE_FAN, flat[0], flat[1]);
                flat = trapezoid.BottomTriangleFan(0);
                DrawSurface(OpenGL.GL_TRIANGLE_FAN, flat[0], flat[1]);
            }
            else
            {
                float[][] flat = trapezoid.TopQuads();
                DrawSurface(OpenGL.GL_QUAD_STRIP, flat[0], flat[1]);
                flat = trapezoid.BottomQuads();
                DrawSurface(OpenGL.GL_QUAD_STRIP, flat[0], flat[1]);
            }
            gl.DisableClientState(OpenGL.GL_NORMAL_ARRAY);
            gl.DisableClientState(OpenGL.GL_VERTEX_ARRAY);
            rotation += 0.5f;//  Nudge the rotation.
        }
        void DrawSurface(uint type, float[] vertices, float[] normals)
        {
            Debug.Assert(vertices.Length == normals.Length);
            Debug.Assert(vertices.Length / 3 * 3 == vertices.Length);
            gl.VertexPointer(3, 0, vertices);
            gl.NormalPointer(OpenGL.GL_FLOAT, 0, normals);
            gl.DrawArrays(type, 0, normals.Length / 3);
        }
        void setLight()
        {
            float[] light_position0 = { 1, 1, 1, 0 };
            float[] light_ambient0 = { 0.2f, 0.2f, 0.2f, 0.2f };
            float[] light_diffuse0 = { 1.0f, 0.9f, 0.8f, 1.0f };
            //float[] mat_ambient = { 1.0f, .0f, 0.0f, 1.0f };
            float[] mat_diffuse0 = { 1f, 1f, 1.0f, 1.0f };
            float[] light_position1 = { -1, -0.5f, 1, 0 };
            float[] light_ambient1 = { 0.2f, 0.2f, 0.2f, 0.2f };
            float[] light_diffuse1 = { 0.8f, 0.9f, 0.99f, 1.0f };
            //float[] mat_ambient = { 1.0f, .0f, 0.0f, 1.0f };
            float[] mat_diffuse1 = { 1f, 1f, 1.0f, 1.0f };
            //float[] mat_back = { 0, 0, 0, 0 };
            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_LIGHT0);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, light_position0);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, light_ambient0);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, light_diffuse0); 
            gl.Enable(OpenGL.GL_LIGHT1);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light_position1);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, light_ambient1);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, light_diffuse1);
            //gl.Material(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT, mat_ambient);
            //gl.Material(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE, mat_diffuse);
            //gl.Material(OpenGL.GL_BACK, OpenGL.GL_DIFFUSE, mat_back);
        }
        private void openGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {  //  TODO: Initialise OpenGL here.
            gl.ClearColor(0, 0, 0, 0);//  Set the clear color.
        }
        private void openGLControl_Resized(object sender, OpenGLEventArgs args)
        {  //  TODO: Set the projection matrix here.          
            gl.MatrixMode(OpenGL.GL_PROJECTION);//  Set the projection matrix. 
            gl.LoadIdentity();//  Load the identity.
            gl.Perspective(60.0f, (double)Width / (double)Height, 0.01, 100.0);//  Create a perspective transformation.
            gl.LookAt(3, 3, 5, 0, 0, 0, 0, 1, 1);// look from 012 to 345 up? 678
            gl.MatrixMode(OpenGL.GL_MODELVIEW);//  Set the modelview matrix.
        }
        private float rotation = 0.0f;
    }
}
