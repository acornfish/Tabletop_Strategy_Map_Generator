using System.Numerics;
using SkiaSharp;

namespace GenMap
{
    public static class Utils
    {
        public static bool Intersects(CRectangle a, CRectangle b)
        {
            return !(a.bottomright.X  <= b.topleft.X ||
                     a.topleft.X  >= b.bottomright.X ||
                     a.bottomright.Y <= b.topleft.Y ||
                     a.topleft.Y    >= b.bottomright.Y);
        }

        public static bool onSegment(SKPoint p, SKPoint q, SKPoint r) {
            return (
                q.X <= Math.Max(p.X, r.X) && 
                q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && 
                q.Y >= Math.Min(p.Y, r.Y));
        }   

        public static int orientation(SKPoint p, SKPoint q, SKPoint r) {
            float val = (q.Y - p.Y) * (r.X - q.X) -
                      (q.X - p.X) * (r.Y - q.Y);
    
            // collinear
            if (val == 0) return 0;
    
            // clock or counterclock wise
            // 1 for clockwise, 2 for counterclockwise
            return (val > 0) ? 1 : 2;
        }

        public static bool doIntersect(Linestring ls1, Linestring ls2) {
        
            // find the four orientations needed
            // for general and special cases
            int o1 = orientation(ls1.p1, ls1.p2, ls2.p1);
            int o2 = orientation(ls1.p1, ls1.p2, ls2.p2);
            int o3 = orientation(ls2.p1, ls2.p2, ls1.p1);
            int o4 = orientation(ls2.p1, ls2.p2, ls1.p2);
    
            // general case
            if (o1 != o2 && o3 != o4)
                return true;
    
            // special cases
            // p1, q1 and p2 are collinear and p2 lies on segment p1q1
            if (o1 == 0 &&
            onSegment(ls1.p1, ls2.p1, ls1.p2)) return true;
    
            // p1, q1 and q2 are collinear and q2 lies on segment p1q1
            if (o2 == 0 &&
            onSegment(ls1.p1, ls2.p2, ls1.p2)) return true;
    
            // p2, q2 and p1 are collinear and p1 lies on segment p2q2
            if (o3 == 0 &&
            onSegment(ls2.p1, ls1.p1, ls2.p2)) return true;
    
            // p2, q2 and q1 are collinear and q1 lies on segment p2q2 
            if (o4 == 0 &&
            onSegment(ls2.p1, ls1.p2, ls2.p2)) return true;
    
            return false;
        }

        public static Tuple<int, int, int> pointsToLine (SKPoint p, SKPoint q)
        {
            int a = 0,b = 0 ,c = 0;

            a = (int)(p.X - q.X);
            b = (int)(p.Y - q.Y);
            c = (int)(a*p.X - b*p.Y);
            return Tuple.Create(a, b, c);
        }

        public static bool isInCircumcircle (APoint a, APoint b, APoint c, APoint d)
        {
            Matrix4x4 matrix = new Matrix4x4(a.x, a.y, a.x*a.x + a.y*a.y, 1,
                                            b.x, b.y, b.x*b.x + b.y*b.y, 1,
                                            c.x, c.y, c.x*c.x + c.y*c.y, 1,
                                            d.x, d.y, d.x*d.x + d.y*d.y, 1);

            float det = matrix.GetDeterminant();
            if (a.cross(b, c) < 0)
                det = -det;

            return  det > 0;
        }

        public static bool InCircle(
            APoint a, APoint b,
            APoint c, APoint d)
        {
            double ax = a.x - d.x;
            double ay = a.y - d.y;
        
            double bx = b.x - d.x;
            double by = b.y - d.y;
        
            double cx = c.x - d.x;
            double cy = c.y - d.y;
        
            double det =
                (ax * ax + ay * ay) * (bx * cy - by * cx)
              - (bx * bx + by * by) * (ax * cy - ay * cx)
              + (cx * cx + cy * cy) * (ax * by - ay * bx);
        
            return det > 0;
        }
    }
}