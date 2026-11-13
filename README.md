# photomosaic-nes

Build photomosaics from NES box art — each tile is a box art image matched by average colour.

## How it works

Every box art image is reduced to its average colour. The source image is
split into a grid of tile-sized cells, and each cell is filled with the box
art whose average colour is closest to that cell's average colour. Tiles
may repeat — with a few dozen box arts, repeats are what make the mosaic
possible. Each tile is tinted slightly toward its target colour so the big
picture reads clearly while the box art stays visible.

## Build

Requires .NET 10.

```sh
dotnet build src/PhotomosaicNes/PhotomosaicNes.csproj -c Release
```

## Usage

```sh
dotnet run --project src/PhotomosaicNes -c Release -- <source-image> \
  [--tiles <dir>] [--output <file>] [--scale <factor>] \
  [--tile-size <px>] [--tint <0..1>]
```

| Option | Default | What it does |
| --- | --- | --- |
| `<source-image>` | — | The image to turn into a mosaic |
| `--tiles <dir>` | `./boxart` | Directory of box art images (png/jpg/jpeg/webp/bmp) |
| `--output <file>` | `./mosaic.png` | Where to write the mosaic |
| `--scale <factor>` | `1.0` | Output size relative to the source: 1.0 = same resolution, 0.5 = half, 2.0 = double |
| `--tile-size <px>` | `48` | How big each box art tile is drawn, in pixels. Smaller = finer detail |
| `--tint <0..1>` | `0.25` | How much to tint each tile toward its target colour (0 = raw box art, 1 = solid colour) |

## Try it with the samples

The `samples/` folder has 24 generated stand-in box arts (colourful
patterns, one per hue — real NES box art not included) and a test source
image:

```sh
dotnet run --project src/PhotomosaicNes -c Release -- \
  samples/test-source.png --tiles samples/boxart --output samples/test-mosaic.png
```

That produces a 640x480 mosaic (14 x 10 tiles at 48px). Try
`--scale 2 --tile-size 32` for a bigger, finer version.

## Notes

- Box art images are centre-cropped to squares when drawn as tiles, so
  portrait box art loses a little off the top and bottom.
- Built with [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
  3.1.12. (ImageSharp.Drawing was deliberately avoided — its v3 package
  needs a commercial licence.)
