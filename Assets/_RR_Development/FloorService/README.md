# External Floor Software integration

This project-local integration lives under `Assets/_RR_Development/FloorService`, outside the SDK. **Floor Service connects and monitors the hardware. Floor Player configures, previews, and triggers each effect.** Python and Dante/ASIO playback remain in the separately running Floor Software.

## Setup and editor testing

1. Start the external Floor Software using its without-lights launcher. Disable the legacy Unity `HapticFloorDriver` so the external service alone owns ASIO.
2. Use the **Floor Service** on the root GameObject in **RR_Calib**. It persists when the experience loads. It defaults to `http://127.0.0.1:8765`; the URL is private and hidden from the Inspector, with existing serialized values preserved. This component shows the connection, tile status, and Stop All control.
3. Create a GameObject for each effect and add **Reality Roost > Floor Player**. Floor Player finds the persistent service automatically. Configure the effect: AudioClip plus tiles/intensity/loop, or a preset.
4. Click **Preview Effect** on Floor Player to test outside Play mode. If needed, the editor opens RR_Calib in a temporary preview scene. Use **Stop All Tiles** to stop the sound and cancel a repeating pattern. Preview affects physical hardware and does not invoke the player's runtime notification events.
5. For runtime playback, choose When To Play > On Start or connect an experience event to the player's Play()/Stop() methods.

Preview continues when you select another object. You can stop it from either Floor Player or Floor Service. Stop requests continue processing after selection changes. Stop and allow completion before entering Play mode, reloading scripts, disabling Floor Service, or closing Unity: teardown can interrupt HTTP requests.

Floor Service's **Diagnostics** foldout retains the previous direct sound/preset controls for troubleshooting and compatibility. Those settings are separate from Floor Player's effect settings. For ordinary effect design and testing, use Floor Player. A preset without its own AudioClip still uses Floor Service's diagnostic source as the legacy fallback; assign the preset's clip to keep an effect self-contained.

AudioClips must use **Decompress On Load** and **Preload Audio Data**. First preview uploads audio; subsequent requests reuse cached uploads while the service connection remains open. Floor Service's monitor shows service-reported playback, not measured vibration.

## Runtime workflow: define an effect, then trigger it

Floor Player defines **what** plays. An event in your experience decides **when** it plays. Both AudioClips and presets use exactly the same `BeginPlay()` and `EndPlay()` methods.

### One sound effect

1. Create a GameObject named for the effect, for example **Track Bump**, and add **Reality Roost > Floor Player**.
2. Choose **Audio Clip**, assign your clip, then select tiles and intensity. Enable Loop only for a continuous effect.
3. Set **When To Play** to **When Triggered**. In the event that represents the impact, press **+**, drag **Track Bump** into the target, and select **RRFloorPlayer > BeginPlay()**.

A non-looping sound finishes naturally. For loops or early cancellation, connect an ending event to the same player's **EndPlay()**. Stop affects all tiles on that Floor Service.

### A reusable tile pattern

1. Use **Create > Reality Roost > Floor Sequence** in the Project window, or duplicate **Train Rolling**. Configure the preset's clip, tile groups, timing, intensity, and Repeat option.
2. Create a GameObject such as **Train Rumble** with Floor Player. Choose **Preset** and assign that asset.
3. Wire your experience's starting event to **Play()**, and its ending event to **EndPlay()**, exactly as for an AudioClip.

Choose **When To Play > On Start** only when the effect should begin automatically. It waits for the host and floor audio. Otherwise trigger Play from your experience. No speed-specific floor component or named-sound list is required.

### Choosing when an effect plays

**When To Play** has two choices:

- **On Start:** start once when the host and floor audio are ready.
- **When Triggered:** your experience calls `BeginPlay()` and `EndPlay()`. These methods work identically for AudioClips and presets.

Under When Triggered, expand **Play from an event** or **Play from a script** for setup instructions. Floor Player waits for your experience to call these methods. There is no Event Source picker. Configure UnityEvent listeners on the component that raises the experience event, using Unity's standard Inspector controls.

**Create Effect Script** remains available for script-based control. The generated script has a Floor Player reference; assign your effect to that field in the Inspector. Call BeginEffect/EndEffect from your scene logic. Do not call Play every frame: it restarts a preset. Call it once when an experience state changes.

For a UnityEvent, add a listener on the component that knows when something happens (for example, the train's On Started Moving). Drag the effect GameObject into the target and select **RRFloorPlayer > BeginPlay()**. Wire the corresponding ending event to **EndPlay()**. A Timeline Signal Receiver reaction uses those same methods. No additional floor-specific trigger component is required.

Floor Player cannot determine when a scene-specific event happens by itself. The event comes from your experience. Its optional playback notifications report requests after they happen; they do not configure the trigger. Stop still affects all tiles on the referenced service. Trigger shared effects on the host.

### One component, with optional custom scene logic

Floor Player now provides both effect configuration and the public **BeginPlay() / EndPlay()** actions. Add **Reality Roost > Floor Player**, choose its clip or preset, and set **When To Play**. The separate Floor Effect component has been removed. Existing Play()/Stop() calls remain supported as compatibility aliases.

For different code logic per experience, choose **Create Effect Script** on Floor Player, or **Assets > Create > Reality Roost > Floor Effect Script**. Name the new script uniquely (for example `TrainFloorEffect`), add it to the experience's GameObject after compilation, and assign its Floor Player. Edit that new script to implement the experience's conditions. The generated file is independent of the shared Floor Player component and other generated classes.

C# code belongs to the script class, not to an individual component instance. Editing `TrainFloorEffect` affects all instances of that class. Use Inspector fields for different settings, or create another uniquely named script for different logic. Duplicating a GameObject or prefab does not isolate its script code. Scripts in custom assembly definitions need a reference to `RealityRoost.LocalFloor`. Keep calls on the host, and remember EndEffect stops all tiles on the referenced service.

### Working example: Train Journey

In **RR_Demo_TrainJourney**, select **Tile Sound Effects > Train Rumble**. It has:

- **Floor Player:** the Train Rolling preset, configured as When Triggered.
- **Train Rumble Effect:** references World Rail / Train Journey and that Floor Player.

`TrainRumbleEffect.cs` reads the current train speed when Floor Service reports that the host's audio is ready, including when the effect is enabled after the connection is established. It then listens to WorldRailTunnelJourney.SpeedChanged: positive speed starts or retimes the preset; zero stops it. A reconnect reads the latest speed again. The effect unsubscribes and stops on disable. It has no coroutine, Update, or LateUpdate.

World Rail Tunnel Journey exposes a general SpeedChanged event, with no floor-specific code. Assign train.speed from scripts as before; its property now raises the event only when the value changes. The Inspector still shows Speed, and existing serialized speed values migrate automatically. Inspector edits are delivered on the main thread by the journey's existing Update. Train Rumble Effect captures the initial positive speed, or the first positive speed if starting at rest. The playback rate is current speed divided by that value, clamped by Floor Player to 0–8. This follows the configured speed directly regardless of journey phase. Only preset timing changes; clip pitch, duration, and gain remain unchanged. Configure Floor Player as When Triggered and use a repeating preset for continuous rumble.

Custom scripts can call `floorPlayer.SetPlaybackRate(multiplier)` for other experiences. Each Floor Player can retime only the preset it currently owns on the shared service. Keep a repeating preset assigned for continuous rumble. At very high rates, HTTP latency and clip overlap limit physical timing; test on the floor. Floor Service and Floor Player do not contain train-specific speed mapping.

### Playback notifications and timing

The collapsed **Playback Notifications (Optional)** section on Floor Player contains outgoing callbacks **After Play Request** and **After Stop Request**. These can trigger additional actions after a command; they are not conditions or subscriptions to scene events, and they do not confirm audible playback or natural completion. Existing serialized callback bindings are preserved.

First playback can wait for an upload; HTTP requests are not sample-accurate. Presets use their own clip or fall back to Floor Service's source when empty. Playing a preset replaces the active preset; a direct clip cancels an active preset. Stop affects all tiles on the service. Explicitly stop and allow commands to finish before tearing down Floor Service. Floor Playback Sequence has been removed; tile-pattern preset assets remain supported.

## Gameplay methods

### Train Rolling sequence preset

Drag `Presets/Train Rolling.asset` into Floor Service's **Sequence** field. Click **Play Sequence** to preview and **Stop Sequence** to stop all tiles. It defines the three row pairs: Tiles 1–2, then 3–4, then 5–6. The asset's current timing, clip and per-row intensities are the source of truth; select the preset asset to tune them. Each step plays once, regardless of the controller's advanced Loop setting.

Select the preset asset to edit its AudioClip, per-step times, tile masks and intensities, repeat option and cycle duration. Times are offsets from the start of a pass. A preset AudioClip overrides Floor Service's clip; leaving it empty uses the controller's source. The sound is uploaded before the first step, and the sequence snapshots its settings when started. Restart to apply preset edits. Duplicate this asset or use **Create > Reality Roost > Floor Sequence** for another pattern.

Bind `PlaySequence()` / `StopSequence()` to UnityEvents or call them from narrative code. Assign the same preset on the host and client: one request starts the host's sequence, with no per-headset timing. The sequence uses the host's preset settings and owns all six tiles; starting it replaces existing playback. Manual play/stop controls cancel the sequence. Stop All also cancels future steps. The last sound of a non-repeating sequence finishes naturally.

HTTP timing is approximate. Steps wait for command completion; stalls stretch the sequence rather than releasing a burst of overdue steps. Service failures stop the sequence. Keep the Floor Service Inspector selected during Edit-mode preview; deselection cancels future steps, and already-dispatched one-shots finish. During Play mode it runs independently of selection. Use Stop Sequence before teardown; physical timing and feel require hardware testing.

```csharp
using RealityRoost.Shared.HapticFloor;
using UnityEngine;

public class TrainExample : MonoBehaviour
{
    public RRFloorService floor;
    public void WheelImpact() => floor.Play();
    public void AllTilesImpact() => floor.PlayAll();
    public void EndExperience() => floor.StopAll();
}
```

- `Play()` / `PlayGroup()` play the configured source on the selected tiles.
- `PlayAll()` plays the configured source on all six tiles.
- `Stop()` stops the selected tiles; `StopAll()` stops all six. Either cancels an active sequence.
- These parameterless methods can be bound to UnityEvents.
- The explicit sound-ID methods remain available: `Play(0, soundId, 0.5f, false)` and `PlayGroup(FloorTiles.Tile1 | FloorTiles.Tile3, soundId, 0.5f, false)`. Single-tile indices are 0â€“5; enum flags and visual labels use Tile 1â€“6.

## Upload and playback

An assigned AudioClip takes priority over **Sound Id**. Unity downmixes its decoded samples into mono PCM16 WAV, uploads to `/v1/sounds`, and plays the returned ID. The first play waits for upload; later requests reuse the uploaded content ID for the connection. The upload limit is 25 MiB. Upload errors never start playback. Stopping a tile while its upload is pending removes that tile from the queued playback.

Leave Audio Clip empty to use an existing service sound ID or select a sound after **Refresh sound catalog**. Uploads can also be managed in the external dashboard. `roost/knock` is an example, not a guaranteed installed sound.

Group playback uses one HTTP batch request so the selected tiles start together. Group stops use individual confirmed requests because the service has no batch stop endpoint. StopAll clears queued plays and queues six stops after any in-flight request; it is not an atomic emergency stop.

## Multiplayer

Use the same scene-placed Floor Service NetworkObject in the host/client scene and let Netcode spawn it. Assign the same imported AudioClip to that controller in both builds. Clients send the asset GUID and playback settings; the host checks its assigned clip and uploads its own copy. Client-only audio buffers are not transferred through Netcode. Direct dynamically generated clips are supported on the host through `PlayClip`.

Only the network host contacts the service. A standalone player requires `RR_HOST` for direct access; standalone Editor testing is supported. Keep the service URL configured from the host's perspective. Multiplayer scene behavior still needs end-to-end validation.

## Monitoring and lifecycle

The Inspector polls `/v1/status` every 0.5 seconds and shows readiness, tile sound, playing/looping state, and gain. It reflects service playback, not measured vibration or actual audio amplitude. Failed, malformed or stale status is unknown rather than idle.

In Edit mode, Floor Player previews are ticked by the preview manager even after selection changes. Legacy diagnostic playback still requires the Floor Service Inspector to remain selected. In Play mode, the component ticks independently of selection. Disabling it or exiting Unity cancels local HTTP work but does not stop audio already running in the external service. Explicitly stop and allow completion before teardown; the external dashboard can stop remaining loops.

Floor Sound has been removed. If an unsaved scene still contains the former component, remove it and assign the AudioClip directly on Floor Service. Existing UnityEvents targeting Floor Sound must be rebound to Floor Service.

## Validation

`Tests/Editor` uses a loopback HTTP server without touching floor hardware. Tests cover WAV encoding, uploads and reuse, upload failures, stop-during-upload cancellation, batch payloads, ordering, validation and status. Run `RealityRoost.Floor.Tests` in Unity Test Runner's EditMode tab.
