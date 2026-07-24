import os

# CRITICAL: Must set this BEFORE importing sounddevice to load PortAudio with ASIO support
os.environ["SD_ENABLE_ASIO"] = "1"

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
    OUTPUT_CHANNELS = 16  # Dante 16x16 ASIO (we use 12, fill rest with silence)
    USED_OUTPUT_CHANNELS = 12  # actual channels we use (6 devices × 2 channels)


class OSCConfig:
    """OSC communication configuration settings."""

    IP = "127.0.0.1"
    PORT = 9000
    EXPECTED_RATE = 60  # how often Unity sends OSC messages (in Hz)
    TIMEOUT_FRAMES = 2.0  # unity frames to wait before considering OSC messages "stopped"


class DeviceConfig:
    """Audio device configuration settings."""

    UNITY_INPUT = "CABLE Output"  # name of VB-Cable output device, may need to change depending on system
    DANTE_ASIO = "Dante Virtual Soundcard (x64)"  # Dante ASIO device name
    NUM_TILES = 16  # number of floor tiles (intensity array size)

    # Channel mapping documentation:
    # intensity[0]  → DVS Transmit 1-2, Left   (output channel 0)
    # intensity[1]  → DVS Transmit 1-2, Right  (output channel 1)
    # intensity[2]  → DVS Transmit 3-4, Left   (output channel 2)
    # intensity[3]  → DVS Transmit 3-4, Right  (output channel 3)
    # intensity[4]  → DVS Transmit 5-6, Left   (output channel 4)
    # intensity[5]  → DVS Transmit 5-6, Right  (output channel 5)
    # intensity[6]  → DVS Transmit 7-8, Left   (output channel 6)
    # intensity[7]  → DVS Transmit 7-8, Right  (output channel 7)
    # intensity[8]  → DVS Transmit 9-10, Left  (output channel 8)
    # intensity[9]  → DVS Transmit 9-10, Right (output channel 9)
    # intensity[10] → DVS Transmit 11-12, Left (output channel 10)
    # intensity[11] → DVS Transmit 11-12, Right (output channel 11)
    # intensity[12-15] → unused (ignored)
    # output channels 12-15 → silence (unused ASIO channels)


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


# ===== SHARED AUDIO BUFFER =====
class AudioBuffer:
    """Thread-safe buffer to pass audio between the WDM input stream and the ASIO output stream.

    Since VB-Cable (WDM) and Dante (ASIO) use different host APIs, they cannot share a single
    sd.Stream. Instead, we use separate InputStream and OutputStream connected by this buffer.
    """

    def __init__(self, blocksize):
        self._buffer = np.zeros(blocksize, dtype=np.float32)
        self._lock = threading.Lock()

    def write(self, mono_audio):
        """Store a block of mono audio from the input callback."""
        with self._lock:
            self._buffer = mono_audio.copy()

    def read(self):
        """Retrieve the latest block of mono audio for the output callback."""
        with self._lock:
            return self._buffer.copy()


# create global audio buffer
audio_buffer = AudioBuffer(AudioConfig.BLOCKSIZE)


# ===== AUDIO CALLBACKS =====
def input_callback(indata, frames, time_info, status):
    """Input stream callback: captures audio from VB-Cable (WDM) and stores mono mix in shared buffer.

    Args:
        indata (numpy.ndarray): Input audio buffer, shape (frames, 2). Stereo audio from Unity.
        frames (int): Number of audio frames in the current block.
        time_info (dict): Timing information from sounddevice.
        status (CallbackFlags): Status flags indicating potential issues.
    """
    if status:
        print(f"[Input] Status: {status}")

    # convert stereo to mono (average L+R channels)
    mono_audio = np.mean(indata[:, :AudioConfig.INPUT_CHANNELS], axis=1).astype(np.float32)
    audio_buffer.write(mono_audio)


def output_callback(outdata, frames, time_info, status):
    """Output stream callback: reads mono audio from shared buffer, applies per-channel intensity
    scaling, and outputs to Dante ASIO channels.

    Args:
        outdata (numpy.ndarray): Output audio buffer to fill, shape (frames, 16). 16 ASIO channels.
        frames (int): Number of audio frames in the current block.
        time_info (dict): Timing information from sounddevice.
        status (CallbackFlags): Status flags indicating potential issues.
    """
    if status:
        print(f"[Output] Status: {status}")

    # dont process until we get at least one OSC message
    if not state.has_received_osc():
        outdata.fill(0)
        return

    # read latest mono audio from shared buffer
    mono_audio = audio_buffer.read()

    # get current intensities (thread-safe)
    current_intensities = state.get_intensities()

    # output to 12 channels, each scaled by its intensity
    for channel in range(AudioConfig.USED_OUTPUT_CHANNELS):
        outdata[:, channel] = mono_audio[:frames] * current_intensities[channel]

    # fill unused channels (12-15) with silence
    for channel in range(AudioConfig.USED_OUTPUT_CHANNELS, AudioConfig.OUTPUT_CHANNELS):
        outdata[:, channel] = 0.0

    # debug logging (only log channels with significant volume)
    for channel in range(AudioConfig.USED_OUTPUT_CHANNELS):
        volume = np.linalg.norm(outdata[:, channel])
        if volume > 0.01:
            print(
                f"[Audio] Ch{channel}: intensity={current_intensities[channel]:.2f}, volume={volume:.2f}"
            )


# ===== DEVICE UTILITIES =====
def get_asio_host_api():
    """Find and return the ASIO host API index.

    Returns:
        int or None: ASIO host API index, or None if not found.
    """
    for i, api in enumerate(sd.query_hostapis()):
        if "ASIO" in api["name"]:
            return i
    return None


def list_available_devices(input_devices=True, asio_only=False):
    """Helper function that prints available input or output devices.

    Args:
        input_devices (bool): if True, list input devices; if False, list output devices.
        asio_only (bool): if True, only list ASIO devices.
    Returns:
        None (prints to console).
    """
    device_type = "input" if input_devices else "output"
    asio_api = get_asio_host_api() if asio_only else None

    print(f"\nAvailable {device_type} devices{' (ASIO only)' if asio_only else ''}:")
    devices = sd.query_devices()
    for i, device in enumerate(devices):
        # skip non-ASIO devices if asio_only is True
        if asio_only and asio_api is not None and device["hostapi"] != asio_api:
            continue

        channels = (
            device["max_input_channels"]
            if input_devices
            else device["max_output_channels"]
        )
        if channels > 0:
            hostapi_name = sd.query_hostapis(device["hostapi"])["name"]
            print(f"  [{i}] {device['name']} ({hostapi_name}, {channels} channels)")


def find_device_index(
    device_name: str, input_device: bool = False, asio_only: bool = False
) -> int | None:
    """Search through available audio devices and return the index of the device matching the given name.

    Args:
        device_name (str): target device name to search for.
        input_device (bool): if True, search for input devices; if False, search for output devices.
        asio_only (bool): if True, only search ASIO devices.
    Returns:
        index (int or None): index of the found device, or None if not found.
    """
    asio_api = get_asio_host_api() if asio_only else None
    devices = sd.query_devices()

    for i, device in enumerate(devices):
        # skip non-ASIO devices if asio_only is True
        if asio_only and asio_api is not None and device["hostapi"] != asio_api:
            continue

        if device_name in device["name"]:
            if input_device and device["max_input_channels"] > 0:
                return i
            elif not input_device and device["max_output_channels"] > 0:
                return i
    return None


# ===== SETUP FUNCTIONS =====
def setup_audio_devices():
    """Find and validate audio devices.o

    Returns:
        tuple: (unity_device_index, dante_device_index) or (None, None) if devices not found.
    """
    # check if ASIO is available
    asio_api = get_asio_host_api()
    if asio_api is None:
        print("[ERROR] ASIO host API not found!")
        print("Make sure:")
        print("  1. PortAudio with ASIO support is installed")
        print("  2. os.environ['SD_ENABLE_ASIO'] = '1' is set BEFORE importing sounddevice")
        return None, None

    print(f"[Setup] ✓ ASIO host API found (index {asio_api})")

    # find unity input device (VB-Cable)
    print(f"\n[Setup] Searching for Unity input device '{DeviceConfig.UNITY_INPUT}'...")
    unity_device_index = find_device_index(DeviceConfig.UNITY_INPUT, input_device=True)

    if unity_device_index is None:
        print(f"[ERROR] Unity device '{DeviceConfig.UNITY_INPUT}' not found!")
        list_available_devices(input_devices=True)
        return None, None

    print(
        f"[Setup] ✓ Found Unity device '{DeviceConfig.UNITY_INPUT}' at index {unity_device_index}"
    )

    # find Dante ASIO output device
    print(
        f"\n[Setup] Searching for Dante ASIO device '{DeviceConfig.DANTE_ASIO}'..."
    )
    dante_device_index = find_device_index(
        DeviceConfig.DANTE_ASIO, input_device=False, asio_only=True
    )

    if dante_device_index is None:
        print(f"[ERROR] Dante ASIO device '{DeviceConfig.DANTE_ASIO}' not found!")
        list_available_devices(input_devices=False, asio_only=True)
        return None, None

    # verify Dante has enough channels
    dante_device = sd.query_devices(dante_device_index)
    if dante_device["max_output_channels"] < AudioConfig.OUTPUT_CHANNELS:
        print(
            f"[ERROR] Dante device only has {dante_device['max_output_channels']} output channels, "
            f"need {AudioConfig.OUTPUT_CHANNELS}!"
        )
        print(
            "Please configure Dante Virtual Soundcard to 16×16 mode in the Dante control panel."
        )
        return None, None

    print(
        f"[Setup] ✓ Found Dante ASIO device '{DeviceConfig.DANTE_ASIO}' at index {dante_device_index}"
    )
    print(f"[Setup] ✓ Dante has {dante_device['max_output_channels']} output channels")

    return unity_device_index, dante_device_index


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


def run_audio_stream(unity_device_index, dante_device_index, osc_server):
    """Run the main audio processing loop with separate WDM input and ASIO output streams.

    Because VB-Cable (WDM) and Dante (ASIO) use different PortAudio host APIs, they cannot
    be combined in a single sd.Stream. Instead we open an InputStream for VB-Cable and an
    OutputStream for Dante, connected through the shared AudioBuffer.

    Args:
        unity_device_index (int): Index of Unity input device (WDM).
        dante_device_index (int): Index of Dante ASIO output device.
        osc_server (ThreadingOSCUDPServer): Running OSC server instance.
    """
    try:
        with sd.InputStream(
            device=unity_device_index,
            samplerate=AudioConfig.SAMPLE_RATE,
            blocksize=AudioConfig.BLOCKSIZE,
            channels=AudioConfig.INPUT_CHANNELS,
            callback=input_callback,
        ), sd.OutputStream(
            device=dante_device_index,
            samplerate=AudioConfig.SAMPLE_RATE,
            blocksize=AudioConfig.BLOCKSIZE,
            channels=AudioConfig.OUTPUT_CHANNELS,
            callback=output_callback,
        ):

            print("=" * 70)
            print("[Status] ✓ Audio processing ACTIVE")
            print(
                f"[Status] Input: {DeviceConfig.UNITY_INPUT} (WDM)"
            )
            print(
                f"[Status] Output: {DeviceConfig.DANTE_ASIO} (ASIO, {AudioConfig.USED_OUTPUT_CHANNELS} channels)"
            )
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
    """Main entry point for the Rumbling Floor audio middleware."""
    print("=" * 70)
    print(" Reality Roost Rumbling Floor Audio Middleware")
    print(" Multi-Channel ASIO Output via Dante Virtual Soundcard")
    print("=" * 70)

    # setup audio devices
    unity_device_index, dante_device_index = setup_audio_devices()
    if unity_device_index is None or dante_device_index is None:
        return

    # start OSC server
    osc_server = start_osc_server()

    # wait for first OSC message before starting audio
    wait_for_initial_osc()

    # run audio processing loop
    run_audio_stream(unity_device_index, dante_device_index, osc_server)


if __name__ == "__main__":
    main()