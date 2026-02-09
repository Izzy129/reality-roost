import os

# apparently need to do this accordingly to docs
os.environ["SD_ENABLE_ASIO"] = "1"

import sounddevice as sd


# List all host APIs
print("Available host APIs:")
for i, api in enumerate(sd.query_hostapis()):
    print(f"{i}: {api['name']}")

# Find ASIO host API index
asio_api = None
for i, api in enumerate(sd.query_hostapis()):
    if 'ASIO' in api['name']:
        asio_api = i
        break

if asio_api is not None:
    print(f"\nASIO devices:")
    devices = sd.query_devices()
    for i, dev in enumerate(devices):
        if dev['hostapi'] == asio_api:
            print(f"{i}: {dev['name']} - {dev['max_output_channels']} channels")
else:
    print("ASIO not available")