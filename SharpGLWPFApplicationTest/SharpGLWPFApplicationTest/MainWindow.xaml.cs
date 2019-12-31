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
    enum TurnFactor
    {
        OnLine = 0,
        One,
        Two,
    }
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
        public float X { get { return edgePoint.y; } }
        public float Y { get { return edgePoint.x; } }
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
    struct FPoint
    {
        public float x;
        public float y;
        public FPoint(float xi, float yi) { x = xi; y = yi; }
    }
    public class TrapezoidEdgeRounding
    {
        float h;    // height of trapezoid
        float f;    // rounding fraction: length of extension / edge width: dc/w
        int rounding; // extra corner rounding points
        float xMin; // floor min X
        float xMax; // floor max X
        float yMin; // floor min Y
        float yMax; // floor max Y
        public float[][][][] EdgeQuadStrips { get; private set; }
        public float[][] TopTriangles { get; private set; }
        public float[][] TopNormals { get; private set; }
        public int nRounding { get { return 2 * rounding + 4; } } // number of points rounding trapezoid edge
        public int EdgeCount { get { return edges.Count; } }
        List<EdgePointSmoothing[]> edges = new List<EdgePointSmoothing[]>();
        public TrapezoidEdgeRounding(float xn, float xx, float yn, float yx, float height, float roundingFraction, int cornerRounding)
        {
            xMin = xn;
            xMax = xx;
            yMin = yn;
            yMax = yx;
            h = height; 
            f = roundingFraction;
            rounding = cornerRounding; 
        }
        public void PrepareDrawing()
        {
            EdgeQuadStrips = new float[EdgeCount][][][];
            //float[][][] roundingContour = new float[EdgeCount][][]; // vertex[edgeInd][contourInd][xyz]
            //float[][][] roundingNormals = new float[EdgeCount][][]; // normal[edgeInd][contourInd][xyz]
            for (int e = 0; e < EdgeCount; e++)
            {
                //roundingContour[e] = new float[nRounding][];
                //roundingNormals[e] = new float[nRounding][];
                //for (int i = 0; i < nRounding; i++)
                //{
                //    float[][][] ret = CreateRoundingContour(edges[e], i);
                //    roundingContour[e][i] = ret[0];
                //}
                EdgeQuadStrips[e] = CreateEdgeQuadStrip(e);

                //EdgeQuadStrips[e] = new float[nRounding - 1][][];
                //float[][][] prev = roundingContour[0];
                //for (int i = 1; i < nRounding; i++)
                //{
                //    EdgeQuadStrips[e][i - 1] = new float[2][];
                //    float[][][] curr = roundingContour[i];
                //    EdgeQuadStrips[e][i - 1][0] = QuadStrip(roundingContour[i - 1][0], roundingContour[i][0]);
                //    EdgeQuadStrips[e][i - 1][1] = QuadStrip(roundingContour[i - 1][1], roundingContour[i][1]);
                //    prev = curr;
                //}

            }
            FPoint[][] topContours = new FPoint[EdgeCount][];
            for (int i = 0; i < EdgeCount; i++)
            {
                topContours[i] = new FPoint[edges[i].Length];
                float[][] cont = CreateRoundingContour(edges[i], nRounding - 1)[0];
                for (int j = 0; j < cont.Length; j++)
                {
                    topContours[i][j] = new FPoint(cont[j][0], cont[j][1]);
                }
            }
            FPoint[][] topPolyArray = makePolygonesWithHoles(topContours);
            TopTriangles = new float[topPolyArray.Length][];
            TopNormals = new float[TopTriangles.Length][];
            for (int i = 0; i < TopTriangles.Length; i++)
            {
                TopTriangles[i] = Triangulate(new List<FPoint>(topPolyArray[i]), h);
                List<float> normals = new List<float>(TopTriangles[i].Length);
                for (int j = 0; j < TopTriangles[i].Length / 3; j++)
                    normals.AddRange(new float[] { 0, 0, 1 });
                TopNormals[i] = normals.ToArray();
            }
        }
        public void AddEdge(EdgePoint[] edgeinput)
        {
            EdgePointSmoothing[] se = new EdgePointSmoothing[edgeinput.Length];
            for (int i = 0; i < edgeinput.Length; i++)
                se[i] = new EdgePointSmoothing(edgeinput[i], h, f);
            edges.Add(se);
        }
#region EdgeQuads
        float[][][] CreateEdgeQuadStrip(int shapeInd)
        {
            float[][][] eqa = new float[nRounding - 1][][];
            float[][][] prev = CreateRoundingContour(edges[shapeInd], 0);
            for (int i = 1; i < nRounding; i++)
            {
                eqa[i - 1] = new float[2][];
                float[][][] curr = CreateRoundingContour(edges[shapeInd], i);
                eqa[i - 1][0] = QuadStrip(prev[0], curr[0]);
                eqa[i - 1][1] = QuadStrip(prev[1], curr[1]);
                prev = curr;
            }
            return eqa;
        }
        float[] QuadStrip(float[][] v1, float[][] v2)
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
        public float[][][] CreateRoundingContour(EdgePointSmoothing[] edge, int ind) //return: [vertex/normal][i-contour][xyz]
        {
            if (ind < 0 || ind >= nRounding)
                return null;
            bool top = ind > rounding + 1;
            float[][][] edgeContour = new float[2][][];    // verteces and normals
            edgeContour[0] = new float[edge.Length][];
            edgeContour[1] = new float[edge.Length][];
            float prevX = edge[edge.Length - 1].X;
            float prevY = edge[edge.Length - 1].Y;
            float currX = edge[0].X;
            float currY = edge[0].Y;
            for (int i = 0; i < edge.Length; i++)
            {
                int j = i < edge.Length - 1 ? i + 1 : 0;
                float nextX = edge[j].X;
                float nextY = edge[j].Y;
                float dx = nextX - prevX;
                float dy = nextY - prevY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                dx /= d;
                dy /= d;
                prevX = currX;
                prevY = currY;
                currX = nextX;
                currY = nextY;
                double dα = edge[i].α / (rounding + 1);
                int αind = top ? nRounding - 1 - ind : ind;
                float c = (float)Math.Cos(dα * αind);
                float s = (float)Math.Sin(dα * αind);
                edgeContour[1][i] = new float[3];   // normals
                edgeContour[1][i][0] = s * dy;
                edgeContour[1][i][1] = -s * dx;
                edgeContour[1][i][2] = c;
                edgeContour[0][i] = new float[3];   // vertexes
                if (top)
                {
                    edgeContour[0][i][0] = edge[i].TopCenterX + edge[i].R * s * edge[i].dXnorm;// edgeContour[1][i][0];
                    edgeContour[0][i][1] = edge[i].TopCenterY + edge[i].R * s * edge[i].dYnorm;//edgeContour[1][i][1];
                    edgeContour[0][i][2] = h - edge[i].R * (1 - edgeContour[1][i][2]);
                }
                else
                {
                    edgeContour[0][i][0] = edge[i].BottomCenterX - edge[i].R * s * edge[i].dXnorm;// edgeContour[1][i][0];
                    edgeContour[0][i][1] = edge[i].BottomCenterY - edge[i].R * s * edge[i].dYnorm;// edgeContour[1][i][1];
                    edgeContour[0][i][2] = edge[i].R * (1 - edgeContour[1][i][2]);
                }
            }
            for (int i = 0; i < edge.Length; i++)
            {

            }
            return edgeContour;
        }
#endregion
#region Triangulation
        float[] Triangulate(List<FPoint> topPoly, float height)
        {
            List<float> vertexes = new List<float>();
            TurnFactor turnInternalFactor = calcInternalTurnFactor(topPoly);
            if (turnInternalFactor == TurnFactor.OnLine)
                Debug.Assert(false);
            TurnFactor turnFactor = TurnFactor.OnLine;
            FPoint[] triangle = new FPoint[3];
            bool isEar;
            int before;
            while (topPoly.Count > 3)
            {
                before = topPoly.Count;
                for (int i = topPoly.Count - 1; i >= 0; i--)
                {
                    float x = topPoly[i].x;
                    float y = topPoly[i].y;
                    int pred_i = i > 0 ? i - 1 : topPoly.Count - 1;
                    int next_i = (i < topPoly.Count - 1) ? i + 1 : 0;
                    triangle[0] = topPoly[pred_i];
                    triangle[1] = topPoly[i];
                    triangle[2] = topPoly[next_i];
                    turnFactor = calcTurnFactor(triangle[0], triangle[1], triangle[2]);
                    if (turnFactor == TurnFactor.OnLine)
                    {
                        // Треугольник выродился в отрезок.
                        topPoly.RemoveAt(i);
                        break;
                    }
                    if (turnFactor != turnInternalFactor)
                        continue;
                    isEar = true;
                    for (int j = 0; j < topPoly.Count; j++)
                    {
                        if (j == pred_i || j == i || j == next_i)
                            continue;
                        bool pb = PointBelong(triangle, topPoly[j]);
                        if (!pb)
                            continue;
                        isEar = false;
                        break;
                    }
                    if (!isEar)
                        continue;
                    cutEar(topPoly, vertexes, i, height);// Нашли ухо. Резать!
                    topPoly.RemoveAt(i);
                    break;
                }
                if (topPoly.Count == before)
                {
                    // Crutch.
                    if (popEqualPoints(topPoly))
                        continue;
                    break;
                }
            }
            if (topPoly.Count == 3)
                triangulateLastTriangle(topPoly, vertexes, height);
            return vertexes.ToArray();
        }
        bool popEqualPoints(List<FPoint> poly)
        {
            bool ptIsPoped = false;
            while (qFuzzyCompare(poly[0].x, poly[poly.Count - 1].x) &&
                qFuzzyCompare(poly[0].y, poly[poly.Count - 1].y))
            {
                poly.RemoveAt(poly.Count - 1);
                ptIsPoped = true;
            }
            for (int i = 0; i < poly.Count - 1; i++)
            {
                if (qFuzzyCompare(poly[i].x, poly[i + 1].x) &&
                    qFuzzyCompare(poly[i].y, poly[i + 1].y))
                {
                    poly.RemoveAt(i + 1);
                    ptIsPoped = true;
                    i--;
                }
            }
            return ptIsPoped;
        }
        static bool qFuzzyCompare(float p1, float p2)
        {
            return (Math.Abs(p1 - p2) * 100000.0f <= Math.Min(Math.Abs(p1), Math.Abs(p2)));
        }
        void cutEar(List<FPoint> poly, List<float> vertexes, int index, float height)
        {
            int pred_i = index > 0 ? index - 1 : poly.Count - 1;
            int next_i = index < poly.Count - 1 ? index + 1 : 0;

            vertexes.Add(poly[pred_i].x);
            vertexes.Add(poly[pred_i].y);
            vertexes.Add(height);

            vertexes.Add(poly[index].x);
            vertexes.Add(poly[index].y);
            vertexes.Add(height);

            vertexes.Add(poly[next_i].x);
            vertexes.Add(poly[next_i].y);
            vertexes.Add(height);
        }
        void triangulateLastTriangle(List<FPoint> poly, List<float> vertexes, float height)
        {
            vertexes.Add(poly[0].x);
            vertexes.Add(poly[0].y);
            vertexes.Add(height);

            vertexes.Add(poly[1].x);
            vertexes.Add(poly[1].y);
            vertexes.Add(height);

            vertexes.Add(poly[2].x);
            vertexes.Add(poly[2].y);
            vertexes.Add(height);
            poly.Clear();
        }
        TurnFactor calcTurnFactor(FPoint a, FPoint b, FPoint c)
        {
            float factor = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (factor < 0.0f)
                return TurnFactor.One;
            else if (factor > 0.0f)
                return TurnFactor.Two;
            else
                return TurnFactor.OnLine;
        }

        TurnFactor calcInternalTurnFactor(List<FPoint> poly)
        {
            TurnFactor turnFactor = TurnFactor.OnLine;
            FPoint[] triangle = new FPoint[3];
            bool isEar;
            // Ищем первое ухо.
            for (int i = poly.Count - 1; i >= 0; i--)
            {
                int pred_i = i > 0 ? i - 1 : poly.Count - 1;
                int next_i = i < poly.Count - 1 ? i + 1 : 0;
                triangle[0] = poly[pred_i];
                triangle[1] = poly[i];
                triangle[2] = poly[next_i];
                isEar = true;
                for (int j = 0; j < poly.Count; j++)
                {
                    if (j == pred_i || j == i || j == next_i)
                        continue;
                    if (!PointBelong(triangle, poly[j]))
                        continue;
                    isEar = false;
                    break;
                }
                if (!isEar)
                    continue;
                // Ни одна точка многоугольника не входит в triangle.
                turnFactor = calcTurnFactor(triangle[0], triangle[1], triangle[2]);
                FPoint midPt = new FPoint(((triangle[0].x + triangle[2].x) / 2.0f + triangle[1].x) / 2.0f, ((triangle[0].y + triangle[2].y) / 2.0f + triangle[1].y) / 2.0f);
                bool belongingMiddlePointAC = PointBelong(poly.ToArray(), midPt);
                if (belongingMiddlePointAC)
                    return turnFactor;

                if (turnFactor == TurnFactor.One)
                    return TurnFactor.Two;
                else if (turnFactor == TurnFactor.Two)
                    return TurnFactor.One;
                else
                    return TurnFactor.OnLine;
            }
            // Impossible.
            Debug.Assert(false);
            return TurnFactor.OnLine;
        }
        FPoint[][] makePolygonesWithHoles(FPoint[][] polygons)
        {
            List<List<FPoint>> polygonsWithHoles=new List<List<FPoint>>();
            int[] parent =new int[polygons.Length];
            for (int i = 0; i < polygons.Length; i++)
            {
                parent[i]=-1;
                for (int j = 0; j < polygons.Length; j++)
                {
                    if (i == j)
                        continue;
                    if (isPolygonInside(polygons[j], polygons[i]))
                    {
                        parent[i] = j;// Polygon i - inside polygon j.
                        break;
                    }
                }
                if (parent[i] >=0 )
                    continue;
            }
            int[] ind = new int[polygons.Length];   // ind[i] is an index of outside contour i in polygonsWithHoles
            for (int i = 0; i < polygons.Length; i++)
            {   // creating ouside polygons from contour not having parents
                ind[i] = -1;
                if (parent[i] < 0)
                {
                    ind[i] = polygonsWithHoles.Count;
                    List<FPoint> pl = new List<FPoint>(polygons[i]);
                    polygonsWithHoles.Add(pl);
                }
            }
            for (int i = 0; i < polygons.Length; i++)
            {
                if (parent[i] >= 0)
                    polygonsWithHoles[ind[parent[i]]].AddRange(polygons[i]);
            }
            FPoint[][] ret=new FPoint[polygonsWithHoles.Count][];
            for (int i = 0; i < polygonsWithHoles.Count; i++)
                ret[i] = polygonsWithHoles[i].ToArray();
            return ret;
        }
        bool isPolygonInside(FPoint[] checkExternal, FPoint[] checkInternal) 
        {
            foreach (FPoint pt in checkInternal)
                if(!PointBelong(checkExternal, pt))
                    return false;
            return true;
        }
        bool PointBelong(FPoint[] poly, FPoint pt) 
        {
            bool oddity = false;
            int j = poly.Length - 1;
            int back = 0;
            int front = 0;
            for (int i = 0; i < poly.Length; i++)
            {
                if (poly[i].y == pt.y && poly[i].x > pt.x)
                {
                    bool isReady = actionOnRayPassesThroughVertexOrSide(poly, pt, ref oddity, ref back, ref front, ref i);
                    if (isReady)
                        return oddity;
                }
                else
                {
                    if (((poly[i].y < poly[j].y) && (poly[i].y <= pt.y) && (pt.y <= poly[j].y) &&
                        ((poly[j].y - poly[i].y) * (pt.x - poly[i].x) < (poly[j].x - poly[i].x) * (pt.y - poly[i].y)) ) ||
                        ((poly[i].y > poly[j].y) && (poly[j].y <= pt.y) && (pt.y <= poly[i].y) &&
                        ((poly[j].y - poly[i].y) * (pt.x - poly[i].x) > (poly[j].x - poly[i].x) * (pt.y - poly[i].y))) )
                    {
                        oddity = !oddity;
                    }
                }
                j = i;
            }
            return oddity;
        }
        bool actionOnRayPassesThroughVertexOrSide(FPoint[] poly, FPoint pt, ref bool oddity, ref int back, ref int front, ref int i)
        {
            int back_index = (i == 0) ? poly.Length - 1 : i - 1;
            int front_index = (i == poly.Length - 1) ? 0 : i + 1;
            lookForBackIndex(poly, pt, ref back, ref back_index, ref i);
            lookForFrontIndex(poly, pt, ref front, ref front_index, ref i);
            if ((back == -1 && front == 1) || (back == 1 && front == -1))
            {
                oddity = !oddity;
                if (front_index <= i)
                    return true;
                i = front_index;
            }
            return false;
        }
        void lookForBackIndex(FPoint[] poly, FPoint pt, ref int back, ref int back_index, ref int i)
        {
            while (back_index != i)
            {
                if (poly[back_index].y < pt.y)
                {
                    back = -1;
                    break;
                }
                else if (poly[back_index].y > pt.y)
                {
                    back = 1;
                    break;
                }
                else
                {
                    back_index = (back_index == 0) ? poly.Length - 1 : back_index - 1;
                    continue;
                }
            }
        }        
        void lookForFrontIndex(FPoint[] poly, FPoint pt, ref int front, ref int front_index, ref int i)
        {
            while (front_index != i)
            {
                if (poly[front_index].y < pt.y)
                {
                    front = -1;
                    break;
                }
                else if (poly[front_index].y > pt.y)
                {
                    front = 1;
                    break;
                }
                else
                {
                    front_index = (front_index == poly.Length - 1) ? 0 : front_index + 1;
                    continue;
                }
            }
        }
#endregion
        public float[][] TopTriangleFan(int shape) { return TriangleFan(CreateRoundingContour(edges[shape], nRounding - 1)[0], new float[] { 0, 0, h }, new float[] { 0, 0, 1 }); }
        public float[][] BottomTriangleFan(int shape) { return TriangleFan(CreateRoundingContour(edges[shape], 0)[0], new float[] { 0, 0, 0 }, new float[] { 0, 0, -1 }); }
        //float[][] Polygon(float[][] v, float[] c, float[] n)
        //{
        //    List<float> q = new List<float>(v.Length);
        //    List<float> l = new List<float>(v.Length);
        //    for (int i = 0; i < v.Length; i++)
        //    {
        //        q.AddRange(v[i]);
        //        l.AddRange(n);
        //    }
        //    float[][] res = new float[2][];
        //    res[0] = q.ToArray();
        //    res[1] = l.ToArray();
        //    return res;
        //}
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
        public float[][] TopQuadStrip()
        {
            float[][] res = new float[2][];
            res[0] = QuadStrip(CreateRoundingContour(edges[0], nRounding - 1)[0], CreateRoundingContour(edges[1], nRounding - 1)[0]);
            List<float> l = new List<float>(res[0].Length);
            for (int i = 0; i < res[0].Length / 3; i++)
                l.AddRange(new float[] { 0, 0, 1 });
            res[1] = l.ToArray();
            return res;
        }
        public float[][] BottomQuads()
        {
            float[][] res = new float[2][];
            res[0] = new float[] { xMin, yMin, 0, xMax, yMin, 0, xMax, yMax, 0, xMin, yMax, 0 };
            List<float> l = new List<float>(res[0].Length);
            for (int i = 0; i < res[0].Length / 3; i++)
                l.AddRange(new float[] { 0, 0, 1 });
            res[1] = l.ToArray();
            return res;
        }
    }
    public partial class MainWindow : Window
    {
        OpenGL gl;
        TrapezoidEdgeRounding trapezoid;
        public MainWindow()
        {
            InitializeComponent();
            gl = openGLControl.OpenGL;
            //trapezoid = new TrapezoidEdgeRounding(-2, 2, -5, 5, 1.5f, 0.4f, 7);
            trapezoid = new TrapezoidEdgeRounding(-4, 4, -4, 4, 1.5f, 0.4f, 7);
            trapezoid.AddEdge(CreateShape(2.5, 150, 4, true));
            trapezoid.AddEdge(CreateShape(1, 150, 4, false));
            trapezoid.PrepareDrawing();
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
                double lr = r * (1 - fr * cm + 0.1 * ran.NextDouble());
                double x = lr * c;
                double y = lr * s;
                double dx = lr * s - r * fr * m * sm * c;
                double dy = lr * c + r * fr * m * sm * s;
                double lw = w * (1 + 0.2 * sm);
                double d = Math.Sqrt(dx * dx + dy * dy) / lw;
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
            for (int i = 0; i < trapezoid.EdgeCount; i++)
            {
                float[][][] edgeQuads = trapezoid.EdgeQuadStrips[i];
                foreach (var quads in edgeQuads)
                    DrawSurface(OpenGL.GL_QUAD_STRIP, quads[0], quads[1]);
            }
            //for (int i = 0; i < trapezoid.TopTriangles.Length; i++)
            //{
            //    DrawSurface(OpenGL.GL_TRIANGLES, trapezoid.TopTriangles[i], trapezoid.TopNormals[i]);
            //}
            float[][] flat;
            if (trapezoid.EdgeCount == 1)
            {
                flat = trapezoid.TopTriangleFan(0);
                DrawSurface(OpenGL.GL_TRIANGLE_FAN, flat[0], flat[1]);
            }
            else
            {
                flat = trapezoid.TopQuadStrip();
                DrawSurface(OpenGL.GL_QUAD_STRIP, flat[0], flat[1]);
            }
            gl.Color(0.55f, 0.5f, 0.55f, 1f);
            flat = trapezoid.BottomQuads();
            DrawSurface(OpenGL.GL_QUADS, flat[0], flat[1]);
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
            gl.LookAt(4, 4, 7, 0, 0, 0, 0, 1, 1);// look from 012 to 345 up? 678
            gl.MatrixMode(OpenGL.GL_MODELVIEW);//  Set the modelview matrix.
        }
        private float rotation = 0.0f;
    }
}
