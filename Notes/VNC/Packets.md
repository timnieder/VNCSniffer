Package Graph in final doc? (like https://upload.wikimedia.org/wikipedia/commons/thumb/5/55/TCP_CLOSE.svg/1280px-TCP_CLOSE.svg.png)

Hex view in latex idea:
![[Pasted image 20230201141438.png]]
So like this:
![[Pasted image 20230201142445.png]]

# [Handshake](https://www.rfc-editor.org/rfc/rfc6143#section-7.1)
## ProtocolVersion
Select compatible protocols
Order: S, C
S:
```
0000   52 46 42 20 30 30 33 2e 30 30 38 0a               RFB 003.008.
```
Parsed:
```
protocol-version: RFB 003.008\n
```
C:
```
0000   52 46 42 20 30 30 33 2e 30 30 38 0a               RFB 003.008.
```
Parsed:
```
protocol-version: RFB 003.008\n
```
## Security
Select supported security type
Order: S, C, ... (depending on the Security type)
S:
```
0000   01 02                                             ..
```
Format:
```
+--------------------------+-------------+--------------------------+
| No. of bytes             | Type        | Description              |
|                          | [Value]     |                          |
+--------------------------+-------------+--------------------------+
| 1                        | U8          | number-of-security-types |
| number-of-security-types | U8 array    | security-types           |
+--------------------------+-------------+--------------------------+
```
Parsed:
```
number-of-security-types: 1
security-types: [VNC Authentication (2)]
```
Supported security types are:
```
+--------+--------------------+
| Number | Name               |
+--------+--------------------+
| 0      | Invalid            |
| 1      | None               |
| 2      | VNC Authentication |
+--------+--------------------+
```

If the number of security types is zero, the connection failed. The server sends the reason and closes the connection:
S:
```
0000   00 00 00 14 75 6e 73 75 70 70 6f 72 74 65 64 20   ....unsupported
0010   70 72 6f 74 6f 63 6f 6c 21                        protocol!
```
Format:
```
+---------------+--------------+---------------+
| No. of bytes  | Type [Value] | Description   |
+---------------+--------------+---------------+
| 4             | U32          | reason-length |
| reason-length | U8 array     | reason-string |
+---------------+--------------+---------------+
```
Parsed:
```
reason-length: 21
reason-string: "unsupported protocol!"
```

C:
```
0000   02                                                .
```
Format:
```
+--------------+--------------+---------------+
| No. of bytes | Type [Value] | Description   |
+--------------+--------------+---------------+
| 1            | U8           | security-type |
+--------------+--------------+---------------+
```
The in the RFB specified security types are:
```
  +--------+--------------------+
  | Number | Name               |
  +--------+--------------------+
  | 0      | Invalid            |
  | 1      | None               |
  | 2      | VNC Authentication |
  +--------+--------------------+
```
Parsed:
```
security-type: VNC Authentication (2)
```

### [VNC Auth](https://www.rfc-editor.org/rfc/rfc6143#section-7.2.2)
Authenticate through Challenge Response
Order: S, C
S:
```
0000   8c 9d 38 ef 5c 76 cc 6c 7a 05 00 c6 f0 28 ce 18   ..8.\v.lz....(..
```
Format:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 16           | U8           | challenge   |
+--------------+--------------+-------------+
```
Parsed:
```
challenge: \b"8c 9d 38 ef 5c 76 cc 6c 7a 05 00 c6 f0 28 ce 18"
```

The client encrypts the challenge with DES using the password as the key. The password is truncated to eight characters or padded with null bytes on the right, resulting in 16 bytes.

C:
```
0000   ce 23 4e 99 84 49 27 13 e4 a9 e8 5c fb 9f bb d1   .#N..I'....\....
```
Format:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 16           | U8           | response    |
+--------------+--------------+-------------+
```
Parsed:
```
response: \b"ce 23 4e 99 84 49 27 13 e4 a9 e8 5c fb 9f bb d1"
```

## SecurityResult
Was the Security Handshake successful

S:
```
0000   00 00 00 00                                       ....
```
Format:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 4            | U32          | status:     |
|              | 0            | OK          |
|              | 1            | failed      |
+--------------+--------------+-------------+
```
Parsed:
```
status: OK (0)
```

If unsuccessful, the server sends a reason for the failure:
```
0000   00 00 00 16 70 61 73 73 77 6f 72 64 20 63 68 65   ....password che
0010   63 6b 20 66 61 69 6c 65 64 21                     ck failed!
```
Format:
```
+---------------+--------------+---------------+
| No. of bytes  | Type [Value] | Description   |
+---------------+--------------+---------------+
| 4             | U32          | reason-length |
| reason-length | U8 array     | reason-string |
+---------------+--------------+---------------+
```
Parsed:
```
reason-length: 22
reason-string: "password check failed!"
```

# [Initialization](https://www.rfc-editor.org/rfc/rfc6143#section-7.3)
## ClientInit
Client options like exclusive access
C:
```
0000   01                                                .
```
Format:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 1            | U8           | shared-flag |
+--------------+--------------+-------------+
```
## ServerInit
Server options like width, height, pixel format, name
S:
```
0000   03 20 02 58 20 20 00 ff 00 ff 00 ff 00 ff 10 08   . .X  ..........
0010   00 00 00 00 00 00 00 05 47 4e 4f 4d 45            ........GNOME
```
Format:
```
+--------------+--------------+------------------------------+
| No. of bytes | Type [Value] | Description                  |
+--------------+--------------+------------------------------+
| 2            | U16          | framebuffer-width in pixels  |
| 2            | U16          | framebuffer-height in pixels |
| 16           | PIXEL_FORMAT | server-pixel-format          |
| 4            | U32          | name-length                  |
| name-length  | U8 array     | name-string                  |
+--------------+--------------+------------------------------+
```
The Pixel Format data structure is defined as:
```
+--------------+--------------+-----------------+
| No. of bytes | Type [Value] | Description     |
+--------------+--------------+-----------------+
| 1            | U8           | bits-per-pixel  |
| 1            | U8           | depth           |
| 1            | U8           | big-endian-flag |
| 1            | U8           | true-color-flag |
| 2            | U16          | red-max         |
| 2            | U16          | green-max       |
| 2            | U16          | blue-max        |
| 1            | U8           | red-shift       |
| 1            | U8           | green-shift     |
| 1            | U8           | blue-shift      |
| 3            |              | padding         |
+--------------+--------------+-----------------+
```
Parsed:
```
framebuffer-width in pixels: 800
framebuffer-height in pixels: 600
server-pixel-format: 
	bits-per-pixel: 32
	depth: 32
	big-endian-flag: False
	true-color-flag: True
	red-max: 255
	green-max: 255
	blue-max: 255
	red-shift: 16
	green-shift: 8
	blue-shift: 0
name-length: 5
name-string: "GNOME"
```

# [Client to Server](https://www.rfc-editor.org/rfc/rfc6143#section-7.5)
## SetPixelFormat
Sets the format in which pixel values should be sent in FramebufferUpdate
C:
```
0000   00 00 00 00 20 18 00 01 00 ff 00 ff 00 ff 10 08   .... ...........
0010   00 00 00 00                                       ....
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [0]       | message-type |
| 3            |              | padding      |
| 16           | PIXEL_FORMAT | pixel-format |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: SetPixelFormat (0)
pixel-format: 
	bits-per-pixel: 32
	depth: 24
	big-endian-flag: False
	true-color-flag: True
	red-max: 255
	green-max: 255
	blue-max: 255
	red-shift: 16
	green-shift: 8
	blue-shift: 0
```

## SetEncodings
Set the encoding in which pixel data can be sent
C:
```
0000   02 00 00 09 00 00 00 00 00 00 00 10 00 00 00 07   ................
0010   00 00 00 05 00 00 00 02 ff ff ff 21 ff ff ff 20   ...........!... 
0020   ff ff ff 18 ff ff ff 11                           ........
```
Format:
```
+--------------+--------------+---------------------+
| No. of bytes | Type [Value] | Description         |
+--------------+--------------+---------------------+
| 1            | U8 [2]       | message-type        |
| 1            |              | padding             |
| 2            | U16          | number-of-encodings |
+--------------+--------------+---------------------+
Followed by number-of-encodings times:
+--------------+--------------+---------------+
| No. of bytes | Type [Value] | Description   |
+--------------+--------------+---------------+
| 4            | S32          | encoding-type |
+--------------+--------------+---------------+
```
Parsed:
```
message-type: SetEncodings (2)
number-of-encodings: 9
encoding-type: Raw (0)
encoding-type: ZRLE (16)
encoding-type: Tight (7)
encoding-type: Hextile (5)
encoding-type: RRE (2)
encoding-type: DesktopSize (-223)
encoding-type: LastRect (-224) => not defined in standard RFC
encoding-type: Pointer pos (-232) => not defined in standard RFC
encoding-type: Cursor (-239)
```

## FramebufferUpdateRequest
Update specific area request
C:
```
0000   03 00 00 00 00 00 03 20 02 58                     ....... .X
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [3]       | message-type |
| 1            | U8           | incremental  |
| 2            | U16          | x-position   |
| 2            | U16          | y-position   |
| 2            | U16          | width        |
| 2            | U16          | height       |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: FramebufferUpdateRequest (3)
incremental: False
x-position: 0
y-position: 0
width: 800
height: 600
```

## KeyEvent
Key press or release
C:
```
0000   04 01 00 00 00 00 00 61                           .......a
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [4]       | message-type |
| 1            | U8           | down-flag    |
| 2            |              | padding      |
| 4            | U32          | key          |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: KeyEvent (4)
down-flag: Yes (1)
key: 'a' (0x00000061)
```

## PointerEvent
Pointer movement or button press/release
C:
```
0000   05 00 01 67 01 40                                 ...g.@
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [5]       | message-type |
| 1            | U8           | button-mask  |
| 2            | U16          | x-position   |
| 2            | U16          | y-position   |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: PointerEvent (5)
button-mask: 0b00000000
x-position: 359
y-position: 320
```

## ClientCutText
Tells the server that text was cut
C:
```
0000   06 00 00 00 00 00 00 04 74 65 78 74               ........text
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [6]       | message-type |
| 3            |              | padding      |
| 4            | U32          | length       |
| length       | U8 array     | text         |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: ClientCutText (6)
length: 4
text: "text"
```


# [Server to Client](https://www.rfc-editor.org/rfc/rfc6143#section-7.6)
## FramebufferUpdate
Send pixel data to client

S:
```
0000   00 00 00 03 00 03 00 03 00 08 00 07 ff ff ff 11   ................
	   ...
00f0   e7 e7 7e 3c 7e e7 e7 00 00 00 00 00 00 00 00 ff   ..~<~...........
0100   ff ff 18 00 00 00 00 03 20 02 58 00 00 00 00 1f   ........ .X.....
	   ...
```
Format:
```
+--------------+--------------+----------------------+
| No. of bytes | Type [Value] | Description          |
+--------------+--------------+----------------------+
| 1            | U8 [0]       | message-type         |
| 1            |              | padding              |
| 2            | U16          | number-of-rectangles |
+--------------+--------------+----------------------+
Followed by number-of-rectangles rectangles, which start with a rectangle header:
+--------------+--------------+---------------+
| No. of bytes | Type [Value] | Description   |
+--------------+--------------+---------------+
| 2            | U16          | x-position    |
| 2            | U16          | y-position    |
| 2            | U16          | width         |
| 2            | U16          | height        |
| 4            | S32          | encoding-type |
+--------------+--------------+---------------+
Followed by the pixel data
```
Parsed:
```
message-type: FrambufferUpdate (0)
number-of-rectangles: 3
Rectangle 1:
	x-position: 3
	y-position: 3
	width: 8
	height: 7
	encoding-type: Cursor (-239)
		Followed by pixel data
Rectangle 3:
	x-position: 0
	y-position: 0
	width: 0
	height: 0
	encoding-type: Pointer pos (-232)
Rectangle 3:
	x-position: 0
	y-position: 0
	width: 800
	height: 600
	encoding-type: Raw (0)
		Followed by pixel data
```


## SetColorMapEntries
Set (parts of) the color map which specify which 16 bit value is mapped to which color

S:
```
0000   01 00 00 00 00 01 ff ff ff ff ff ff               ............
```
Format:
```
+--------------+--------------+------------------+
| No. of bytes | Type [Value] | Description      |
+--------------+--------------+------------------+
| 1            | U8 [1]       | message-type     |
| 1            |              | padding          |
| 2            | U16          | first-color      |
| 2            | U16          | number-of-colors |
+--------------+--------------+------------------+
Followed by number-of-colors times:
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 2            | U16          | red         |
| 2            | U16          | green       |
| 2            | U16          | blue        |
+--------------+--------------+-------------+
```
Parsed:
```
message-type: SetColorMapEntries (1)
first-color: 0
number-of-colors: 1
Color 1:
	red: 65535
	green: 65535
	blue: 65535
```


## Bell
Produce bell sound on client

S:
```
0000   02                                                .
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [2]       | message-type |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: Bell (2)
```

## ServerCutText
Server has new text in its cut buffer

S:
```
0000   03 00 00 00 00 00 00 04 74 65 78 74               ........text
```
Format:
```
+--------------+--------------+--------------+
| No. of bytes | Type [Value] | Description  |
+--------------+--------------+--------------+
| 1            | U8 [3]       | message-type |
| 3            |              | padding      |
| 4            | U32          | length       |
| length       | U8 array     | text         |
+--------------+--------------+--------------+
```
Parsed:
```
message-type: ServerCutText (3)
length: 4
text: "text"
```