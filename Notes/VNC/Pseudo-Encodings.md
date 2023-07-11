## Cursor/RichCursor (-239)
Defines the position and shape of the cursor (as a bitmap)
```
+----------------------------+--------------+---------------+
| No. of bytes               | Type [Value] | Description   |
+----------------------------+--------------+---------------+
| width*height*bytesPerPixel | PIXEL array  | cursor-pixels |
| div(width+7,8)*height      | U8 array     | bitmask       |
+----------------------------+--------------+---------------+
```
The bitmask is parsed from left-to-right, top-to-bottom scan lines where each scan line is padded to a whole number of bytes. A bit set to 1 means the pixel of the cursor is valid.

## DesktopSize/NewFBSize (-223)
Changes the framebuffer size. DesktopSize is always the last rectangle in an update. The x and y positions are ignored, using the width and height as the new framebuffer's size.