import sounddevice as sd
import numpy as np

# all audio devices
print("available audio devices:")
print(sd.query_devices())
print("\n")

# finding vb cable
device_index = None
devices = sd.query_devices()
for i, device in enumerate(devices):
    if 'CABLE Output' in device['name'] and device['max_input_channels'] > 0:
        device_index = i
        print(f"found vb-cable output at index {i}")
        break

if device_index is None:
    print("vb-cable not found. set device_index manually.")
    exit()
audio_received = False
# called when audio received
def audio_callback(indata, frames, time, status):
    global audio_received
    if status:
        print(status)
   
    # volume
    volume = np.linalg.norm(indata) * 10
    print(f"Volume: {volume:.2f}")
    
    if volume > 1.0:
        audio_received = True 
        print(f"AUDIO DETECTED")
   
    # TODO: what to do with audio?
    # indata has audio as a numpy array

# testing for listening

try:   
    with sd.InputStream(device=device_index, channels=2, callback=audio_callback):
        sd.sleep(10000)
    
    if not audio_received:
        print("\n NO AUDIO")
    else: 
        print("\n SUCCESS")
except KeyboardInterrupt:
    print("\n STOPPED")
    if audio_received:
        print("SUCCESSFUL")
    else:
        print("NO AUDIO")