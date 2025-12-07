from pythonosc import dispatcher, osc_server

def on_rumble(addr, *args):
    print(f"OSC {addr} -> {args}")

if __name__ == "__main__":
    disp = dispatcher.Dispatcher()
    disp.map("/rumble", on_rumble)        # listen for /rumble
    ip, port = "127.0.0.1", 9000          # match Unity sender
    server = osc_server.ThreadingOSCUDPServer((ip, port), disp)
    print(f"Listening on {ip}:{port}")
    server.serve_forever()