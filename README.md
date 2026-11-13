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
| `--pick-from <n>` | `1` | Pick randomly from the n closest-matching tiles instead of always the best one. Higher values (e.g. 5) break up repetition in flat areas of the source — a plain white background otherwise fills with copies of the single whitest box art. Seeded, so runs are reproducible |

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

## Try it with real box art

The sample box arts are stand-ins. For the real thing, fetch 250 actual
NES box arts from libretro-thumbnails (the set RetroArch uses):

```sh
./fetch-boxart.sh
```

Box art is copyrighted by its publishers, so it is **not** committed to
this repo — `boxart/` is gitignored, and the script re-fetches it
anytime. Then run the mosaic against a real source image. The repo
includes one: a public-domain photo of the NES console and controller
(`samples/nes-console-source.jpg`, 5560x3020, by Evan-Amos via Wikimedia
Commons):

```sh
dotnet run --project src/PhotomosaicNes -c Release -- \
  samples/nes-console-source.jpg --tiles boxart \
  --output samples/nes-mosaic.png --scale 0.5 --pick-from 5
```

That makes a 2780x1510 mosaic (58 x 32 tiles) in about 10 seconds — the
console and controller read clearly, built entirely out of box art.

## Notes

- Box art images are centre-cropped to squares when drawn as tiles, so
  portrait box art loses a little off the top and bottom.
- Built with [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
  3.1.12. (ImageSharp.Drawing was deliberately avoided — its v3 package
  needs a commercial licence.)
