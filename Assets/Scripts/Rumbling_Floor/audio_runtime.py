from pythonosc.dispatcher import Dispatcher
from pythonosc.osc_server import ThreadingOSCUDPServer

# storage for incoming intensities
tile_intensities = [0.0] * 16

def handle_tile_intensities(address, *args):
    global tile_intensities

    # convert OSC args → python list
    floats = list(args)

    if len(floats) != 16:
        print("[Python] ERROR: Received wrong length (expected 16 floats)")
        return

    tile_intensities = floats
    print("[Python] Tile intensities updated:", tile_intensities)

def handle_tile_intensities(address, *args):
    print("Received intensities:", list(args))

dispatcher = Dispatcher()
dispatcher.map("/tile/intensities", handle_tile_intensities)

server = ThreadingOSCUDPServer(("127.0.0.1", 8000), dispatcher)
print("Python OSC listening on 127.0.0.1:8000")
server.serve_forever()