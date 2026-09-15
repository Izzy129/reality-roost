# Roost UI single-pass stereo migration

Applied the approach described in `Unity-6.6-Single-Pass-fix.md` to this checkout.
The supplied document is a report, not a source patch; these shader implementations
were recreated locally. The project remains on Unity 6000.0.60f1 / URP 17.0.4.

## Changes

- Added URP `HLSLPROGRAM` SubShaders to TMP Mobile Distance Field, Distance Field,
  and Distance Field Overlay, retaining the original Built-in SubShaders and GUIDs.
- Added the URP path to both SDK and Assets copies of the custom UI shaders.
- Added `RoostURPUIBridge.hlsl`, UI/laser shaders, and materials with `.meta` files
  under `Packages/net.xpixels.reality-roost/Runtime/Shared/Shaders`. Keeping these
  together in the SDK avoids a package dependency on an `Assets/Astra` directory.
- Replaced only serialized material references: seven default UI Images and six
  LineRenderers in Roost Origin; four default UI Images in the calibration prefab.
  Existing one-sided materials retain their culling behavior and use the patched shader.
  TMP components retain their font materials. Other prefab records are preserved.
- Both copies of RR_Boot inherit Roost Origin directly, with no material overrides;
  they require no scene edits in this checkout.
- Set Android OpenXR to Single Pass Instanced. Standalone already used SPI.

## Verification and deployment

The supplied document's simulated stereo comparison tool and source assets were not
included. Its reported two-eye test results do not validate this recreated patch.
Local shader validation artifacts are under `Logs/StereoShaderValidation*`.

The isolated Unity 6000.0.60f1 / Direct3D 11 check compiled nine shaders across
54 variants with zero shader errors. Test copies explicitly expose stereo
instancing and multiview keywords for offline compilation; this does not exercise
XR rendering or validate Vulkan multiview. TMP emitted vector-truncation warnings
in retained shading expressions. Structural checks verified the original fallback
SubShaders and GUIDs and exactly 13 + 4 material-only prefab changes.

Rebuild and redeploy the **XR client**, then verify both controller rays,
calibration UI, scene menu, text, masking, and transparency in both eyes on Unity
6.6. Also check Android/Vulkan if used. A host-only rebuild does not update client
shaders. Physical-headset rendering and player shader stripping require this check.

TMP Essential Resources reimports and SDK replacements can overwrite patched files.
When transferring this fix, include the modified TMP shaders (with original metas),
SDK shaders/materials and metas, and the prefab assignments.
