# Haptic Floor SDK Implementation Spec

**Status:** Describes the implemented architecture as of the custom-audio revision. The original OSC/Python-middleware/sound-library design (`HapticDriver.cs`, `middleware.py`) has been fully retired now that FMOD's direct ASIO output has been validated against physical transducers. This now documents what's actually in `Packages/net.xpixels.reality-roost/`.

## Goal

Students trigger tile rumbles by dragging their own Unity `AudioClip`s onto a component and calling `Play()`. An NGO ServerRpc carries the request (a `Resources/`-relative path, not the clip itself) to the host; the host loads that same clip locally, converts it to an `FMOD.Sound`, and plays it directly to the tile's Dante channels over ASIO. Students never see FMOD, tile indices (unless they want manual control), or any sound registry/index — the SDK does not maintain a sound library.

## File Placement

All files live inside `Packages/net.xpixels.reality-roost/Runtime/`:

- `Shared/Core/` — `RRSubsystem` base class (lifecycle + logging for Host/Client subsystems)
- `Shared/HapticFloor/` — constants, validation utils, the static event bridge
- `Client/HapticFloor/` — student-facing API + `NetworkBehaviour` with ServerRpcs
- `Host/HapticFloor/` — the driver that actually moves audio to the floor

## Architecture

### The Core Problem

NGO delivers RPCs only to the `NetworkBehaviour` that declares them, and that `NetworkBehaviour` must compile into both client and host builds. `RealityRoost.Host` has `defineConstraints: RR_HOST`, so it only compiles into host builds — RPC methods can't live there (client builds wouldn't have them, breaking NGO's prefab-hash matching).

A second, related problem: NGO `ServerRpc`s can only carry primitives/blittable/`INetworkSerializable` data — not a `UnityEngine.Object` reference like `AudioClip`. Since Host and Client are the same Unity project shipping the same Assets in both builds, the fix is to send a **string identifier** (a `Resources`-relative path) instead of the clip itself; the Host resolves that string to its own local copy of the same `AudioClip` via `Resources.Load`.

### The Solution: Event Bridge + Resources-path Identifiers

`HapticFloorClient` (a `NetworkBehaviour` in the Client assembly, compiled into both builds) declares the ServerRpcs and exposes the student API. When a ServerRpc executes on the host, it raises a static event on `HapticFloorEvents` (Shared assembly). `HapticFloorDriver` (Host assembly) subscribes to that event and does the actual FMOD/ASIO work. Client and Host never reference each other directly.

```
Student code                          Network                        Host machine
-----------                          -------                        ------------
RRHapticEmitter.Play()
  [Client assembly]
      ↓ HapticFloorClient.Instance.PlayClip(tileIndex, clipResourcePath, intensity, loop)
  HapticFloorClient.PlayClip()   →   ServerRpc crosses network  →   PlayClipServerRpc()
  [Client assembly]                                                   [Client assembly, running on host]
                                                                          ↓
                                                                      raises static event
                                                                      HapticFloorEvents.OnPlayClipRequested(tileIndex, clipResourcePath, intensity, loop)
                                                                          ↓
                                                                      HapticFloorDriver
                                                                        [Host assembly]
                                                                          ↓
                                                                      Resources.Load<AudioClip>(clipResourcePath) → FMOD.Sound (cached) → per-tile FMOD.Channel
```

## Implemented Files

### `Shared/Core/RRSubsystem.cs`

Namespace: `RealityRoost.Shared.Core`

Base `MonoBehaviour` for Host/Client subsystems. Wraps `Awake`/`OnEnable`/`OnDisable` and exposes `OnSubsystemAwake`/`OnSubsystemStart`/`OnSubsystemStop` hooks. Also exposes `LogInfo`/`LogDebug`/`LogWarning`/`LogError` helpers that prefix messages with `[RR][LEVEL] {SubsystemName}:` automatically (`LogDebug` compiled out except in-Editor or with `RR_VERBOSE` defined).

### `Shared/HapticFloor/HapticConstants.cs`

```csharp
public static class HapticConstants
{
    public const int GRID_COLS = 2;
    public const int GRID_ROWS = 3;
    public const int TILE_COUNT = GRID_COLS * GRID_ROWS; // 6
    public const float TILE_SIZE = 0.9144f;    // 36 in, meters
    public const float TILE_SPACING = 0.0127f; // 0.5 in, meters
}
```

No sound-index constants of any kind.

### `Shared/HapticFloor/HapticFloorUtils.cs`

Pure static utilities:
- `PositionToTileIndex(Vector3 worldPosition)` — maps a world position to a tile index (0–5) using the grid constants above. Used by `RRHapticEmitter` to auto-detect which tile it's over.
- `IsValidTileIndex(int tileIndex, string caller)` — range check + `[RR][ERROR]` log.
- `ClampIntensity(float intensity, string caller)` — clamps to [0, 1] with a `[RR][WARN]` if out of range.

### `Shared/HapticFloor/HapticFloorEvents.cs`

```csharp
public static class HapticFloorEvents
{
    // tileIndex, clipResourcePath (Resources-relative, no extension), intensity, loop
    public static event Action<int, string, float, bool> OnPlayClipRequested;
    public static event Action<int> OnRumbleStopped;

    public static void RaisePlayClipRequested(int tileIndex, string clipResourcePath, float intensity, bool loop);
    public static void RaiseRumbleStopped(int tileIndex);
}
```

There is no clip-less "sustained intensity" event anymore — every rumble references a specific clip.

### `Client/HapticFloor/HapticFloorClient.cs`

Namespace: `RealityRoost.Client.HapticFloor`. `NetworkBehaviour` on a scene-placed `NetworkObject`.

Exposes a static `Instance` (set in `Awake`, cleared in `OnDestroy`) — the first singleton in the SDK, since it's a scene-placed `NetworkObject` expected once per scene. Other code (`RRHapticEmitter`, or students calling it directly) uses `HapticFloorClient.Instance` rather than a manually wired Inspector reference.

**Public API:**

```csharp
public void PlayClip(int tileIndex, string clipResourcePath, float intensity, bool loop)
public void StopRumble(int tileIndex)
```

Both validate `tileIndex`/`intensity` client-side before firing the corresponding `[ServerRpc(RequireOwnership = false)]` (any client can trigger any tile), which raises the matching `HapticFloorEvents` event on the host.

### `Client/HapticFloor/RRHapticEmitter.cs` — the primary student-facing component

Namespace: `RealityRoost.Client.HapticFloor`. Plain `MonoBehaviour` (not networked itself — it calls through `HapticFloorClient.Instance`).

Dropped onto whatever GameObject "causes" a rumble (a player's foot, a rolling ball, a vehicle).

**Inspector fields:**
- `AudioClip rumbleClip` — drag-and-drop. Must live under a `Resources/` folder.
- `string rumbleClipResourcePath` (hidden) — auto-computed in an Editor-only `OnValidate()` via `AssetDatabase.GetAssetPath` whenever `rumbleClip` changes. Logs `[RR][ERROR]` and clears itself if the clip isn't under `Resources/`. This is the string that actually crosses the network — students never touch it directly.
- `[Range(0,1)] float intensity`
- `bool loop` — if true, repeats until `Stop()`; if false, plays once and the Host stops it automatically when finished. Same idiom as `AudioSource.loop`.

**Public API:**

```csharp
public void Play()                  // uses the Inspector-set intensity
public void Play(float intensity)   // overrides it for this call
public void Stop()                  // stops whatever is playing on this emitter's current tile
```

`Play()` computes `HapticFloorUtils.PositionToTileIndex(transform.position)` at call time (not cached), so a moving GameObject naturally targets whichever tile it's currently over. Students never pass a tile index directly through this path.

### `Host/HapticFloor/HapticFloorDriver.cs` — the haptic backend

Namespace: `RealityRoost.Host.HapticFloor`. `RRSubsystem`.

Drives the Dante floor directly via **FMOD's Core/Low-Level API over ASIO** — confirmed to work against physical transducers. Owns its own `FMOD.System` bound to the Dante ASIO device (found by name substring, not a hardcoded index), independent of Unity's own audio pipeline.

- **Clip cache**: `Dictionary<string, CachedClip>` keyed by resource path. On first request for a given path: `Resources.Load<AudioClip>(path)` → pull raw PCM floats via `AudioClip.GetData` → `FMOD.System.createSound` with `MODE.OPENMEMORY | OPENRAW | CREATESAMPLE` (raw PCM, no file header) → cached `FMOD.Sound` + channel count. Repeat triggers of the same clip skip the reload.
- **One `FMOD.Channel` per tile** (`_tileChannels[TILE_COUNT]`), not a single shared channel — different tiles can play different clips simultaneously. Each tile's channel gets its own mix-matrix slice routing into that tile's 2 physical output channels (`BuildTileMixMatrix`): mono clips fan out to both; stereo clips keep left/right separation.
- **Interrupt semantics**: a `PlayClip` request for a tile that already has something playing stops the existing channel first, then starts the new one. (Confirmed product decision — not queued, not ignored.)
- **Loop handling**: `Channel.setMode`/`setLoopCount` per-trigger, not baked into the cached `Sound` — the same cached clip can be played looped or one-shot depending on what a given `PlayClip` call asked for.
- Stopped one-shot channels self-invalidate their FMOD handle when they finish; `StopTileChannel` treats `ERR_INVALID_HANDLE` from `Channel.stop()` as benign rather than logging it as an error.

There is no OSC/Python-middleware driver anymore. `HapticDriver.cs` and `middleware.py` have been fully retired — FMOD talks to the Dante ASIO device directly from the Host build. `middleware.py` (`Assets/_RR_Development/Scripts/Old_RumbleFloor/`) stays in that folder as reference only.

**Debug OSC tap**: an optional `OSCTransmitter debugTransmitter` field, unrelated to the audio path itself. When assigned, `HapticFloorDriver` broadcasts a 12-float `/rr/debug/tile_levels` message (one value per Dante channel, mirroring the same address `FmodAsioSpike.cs` used) on every `PlayClipOnTile`/`StopTileChannel`, so `dante_channel_monitor.py` can visualize tile activity without needing physical transducers or a Dante Tx→Rx loopback route (which Dante disallows for a single device subscribing to itself).

## Scene Setup

A scene-placed GameObject (suggested name: `RR_HapticFloor`) with:
- `NetworkObject` component (required for NGO RPC delivery)
- `HapticFloorClient` component (compiles in both builds)
- `HapticFloorDriver` component (host build only, gated by `RR_HOST`)

This GameObject must be in the scene before NGO starts — it's a scene-placed `NetworkObject`, not dynamically spawned. Any `RRHapticEmitter` in the scene finds it via `HapticFloorClient.Instance`, no manual wiring required.

ALWAYS ASK THE USER FOR ANY UNITY SCENE OPERATIONS! DO NOT UTILIZE EDITOR SCRIPTS OR MANUALLY EDIT SCENES!

## Student Usage Example

```
1. Drop a .wav under Assets/Resources/RumbleSounds/Footstep.wav
2. Add RRHapticEmitter to the GameObject that should cause the rumble (e.g. a player's foot)
3. Drag Footstep.wav into the emitter's "Rumble Clip" field
4. Set Intensity / Loop in the Inspector
5. From gameplay code: GetComponent<RRHapticEmitter>().Play();
6. For looping clips: call .Stop() when the rumble should end (e.g. player stops running)
```

## Open Questions / Not Yet Covered

- `HapticFloorClient`/`HapticFloorEvents` only carry a single intensity per tile — independent left/right puck control would need the event/RPC signatures to grow a channel argument.
- `BuildTileMixMatrix` only uses the first 2 channels of a source clip — a >2-channel clip would silently drop the extra channels.
- Timed/pulsing rumble patterns — layer on later.
- Fan subsystem — separate implementation, same architectural pattern.
- FinalIK integration — lives outside the SDK package for now.
