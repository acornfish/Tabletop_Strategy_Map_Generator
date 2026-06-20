using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SkiaSharp;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 
namespace GenMap
{
    public struct Polygon
    {
        public Polygon (CRectangle rect)
        {
            points = new SKPoint[4];
            points[1] = rect.topleft;
            points[2] = new SKPoint(rect.topleft.X, rect.bottomright.Y);
            points[3] = rect.bottomright;
            points[0] = new SKPoint(rect.bottomright.X, rect.topleft.Y);
        }

        public SKPoint[] points;
    }   
    
    public struct CRectangle 
    {
        public CRectangle(int x, int y, int xe, int ye)
        {
            this.topleft = new SKPoint(x,y);
            this.bottomright = new SKPoint(xe,ye);
        }       
        
        public CRectangle(SKPoint center, int w, int h)
        {
            this.topleft = new SKPoint(center.X-w/2,center.Y+h/2);
            this.bottomright = new SKPoint(center.X+w/2, center.Y-h/2);
        }
        public SKPoint topleft, bottomright;
    }

    public struct Linestring
    {
        public Linestring(SKPoint p1, SKPoint p2){this.p1 = p1; this.p2 = p2;}

        public SKPoint p1;
        public SKPoint p2;

        public override int GetHashCode()
        {
            int i1 = (int)p1.X + (int)p1.Y;
            int l = (int)Math.Log10(i1)+1;
            return + ((int)p2.X + (int)p2.X)*(int)Math.Pow(10,l) + i1;
        }
    }

    public struct LineMap
    {
        public LineMap(Linestring[] lines)
        {
            this.lines = lines;
        }
        public Linestring[] lines;
    }

    public struct APoint : IEquatable<APoint>
    {
        public float x, y;

        public APoint()
        { }
        public APoint(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static APoint operator -(APoint p1, APoint p2) => new APoint(p1.x - p2.x, p1.y - p2.y);
        public static APoint operator +(APoint p1, APoint p2) => new APoint(p1.x + p2.x, p1.y + p2.y);
        public static APoint operator /(APoint p1, float x) => new APoint(p1.x/x, p1.y/x);
        public static APoint operator *(APoint p1, float x) => new APoint(p1.x*x, p1.y*x);
        // Tolerance for floating-point comparisons
        private const float EPS = 1e-6f;
        public static bool operator ==(APoint p1, APoint p2) => Math.Abs(p1.x - p2.x) <= EPS && Math.Abs(p1.y - p2.y) <= EPS;
        public static bool operator !=(APoint p1, APoint p2) => !(p1 == p2);
        
        //Cross product of this and something else with (0,0) origin
        public float cross(APoint p)
        {
            return x*p.y-y*p.x;
        } 

        // Take this as origin and cross product
        public float cross(APoint p1, APoint p2)
        {
            return (p1-this).cross((p2-this));
        }

        //Dot product of this and something else with (0,0) origin
        public float dot(APoint p1)
        {
            return x*p1.x + y*p1.y;
        }

        // Take this as origin and dot product
        public float dot(APoint p1, APoint p2)
        {
            return (p1-this).dot(p2-this);
        }

        public float sqrLength () => this.dot(this);

        public bool Equals(APoint other)
        {
            return Math.Abs(this.x - other.x) <= EPS && Math.Abs(this.y - other.y) <= EPS;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is APoint p && Equals(p);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                return hash;
            }
        }

        public static explicit operator SKPoint(APoint sp)
        {
            return new SKPoint(sp.x, sp.y);
        }

        public static explicit operator APoint(SKPoint sp)
        {
            return new APoint(sp.X, sp.Y);
        }

    }

    public class QuadEdge
    {
        public APoint origin;
        public QuadEdge? rot;
        public QuadEdge? onext;
        public bool used = false;
        public bool active = true;

        public QuadEdge rev => rot.rot;
        public QuadEdge lnext => rev.rot.onext.rot;
        public QuadEdge oprev => rot.onext.rot;
        public APoint dest => rot.rot.origin;

        public static APoint infinitePoint = new APoint(1e20f, 1e20f);

        public static QuadEdge makeEdge(APoint from, APoint to)
        {
            QuadEdge? e1 = new QuadEdge(), e2 = new QuadEdge(), e3 = new QuadEdge(), e4 = new QuadEdge();
            e1.origin = from;
            e2.origin = to;
            e3.origin = e4.origin = infinitePoint;
            e1.rot = e3;
            e2.rot = e4;
            e3.rot = e2;
            e4.rot = e1;
            e1.onext = e1;
            e2.onext = e2;
            e3.onext = e4;
            e4.onext = e3;
            return e1;
        }

        public static void splice (QuadEdge e1, QuadEdge e2)
        {
            (e1.onext.rot.onext, e2.onext.rot.onext) = (e2.onext.rot.onext, e1.onext.rot.onext);
            (e1.onext, e2.onext) = (e2.onext, e1.onext);
        }

        public static void deleteEdge (QuadEdge e)
        {
            splice (e, e.oprev);
            splice (e.rev, e.rev.oprev);
        } 

        public static QuadEdge connect(QuadEdge e1, QuadEdge e2)
        {
            QuadEdge e = makeEdge(e1.dest, e2.origin);
            splice(e, e1.lnext);
            splice(e.rev, e2);
            return e;
        }

        public static bool leftOf (APoint p, QuadEdge e)
        {
            return p.cross(e.origin, e.dest) > 0;
        }

        public static bool rightOf (APoint p, QuadEdge e)
        {
            return p.cross(e.origin, e.dest) < 0;
        }

        public APoint voronoiVertex()
        {
            float x = (origin.x + dest.x + lnext.dest.x)/3;
            float y = (origin.y + dest.y + lnext.dest.y)/3;
            return new APoint(x,y);
        }        
        
        public float area()
        {
            APoint a = origin, b = dest, c = lnext.dest;
            float res = (
                a.x * (b.y-c.y) +
                b.x * (c.y-a.y) +
                c.x * (a.y-b.y)
            );
            return res;
        }
    };
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
