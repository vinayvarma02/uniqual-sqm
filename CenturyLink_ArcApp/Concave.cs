using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using static CenturyLink_ArcApp.Arclib;
using static CenturyLink_ArcApp.Program;
using System.Runtime.InteropServices;
using System.Drawing;


namespace CenturyLink_ArcApp
{
    public class Edge
    {
        public IPoint A { get; set; }
        public IPoint B { get; set; }
    }
    public class AlphaShape
    {
        public List<Edge> BorderEdges { get; private set; }

        public AlphaShape(List<PointF> points, float alpha)
        {
            // 0. error checking, init
            if (points == null || points.Count < 2) { throw new ArgumentException("AlphaShape needs at least 2 points"); }
            BorderEdges = new List<Edge>();
            var alpha_2 = alpha * alpha;

            // 1. run through all pairs of points
            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    try
                    {

                        if (points[i] == points[j]) continue;// { throw new ArgumentException("AlphaShape needs pairwise distinct points"); } // alternatively, continue
                        var dist = Dist(points[i], points[j]);
                        if (dist > 2 * alpha) { continue; } // circle fits between points ==> p_i, p_j can't be alpha-exposed                    

                        float x1 = points[i].X, x2 = points[j].X, y1 = points[i].Y, y2 = points[j].Y; // for clarity & brevity

                        var mid = new PointF((x1 + x2) / 2, (y1 + y2) / 2);

                        // find two circles that contain p_i and p_j; note that center1 == center2 if dist == 2*alpha
                        var center1 = new PointF(
                            mid.X + (float)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (y1 - y2) / dist,
                            mid.Y + (float)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (x2 - x1) / dist
                            );

                        var center2 = new PointF(
                            mid.X - (float)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (y1 - y2) / dist,
                            mid.Y - (float)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (x2 - x1) / dist
                            );

                        // check if one of the circles is alpha-exposed, i.e. no other point lies in it
                        bool c1_empty = true, c2_empty = true;
                        for (int k = 0; k < points.Count && (c1_empty || c2_empty); k++)
                        {
                            if (points[k] == points[i] || points[k] == points[j]) { continue; }

                            if ((center1.X - points[k].X) * (center1.X - points[k].X) + (center1.Y - points[k].Y) * (center1.Y - points[k].Y) < alpha_2)
                            {
                                c1_empty = false;
                            }
                            if ((center2.X - points[k].X) * (center2.X - points[k].X) + (center2.Y - points[k].Y) * (center2.Y - points[k].Y) < alpha_2)
                            {
                                c2_empty = false;
                            }
                        }
                        if (c1_empty || c2_empty)
                        {
                            // yup!
                            IPoint ip1 = new PointClass();
                            ip1.X = points[i].X; ip1.Y = points[i].Y;
                            IPoint ip2 = new PointClass();
                            ip2.X = points[j].X; ip2.Y = points[j].Y;
                            BorderEdges.Add(new Edge() { A = ip1, B = ip2 });
                        }
                    }

                    catch (Exception ex)
                    {

                        throw ex;
                    }
                }

            }
        }

        // Euclidian distance between A and B
        public static float Dist(PointF A, PointF B)
        {
            return (float)Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }
    }
    public class Concave
    {
        public List<Edge> BorderEdges { get; private set; }

        public Concave(List<IPoint> points, float alpha)
        {
            try
            {
                // 0. error checking, init
                if (points == null || points.Count < 2) { throw new ArgumentException("AlphaShape needs at least 2 points"); }
                BorderEdges = new List<Edge>();
                var alpha_2 = alpha * alpha;

                // 1. run through all pairs of points
                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        if (points[i] == points[j]) { throw new ArgumentException("AlphaShape needs pairwise distinct points"); } // alternatively, continue
                        var dist = Dist(points[i], points[j]);
                        if (dist > 2 * alpha) { continue; } // circle fits between points ==> p_i, p_j can't be alpha-exposed                    

                        double x1 = points[i].X, x2 = points[j].X, y1 = points[i].Y, y2 = points[j].Y; // for clarity & brevity

                        IPoint mid = new PointClass();
                        mid.X = (x1 + x2) / 2;
                        mid.Y = (y1 + y2) / 2;

                        // find two circles that contain p_i and p_j; note that center1 == center2 if dist == 2*alpha
                        IPoint center1 = new PointClass();
                        IPoint center2 = new PointClass();
                        double yroot = (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (y1 - y2) / dist;
                        double xroot = (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (x2 - x1) / dist;

                        // if (Double.IsNaN(yroot)) yroot = 0.0;
                        // if (Double.IsNaN(xroot)) xroot = 0.0;

                        center1.X = mid.X + yroot;
                        center1.Y = mid.Y + xroot;

                        center2.X = mid.X - yroot;
                        center2.Y = mid.Y - xroot;


                        // check if one of the circles is alpha-exposed, i.e. no other point lies in it
                        bool c1_empty = true, c2_empty = true;
                        for (int k = 0; k < points.Count && (c1_empty || c2_empty); k++)
                        {
                            try
                            {
                                if (points[k] == points[i] || points[k] == points[j]) { continue; }

                                if ((center1.X - points[k].X) * (center1.X - points[k].X) + (center1.Y - points[k].Y) * (center1.Y - points[k].Y) < alpha_2)
                                {
                                    c1_empty = false;
                                }

                                if ((center2.X - points[k].X) * (center2.X - points[k].X) + (center2.Y - points[k].Y) * (center2.Y - points[k].Y) < alpha_2)
                                {
                                    c2_empty = false;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }

                        if (c1_empty || c2_empty)
                        {
                            // yup!
                            BorderEdges.Add(new Edge() { A = points[i], B = points[j] });
                        }
                    }
                }

            }
            catch (Exception EX)
            {
                throw EX;
            }
        }

        // Euclidian distance between A and B
        public static float Dist(IPoint A, IPoint B)
        {
            return (float)Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }
    }
}
