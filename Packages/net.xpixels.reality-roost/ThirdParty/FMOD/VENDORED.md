# FMOD
**Not authored by Rice EXP Lab!!** Do not refactor or restyle anything in this folder.

| | |
| --- | --- |
| Package | `com.firelight.fmod-for-unity` |
| Version | `2.3.14` |
| Source | `https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-2-02-161631` |
| License | Custom License, see https://fmod.com/licensing |
| Vendored | 2026-08-14 |

## Why vendored

Reality Roost uses FMOD to communicate with the haptic floor via ASIO Dante output.
Instead of making users install FMOD separately (they are never exposed to FMOD), we decided to include FMOD directly in the SDK, aka vendoring.
Vendoring eliminates the need for RR users to install FMOD to use the SDK. 

## Local modifications

None. Only `package.json` and `package.json.meta` are removed to prevent nested package manifest issues within UPM.
