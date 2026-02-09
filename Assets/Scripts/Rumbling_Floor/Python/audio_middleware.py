import sounddevice as sd
import numpy as np
from pythonosc.dispatcher import Dispatcher
from pythonosc.osc_server import ThreadingOSCUDPServer
import threading
import time


# ===== CONFIGURATION =====
class AudioConfig:
    """Audio stream configuration settings."""
    SAMPLE_RATE = 48000  # sample rate of audio played (currently the rumble.mp3 file is at 48 kHz)
    BLOCKSIZE = 512  # audio buffer size
    INPUT_CHANNELS = 2  # stereo from Unity
    OUTPUT_CHANNELS = 1  # mono for testing


class OSCConfig:
    """OSC communication configuration settings."""
    IP = "127.0.0.1"
    PORT = 9000
    EXPECTED_RATE = 60  # how often Unity sends OSC messages (in Hz)
    TIMEOUT_FRAMES = 2.0  # unity frames to wait before considering OSC messages "stopped"


class DeviceConfig:
    """Audio device configuration settings."""
    UNITY_INPUT = "CABLE Output"  # name of VB-Cable output device, may need to change depending on system
    # TODO: later on, we'll change this to match the Dante Virtual Sound Card audio devices... prolly with a list of output devices
    OUTPUT = (
        "Headphones (Realtek(R) Audio)"  # for testing, output to Realtek headphones
    )
    NUM_TILES = 16  # number of floor tiles


# ===== GLOBAL STATE =====
class TileState:
    """Thread-safe state manager for tile intensities.

    The OSC server and audio processing run on separate threads.
    This class encapsulates all shared state between the OSC and audio threads,
    ensuring thread-safe access through a lock. This prevents race conditions that could lead to corrupted data or crashes.
    Basically, if both the OSC and audio thread access the tile intensities at the same time,
    we may get corrupted data. So we use a lock to make sure only one thread can access the data at a time.
    """

    def __init__(self, num_tiles=DeviceConfig.NUM_TILES):
        self.intensities = np.zeros(num_tiles, dtype=np.float32)
        self.osc_received = False
        self.last_osc_time = None
        self._lock = threading.Lock()

    def update(self, intensities):
        """Update tile intensities from OSC message (thread safe)."""
        with self._lock:
            self.intensities = np.array(intensities, dtype=np.float32)
            self.osc_received = True
            self.last_osc_time = time.time()

    def get_intensities(self):
        """Get copy of current intensities (thread safe)."""
        with self._lock:
            return self.intensities.copy()

    def reset_intensities(self):
        """Reset all intensities to zero (thread safe)."""
        with self._lock:
            if not np.all(self.intensities == 0):
                self.intensities = np.zeros(len(self.intensities), dtype=np.float32)
                return True  # indicate that reset occurred
            return False  # already zero

    def has_received_osc(self):
        """Check if any OSC messages have been received."""
        return self.osc_received

    def get_last_osc_time(self):
        """Get timestamp of last OSC message."""
        return self.last_osc_time


# create global state instance
state = TileState()


# ===== OSC HANDLER =====
def handle_tile_intensities(address: str, *args: float) -> None:
    """Handle incoming OSC messages with tile intensity values."""
    # args will be the floats array sent from Unity
    intensity_values = list(args)  # convert OSC message args to python list

    if len(intensity_values) != DeviceConfig.NUM_TILES:
        print(
            f"[OSC] ERROR: Received {len(intensity_values)} values, expected {DeviceConfig.NUM_TILES}"
        )
        return

    # update state (thread-safe)
    state.update(intensity_values)
    print(f"[OSC] Updated intensities: {state.get_intensities()}")


# ===== AUDIO CALLBACK AND PROCESSING =====
def audio_callback(indata, outdata, frames, time_info, status):
    """This function is called (from a separate thread) for each audio block.
        It is responsible for any audio processing before output.
        Currently just converts stereo input to mono and scales volume based on tile intensities.

    Args:
        indata (numpy.ndarray): Input audio buffer, shape (frames, channels). Contains stereo audio from Unity's output.
        outdata (numpy.ndarray): Output audio buffer to fill, shape (frames, channels).
            ***This will be sent to the output device.***
        frames (int): Number of audio frames in the current block (currently BLOCKSIZE from config at top of file).
        time_info (dict): Timing information from sounddevice.
        status (CallbackFlags): Status flags indicating potential issues like input/output overflow or underflow.
    Returns:
        None: Function modifies outdata in place.
    """
    if status:
        print(f"[Audio] Status: {status}")

    # dont process until we get at least one OSC message
    if not state.has_received_osc():
        outdata.fill(0)  # output nothing
        return

    # convert stereo to mono
    # TODO: maybe remove since this was just for testing
    # this just gets the average of the 2 channels
    mixed_audio = np.mean(indata, axis=1)

    # get current intensities (thread-safe)
    current_intensities = state.get_intensities()

    # TODO: testing only output to channel 0
    channel_0_audio = mixed_audio * current_intensities[0]
    outdata[:, 0] = channel_0_audio

    # debug logging
    volume = np.linalg.norm(channel_0_audio)
    if volume > 0.01:
        print(
            f"[Audio] Ch0: intensity={current_intensities[0]:.2f}, volume={volume:.2f}"
        )


# ===== DEVICE UTILITIES =====
def list_available_devices(input_devices=True):
    """Helper function that prints available input or output devices.

    Args:
        input_devices (bool): if True, list input devices; if False, list output devices.
    Returns:
        None (prints to console).
    """
    device_type = "input" if input_devices else "output"
    print(f"\nAvailable {device_type} devices:")
    devices = sd.query_devices()
    for i, device in enumerate(devices):
        channels = (
            device["max_input_channels"]
            if input_devices
            else device["max_output_channels"]
        )
        if channels > 0:
            print(f"  [{i}] {device['name']}")


def find_device_index(device_name: str, input_device: bool = False) -> int | None:
    """This helper function searches through available audio devices and returns the index of the device matching the given name.

    Args:
        device_name (str): target device name to search for.
        input_device (bool): if True, search for input devices; if False, search for output devices.
    Returns:
        index (int or None): index of the found device, or None if not found.
    """
    # get all devices
    devices = sd.query_devices()
    # iterate through devices until we find target device
    for i, device in enumerate(devices):
        if device_name in device["name"]:
            if input_device and device["max_input_channels"] > 0:
                return i
            elif not input_device and device["max_output_channels"] > 0:
                return i
    return None


# ===== SETUP FUNCTIONS =====
def setup_audio_devices():
    """Find and validate audio devices.

    Returns:
        tuple: (unity_device_index, output_index) or (None, None) if devices not found.
    """
    # find unity output device (currently VB-Cable)
    print(f"\n[Setup] Searching for unity device {DeviceConfig.UNITY_INPUT}...")
    unity_device_index = find_device_index(DeviceConfig.UNITY_INPUT, input_device=True)

    # error if not found
    if unity_device_index is None:
        print(f"[ERROR] Unity device {DeviceConfig.UNITY_INPUT} not found!")
        list_available_devices(input_devices=True)
        return None, None

    print(
        f"[Setup] Found unity device {DeviceConfig.UNITY_INPUT} at device index {unity_device_index}"
    )

    # find output device (Dante Virtual Soundcard...? or Realtek for testing)
    print(f"\n[Setup] Searching for output device...")
    output_index = find_device_index(DeviceConfig.OUTPUT, input_device=False)

    if output_index is None:
        print(f"[ERROR] Could not find output device '{DeviceConfig.OUTPUT}'")
        list_available_devices(input_devices=False)
        return None, None

    print(
        f"[Setup] ✓ Found output device at index {output_index}: {DeviceConfig.OUTPUT}"
    )

    return unity_device_index, output_index


def start_osc_server():
    """Initialize and start the OSC server.

    Returns:
        ThreadingOSCUDPServer: The running OSC server instance.
    """
    print(f"\n[OSC] Starting server on {OSCConfig.IP}:{OSCConfig.PORT}...")
    dispatcher = Dispatcher()

    # will call the handle_tile_intensities function on receiving messages at /tile/intensities
    dispatcher.map("/tile/intensities", handle_tile_intensities)

    # start OSC server on separate thread
    osc_server = ThreadingOSCUDPServer((OSCConfig.IP, OSCConfig.PORT), dispatcher)
    osc_thread = threading.Thread(target=osc_server.serve_forever, daemon=True)
    osc_thread.start()
    print(f"[OSC] ✓ Listening on {OSCConfig.IP}:{OSCConfig.PORT}")

    return osc_server


def wait_for_initial_osc():
    """Wait for first OSC message before starting audio."""
    print("\n" + "=" * 70)
    print("[Status] Waiting for first OSC message from Unity...")
    print("=" * 70)
    while not state.has_received_osc():
        time.sleep(0.1)

    print("\n[Status] ✓ First OSC message received!")
    print("[Status] Starting audio processing...\n")


def monitor_osc_timeout():
    """Check for OSC timeout and reset intensities if needed."""
    last_osc_time = state.get_last_osc_time()
    if state.has_received_osc() and last_osc_time is not None:
        time_since_last = time.time() - last_osc_time
        timeout_threshold = (
            OSCConfig.TIMEOUT_FRAMES / OSCConfig.EXPECTED_RATE
        )  # how long to wait before considering OSC "stopped"

        if time_since_last > timeout_threshold:
            # OSC stopped, zero out intensities
            if state.reset_intensities():
                print(
                    f"[OSC] WARNING: No messages for {time_since_last:.2f}s, intensities → 0"
                )


def run_audio_stream(unity_device_index, output_index, osc_server):
    """Run the main audio processing loop.

    Args:
        unity_device_index (int): Index of Unity input device.
        output_index (int): Index of output device.
        osc_server (ThreadingOSCUDPServer): Running OSC server instance.
    """
    # start synchronized audio stream
    # async didnt work well here... was getting duplicate audio callbacks
    # havent tested with different audio devices yet
    try:
        with sd.Stream(
            device=(unity_device_index, output_index),
            samplerate=AudioConfig.SAMPLE_RATE,
            blocksize=AudioConfig.BLOCKSIZE,
            # TODO: adjust channel count based on actual setup
            channels=(AudioConfig.INPUT_CHANNELS, AudioConfig.OUTPUT_CHANNELS),
            callback=audio_callback,
        ):  # audio callback function, called for processing audio

            print("=" * 70)
            print("[Status] ✓ Audio processing ACTIVE")
            print("[Status] Press Ctrl+C to stop")
            print("=" * 70)
            print()

            # keep running and monitor for OSC timeout
            while True:
                time.sleep(0.1)
                monitor_osc_timeout()

    except KeyboardInterrupt:
        print("\n\n[Status] Shutting down...")
        osc_server.shutdown()
        print("[Status] ✓ Stopped successfully!")
        print("=" * 70)


# ===== MAIN =====
def main():
    """Main entry point"""
    print("=" * 70)
    print(" Reality Roost Rumbling Floor Audio Middleware")
    print("=" * 70)

    # setup audio devices
    unity_device_index, output_index = setup_audio_devices()
    if unity_device_index is None or output_index is None:
        return

    # start OSC server
    osc_server = start_osc_server()

    # wait for first OSC message before starting audio
    wait_for_initial_osc()

    # run audio processing loop
    run_audio_stream(unity_device_index, output_index, osc_server)


if __name__ == "__main__":
    main()
