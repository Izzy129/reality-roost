"""Visual debug tool: shows what's currently being sent to each Dante haptic channel,
without needing the physical floor tiles wired up.

Displays a 2x3 grid (one square per tile). Each square is split into a left and right
half; the half lights up in proportion to the level being sent to that tile's left/right
Dante channel.

This listens over OSC rather than tapping real Dante audio. Dante blocks a device from
subscribing to its own transmit channels (self Tx->Rx loopback isn't allowed for a single
Dante Virtual Soundcard instance), so reading the actual ASIO/Dante signal back into this
script isn't possible without a second physical/virtual Dante device on the network.
Instead, whatever is driving the floor (currently FmodAsioSpike.cs, eventually the real
haptic subsystem) broadcasts the same per-channel levels it's sending to Dante over OSC,
at address `/rr/debug/tile_levels`, as 12 floats (channel 0 = tile 0 left, 1 = tile 0
right, 2 = tile 1 left, ...). This script just visualizes that.

Run standalone: python dante_channel_monitor.py
"""

import threading
import tkinter as tk

from pythonosc.dispatcher import Dispatcher
from pythonosc.osc_server import ThreadingOSCUDPServer

# ===== CONFIGURATION =====
OSC_IP = "127.0.0.1"
OSC_PORT = 9001  # separate from middleware.py's 9000 so both can run at once
OSC_ADDRESS = "/rr/debug/tile_levels"

NUM_TILES = 6
CHANNELS_PER_TILE = 2  # left, right
NUM_CHANNELS = NUM_TILES * CHANNELS_PER_TILE  # 12

GRID_ROWS = 3
GRID_COLS = 2

ATTACK = 0.6   # how quickly a square brightens when level rises (0-1, higher = snappier)
RELEASE = 0.08  # how quickly a square dims when level falls (0-1, higher = snappier)

SQUARE_SIZE = 160
PADDING = 20  # horizontal gap between columns
ROW_SPACING = 50  # vertical gap between rows (includes room for the tile label)
BG_COLOR = "#1a1a1a"
IDLE_COLOR = (40, 40, 45)
LIT_COLOR = (60, 220, 120)


# ===== LEVEL STATE =====
class ChannelLevels:
    """Thread-safe smoothed per-channel level state (0.0-1.0), updated from the OSC thread."""

    def __init__(self, num_channels):
        self._target = [0.0] * num_channels
        self._smoothed = [0.0] * num_channels
        self._lock = threading.Lock()

    def set_targets(self, values):
        with self._lock:
            self._target = list(values)

    def tick_smoothing(self):
        """Advance the smoothed values one frame toward their targets. Call from the GUI thread."""
        with self._lock:
            for i in range(len(self._smoothed)):
                delta = self._target[i] - self._smoothed[i]
                rate = ATTACK if delta > 0 else RELEASE
                self._smoothed[i] += delta * rate
            return list(self._smoothed)


levels = ChannelLevels(NUM_CHANNELS)


# ===== OSC =====
def handle_tile_levels(address, *args):
    values = list(args)
    if len(values) != NUM_CHANNELS:
        print(f"[OSC] ERROR: Received {len(values)} values, expected {NUM_CHANNELS}")
        return
    levels.set_targets(values)


def start_osc_server():
    dispatcher = Dispatcher()
    dispatcher.map(OSC_ADDRESS, handle_tile_levels)

    server = ThreadingOSCUDPServer((OSC_IP, OSC_PORT), dispatcher)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    print(f"[OSC] Listening on {OSC_IP}:{OSC_PORT}{OSC_ADDRESS}")
    return server


# ===== GUI =====
class MonitorWindow:
    def __init__(self, root):
        self.root = root
        self.root.title("Dante Haptic Channel Monitor (OSC)")
        self.root.configure(bg=BG_COLOR)

        canvas_width = GRID_COLS * SQUARE_SIZE + (GRID_COLS + 1) * PADDING
        canvas_height = GRID_ROWS * (SQUARE_SIZE + ROW_SPACING) + PADDING
        self.canvas = tk.Canvas(root, width=canvas_width, height=canvas_height, bg=BG_COLOR, highlightthickness=0)
        self.canvas.pack()

        # Lock the window to the canvas size so a manual resize from a previous layout
        # (e.g. the old 2x3 grid) can't leave stale, uncleared pixels around a smaller grid.
        self.root.resizable(False, False)
        self.root.geometry(f"{canvas_width}x{canvas_height}")
        self.root.update_idletasks()

        self.left_rects = []
        self.right_rects = []

        for tile_index in range(NUM_TILES):
            row = tile_index // GRID_COLS
            col = tile_index % GRID_COLS
            x0 = PADDING + col * (SQUARE_SIZE + PADDING)
            y0 = PADDING + row * (SQUARE_SIZE + ROW_SPACING)
            x_mid = x0 + SQUARE_SIZE // 2
            y1 = y0 + SQUARE_SIZE

            left_rect = self.canvas.create_rectangle(x0, y0, x_mid, y1, fill=self._color(0.0), outline="#000000")
            right_rect = self.canvas.create_rectangle(x_mid, y0, x0 + SQUARE_SIZE, y1, fill=self._color(0.0), outline="#000000")
            self.canvas.create_line(x_mid, y0, x_mid, y1, fill="#000000")
            self.canvas.create_text(x0 + SQUARE_SIZE // 2, y1 + 10, fill="#cccccc", text=f"Tile {tile_index}", anchor="n")

            self.left_rects.append(left_rect)
            self.right_rects.append(right_rect)

        self.root.protocol("WM_DELETE_WINDOW", self.on_close)
        self._closing = False

    @staticmethod
    def _color(level):
        level = max(0.0, min(1.0, level))
        r = int(IDLE_COLOR[0] + (LIT_COLOR[0] - IDLE_COLOR[0]) * level)
        g = int(IDLE_COLOR[1] + (LIT_COLOR[1] - IDLE_COLOR[1]) * level)
        b = int(IDLE_COLOR[2] + (LIT_COLOR[2] - IDLE_COLOR[2]) * level)
        return f"#{r:02x}{g:02x}{b:02x}"

    def redraw(self):
        if self._closing:
            return

        current = levels.tick_smoothing()
        for tile_index in range(NUM_TILES):
            left_level = current[tile_index * CHANNELS_PER_TILE]
            right_level = current[tile_index * CHANNELS_PER_TILE + 1]
            self.canvas.itemconfig(self.left_rects[tile_index], fill=self._color(left_level))
            self.canvas.itemconfig(self.right_rects[tile_index], fill=self._color(right_level))

        self.root.after(33, self.redraw)  # ~30 fps

    def on_close(self):
        self._closing = True
        self.root.destroy()


def main():
    server = start_osc_server()

    root = tk.Tk()
    window = MonitorWindow(root)
    window.redraw()

    try:
        root.mainloop()
    finally:
        server.shutdown()
        print("[Status] Stopped.")


if __name__ == "__main__":
    main()
