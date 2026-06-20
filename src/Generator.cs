
using System.Collections;
using System.Diagnostics;
using SkiaSharp;

namespace GenMap
{
    class Generator
    {
        int seed;
        Random random;

        public Generator (int seed){
            this.seed = seed;
            random = new Random(seed);
        }

        public bool intersectsAny(ref List<Linestring> ls, Linestring l1, Linestring l2, Linestring nl)
        {
            if (ls.Any(e => {
                if (e.Equals(l1)) return false; 
                if (e.Equals(l2)) return false; 
                return GenMap.Utils.doIntersect(nl, e);
            }))
            {
                return true;
            }
            return false;
        }

        public int getBoundArea (CRectangle b)
        {
            return Math.Abs((int)((b.topleft.Y - b.bottomright.Y)*(b.topleft.X - b.bottomright.X)));
        }

        public float getDistanceSquared (SKPoint s1, SKPoint s2)
        {
            return (float)(
                (s1.X - s2.X)*(s1.X - s2.X) +
                (s1.Y - s2.Y)*(s1.Y - s2.Y)
            );
        }

        public int[] findClosestDots (SKPoint s, ref SKPoint[] dots, int count)
        {
            return dots
            .Select((p, i) => (i, getDistanceSquared(s, p)))
            .OrderBy(x => x.Item2)
            .Take(count)
            .Select((p,i) => p.i)
            .ToArray();
        }

        public SKPoint[] radialSweepSort (SKPoint[] dots)
        {
            SKPoint[] radiallySortedDots = new SKPoint[dots.Length];

            SKPoint centerOfDots = SKPoint.Empty;
            foreach (var dot in dots)
            {
                centerOfDots.X += dot.X / dots.Length;    
                centerOfDots.Y += dot.Y / dots.Length;    
            }

            int dotsFound = 0;
            float radius = 0;
            bool[] dotProcessed = new bool[dots.Length];
            dotProcessed.Initialize();

            while (dotsFound < dots.Length)
            {
                radius += 5;
                for(int i = 0; i < dots.Length; i++)
                {
                    if(dotProcessed[i] == true) continue;
                    if(getDistanceSquared(dots[i], centerOfDots) <= radius * radius)
                    {
                        radiallySortedDots[dotsFound++] = dots[i];  
                        dotProcessed[i] = true;
                    } 
                }
            }

            return radiallySortedDots;
        }

        // l is how far we processed, r is total points in possesion
        Tuple<QuadEdge, QuadEdge> buildQuadEdgeTree(long l, long r, APoint[] dots)
        {
            if (r - l + 1 <= 0)
            {
                throw new Exception($"Invalid range [{l},{r}] \n{Environment.StackTrace} \n");
            }

            if (r - l + 1 == 1)
            {
                // Either return nulls or whatever your algorithm expects
                throw new Exception("Single-point case reached");
            }
            if(r - l + 1 == 2)
            { // There are only two nodes. Connect 'em and return
                QuadEdge newEdge = QuadEdge.makeEdge(dots[l], dots[r]);
                return Tuple.Create(newEdge, newEdge.rev);
            }
            else if(r - l + 1 == 3)
            { // There are three nodes. We can form a triangle, YAY!

                //First form edge templates
                QuadEdge edge1 = QuadEdge.makeEdge(dots[l], dots[l+1]);
                QuadEdge edge2 = QuadEdge.makeEdge(dots[l+1], dots[r]);

                //Second connect their common vertex
                QuadEdge.splice(edge1.rev, edge2);

                //Determine orientation of 3 points
                float crossProduct = dots[l].cross(dots[l+1], dots[r]);
                int orientation = crossProduct > 0 ? 1 : (crossProduct < -float.Epsilon ? -1 : 0);
                if(orientation == 0)
                { // Points are colinear. Sadly no triangle :<
                    return Tuple.Create(edge1, edge2.rev);
                }

                // Points are not colinear. We can certainly form a triangle :>
                // In fact, lets do that now
                QuadEdge edge3 = QuadEdge.connect(edge2, edge1);

                // Find the edge that points OUT OF THIS TRIANGLE
                if(orientation == 1)
                { //P_l -> P_l+1 -> P_r is counterclockwise
                    return Tuple.Create(edge1, edge2.rev);
                }else
                { //P_l -> P_l+1 -> P_r is clockwise
                    return Tuple.Create(edge3.rev, edge3);
                }
            }

            // There are too many nodes! We gotta delegate em!
            long mid = (l+r) / 2; // Find the middle of the range

            // Divide the list in two until we reach one of the base cases (1 or 2 nodes)
            (QuadEdge ldo, QuadEdge ldi) = buildQuadEdgeTree(l, mid, dots);
            (QuadEdge rdi, QuadEdge rdo) = buildQuadEdgeTree(mid+1, r, dots);

            // To remove intersecting edges I guess?
            // y'know if the thing that is supposedly on our right is on our left, chances are
            // we crossed paths along the way
            // We know they should be in that direction because dots are sorted by X cordinate
            while (true)
            {
                if(QuadEdge.leftOf(rdi.origin, ldi))
                {
                    ldi = ldi.lnext;
                    continue;
                }

                if(QuadEdge.rightOf(ldi.origin, rdi))
                {
                    rdi = rdi.rev.onext;
                    continue;
                }
                break;
            }

            // Connect end of right edge with origin of left edge
            QuadEdge base1 = QuadEdge.connect(rdi.rev, ldi);
            Predicate<QuadEdge> valid = (QuadEdge e) =>
            { 
                return QuadEdge.rightOf(e.dest, base1);
            };
            if (ldi.origin == ldo.origin) ldo = base1.rev;
            if (rdi.origin == rdo.origin) rdo = base1;

            // Start stitching 'em edges up
            while (true)
            {

                QuadEdge lcand = base1.rev.onext;
                if (valid(lcand))
                {
                    while(Utils.InCircle(base1.dest, base1.origin, lcand.dest, lcand.onext.dest))
                    {
                        QuadEdge t = lcand.onext;
                        QuadEdge.deleteEdge(lcand);
                        lcand = t;
                    }
                }

                QuadEdge rcand = base1.oprev;
                if (valid(rcand))
                {
                    while(Utils.InCircle(base1.dest, base1.origin, rcand.dest, rcand.oprev.dest))
                    {
                        QuadEdge t = rcand.oprev;
                        QuadEdge.deleteEdge(rcand);
                        rcand = t;
                    }
                }

                if (!valid(lcand) && !valid(rcand)) break;
                
                if(!valid(lcand) || (valid(rcand) && Utils.InCircle(lcand.dest, lcand.origin, rcand.origin, rcand.dest)))
                {
                    base1 = QuadEdge.connect(rcand, base1.rev);
                }
                else
                {
                    base1 = QuadEdge.connect(base1.rev, lcand.rev);                    
                }

                if (base1.onext == null || base1.rev.onext == null)
                    throw new Exception("Broken quad-edge linkage");
            }
            return Tuple.Create(ldo, rdo);
        }

        public  Tuple<QuadEdge, QuadEdge> buildDelaunayTriangulation(SKPoint[] dots)
        {
            APoint[] pointsSorted = dots.OrderBy((p) => p.X).ThenBy((p) => p.Y).Select((p,i) => new APoint(p.X, p.Y)).ToArray();
            if(pointsSorted.Length < 1) throw new Exception("You dumbass haven't given me enough dots to work with!");
            return buildQuadEdgeTree(0, pointsSorted.Length-1, pointsSorted);
        }

        public SKPoint[] ApplyLloydRelaxation(SKPoint[] input, CRectangle bounds)
        {
            APoint sum = new APoint(0, 0);
            
            for (int i = 0; i < input.Length; i++)
            {
                sum.x += input[i].X;
                sum.y += input[i].Y;
            }
            
            APoint center = new APoint(
                sum.x / input.Length,
                sum.y / input.Length
            );
            
            // 1. Build Delaunay
            var tri = buildDelaunayTriangulation(input);

            // 2. Collect one incident edge per vertex (IMPORTANT)
            Dictionary<APoint, QuadEdge> seedEdge = new();

        
            APoint Circumcenter(APoint A, APoint B, APoint C)
            {
                double ax = A.x, ay = A.y;
                double bx = B.x, by = B.y;
                double cx = C.x, cy = C.y;

                double d = 2.0 * (ax*(by - cy) + bx*(cy - ay) + cx*(ay - by));

                if (Math.Abs(d) < 1e-12)
                    return new APoint(0, 0); // degenerate (collinear)

                double a2 = ax*ax + ay*ay;
                double b2 = bx*bx + by*by;
                double c2 = cx*cx + cy*cy;

                double ux =
                    (a2*(by - cy) +
                     b2*(cy - ay) +
                     c2*(ay - by)) / d;

                double uy =
                    (a2*(cx - bx) +
                     b2*(ax - cx) +
                     c2*(bx - ax)) / d;

                return new APoint((float)ux, (float)uy);
            }

            APoint Centroid(APoint A, APoint B, APoint C)
            {
                return (A + B + C)/3;
            }

            void RegisterEdge(QuadEdge e)
            {
                if (!seedEdge.ContainsKey(e.origin))
                    seedEdge[e.origin] = e;
            }

            // Walk entire structure ONCE to seed vertices
            Queue<QuadEdge> q = new();
            q.Enqueue(tri.Item1);

            HashSet<QuadEdge> seen = new();

            while (q.Count > 0)
            {
                var e = q.Dequeue();
                if (e == null || seen.Contains(e)) continue;
                seen.Add(e);

                RegisterEdge(e);

                if (e.onext != null) q.Enqueue(e.onext);
                if (e.rev != null) q.Enqueue(e.rev);
                if (e.lnext != null) q.Enqueue(e.lnext);
            }

            // 3. Lloyd step: compute Voronoi cell centroid per vertex
            List<SKPoint> newPoints = new();

            foreach (var kv in seedEdge)
            {
                QuadEdge start = kv.Value;
                QuadEdge e = start;

                List<APoint> cell = new();

                // IMPORTANT: walk around vertex using ONEXT
                do
                {
                    // each incident triangle contributes a Voronoi vertex
                    APoint a = e.origin;
                    APoint b = e.dest;
                    APoint c = e.lnext.dest;

                    cell.Add(Circumcenter(a, b, c));

                    e = e.onext;
                }
                while (e != start);

                // skip degenerate / hull cases
                if (cell.Count < 3)
                    continue;

                APoint centroid = Centroid(cell[0], cell[1], cell[2]);

                if(centroid.x > ((APoint)bounds.bottomright).x || centroid.x < ((APoint)bounds.topleft).x) continue;
                if(centroid.y > ((APoint)bounds.bottomright).y || centroid.y < ((APoint)bounds.topleft).y) continue;
                //if(getDistanceSquared((SKPoint)center, (SKPoint)centroid) > input.Length*2000) continue;
                newPoints.Add((SKPoint)centroid);
            }

            if(newPoints.Count < 3) return input;
            return newPoints.Select(p => (SKPoint)p).ToArray();
        }
        public Linestring[] dotArrayToTriangleEdges(ref SKPoint[] dots)
        {
            Tuple<QuadEdge, QuadEdge> edgeTree = buildDelaunayTriangulation(dots);  

            Console.WriteLine("Building QuadEdge tree complete!");
            QuadEdge e = edgeTree.Item1;
            List<QuadEdge> edges = new List<QuadEdge>{e};
            while (e.onext.dest.cross(e.dest, e.origin) < 0)
            {
                e = e.onext;                
            }

            List<QuadEdge> res = new List<QuadEdge>();
            Action add = () =>
            {
                QuadEdge curr = e;
                do
                {
                    if (curr.rev.area() > 1000) res.Add(curr);
                    edges.Add(curr.rev);
                    curr.used = true;
                    curr = curr.lnext;
                }while(curr != e);
            };

            add();
            int it = 0;
            while(it < edges.Count)
            {
                if (!(e = edges[it++]).used)
                {
                    add();
                    //Console.WriteLine("Adding edges. Current edges: " + edges.Count);
                }
            }
            
            Console.WriteLine("Final triangulation complete. Edge count: " + edges.Count);

            return res.Select((p,i) => new Linestring(new SKPoint(p.origin.x, p.origin.y), new SKPoint(p.dest.x, p.dest.y))).ToArray();
        }

        public Linestring[] cleanPolygon(Linestring[] lss)
        {
            List<Linestring> res = new List<Linestring>();

            foreach (Linestring l in lss)
            {
                if(!res.Any(k => (k.p1 == l.p1 && k.p2 == l.p2) || (k.p2 == l.p1 && k.p1 == l.p2)))
                {
                    res.Add(l);
                }
            }
            return res.ToArray();
        }     
        
        public Linestring[] cleanLoneEdges(Linestring[] lss)
        {
            List<Linestring> res = new List<Linestring>();

            foreach (Linestring l in lss)
            {
                bool p1C = false, p2C = false;
                if(lss.Any(k => !k.Equals(l) && (l.p1 == k.p1 || l.p1 == k.p2)))
                {
                    p1C = true;
                }
                if(lss.Any(k => !k.Equals(l) && (l.p2 == k.p1 || l.p2 == k.p2)))
                {
                    p2C = true;
                }
                if(p1C && p2C) res.Add(l);
            }
            return res.ToArray();
        }

        public Linestring[] cullEdges(Linestring[] lss, float chance)
        {
            List<Linestring> res = new List<Linestring>();

            int it = 0;
            while(it < lss.Length)
            {
                if(random.Next(0, 100) > chance*100)
                {
                    res.Add(lss[it]);
                }
                it++;
            }

            return cleanLoneEdges(res.ToArray());  
        }

        public Linestring[] addWiggling(Linestring[] lss)
        {
            List<Linestring> res = new List<Linestring>();

            int it = 0;
            while(it < lss.Length)
            {
                if(random.Next(0,4) == 0)
                {
                    //Skip wiggling this edge
                    res.Add(lss[it]);
                    it++;
                    continue;
                }
                float mag = random.Next(-5, +5);
                float perpindicularXYRatio = -1/(lss[it].p1.Y-lss[it].p2.Y)/(lss[it].p1.X+lss[it].p2.X);

                SKPoint c = new SKPoint (
                    (lss[it].p1.X + lss[it].p2.X)/2 + mag*perpindicularXYRatio,
                    (lss[it].p1.Y + lss[it].p2.Y)/2 + mag 
                );
                
                res.Add(new Linestring(lss[it].p1, c));
                res.Add(new Linestring(c, lss[it].p2));
                it++;
            }

            return res.ToArray();
        }

        public SKPoint[] generateDotArray(CRectangle continent)
        {

            int cityArea = 5000;
            int area = getBoundArea(continent);
            int dotCount = area / cityArea;
            int distanceReq = (int)Math.Sqrt(cityArea) * 10;

            List<SKPoint> p = new List<SKPoint>();

            while (p.Count < dotCount)
            {
                int x = Math.Min((int)continent.topleft.X, (int)continent.bottomright.X);
                int xe = Math.Max((int)continent.topleft.X, (int)continent.bottomright.X);
                int y = Math.Min((int)continent.bottomright.Y, (int)continent.topleft.Y);
                int ye = Math.Max((int)continent.bottomright.Y, (int)continent.topleft.Y);

                SKPoint candidate = new SKPoint
                {
                    X = random.Next(x, xe),
                    Y = random.Next(y, ye)
                };    

                if(p.All(x => getDistanceSquared(x, candidate) >= distanceReq))
                {
                    p.Add(candidate);                   
                }                            
            }


            return p.ToArray();
        }

        public CRectangle[] generateContinentBounds()
        {
            CRectangle[] divide(CRectangle bounds)
            {

                int xOrY = random.Next(0,2);
                if (xOrY == 0)
                {//X
                    int higher_bound = Math.Max((int)bounds.topleft.X, (int)bounds.bottomright.X);
                    int lower_bound = Math.Min((int)bounds.topleft.X, (int)bounds.bottomright.X);
                    int cutCordinate = Math.Max(random.Next(lower_bound, higher_bound), 0);
                    return new CRectangle[]
                    {
                      new CRectangle((int)bounds.topleft.X, (int)bounds.topleft.Y, cutCordinate-60, (int)bounds.bottomright.Y),
                      new CRectangle(cutCordinate+60, (int)bounds.topleft.Y, (int)bounds.bottomright.X, (int)bounds.bottomright.Y)
                    };
                }else
                {//Y
                    int higher_bound = Math.Max((int)bounds.topleft.Y, (int)bounds.bottomright.Y);
                    int lower_bound = Math.Min((int)bounds.topleft.Y, (int)bounds.bottomright.Y);
                    int cutCordinate = Math.Max(random.Next(lower_bound, higher_bound), 0);
                    return new CRectangle[]
                    {
                      new CRectangle((int)bounds.topleft.X, (int)bounds.topleft.Y, (int)bounds.bottomright.X, cutCordinate-60),
                      new CRectangle((int)bounds.topleft.X, cutCordinate+60, (int)bounds.bottomright.X, (int)bounds.bottomright.Y)
                    };
                }
            }

            CRectangle initialBounds = new CRectangle(0,0, Program.WIDTH, Program.HEIGHT);
            List<CRectangle> res = divide(initialBounds).ToList();
            Queue<CRectangle> toBeDivided = new Queue<CRectangle>();

            int counter = 0;
            int limit = 3;
            while(counter < 3)
            {
                if (counter <= limit / 2)
                {
                    toBeDivided.Enqueue(res[res.Count-1]);
                    toBeDivided.Enqueue(res[res.Count-2]);
                    res.RemoveAt(res.Count-1);
                    res.RemoveAt(res.Count-1);
                    Console.WriteLine(res.Count);

                }

                if (toBeDivided.Count > 0)
                {
                    res = res.Concat(divide(toBeDivided.Dequeue())).ToList();
                    counter++;
                }
            }
            return divide(initialBounds);
        }


    }
   
}