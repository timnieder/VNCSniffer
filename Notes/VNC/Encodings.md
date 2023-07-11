## Raw (0)
Values are pixels in left-to-right scan order
```
+----------------------------+--------------+-------------+
| No. of bytes               | Type [Value] | Description |
+----------------------------+--------------+-------------+
| width*height*bytesPerPixel | PIXEL array  | pixels      |
+----------------------------+--------------+-------------+
```

## CopyRect (1)
Specifies that a rectangle should be copied to a location from a location in the existing framebuffer
```
+--------------+--------------+----------------+
| No. of bytes | Type [Value] | Description    |
+--------------+--------------+----------------+
| 2            | U16          | src-x-position |
| 2            | U16          | src-y-position |
+--------------+--------------+----------------+
```

## RRE (2)
Background color specified in header, subrectangles fill in the rest of the framebuffer

Header:
```
+---------------+--------------+-------------------------+
| No. of bytes  | Type [Value] | Description             |
+---------------+--------------+-------------------------+
| 4             | U32          | number-of-subrectangles |
| bytesPerPixel | PIXEL        | background-pixel-value  |
+---------------+--------------+-------------------------+
```
Followed by number-of-subrectangles subrectangles:
```
+---------------+--------------+---------------------+
| No. of bytes  | Type [Value] | Description         |
+---------------+--------------+---------------------+
| bytesPerPixel | PIXEL        | subrect-pixel-value |
| 2             | U16          | x-position          |
| 2             | U16          | y-position          |
| 2             | U16          | width               |
| 2             | U16          | height              |
+---------------+--------------+---------------------+
```

## Hextile (5)
Rectangles are split up into 16px\*16px tiles, from left-to-right, top-to-bottom
If the width/height isn't dividable by 16, the last tile/row will be smaller
Each tile is encoded as raw or a RRE variation, beginning with a subencoding type byte:
```
+--------------+--------------+---------------------+
| No. of bytes | Type [Value] | Description         |
+--------------+--------------+---------------------+
| 1            | U8           | subencoding-mask:   |
|              | [1]          | Raw                 |
|              | [2]          | BackgroundSpecified |
|              | [4]          | ForegroundSpecified |
|              | [8]          | AnySubrects         |
|              | [16]         | SubrectsColored     |
+--------------+--------------+---------------------+
```
- If the Raw bit is set, the other bits are irrelevant, `width*height` pixel values follow
- If the BackgroundSpecified bit is set, a pixel value follows and specifies the background color for this tile. If this isn't set the background is the same as the last tile
- If the ForegroundSpecified bit is set, a pixel value follows and specifies the foregorund color to be used for all subrectangles in this tile
- If the AnySubrects bit is set, a single byte follows giving the number of subrectangles
- If the SubrectsColored bit is set, each subrectangle is preceded by a pixel value giving the color of that subrectangle, resulting in:
```
+---------------+--------------+---------------------+
| No. of bytes  | Type [Value] | Description         |
+---------------+--------------+---------------------+
| bytesPerPixel | PIXEL        | subrect-pixel-value |
| 1             | U8           | x-and-y-position    |
| 1             | U8           | width-and-height    |
+---------------+--------------+---------------------+
```
else the foreground color is used:
```
+--------------+--------------+------------------+
| No. of bytes | Type [Value] | Description      |
+--------------+--------------+------------------+
| 1            | U8           | x-and-y-position |
| 1            | U8           | width-and-height |
+--------------+--------------+------------------+
```
`x-and-y-position`: 4 bits X position, 4 bits Y position (reversed on LE)
`width-and-height`: 4 bits (width - 1), 4 bits (height - 1) (reversed on LE)

## TRLE (15)
Rectangles are split up into 16px\*16px tiles, from left-to-right, top-to-bottom
If the width/height isn't dividable by 16, the last tile/row will be smaller
TRLE uses a CPIXEL (compressed pixel), which is the same as a PIXEL, except when True-Color != 0, BPP=32, Depth<=24,rgb bits fit in 3 bytes (which should be depth <= 24), => cpixels are only 3 bytes long

Each tile begins with a subencoding type byte.
The top bit is set if the tile has been run-length encoded
The other 7 bits indicate the size of the palette:
- 0: no palette
- 1: tile is single color
- 2-127 indicate the palette size
- 127 and 129 indicate that the palette is to be reused

Subencoding types:
0: Raw, `width*height` pixel data follows
```
+-----------------------------+--------------+-------------+
| No. of bytes                | Type [Value] | Description |
+-----------------------------+--------------+-------------+
| width*height*BytesPerCPixel | CPIXEL array | pixels      |
+-----------------------------+--------------+-------------+
```
1: Solid tile consisting of a single color. The pixel value follows:
```
+----------------+--------------+-------------+
| No. of bytes   | Type [Value] | Description |
+----------------+--------------+-------------+
| bytesPerCPixel | CPIXEL       | pixelValue  |
+----------------+--------------+-------------+
```
2-16: Packed palette. Palette size is the value of the subencoding, followed by the palette consisting of pixel values. Packed pixels follow with each pixel represented as a index (the index being 1-bit for palette size 2, 2-bits for 3-4, 4-bits for 5-16):
If the tile is not a multiple of 8, 4, or 2, padding bits are used to align each row to an exact number of bytes.
```
+----------------------------+--------------+--------------+
| No. of bytes               | Type [Value] | Description  |
+----------------------------+--------------+--------------+
| paletteSize*bytesPerCPixel | CPIXEL array | palette      |
| m                          | U8 array     | packedPixels |
+----------------------------+--------------+--------------+
```
	Where m is depending on the palette size:
	- 2: `(width+7)/8 * height`
	- 3-4: `(width+3)/4 * height`
	- 5-16: `(width+1)/2 * height`
17-126: Unused
127: Reuse palette from previous type. Followed by packed pixels as described above.
128: Plain RLE. Data consits of a number of runs, repeated till the tile ends. Each run is represented by a pixel value followed by the length of the run. The length is represented as one or more bytes. The end is indicated by a value other than 255.
```
+-------------------------+--------------+-----------------------+
| No. of bytes            | Type [Value] | Description           |
+-------------------------+--------------+-----------------------+
| bytesPerCPixel          | CPIXEL       | pixelValue            |
| div(runLength - 1, 255) | U8 array     | 255                   |
| 1                       | U8           | (runLength-1) mod 255 |
+-------------------------+--------------+-----------------------+
```
Ex: Length 1: `[0]`, 256: `[255,0]`, 510: `[255,254]`
129: Reuse palette from previous type (RLE). Followed by a number of runs as described below.
130-255: Palette RLE. Palette size is `subencoding - 128`.
```
+----------------------------+--------------+-------------+
| No. of bytes               | Type [Value] | Description |
+----------------------------+--------------+-------------+
| paletteSize*bytesPerCPixel | CPIXEL array | palette     |
+----------------------------+--------------+-------------+
```
Following the palette is a number of runs repeated till the tile is done. A run of length one is represented by a palette index:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8           | paletteIndex |
+--------------+--------------+--------------+
```
Otherwise it's represented by a palette index with the top bit set, followed by the length of the run as for plain RLE:
```
+-------------------------+--------------+-----------------------+
| No. of bytes            | Type [Value] | Description           |
+-------------------------+--------------+-----------------------+
| 1                       | U8           | paletteIndex + 128    |
| div(runLength - 1, 255) | U8 array     | 255                   |
| 1                       | U8           | (runLength-1) mod 255 |
+-------------------------+--------------+-----------------------+
```

## ZRLE (16)
Same as TRLE, but with tiles of size `64px*64px` and without the reuse functionality (subencoding `127` and `129`).
Additionally the data is zlib deflated, so the data looks like this:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 4            | U32          | length      |
| length       | U8 array     | zlibData    |
+--------------+--------------+-------------+
```
The zlib stream is connection bound (one stream object per connection). The server should flush the zlib stream between rectangles.