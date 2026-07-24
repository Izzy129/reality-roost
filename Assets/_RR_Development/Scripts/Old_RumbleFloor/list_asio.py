import sounddevice as sd


def main():
    print("Querying audio devices...\n")
    devices = sd.query_devices()
    hostapis = sd.query_hostapis()

    asio_hostapi_indices = [
        i for i, api in enumerate(hostapis)
        if "asio" in api["name"].lower()
    ]

    if not asio_hostapi_indices:
        print("No ASIO host API found. Is an ASIO driver installed?")
        return

    found_any = False
    for idx, dev in enumerate(devices):
        if dev["hostapi"] in asio_hostapi_indices:
            found_any = True
            print(f"Index {idx}: {dev['name']}")
            print(f"    Host API: {hostapis[dev['hostapi']]['name']}")
            print(f"    Max Input Channels:  {dev['max_input_channels']}")
            print(f"    Max Output Channels: {dev['max_output_channels']}")
            print(f"    Default Sample Rate: {dev['default_samplerate']}")
            print()

    if not found_any:
        print("No ASIO devices found among available devices.")


if __name__ == "__main__":
    main()