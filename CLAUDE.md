# Reality Roost — Unity Project

Colocated multiplayer VR facility at Rice University (EXP Lab / XPixels). Ships a Unity SDK for ELEC 328 students to build VR experiences on specialized room hardware.

## Build Architecture

One Unity project → two builds via Unity 6 Build Profiles (`RR-Client`, `RR-Host`).

- Host build is gated by custom scripting define `RR_HOST` (set in the RR-Host profile's Build Data → Scripting Defines).
- Do NOT use Unity's Dedicated Server build target — it strips rendering. The host must render (bird's-eye camera → KlakNDI → NDI output).
- `UNITY_SERVER` is not used anywhere.

## SDK Package

Embedded UPM package at `Packages/net.xpixels.reality-roost/`.

Three runtime assemblies, organized layer-first:

- `Runtime/Shared/` → `RealityRoost.Shared.asmdef` — network message types, enums, `INetworkSerializable` structs, `RRSubsystem` base class, `RRConfig`. No define constraints. `autoReferenced: true`.
- `Runtime/Client/` → `RealityRoost.Client.asmdef` — student-facing API (`RRHapticFloor.Rumble()`, etc.), XR integration, NGO client bootstrap. Refs Shared. `autoReferenced: true`.
- `Runtime/Host/` → `RealityRoost.Host.asmdef` — OSC middleware connectors, NGO host bootstrap, NDI rig. Refs Shared. `defineConstraints: RR_HOST`. `autoReferenced: false`.
- `Editor/` → `RealityRoost.Editor.asmdef` — inspectors, editor tools. Refs Shared + Client. `includePlatforms: Editor`.

**Dependency rule:** Client → Shared, Host → Shared. Client ↔ Host never reference each other. If both need a type, it goes in Shared.

## Assets Layout

- `_RR_DemoAssets/` — third-party Asset Store packs for demo scenes only
- `_RR_DevAssets/` — third-party dev assets (RootMotion/FinalIK). No asmdef — FinalIK-dependent code stays outside the SDK package for now.
- `RR_Development/` — dev scratch scripts/scenes, per-member subfolders. Not shipped.
- OpenCVForUnity is removed/legacy — do not use or reference it.

## Hardware

- Headsets: Meta Quest 3S, hand tracking only (no VR controllers)
- Haptic floor: 6 tiles × 2 Dayton Audio TT25-8 pucks each = 12 Dante channels
- Amplifier: MA1240a (not MA1260). Dante interface: AVN-AO16.
- Fan array for wind simulation
- Room cameras: 4× Zed X (fixed positions)
- Full body tracking: Vive 3.0 trackers
- Streaming: Virtual Desktop (CloudXR custom client planned) → Quest 3S

## Client/Host Split

- Host owns the room: floor, fans, cameras, ArUco, calibration, NGO host, middleware comms (OSC to Python).
- Client owns the headset: rendering, streaming, hand/FBT tracking, NGO client.
- Clients never touch hardware directly — they send ServerRpcs to the host.
- Students produce client builds. The host is an internal RR team application.

## Key Dependencies

- Unity 6000.0.60f1, URP
- NGO: `com.unity.netcode.gameobjects` 2.7.0 (assembly: `Unity.Netcode.Runtime`)
- ExtOSC: `com.iam1337.extosc` 1.21.0 (verify assembly name — needed by Host asmdef)
- KlakNDI: not yet installed (add to Host asmdef when installed)
- XR: OpenXR, XR Hands, XR Interaction Toolkit, XR Core Utils, Input System

## External Systems (not in this repo)

- Python middleware: receives OSC from Unity host, drives Dante audio channels (haptics) and fan hardware. Separate repository.
- CDS (Centralized Deployment System): Python-based, sends client.exe builds from host PC to client PCs. Separate repository.
- Operator dashboard: web/Flask, separate design doc. Only its Unity hooks (bird's-eye NDI, heartbeat) live in this repo.
- ArUco CV pipeline: Python, emits finished room-space poses over OSC. Unity consumes final poses, does not own CV.

## Style

- Direct, concise. No vague phrasing ("does the real work", "gives teeth to").
- Define jargon on first use when writing docs.
- Doc house style: bullet-heavy, inline `code`, `Note:` callouts, **bold** key terms on first use.