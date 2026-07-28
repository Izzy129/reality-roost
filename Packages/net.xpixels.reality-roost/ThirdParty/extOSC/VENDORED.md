# extOSC
**Not authored by Rice EXP Lab!!** Do not refactor or restyle anything in this folder.

| | |
| --- | --- |
| Package | `com.iam1337.extosc` |
| Version | `1.21.0` |
| Commit | `b7c2bfa81633cbcbc8cc4312e15cb5fbd0ed7d1d` |
| Source | `https://github.com/iam1337/extOSC.git#upm` |
| License | MIT — © 2018-2021 Vladimir Sigalkin, see `LICENSE` |
| Vendored | 2026-07-28 |

## Why vendored

Reality Roost uses extOSC to send/receive messages from external software, like our OpenCV scripts.
Instead of making users install extOSC separately (they are never exposed to extOSC), we decided to include extOSC directly in the SDK, aka vendoring.
Vendoring eliminates the need for RR users to install extOSC to use the SDK. 

## Local modifications

None. Only `package.json` and `package.json.meta` are removed to prevent nested package manifest issues within UPM.

## Re-syncing
If you need to resync our version of extOSC with the upstream version, follow these steps:

1. `git clone -b upm https://github.com/iam1337/extOSC.git`
2. Diff against this folder, then replace contents.
3. Delete `package.json` and `package.json.meta` again.
4. Update **Version**, **Commit**, and **Vendored** above; record any changes you make to our extOSC copy under the *Local modifications* section.

**Note:** extOSC must not *also* be installed via UPM or OpenUPM. 
