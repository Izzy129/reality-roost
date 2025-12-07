import sounddevice as sd
import numpy as np


#print("available audio devices:")
#print(sd.query_devices())
#print("\n")

# mock intensity array for OSC output
MOCK_INTENSITY_ARRAY = np.array([1.0, 0.5, 0.3, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0], dtype = np.float32)


# finding vb cable
device_index = None
devices = sd.query_devices()
for i, device in enumerate(devices):
    if 'CABLE Output' in device['name'] and device['max_input_channels'] > 0:
        device_index = i
        print(f"found vb-cable output at index {i}")
        break

if device_index is None:
    print("vb-cable not found.")
    exit()
audio_received = False
# called when audio received
def audio_callback(indata, frames, time, status):
    global audio_received
    if status:
        print(status)
        
    # turn stereo audio into mono audio, since each tile only has one audio output
    # tentatively doing this by averaging the 2 channels
    # shape should now be (frames, 1)
    mono_audio = np.mean(indata, axis = 1, keepdims = True)
    
    # numpy should allow easy intensity modulation, (frames, 1) * (16,) => (freames, 16)
    # each channel is amplified by the mono signal
    # trying it on dummy intensity array for now
    processed_audio = mono_audio * MOCK_INTENSITY_ARRAY
    
    # volume of processed_audio
    volume = np.linalg.norm(processed_audio)
    # print(f"Volume: {volume:.2f}")
    
    # lowered the value from 1.0 to 0.1, since it looks like we have a lot of 0's
    if volume > 0.1: 
        audio_received = True 
        print(f"AUDIO DETECTED/PROCESSED")
    
    # test print statements to see if the first few values are as expected
  # print(f"  Max levels: [Ch 0: {np.max(np.abs(processed_audio[:, 0])):.2f}] "
            # f"[Ch 1: {np.max(np.abs(processed_audio[:, 1])):.2f}] "
            # f"[Ch 2: {np.max(np.abs(processed_audio[:, 2])):.2f}] "
            # f"[Ch 3: {np.max(np.abs(processed_audio[:, 3])):.2f}]")
    

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