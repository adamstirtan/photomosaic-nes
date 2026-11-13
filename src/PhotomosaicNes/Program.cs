// PhotomosaicNes — build a photomosaic out of NES box art.
//
// Usage:
//   photomosaic <source-image> [--tiles <dir>] [--output <file>]
//               [--scale <factor>] [--tile-size <px>] [--tint <0..1>]
//
//   <source-image>   The image to turn into a mosaic.
//   --tiles <dir>    Directory of box art images (png/jpg/jpeg/webp/bmp).
//                    Default: ./boxart
//   --output <file>  Where to write the mosaic. Default: ./mosaic.png
//   --scale <factor> Output size relative to the source image.
//                    1.0 = same resolution as the source (default).
//                    0.5 = half size, 2.0 = double size.
//   --tile-size <px> How big each box art tile is drawn, in pixels.
//                    Default: 48. Smaller tiles = finer detail, more tiles.
//   --tint <0..1>    How much to tint each tile toward the colour of the
//                    source region it stands in (0 = raw box art,
//                    1 = solid colour). Default: 0.25 — keeps the box art
//                    visible while making the big picture read clearly.
//   --pick-from <n>  Pick randomly from the n closest-matching tiles
//                    instead of always the single best match. Default: 1.
//                    Higher values (e.g. 5) break up the repetition you
//                    get in flat areas of the source — a plain white
//                    background otherwise fills with copies of the single
//                    whitest box art. Seeded, so runs are reproducible.
//
// How it works: every tile image is reduced to its average colour, the
// source image is split into a grid of tile-sized cells, and each cell is
// filled with the tile whose average colour is closest to that cell's
// average colour. Tiles may repeat — with a few dozen box arts, repeats
// are what make the mosaic possible.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var argsList = args.ToList();

if (argsList.Count == 0 || argsList.Contains("--help") || argsList.Contains("-h"))
{
    Console.WriteLine("""
        photomosaic — make a photomosaic out of NES box art

        Usage:
          photomosaic <source-image> [--tiles <dir>] [--output <file>]
                      [--scale <factor>] [--tile-size <px>] [--tint <0..1>]

          --tiles <dir>    Box art directory. Default: ./boxart
          --output <file>  Output file. Default: ./mosaic.png
          --scale <factor> Output size vs source. Default: 1.0
          --tile-size <px> Tile size in pixels. Default: 48
          --tint <0..1>    Tint tiles toward target colour. Default: 0.25
          --pick-from <n>  Random pick from n closest tiles. Default: 1
        """);
    return argsList.Count == 0 ? 1 : 0;
}

string sourcePath = argsList[0];
string tilesDir = Option("--tiles") ?? "boxart";
string outputPath = Option("--output") ?? "mosaic.png";
double scale = double.TryParse(Option("--scale"), out var s) ? s : 1.0;
int tileSize = int.TryParse(Option("--tile-size"), out var t) ? t : 48;
double tint = double.TryParse(Option("--tint"), out var tn) ? tn : 0.25;
int pickFrom = int.TryParse(Option("--pick-from"), out var pf) ? pf : 1;
var rng = new Random(42); // fixed seed: same inputs, same mosaic

string? Option(string name)
{
    var i = argsList.IndexOf(name);
    return i >= 0 && i + 1 < argsList.Count ? argsList[i + 1] : null;
}

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source image not found: {sourcePath}");
    return 1;
}
if (!Directory.Exists(tilesDir))
{
    Console.Error.WriteLine($"Tiles directory not found: {tilesDir}");
    return 1;
}
if (scale <= 0 || tileSize <= 0 || tint < 0 || tint > 1 || pickFrom < 1)
{
    Console.Error.WriteLine("Invalid option: --scale and --tile-size must be > 0, --tint must be 0..1, --pick-from must be >= 1.");
    return 1;
}

// ---- Load tiles: each one becomes a square tile + its average colour. ----
var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

var tileFiles = Directory.GetFiles(tilesDir)
    .Where(f => extensions.Contains(Path.GetExtension(f)))
    .OrderBy(f => f)
    .ToList();

if (tileFiles.Count == 0)
{
    Console.Error.WriteLine($"No images found in {tilesDir} (looking for png/jpg/jpeg/webp/bmp).");
    return 1;
}

var tiles = new List<Tile>();
foreach (var file in tileFiles)
{
    try
    {
        using var img = Image.Load<Rgba32>(file);
        var avg = AverageColor(img);
        // Centre-crop to a square tile at the requested tile size.
        var tileImg = img.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(tileSize, tileSize),
            Mode = ResizeMode.Crop,
        }));
        tiles.Add(new Tile(Path.GetFileName(file), tileImg, avg));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Skipping unreadable tile {file}: {ex.Message}");
    }
}

if (tiles.Count == 0)
{
    Console.Error.WriteLine("No usable tile images found.");
    return 1;
}

// ---- Build the mosaic. ----
using var source = Image.Load<Rgba32>(sourcePath);
int outWidth = Math.Max(tileSize, (int)Math.Round(source.Width * scale));
int outHeight = Math.Max(tileSize, (int)Math.Round(source.Height * scale));
int cols = (outWidth + tileSize - 1) / tileSize;
int rows = (outHeight + tileSize - 1) / tileSize;

using var mosaic = new Image<Rgba32>(cols * tileSize, rows * tileSize);

for (int row = 0; row < rows; row++)
{
    for (int col = 0; col < cols; col++)
    {
        // Source region for this cell (in source pixels).
        int sx = (int)Math.Round((double)col * tileSize / scale);
        int sy = (int)Math.Round((double)row * tileSize / scale);
        int sw = Math.Max(1, (int)Math.Round((double)tileSize / scale));
        int sh = Math.Max(1, (int)Math.Round((double)tileSize / scale));
        sx = Math.Min(sx, source.Width - 1);
        sy = Math.Min(sy, source.Height - 1);
        sw = Math.Min(sw, source.Width - sx);
        sh = Math.Min(sh, source.Height - sy);

        var target = AverageColor(source, sx, sy, sw, sh);
        var best = pickFrom == 1
            ? tiles.MinBy(t => ColorDistance(t.Average, target))!
            : tiles.OrderBy(t => ColorDistance(t.Average, target))
                   .Take(pickFrom)
                   .ElementAt(rng.Next(Math.Min(pickFrom, tiles.Count)));

        var dest = new Point(col * tileSize, row * tileSize);
        mosaic.Mutate(ctx => ctx.DrawImage(best.Image, dest, 1f));

        if (tint > 0)
        {
            // Tint without the (commercially licensed) ImageSharp.Drawing
            // package: draw a solid-colour overlay at partial opacity.
            using var overlay = new Image<Rgba32>(tileSize, tileSize);
            overlay.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var span = accessor.GetRowSpan(y);
                    for (int x = 0; x < span.Length; x++)
                        span[x] = new Rgba32(target.R, target.G, target.B, 255);
                }
            });
            mosaic.Mutate(ctx => ctx.DrawImage(overlay, dest, (float)tint));
        }
    }
}

// Crop the padded grid back to the exact output size.
using var final = mosaic.Clone(ctx => ctx.Crop(outWidth, outHeight));
await final.SaveAsPngAsync(outputPath);

Console.WriteLine($"Tiles:   {tiles.Count} box art images from {tilesDir}");
Console.WriteLine($"Grid:    {cols} x {rows} tiles ({tileSize}px each)");
Console.WriteLine($"Source:  {source.Width}x{source.Height} -> output {outWidth}x{outHeight} (scale {scale})");
Console.WriteLine($"Wrote:   {outputPath}");
return 0;

// ---- Helpers ----

static Rgba32 AverageColor(Image<Rgba32> img, int x = 0, int y = 0, int w = -1, int h = -1)
{
    if (w < 0) w = img.Width;
    if (h < 0) h = img.Height;
    // Downscale the region to 1x1: ImageSharp averages it for us.
    using var region = img.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h)));
    region.Mutate(ctx => ctx.Resize(1, 1));
    return region[0, 0];
}

static double ColorDistance(Rgba32 a, Rgba32 b)
{
    // Plain Euclidean distance in RGB space.
    double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
    return dr * dr + dg * dg + db * db;
}

sealed record Tile(string Name, Image<Rgba32> Image, Rgba32 Average);
