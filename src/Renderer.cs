using System.Drawing;
using SkiaSharp;

namespace GenMap;
class Renderer
{
    
    SKBitmap bitmap = new SKBitmap(Program.WIDTH, Program.HEIGHT);
    SKCanvas canvas;
    public Renderer()
    {
        canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);        
    }
    public void render(ref Linestring[] lines)
    {
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Blue,
            IsAntialias = true
        };

        foreach(var line in lines)
        {
            canvas.DrawLine(line.p1, line.p2, strokePaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes("output.png", data.ToArray());
    }

    public void render(ref Polygon[] cities)
    {
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Blue,
            IsAntialias = true
        };

        foreach(var city in cities)
        {
            using var path = new SKPath();
            path.MoveTo(city.points[0]);

            for (int i = 1; i < city.points.Length; i++)
                path.LineTo(city.points[i]);

            path.Close();

            //canvas.DrawPath(path, fillPaint);
           canvas.DrawPath(path, strokePaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes("output.png", data.ToArray());
    }
    public void render(ref LineMap map)
    {
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Blue,
            IsAntialias = true
        };

        foreach (var line in map.lines) canvas.DrawLine(line.p1, line.p2, strokePaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes("output.png", data.ToArray());
    }
    public void render(ref SKPoint[] points, SKColor c)
    {
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = c,
            IsAntialias = true
        };

        foreach (var point in points)canvas.DrawCircle(point, 5, fillPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes("output.png", data.ToArray());
    }

    public void render(ref CRectangle[] bounds)
    {
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Blue,
            IsAntialias = true
        };

        foreach (var bound in bounds)
        {
            canvas.DrawRect(
                bound.topleft.X,
                bound.topleft.Y,
                bound.bottomright.X - bound.topleft.X,
                bound.bottomright.Y - bound.topleft.Y,
                strokePaint
            );
        }       

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        File.WriteAllBytes("output.png", data.ToArray());
    }


}
