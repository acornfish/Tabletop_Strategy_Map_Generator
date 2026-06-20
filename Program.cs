using SkiaSharp;

namespace GenMap
{
    class Program
    {
        public static int SEED = 112213;
        public static int WIDTH = 1587;
        public static int HEIGHT = 1123;
        public static int RELAXATION_COUNT = 3;
        public static int WIGGLE_COUNT = 0;
        public static float EDGE_CULL_CHANCE = 0.2f;

        [System.STAThread]
        public static void Main()
        {
            Console.Write("Seed (Enter a number): ");
            SEED = int.Parse(Console.ReadLine());
            Console.Write("Size (A4 or A3): ");
            (WIDTH, HEIGHT) = Console.ReadLine().ToLower() == "a3" ? (1587, 1123) : (992, 1403);
            Console.Write("Entropy (0-4): ");
            RELAXATION_COUNT = 4-int.Parse(Console.ReadLine());
            Console.Write("Wiggling (0-4): ");
            WIGGLE_COUNT = int.Parse(Console.ReadLine());
            Console.Write("Edge Cull Chance (0: Keep all triangles, 10: leave nothing): ");
            EDGE_CULL_CHANCE = float.Parse(Console.ReadLine())/10;


            Renderer renderer = new Renderer();
            Generator generator = new Generator(SEED);
            
            CRectangle[] contintentBounds =  generator.generateContinentBounds();
            Console.WriteLine("Generating continent bounds complete! Continent count: " + contintentBounds.Length);
            
            for(int i = 0; i < contintentBounds.Length; i++)
            {
                SKPoint[] points = generator.generateDotArray(contintentBounds[i]);
                Console.WriteLine("Generating dot array complete! Dot count: " + points.Length);
                SKPoint[] relaxedDots = generator.ApplyLloydRelaxation(points, contintentBounds[i]);
                for(int x=0;x<RELAXATION_COUNT;x++) relaxedDots = generator.ApplyLloydRelaxation(relaxedDots, contintentBounds[i]);
                Console.WriteLine("Applied relaxation to dot array");
                Linestring[] triangulation = generator.dotArrayToTriangleEdges(ref relaxedDots);
                Console.WriteLine("Triangulating dot array complete!");
                Linestring[] cleanedTriangles = triangulation;
                cleanedTriangles = generator.cleanPolygon(cleanedTriangles);
                Console.WriteLine("Cleaning polygon complete! Removed Edges: " + (triangulation.Length - cleanedTriangles.Length));
                Linestring[] CulledEdges = generator.cullEdges(cleanedTriangles, EDGE_CULL_CHANCE);
                Console.WriteLine("Culling edges complete! Removed Edges: " + (cleanedTriangles.Length - CulledEdges.Length));
                Linestring[] wiggledEdges = CulledEdges;
                for(int x=0;x<WIGGLE_COUNT;x++) wiggledEdges = generator.addWiggling(wiggledEdges);
                Console.WriteLine("Wiggling the edges complete");
   
                renderer.render(ref wiggledEdges);
                //renderer.render(ref contintentBounds);
                //renderer.render(ref relaxedDots, SKColors.Red);
            }
            //renderer.render(ref points, SKColors.Blue);
        }
    }
}