https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst

# Security Types
## Tight (16)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#tight-security-type
Allows for:
- Tunneling of Data
- Authentication
- Server capabilities

The server starts sending a list of supported tunnels:
```
+--------------------------+---------------------+------------------+
| No. of bytes             | Type                | Description      |
|                          | [Value]             |                  |
+--------------------------+---------------------+------------------+
| 4                        | U32                 | number-of-tunnels|
| number-of-tunnels*16     | `CAPABILITY` array  | tunnel           |
+--------------------------+---------------------+------------------+
```
`CAPABILITY is defined as
```
No. of bytes                Type                Description
1                           ``S32``             *code*
4                           ``U8 array``        *vendor*
8                           ``U8 array``        *signature*
```
The following tunnel capabilities are registered:
```
Code  Vendor Signature  Description
0     "TGHT" "NOTUNNEL" No tunneling
```

If `number-of-tunnels` is non-zero, the client has to request a tunnel from the list:
```
No. of bytes Type Description
4            S32  code
```
Else the server carries on with the next request.
```
+--------------------------+---------------------+------------------+
| No. of bytes             | Type                | Description      |
|                          | [Value]             |                  |
+--------------------------+---------------------+------------------+
| 4                        | U32                 | number-of-auth-types|
| number-of-auth-types*16  | `CAPABILITY` array  | auth-type        |
+--------------------------+---------------------+------------------+
```

The following authentication capabilities are registered:
```
Code  Vendor Signature  Description
1     "STDV" "NOAUTH__" None
2     "STDV" "VNCAUTH_" VNC Authentication
19    "VENC" "VENCRYPT" VeNCrypt
20    "GTKV" "SASL____" Simple Authentication and Security Layer
129   "TGHT" "ULGNAUTH" Tight Unix Login Authentication
130   "TGHT" "XTRNAUTH" External Authentication
```

If `number-of-auth-types` is non-zero, the client has to request a authentication type from the list:
```
No. of bytes Type Description
4            S32  code
```
Else the server carries on with the SecurityResult message.

The server then sends messages depending on the selected authentication type.
Note that the ServerInit message is extended if the Tight Security Type was selected:
```
No. of bytes    Type        [Value]    Description
2               ``U16``                *number-of-server-messages*
2               ``U16``                *number-of-client-messages*
2               ``U16``                *number-of-encodings*
2               ``U16``     0          *padding*
number-of-server-messages*16 ``CAPABILITY`` *server-message*
number-of-client-messages*16 ``CAPABILITY`` *client-message*
number-of-encodings*16 ``CAPABILITY`` *encoding*
```
The following server-message capabilties are registered
```
Code    Vendor      Signature       Description
130     "``TGHT``"  "``FTS_LSDT``"  File List Data
131     "``TGHT``"  "``FTS_DNDT``"  File Download Data
132     "``TGHT``"  "``FTS_UPCN``"  File Upload Cancel
133     "``TGHT``"  "``FTS_DNFL``"  File Download Failed
150     "``TGHT``"  "``CUS_EOCU``"  EndOfContinuousUpdates
253     "``GGI_``"  "``GII_SERV``"  gii Server Message
```
The following client-message capabilties are registered
```
Code    Vendor      Signature       Description
130     "``TGHT``"  "``FTC_LSRQ``"  File List Request
131     "``TGHT``"  "``FTC_DNRQ``"  File Download Request
132     "``TGHT``"  "``FTC_UPRQ``"  File Upload Request
133     "``TGHT``"  "``FTC_UPDT``"  File Upload Data
134     "``TGHT``"  "``FTC_DNCN``"  File Download Cancel
135     "``TGHT``"  "``FTC_UPFL``"  File Upload Failed
136     "``TGHT``"  "``FTC_FCDR``"  File Create Directory Request
150     "``TGHT``"  "``CUC_ENCU``"  EnableContinuousUpdates
151     "``TGHT``"  "``VRECTSEL``"  Video Rectangle Selection
253     "``GGI_``"  "``GII_CLNT``"  gii Client Message
```
The following encoding capabilties are registered
```
0       "``STDV``"  "``RAW_____``"  Raw Encoding
1       "``STDV``"  "``COPYRECT``"  CopyRect Encoding
2       "``STDV``"  "``RRE_____``"  RRE Encoding
4       "``STDV``"  "``CORRE___``"  CoRRE Encoding
5       "``STDV``"  "``HEXTILE_``"  Hextile Encoding
6       "``TRDV``"  "``ZLIB____``"  ZLib Encoding
7       "``TGHT``"  "``TIGHT___``"  Tight Encoding
8       "``TRDV``"  "``ZLIBHEX_``"  ZLibHex Encoding
-32     "``TGHT``"  "``JPEGQLVL``"  JPEG Quality Level
                                    Pseudo-encoding
-223    "``TGHT``"  "``NEWFBSIZ``"  DesktopSize Pseudo-encoding (New
                                    FB Size)
-224    "``TGHT``"  "``LASTRECT``"  LastRect Pseudo-encoding
-232    "``TGHT``"  "``POINTPOS``"  Pointer Position
-239    "``TGHT``"  "``RCHCURSR``"  Cursor Pseudo-encoding (Rich
                                    Cursor)
-240    "``TGHT``"  "``X11CURSR``"  X Cursor Pseudo-encoding
-256    "``TGHT``"  "``COMPRLVL``"  Compression Level
                                    Pseudo-encoding
-305    "``GGI_``"  "``GII_____``"  gii Pseudo-encoding
-512    "``TRBO``"  "``FINEQLVL``"  JPEG Fine-Grained Quality Level
                                    Pseudo-encoding
-768    "``TRBO``"  "``SSAMPLVL``"  JPEG Subsampling Level
                                    Pseudo-encoding
```

## MSLogon/LegacyMSLogon (0xfffffffa)
## MSLogon2 (0x71/113)
LibVNC C: https://github.com/LibVNC/libvncserver/blob/master/src/libvncclient/rfbclient.c#L750-L803
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#mslogonii-authentication
Ultra S: https://github.com/ultravnc/UltraVNC/blob/7ee5dddd693c0e2f04ea7bf9dc2aeb9b11a94fb1/winvnc/winvnc/vncclient.cpp#L1715C18-L1757
Ultra C: https://github.com/ultravnc/UltraVNC/blob/7ee5dddd693c0e2f04ea7bf9dc2aeb9b11a94fb1/vncviewer/ClientConnection.cpp#L3363-L3422

server:
```
8 U64 generator
8 U64 modulus
8 U64 public-value
```
=> key = (public-value ^ priv % modulus) (diffie hellman)
DES in CBC, fields utf8, random data padded to 256/64 bytes. same des algo as vnc auth => rfbEncryptBytes2 is DES CBC

```
8 U8 array public-value
256 U8 array encrypted-username
64 U8 array encrypted-password
```
server then uses public key to generate shared secret

either legacy mslogon was deprecated or mslogon1=mslogon2

advantages:
- allows to specify username and password
- allows passwords bigger than 8 chars (64)
problem: 
- still only uses 64bit key + DES
- there are better modes than cbc

## VeNCrypt (19)
https://github.com/TigerVNC/tigervnc/blob/57cdcedf1bde06195f782fd2cfcf302050245da6/common/rfb/CSecurityVeNCrypt.cxx
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#vencrypt

Wrapper around subtypes
#### Subtype version exchange
Server: sends highest version
Client: sends highest version
```
1 u8 major version
2 u8 minor version
```
Server: version response, 0 = ok
```
1 u8 ack
```

server: sends supported subtypes:
```
1 u8 number-of-subtypes
number-of-subtypes*4 u32 array subtypes
```

subtypes:
```
256 	Plain 	Plain authentication (should be never used)
257 	TLSNone 	TLS encryption with no authentication
258 	TLSVnc 	TLS encryption with VNC authentication
259 	TLSPlain 	TLS encryption with Plain authentication
260 	X509None 	X509 encryption with no authentication
261 	X509Vnc 	X509 encryption with VNC authentication
262 	X509Plain 	X509 encryption with Plain authentication
263 	TLSSASL 	TLS encryption with SASL authentication
264 	X509SASL 	X509 encryption with SASL authentication
```

client: responds with the selected one
```
4 U32 selected subtype
```
The server then responds with a ack like above and continues, if no error occured, with the subtype

#### Subtypes with TLS/X509 Prefix
Starts with TLS handshake using a anonymous x509 cert (TLS prefix) or valid x509 certificate. After the negotiation all traffic is sent via this encrypted channel.

tls handshake uses default gnu tsl/openssl implementations through vnc channel

#### Subtypes with None suffix
After TLS handshake, continues with the SecurityResult message

#### Subtypes with Plain suffix
Client sends username+pw:
```
4 U32 username-length
4 U32 password-length
username-length U8 array username
password-length U8 array password
```
If the credentials are correct, the server continues with the SecurityResult message

#### Subtypes with VNC suffix
Continues with VNC auth



## SASL (20)
Bro idfk. this shit complex

## ARD/Diffie-Hellman (30)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#diffie-hellman-authentication
Server sends parameters:
```
2 U16 generator
2 U16 key-size
key-size U8 array prime-modulus
key-size U8 array public-value
```
Client generates shared secret with parameters using diffie hellman
Then encrypts username and password using AES ECB. both fields are encoded using UTF-8, null terminated, padded with random data to 64 bytes. concat and encrypt using the md5 hashes shared secret as a aes key


Client sends encrypted credentials and public value:
```
128 U8 array encrypted-credentials
key-size U8 array public value
```

After that continues with the SecurityResult message


## RSA AES/RA2 (5)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#rsa-aes-security-type
### Public key
Server: sends public key
```
4 U32 server-key-length
ceil(server-key-length / 8) U8 array modulus
ceil(server-key-length / 8) U8 array public exponent
```
Client: Verify public key + send public key
```
4 U32 client-key-length
ceil(client-key-length / 8) U8 array modulus
ceil(client-key-length / 8) U8 array public exponent
```

### Random
random number length = ceil(key-length / 8) = 16 bytes in RA2
Client: sends with server public key encrypted random number
Server: sends with client public key encrypted random number
```
2 U16 key-size
key-size U8 array encrypted random number
```

#TODO: rfbproto.rst says server reads first, tigervnc source says otherwise => doesn't matter honestly as both read+write at the same time

Both random numbers are decrypted using the own private key. After that the session key is calculated using
```
ClientSessionKey = the first 16 bytes of SHA1(ServerRandom || ClientRandom)
ServerSessionKey = the first 16 bytes of SHA1(ClientRandom || ServerRandom)
```

After that all messages will be encrypted with the aes-eax mode (aes-ctr+cmac). For each message there is a header:
```
2 U16 message-length
message-length U8 array encrypted message
16 U8 array MAC generated by the AES-EAX mode
```

### Hash
```
ClientHash = SHA1(ClientPublicKey || ServerPublicKey)
ServerHash = SHA1(ServerPublicKey || ServerClientKey)
```
Client: sends hash
Server: sends hash
```
20 U8 array server/client hash
```

#TODO: rfbproto.rst says server reads first, tigervnc source says otherwise => doesn't matter honestly as both read+write at the same time

Client and server should compare the received hashs with one it computes

### Subtype
Server: sends subtype
```
1 U8 array subtype
```
supported subtypes:
```
1 username+password
2 password
```
client: sends credentials (utf8-encoded)
```
1 U8 length-username (0 if subtype=2)
length-username U8 array username
1 U8 length-password
length-password U8 array password
```

after that the connection continues encrypted with the SecurityResult message

### Advantages:
- encrypts security + connection
- forward secrecy by using session keys (as you need session keys to decrypt, youd need both private keys to read traffic afterwards (as you need both random numbers to get session key))
Disadvantages:
- session key only 16 bytes?
- rsa? EME-PKCS1-v1_5?

## RSA-AES Unencrypted/RA2ne (6)
Identical to RA2 except that only the Security Handshake is encrypted. The SecurityResult message and all following data is unencrypted

## RSA-AES Two-Step/RA2r (13)
Identical to RA2 except that at the end of the security handshake (after the server receives the credentials) a new round of key derivation happens by sending 2 new random numbers and generating a new pair of sesion keys. The random numbers are encrypted with AES-EAX instead of RSA

Advantages:
- Server can only use session keys for security? => but could just read traffic and generate new keys?? => only works if the attacker gets hold of the

## RSA-AES 256/RA256 (129)
Identical to RA2 except that SHA256 is used
```
ClientSessionKey = SHA256(ServerRandom || ClientRandom)
ServerSessionKey = SHA256(ClientRandom || ServerRandom)
ServerHash = SHA256(ServerPublicKey || ClientPublicKey)
ClientHash = SHA256(ClientPublicKey || ServerPublicKey)
```

## RSA-AES 256 Unencrypted/RA256ne (130)
Same as RA2ne except SHA256 is used (like RA256)

## RSA-AES-256 Two-Step/RA256r (133)
Same as RA2r except SHA256 is used (lika RA256)

# Encodings
## CoRRE (4)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#corre-encoding
Similiar to RRE, but pos and size of a rectangle is limited to 255 pixels.
Header:
```
+---------------+--------------+-------------------------+
| No. of bytes  | Type [Value] | Description             |
+---------------+--------------+-------------------------+
| 4             | U32          | number-of-subrectangles |
| bytesPerPixel | PIXEL        | background-pixel-value  |
+---------------+--------------+-------------------------+
```
Followed by `number-of-subrectangles` subrectangles:
```
+---------------+--------------+---------------------+
| No. of bytes  | Type [Value] | Description         |
+---------------+--------------+---------------------+
| bytesPerPixel | PIXEL        | subrect-pixel-value |
| 1             | U8           | x-position          |
| 1             | U8           | y-position          |
| 1             | U8           | width               |
| 1             | U8           | height              |
+---------------+--------------+---------------------+
```
## zlib (6)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#zlib-encoding
Sends zlib compressed raw encoded rectangles. One stream object per Connection
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 4            | U32          | length      |
| length       | U8 array     | zlibData    |
+--------------+--------------+-------------+
```
## TODO: Tight (7)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#tight-encoding


## zlibhex (8)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#zlibhex-encoding
Extends the Hextile encoding by optionally compressing subrectangles. One stream object for "Raw" subrectangles and one for all other subrectangles.

The subencoding field is extended by:
```
+--------------+--------------+---------------------+
| No. of bytes | Type [Value] | Description         |
+--------------+--------------+---------------------+
| 1            | U8           | subencoding-mask:   |
|              | [32]         | ZlibRaw             |
|              | [64]         | Zlib                |
+--------------+--------------+---------------------+
```
If either one of those subencodings is set, the subrectangle is zlib compressed:
```
+--------------+--------------+-------------+
| No. of bytes | Type [Value] | Description |
+--------------+--------------+-------------+
| 2            | U16          | length      |
| length       | U8 array     | zlibData    |
+--------------+--------------+-------------+
```

The ZlibRaw bit cancels all other bits like the raw bit. The uncompressed subrectangle should be interpreted as Raw data.
With the Zlib bit the uncompressed subrectangle should be interpreted according to the lower 5 bits of the subencodings.
Else the normal hextile rules should be followed.

## TODO: Ultra (9)

## TODO: UltraZip (0xFFFF0009)

## TODO: ZYWRLE (17)
https://github.com/LibVNC/libvncserver/blob/master/src/libvncclient/zrle.c#L255-L266
https://sourceforge.net/p/vnc-tight/mailman/message/12941855/
Uses the ZRLE, except when sending Raw tiles?

# Pseudo-Encodings
## KeyboardLedState (0xFFFE0000)
https://github.com/LibVNC/libvncserver/blob/master/src/libvncclient/rfbclient.c#L2066-L2074
State is contained in `rect.x`

## Supported Messages (0xFFFE0001)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L588-L591
Server sends the supported messages
```
No. of bytes    Type                       Description
32              U8                         *client2server messages*
32              U8                         *server2client messages*
```
`rect->w` contains byte count
Bitflags indicate what messages are supported (max of 256 message types)

## Supported Encodings (0xFFFE0002)
https://github.com/LibVNC/libvncserver/blob/master/src/libvncserver/rfbserver.c#L1030-L1100
Server sends the supported encodings
```
No. of bytes    Type                       Description
4*rect.h        U32 Array                  *encodings*
```
`rect->w` contains byte count. `rect->h` the number of encodings

## Server Identity (0xFFFE0003)
https://github.com/LibVNC/libvncserver/blob/master/src/libvncclient/rfbclient.c#L2162-L2175
Server sends its identity
```
No. of bytes    Type                       Description
rect.w          U8 Array                   *server-identity*
```
`server-identity` is the server name

## PointerPos (-232)
https://github.com/ultravnc/UltraVNC/blob/main/vncviewer/ClientConnection.cpp#L5827-L5831
Moves the software pointer to the position `x`,`y`

## XCursor (-240)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#x-cursor-pseudo-encoding
Similiar to RichCursor, sends the cursor shape, but only with two colors
```
No. of bytes           Type                            Description
1                      ``U8``                          *primary-r*
1                      ``U8``                          *primary-g*
1                      ``U8``                          *primary-b*
1                      ``U8``                          *secondary-r*
1                      ``U8``                          *secondary-g*
1                      ``U8``                          *secondary-b*
div(width+7,8)*height  ``U8`` array                    *bitmap*
div(width+7,8)*height  ``U8`` array                    *bitmask*
```
Bitmap: each bit represents a pixel, 1 meaning primary color, 0 secondary
Bitmask: each bit represents a pixel, 1 meaning draw pixel, 0 pixel is transparent

## Extended Desktop Size (-308)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#extendeddesktopsize-pseudo-encoding
The server has changed the desktop size. Extended version of DesktopSize.
`X` indicates the reason:
```
Code    Description
0       The screen layout was changed via non-RFB means on the server.
1       The client receiving this message request a change.
2       Another client request a change and the server approved it.
```
`Y` indicates the status code:
```
Code    Description
0       No error
1       Resize is administratively prohibited
2       Out of resources
3       Invalid screen layout
4       Request forwarded (might complete asyncronously)
```
`width` and `height` indicate the new width and height of the framebuffer
Data:
```
No. of bytes                Type                Description
1                           ``U8``              *number-of-screens*
3                                               *padding*
*number-of-screens* * 16    ``SCREEN`` array    *screens*
```
Followed by `number-of-screens` of:
```
No. of bytes    Type                            Description
4               ``U32``                         *id*
2               ``U16``                         *x-position*
2               ``U16``                         *y-position*
2               ``U16``                         *width*
2               ``U16``                         *height*
4               ``U32``                         *flags*
```

Gives the client info about the screens used to give multi screen support

## Extended View Size (-307)
Overlaps with DesktopName pseudo?
https://github.com/ultravnc/UltraVNC/blob/ee9954b90ab6b52a2332b349d55f6a98af3f7424/vncviewer/ClientConnection.cpp#L5792-L5803
https://github.com/ultravnc/UltraVNC/blob/ee9954b90ab6b52a2332b349d55f6a98af3f7424/winvnc/winvnc/vncclient.cpp#L5169-L5240 => alternative to extdesktopsize?
Allows the server to use multiple displays?
`rect.x` and `rect.y` are the x,y offset of the display
`rect.w` and `rect.h` are the size of the display

## QemuExtendedKeyEvent (-258)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#qemu-extended-key-event-pseudo-encoding
Client indicates that its able to send raw keycodes. Alternatively it can use the QEMU Extended Key Event message instead of the usual KeyEvent message

## LastRect (-224)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#lastrect-pseudo-encoding
Notifies the client that this is the last rectangle in a framebuffer update.

## xvp (-309)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#xvp-pseudo-encoding
Declares that a client is able to use the xvp extension. See xvp C2S

## ExtendedClipboard (0xC0A1E5CE)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2746-L2751
Indicates that a client is able to use the ExtendedClipboard extension. See ExtendedClientCut C2S

## ServerState (0xFFFF8000)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2712-L2717
Indicates that the client supports the ServerState extension and wants updates. See S2C ServerState.

## EnableKeepAlive (0xFFFF8001)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2725-L2731
Indicates that the client supports the KeepAlive extension and wants updates. See C2S KeepAlive.

## FTProcolVersion (0xFFFF8002)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2739-L2743
Indicates that the client supports the FTProtocolVersion extension. See C2S FileTransfer FileTransferProtocolVersion.

## PseudoSession (0xFFFF8003)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2719-L2723
Enables `m_client->m_session_supported` (the session extension?). Use unknown. Related to Request/SetSession C2S?

## EnableIdleTime (0xFFFF8004)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2733-L2737
Indicates that the client supports the IdleTime extension and wants updates. See S2C ServerState.

## PluginStreaming (0xC0A1E5CF)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2754-L2758
Indicates that the client supports the Streaming DSM Plugin. See C2S NotifyPluginStreaming.

## GII (0xFFFFFECF)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2761-L2765
Indicates that the client supports the GII Extension. See C2S/S2C GII.

## CacheEnable (0xFFFF0001)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2622-L2627
Indicates that the client supports the Cache Extension. Related to the cache encoding.

## QueueEnable (0xFFFF000B)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2630-L2634
Indicates that the client supports the Queue Extension.

## CompressLevel0-9 (0xFFFFFF00-0xFFFFFF09)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2637-L2645
Indicates the wanted compression level.

## QualityLevel0 (0xFFFFFFE0-0xFFFFFFE9)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2648-L2656
Indicates the wanted image quality level.

## FineQualityLevel0/100 (0xFFFFFE00/0xFFFFFE64)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2659-L2667
Indicates the wanted fine-grained image quality level.

## EncodingSubsamp1X (0xFFFFFD00-0xFFFFFD05)
https://github.com/ultravnc/UltraVNC/blob/main/winvnc/winvnc/vncclient.cpp#L2670-L2678
Indicates the wanted subsampling.

# C2S
## FixColourMapEntries (1)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1318-L1329
```
No. of bytes             Type              [Value] Description
1                        ``U8``            1       *type*
1                                                  *padding*
2                        ``U16``                   *firstColour*
2                        ``U16``                   *nColours*
```
Followed by nColours times r,g,b (3}\*U16)

## ExtendedClientCutText (6)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1446-L1464
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#extended-clipboard-pseudo-encoding
A positive value of _length_ indicates that the message follows the original format with 8859-1 text data.
A negative value of _length_ indicates that the extended message format is used and _abs(length)_ is the total number of following bytes.

All messages start with a header:
```
No. of bytes Type         Description
4            `U32`        flags
```

Flags:
```
Bit             Description
0               *text (utf8, null terminated)*
1               *rtf*
2               *html*
3               *dib*
4               *files*
5-15            Reserved for future formats
16-23           Reserved
24              *caps*
25              *request*
26              *peek*
27              *notify*
28              *provide*
29-31           Reserved for future actions
```
Caps: defines which file formats can be transfered. followed by an array of max format sizes. the number of entries corresponds to the number of format bits set:
```
No. of bytes Type         Description
formats * 4  `U32` array  sizes
```
Request: The recipient should respond with a provide message with the clipboard data for the formats indicated through the flags
Peek: The recipient should send a new notify message indicating which formats are available
Notify: This message indicates which formats are available and should be sent whenever the clipboard changes/as a response to peek. The formats are indicated through the flags
Provide: This message includes the actual clipboard data. The header is followed by a zlib stream which contains a pair of size and data:
```
No. of bytes    Type                   Description
4               ``U32``                *size*
*size*          ``U8`` array           *data*
```

## FileTransfer (7)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1026-L1092
C: https://github.com/ultravnc/UltraVNC/blob/main/vncviewer/FileTransfer.cpp#L487
```
No. of bytes             Type              [Value] Description
1                        ``U8``            7       *type*
1                        ``U8``                    *contentType*
1                        ``U8``                    *contentParam*
1                        ``U8``                    *padding*
4                        ``U32``                   *size*
4                        ``U32``                   *length*
```
Followed by `data`: `U8 array` of size `length`

RDrivesList: Request list of local drives
RDirContent: Request content of a dir. Path is in `data`
DirPacket: Response to `RDrivesList`, `RDirContent`. ContentParam can be:
	- ADrivesList: drives in `data`
	- ADirectory: directory name in `data`
	- AFile: file name in `data`
FileTransferAccess: Ignored on libvnsserver, used to test permissions on client
FileAcceptHeader: Response to FileTransferOffer. Server accepts/declines offer.
FileChecksums: Response to FileTransferOffer. Server sends checksum of existing file, before acking through FileAcceptHeader
FileTransferRequest: Requests a file to be transfered. Path is in `data`. Compression supported if `size` == `1`
FileHeader: S: Destination file is ready for reception (size > 0) or not (size == -1); C: server starts sending file
FileTransferOffer: Client offers file. `data` contains file path+filetime (seperated by ','). `size` contains file size
FilePacket: Chunk of a file
EndOfFile: File has been received of error
AbortFileTransfer: File Transfer is aborted. Also used for FileTransferRights (client asks for filetransfer permission) (`contentParam == 0` => old, else libvncserver)
Command: command in `contentParam`
- CDirCreate: Create dir specified in `data`
- CFileDelete: Delete file specified in `data`
- CFileRename: Rename file. Current name and new name are in `data`, seperated by `*`
CommandReturn: response to cmd. response type in `contentParam`

## SetScale (8)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1473-L1479
```
No. of bytes             Type              [Value] Description
1                        ``U8``            8       *type*
1                        ``U8``                    *scale*
2                                                  *padding*
```

## SetServerInput (9)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1503-L1509
Enables/disables the server using local input
```
No. of bytes             Type              [Value] Description
1                        ``U8``            9       *type*
1                        ``U8``                    *status*
2                                                  *padding*
```

## SetSW (10)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1516-L1523
Sets the framebuffer to a single window underneath `x` and `y` or sets the whole display if `x` and `y` equal `1`
```
No. of bytes             Type              [Value] Description
1                        ``U8``            10      *type*
1                        ``U8``                    *status*
2                        ``16``                    *x*
2                        ``16``                    *y*
```

## TextChat (11)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1102-L1115
Bidirectional
```
No. of bytes             Type              [Value] Description
1                        ``U8``            11      *type*
3                                                  *padding*
2                        ``U16``                   *length*
```
Special commands indicated by length:
```
Length            Description
0xFFFFFFFF        Open
0xFFFFFFFE        Close
0xFFFFFFFD        Finished
```
On the server they reset the string.
On Client:
- Open: Open a textchat window
- Close: Close the window
- Finished: Close the window
MaxLength is `4096`.
Else the header is followed by `length` bytes message

## KeepAlive (13)
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1325-L1327
```
No. of bytes             Type              [Value] Description
1                        ``U8``            13      *type*
```


## PalmVNC SetScaleFactor (15)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1488-L1495
```
No. of bytes             Type              [Value] Description
1                        ``U8``            15      *type*
1                        ``U8``                    *scale*
2                                                  *padding*
```

## RequestSession (20)
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1329-L1331
unused?
```
No. of bytes             Type              [Value] Description
1                        ``U8``            20      *type*
```

## SetSession (21)
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1333-L1336
```
No. of bytes             Type              [Value] Description
1                        ``U8``            21      *type*
1                        ``U8``                    *number*
```

## NotifyPluginStreaming (80)
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1090-L1096
```
No. of bytes             Type              [Value] Description
1                        ``U8``            80      *type*
1                                                  *padding*
2                        ``U16``                   *flags*
```

## EnableContinuousUpdates (150)
Informs the server that it should only send framebufferupdates as a response to framebufferupdaterequests or send them continuously.
```
No. of bytes             Type              [Value] Description
1                        ``U8``            150     *message-type*
1                        ``U8``                    *enable-flag*
2                        ``U16``                   *x-position*
2                        ``U16``                   *y-position*
2                        ``U16``                   *width*
2                        ``U16``                   *height*
```
If the `enable-flag` is non-zero the server can start sending framebufferupdate messages for the specified rectangle. If continuous updates are already active, the coordinates should be updated.
The server must ignore all incremental framebufferupdaterequests as long as continuous updates are active.

The client should have established that the server supports this extension by requesting the ContinuousUpdates Pseudo-encoding

## xvp (250)
https://github.com/LibVNC/libvncserver/blob/master/include/rfb/rfbproto.h#L1139-L1154
Client sets xvp pseudo in setencodings. Server sends xvp_init msg. Server responds to a client msg it cant perform with XVP_FAIL
The server should initiate a shutdown/reboot/reset of the system.
```
No. of bytes             Type              [Value] Description
1                        ``U8``            250     *message-type*
1                                                  *padding*
1                        ``U8``            1       *xvp-extension-version*
1                        ``U8``                    *xvp-message-code*
```

The message codes are defined as following:
```
Code    Description
// S2C
0       XVP_FAIL
1       XVP_INIT
// C2S
2       XVP_SHUTDOWN
3       XVP_REBOOT
4       XVP_RESET
```
The client should have established that the server supports this extension by requesting the xvp Pseudo-encoding.


## SetDesktopSize (251)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#setdesktopsize
Requests a change of desktop size. Can only be sent after receiving a `ExtendedDesktopSize` rectangle.
```
No. of bytes             Type              [Value] Description
1                        ``U8``            251     *message-type*
1                                                  *padding*
2                        ``U16``                   *width*
2                        ``U16``                   *height*
1                        ``U8``                    *number-of-screens*
1                                                  *padding*
*number-of-screens* * 16 ``SCREEN`` array          *screens*
```
Followed by `number-of-screens` times:
```
No. of bytes    Type                            Description
4               ``U32``                         *id*
2               ``U16``                         *x-position*
2               ``U16``                         *y-position*
2               ``U16``                         *width*
2               ``U16``                         *height*
4               ``U32``                         *flags*
```
`width` and `height` indicate the requested framebuffer size.

## GII (253)
https://github.com/ultravnc/UltraVNC/blob/main/rfb/gii.h#L51-L56
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#gii-client-message
General Input Interface extension. Needs gii Server message before sending c2s (which needs gii pseudo encoding set).
Each message has a header:
```
No. of bytes    Type                  [Value]   Description
1               ``U8``                253       *message-type*
1               ``U8``                          *endian-and-sub-type*
2               ``U16``                         *length*
```
Followed by the message subtype specific content.
`endian-and-subtype` contains the endianess of the message and the subtype. The leftmost bit indicates if big endian is used (1 = BE, 0 = LE).

### Event Injection (0)
https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#injecting-events
```
No. of bytes    Type                            Description
length          ``EVENT`` array                 *events*
```
`EVENT` can be `KEY_EVENT`, `PTR_MOVE_EVENT`, `PTR_BUTTON_EVENT`, `VALUATOR_EVENT`
#### KEY_EVENT
```
No. of bytes    Type                 [Value]    Description
1               ``U8``               24         *event-size*
1               ``U8``               5, 6 or 7  *event-type*
2               ``EU16``                        *padding*
4               ``EU32``                        *device-origin*
4               ``EU32``                        *modifiers*
4               ``EU32``                        *symbol*
4               ``EU32``                        *label*
4               ``EU32``                        *button*
```
`event-type` values:
- 5: key pressed
- 6: key released
- 7: key repeat
#### PTR_MOVE_EVENT
```
No. of bytes    Type                 [Value]    Description
1               ``U8``               24         *event-size*
1               ``U8``               8 or 9     *event-type*
2               ``EU16``                        *padding*
4               ``EU32``                        *device-origin*
4               ``ES32``                        *x*
4               ``ES32``                        *y*
4               ``ES32``                        *z*
4               ``ES32``                        *wheel*
```
`event-type` values: 
- 8: pointer relative
- 9: pointer absolute
#### PTR_BUTTON_EVENT
```
No. of bytes    Type                 [Value]    Description
1               ``U8``               12         *event-size*
1               ``U8``               10 or 11   *event-type*
2               ``EU16``                        *padding*
4               ``EU32``                        *device-origin*
4               ``EU32``                        *button-number*
```
`event-type` values:
- 10: pointer button press
- 11: pointer button release

#### VALUATOR_EVENT
```
No. of bytes    Type              [Value]            Description
1               ``U8``            16 + 4 * *count*   *event-size*
1               ``U8``            12 or 13           *event-type*
2               ``EU16``                             *padding*
4               ``EU32``                             *device-origin*
4               ``EU32``                             *first*
4               ``EU32``                             *count*
4 * *count*     ``ES32`` array                       *value*
```
`event-type` values:
- 12: relative valuator
- 13: absolute valuator

### Version (1)
Set the used version
```
No. of bytes    Type                            Description
2               ``EU16``                        *version*
```

### Device Creation (2)
Create a device
```
No. of bytes          Type            [Value]                    Description
31                    ``U8`` array                               *device-name*
1                     ``U8``          0                          *nul-terminator*
4                     ``EU32``                                   *vendor-id*
4                     ``EU32``                                   *product-id*
4                     ``EVENT_MASK``                             *can-generate*
4                     ``EU32``                                   *num-registers*
4                     ``EU32``                                   *num-valuators*
4                     ``EU32``                                   *num-buttons*
*num-valuators* * 116 ``VALUATOR``                               *valuators*
```
With `VALUATOR`:
```
No. of bytes    Type                 [Value]    Description
4               ``EU32``                        *index*
74              ``U8`` array                    *long-name*
1               ``U8``               0          *nul-terminator*
4               ``U8`` array                    *short-name*
1               ``U8``               0          *nul-terminator*
4               ``ES32``                        *range-min*
4               ``ES32``                        *range-center*
4               ``ES32``                        *range-max*
4               ``EU32``                        *SI-unit*
4               ``ES32``                        *SI-add*
4               ``ES32``                        *SI-mul*
4               ``ES32``                        *SI-div*
4               ``ES32``                        *SI-shift*
```

### Device Destruction (3)
Destroy a device.
```
No. of bytes    Type                            Description
4               ``EU32``                        *device-origin*
```


# S2C
## ExtendedServerCutText (3)
See C2S ExtendedClientCutText

## ResizeFramebuffer (4)
Resizes the framebuffer to `framebuffer-width` x `framebuffer-height`
```
+--------------+--------------+---------------------+
| No. of bytes | Type [Value] | Description         |
+--------------+--------------+---------------------+
| 1            | U8 [4]       | message-type        |
| 1            |              | padding             |
| 2            | U16          | framebuffer-width   |
| 2            | U16          | framebuffer-height  |
+--------------+--------------+---------------------+
```
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1272-L1279

## FileTransfer (7)
See C2S FileTransfer

## TextChat (11)
See C2S TextChat

## KeepAlive (13)
See C2S KeepAlive

## PalmVNC ReSizeFrameBuffer (15)
Resizes the framebuffer to `framebuffer-width`x`framebuffer-height`.
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1290-L1301
```
No. of bytes    Type                       Description
1               U8                         *message-type*
1                                          *padding*
2               U16                        *Desktop Width*
2               U16                        *Desktop Height*
2               U16                        *Framebuffer Width*
2               U16                        *Framebuffer Height*
```

## NotifyPluginStreaming (80)
See C2S NotifyPluginStreaming

## ServerState (173)
Sends a server state to the client
https://github.com/ultravnc/UltraVNC/blob/main/rfb/rfbproto.h#L1306-L1322
```
No. of bytes    Type                       Description
1               U8                         *message-type*
3                                          *padding*
4               U32                        *State Type*
4               U32                        *State Value*
```
The types are defined as followed:
```
Code    Description
1       Server Remote Inputs State (Sets if the client is allowed to send inputs)
2       Keep Alive Interval
3       Idle Input Timeout
```

## xvp (250)
See C2S xvp

## GII (253)
Like C2S:
Each message has a header:
```
No. of bytes    Type                  [Value]   Description
1               ``U8``                253       *message-type*
1               ``U8``                          *endian-and-sub-type*
2               ``U16``                         *length*
```
Followed by the message subtype specific content.
`endian-and-subtype` contains the endianess of the message and the subtype. The leftmost bit indicates if big endian is used (1 = BE, 0 = LE).

Submessages:
### Version (1)
Declares the allowed versions
```
No. of bytes    Type                            Description
2               ``EU16``                        *maximum-version*
2               ``EU16``                        *minimum-version*
```

### Device Creation Response (2)
Response to a Device Creation Request
```
No. of bytes    Type                            Description
4               ``EU32``                        *device-origin*
```