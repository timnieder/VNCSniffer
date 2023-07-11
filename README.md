# VNCSniffer
Written as part of my master thesis.

A program that is able to sniff unencrypted VNC data and show it to the user. 
Also able to insert packets, if the interface supports it.

## Project Structure
- VNCSniffer.Core: Library that handles detection and analysis of VNC data
- VNCSniffer.Core.Tests: VNCSniffer.Core Tests
- VNCSniffer.Cli: Terminal application, which logs VNC messages (incomplete, was used for pre-gui dev)
- VNCSniffer.GUI: Graphical application based on Avalonia, which uses VNCSniffer.Core to sniff and render VNC data

## Known issues
- Can fail during ZRLE encodings (or general zlib stuff? not sure what the cause is)
- The client-server detection fails at times if VNC connection started before the sniffing (cause I only really set the participants in the init phases). This can cause the sniffer not being able to insert packets (as it doesn't know who should get the packets).
- Image parsing can get slow if the data is big. This is because i don't have a stream approach, but rather a packet based one and i realised that too late. Tried to mitigate by saving the data offsets and starting from there (still not as fast as other VNC renderers)
- Encoding test data (mostly) isn't sourced from real traffic, but rather from a custom implementation, so there may be issues there