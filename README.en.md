<p align="center">
  <a href="README.md"><img alt="한국어" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%95%9C%EA%B5%AD%EC%96%B4%EB%A1%9C%20%EB%B3%B4%EA%B8%B0-6e7681?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English (this page)" src="https://img.shields.io/badge/English-This%20page-1f6feb?style=for-the-badge"></a>
</p>

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

- **Original Author:** SleepingPills
- **Original Repository:** HollywoodGraphics
- **Original Link:** https://github.com/SleepingPills/HollywoodGraphics
- **License:** MIT
- **This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original author.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## Changelog

The full changelog (1 entry) is in the [`changelog/`](changelog/) folder, 10 per file (higher number = newer, latest: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). Per-release summaries: [Releases](../../releases).

# HollywoodGraphics (SPT 4.1.5)

> **Original author · Original repository**
> **SleepingPills** — https://github.com/SleepingPills/HollywoodGraphics
> Also published on SPT Forge.
>
> **License: MIT** (the original repository's `LICENSE`, included unchanged in this fork)
>
> This repository is a **fork ported to run on SPT 4.1.5**.
> No features were added or changed; it only restores references broken
> by 4.1's client deobfuscation. The mod's design belongs entirely to the original author.

---

Original: SleepingPills / This fork: SPT 4.1.5 support (migrated from the previous 4.0.10-compatible version)

**The original has no README.** The Mod description below was therefore compiled from code/settings
rather than translated, and was not written by the original author. The Detailed changes below it
describe what this fork actually changed.

---

\<Mod description — compiled from code because the original has no README>

A client plugin that adjusts post-processing graphics. No server mod.

- **Ambient occlusion (HBAO)** — intensity, radius, bias, color bleeding, offscreen influence
- **Bloom (Ultimate Bloom)** — separate intensity for each brightness range
  (dark/mid/bright/highlights); lens dust, anamorphic flare, star flare
- **Motion blur** — shutter angle and sample count
- **Terrain detail** — grass/weed draw distance and density overrides
- **Per-map presets** — checks the map at raid start and applies its settings

**Automatically disables itself if Amand's Graphics is installed**, because both modify
the same post-processing.

Adjust through F12 BepInEx Configuration Manager; settings are saved separately for each map.

**Build** — Requires .NET SDK and an SPT 4.1 installation.

```
dotnet build
dotnet build -p:SptRoot="D:\내 SPT 경로"     # 기본값이 아닐 때
dotnet build -p:AutoInstall=false            # 설치본에 복사하지 않기
```

Automatically copies to `BepInEx\plugins\HollywoodGraphics\` after building. If the game is running,
the DLL is locked and copying fails; this produces a warning rather than failing the build.

Post-build installation copying includes `RELEASE_README.txt` and `LICENSE`. MIT requires
the full license and copyright notice with redistribution, and the original author requested
credits beside the files. They are already in the plugin folder, so there is no need to add them
separately when making a distribution zip.


---

<26/08/30 Detailed changes>

- Game assembly references were hardcoded to the original author's local folder layout
  (`..\..\..\Client_Dev\...` relative paths), preventing resolution elsewhere. Made them
  overridable with the `SptRoot` property

- **NullReferenceException spam every frame** — The `Bloom.cs` constructor accessed fields
  immediately after `AddComponent<UltimateBloom>()`, before `Start()` had run.
  Added null checks in three places:
  - Array check in `ResetIntensities`
  - `_ultimateBloom` check in `Bloom.Update()`
  - `_bloom` check in `GraphicsController.Update()` — the decisive fix, a final safety net
    against per-frame spam regardless of why construction fails
  - Also made externally called public methods such as `UpdateMapSettings` / `UpdateBloomSettings` /
    `UpdateLensDust` null-safe for the same reason
  - The constructor can still fail about once per raid (harmless, no visual difference),
    but repeated spam is gone — confirmed in actual raid logs

- Same bug in `AmbientOcclusion.cs`: accessed fields on the next line after
  `camera.GetComponent<HBAO>()` without checking null. Added a check

- **Abnormally dark screen with night vision** — Image effects run in component order.
  HBAO (AO) ran before NightVision, darkening the image before night vision amplified it.
  With no safe runtime API to reorder components, the fix disables AO
  only while night vision is on and restores it when off
  (reproduced on all maps; improvement confirmed in field testing)

---

<26/09/03 Detailed changes>

- **(Correction to the 08/30 fix)** Confirming that the night-vision AO guard made things brighter
  was only a partial check. It used `NightVision.enabled`, which changes on activation
  but **never changes on deactivation** (even repeated N-key toggles never logged
  an off transition). Using night vision once in a raid therefore left AO suppressed
  until raid end even after switching it off. Discovered from a field report
  that AO seemed disabled even without night vision

- Temporarily added diagnostics using reflection to scan all `NightVision` bool fields,
  logging actual changes on toggles. Repeated N-key testing in a raid confirmed
  that private field `_on` followed both directions correctly
  (`.enabled` stayed True)

- Switched the guard to `_on` and removed the diagnostic field dump

---

<26/09/07 Detailed changes — SPT 4.1.5 support>

**SPT 4.1 deobfuscated the client.** Types now have real names and namespaces,
and **no 4.0 client mod loads on 4.1**.

## Renamed identifiers

Every identifier used by this mod was checked against the official wiki's 5,957-line mapping table.
**Only one changed**, because this mod touches relatively few EFT types:

| 4.0 | 4.1 |
| --- | --- |
| `CameraClass` | `EFT.CameraControl.CameraManager` |

Three locations (`AmbientOcclusion.cs`, `Bloom.cs`, `MotionBlur.cs`) + `using EFT.CameraControl;`.

The others (`GameWorld`, `TarkovApplication`, `LampController`, `GPUInstancerDetailManager`,
`FlareLight`, `MaterialEmission`, `RaidSettings`, `BSG.CameraEffects.NightVision`,
`HBAO`, `UltimateBloom`) are absent from the table, so their names are unchanged.
The serialized-field names restored in 4.1.4 were also checked; none apply.

## `method_41` → `LocalGameMatching`

This is the raid-initialization patch target. The wiki maps **types only**, not methods,
so a different approach was needed.

Two assembly dumps (before/after deobfuscation) came from the same file and retained method order.
Numbering only obfuscated methods 0,1,2… reconstructs de4dot numbering.
Using 11 methods named by `assembly-tool` as anchors, **all 11 matched
at offset +2** (`method_38 = OnApplicationLoaded`, `method_49 = LocalGameCreate` …).

In that alignment:

```
method_40 = OnAbortFinished
method_41 = LocalGameMatching      ← Task LocalGameMatching(TimeAndWeatherSettings, bool)
method_42 = NetworkGameMatching    ← 네트워크 게임용 짝
```

This is the entry point for matching into a single-player raid. `_raidSettings` is populated
and the map is not loaded yet, fitting the per-map graphics-override patch semantically too.
Confirmed in actual raids:

```
[HollywoodGraphics] Running raid initialization
[HollywoodGraphics] Graphics overrides map: woods - Woods enabled: True
```

Now it has a real name, so future removal causes a **compile error**,
rather than silently patching the wrong method.

## Night-vision AO guard — inactive on 4.1

The guard fixed on 09/03 was **ineffective on 4.1**. Its fallback safely prevented
crashes as designed, but the fallback was the pre-fix state, so **the original
night-vision darkening problem returned**.

The cause was not a rename. Confirmed by assembly dump:

```
=== BSG.CameraEffects.NightVision
    field    bool _on   [Public]
```

**`_on` had become `public`, not changed names.** The 09/03 lookup used
`BindingFlags.Instance | NonPublic`, so it could not see a now-public field.
Its logs looked exactly like a missing field.

A public field needs no lookup, so **reflection was removed in favor of direct access**:

```csharp
var nvOn = _nightVision != null && _nightVision._on;
```

This is not merely shorter. Reflection can silently fail at runtime and fall back to the
pre-fix state, hiding the returning bug — exactly what happened. Direct access
causes a **compile error on the build machine** if BSG makes it private again.

There is also an `On` property, but `_on` is used because it was verified with both N-key
toggle directions on 09/03; `On` has its own getter and is not guaranteed to match `_on`.

A missing component has a different cause/remedy, so it gets a separate warning.

## Entire mod failing on custom maps (2026-09-15)

On Icebreaker, **HollywoodGraphics stopped working entirely**: bloom,
AO, motion blur, and per-map settings. Logs stopped here:

```
[Info :Janky-HollywoodGraphics] Resetting Star Bloom intensities
[Warning] FIRST Exception: NullReferenceException
  HollywoodGraphics.Components.Bloom..ctor ()
  HollywoodGraphics.GraphicsController.Start ()
```

Lighthouse logs from the same day showed the normal path for comparison:
`Bloom: Ultimate Bloom effect applied to camera FPS Camera` → `Bloom initialized` →
`Ambient Occlusion initialized` → `Updated all settings`. All four lines
were absent from Icebreaker logs.

### Cause

The `Bloom` constructor adds `UltimateBloom` via `AddComponent` if the camera lacks it.
However, **`AddComponent` runs only `Awake` synchronously; `Start` runs next frame**,
and `UltimateBloom` fills its arrays (`m_BloomIntensities`, `m_BloomUsages`, etc.)
in `Start`. A newly added component therefore has all its arrays set to `null` that frame.

Standard maps already serialize `UltimateBloom` in camera prefabs and avoid this path.
It fails only on **custom maps that create their own camera**.

`ResetIntensities` already had a `null` guard. The problem was **the very next line**,
which had none:

```csharp
_ultimateBloom.m_BloomUsages[0] = _ultimateBloom.m_BloomUsages[1] = false;  // ← NRE
```

The evidence was three `Resetting ... intensities` lines with no `Intensity: 1` below them:
all three returned immediately through the guard, so the bool array was also
`null` for the same reason.

### Fix

Moved array-dependent configuration out of the constructor into **`TryConfigure()`**,
returning `false` instead of throwing when not ready. `GraphicsController.Update()`
retries on later frames until ready (maximum 120 frames; usually 1~2).

- Standard maps: succeeds on the first constructor call → retry code never runs
- Custom maps: succeeds/logs on the frame after `UltimateBloom.Start()`
- Persistent failure: **only bloom** stays at defaults; AO/motion blur work normally, with one error line

Also changed:

- Wrapped bloom and AO initialization in `GraphicsController.Start()` in **separate try/catch blocks**.
  Sequential initialization was why this failure disabled so much. One failing stage
  must not prevent the rest
- Added `_ultimateBloom == null` guards in `Bloom.UpdateSettings()` / `UpdateLensDust()`.
  `AmbientOcclusion.UpdateSettings()` already had one; bloom did not.
  The constructor returns early without a camera, but `GraphicsController`'s `_bloom?.`
  only checks the **Bloom object**, not its internal `UltimateBloom`

### Follow-up — parked bloom is not a failure

Logs from the fixed build showed no crash, and **AO, motion blur, and map settings worked again**
(they previously failed together). Only bloom still could not be configured:

```
Bloom initialized
Ambient Occlusion initialized
Updated all settings
[Error] Bloom: the camera's UltimateBloom still has no usable arrays after 120 frames
```

This was not a timing issue. Icebreaker's log explained it:

```
[CamDonor] UltimateBloom provisioned for HollywoodGraphics
           (keeps its init alive; the renderer itself gets parked by the guard)
```

The map mod **provides a component for our constructor to find but disables it
because it does not want that effect rendered on its camera**. A disabled MonoBehaviour
never runs `Start`, so its arrays are never filled. **An intentional setup, not a fault.**

This case is now identified and logged as `Info` rather than `Error`. The map mod
was left unchanged again; there is nothing to fix there.

**Verified in actual raids:** Post-fix logs show no crash and the complete normal path.
Zero `Error` entries.

```
Bloom initialized
Ambient Occlusion initialized
Updated all settings
[Info] Bloom: this camera's UltimateBloom is present but disabled, so it never
       starts and never fills its arrays. That is the map deliberately opting out ...
```

Previously **none** of these four lines appeared. AO/motion blur were dying together
with bloom; they now work.

Icebreaker's camera-donor JSON was also checked: of its 71 `UltimateBloom` fields,
**none are arrays (`m_BloomIntensities` / `m_BloomUsages`, etc.)**.
The graft could never fill them by itself, so retries were needed regardless
of whether the component was parked.

The map mod was not changed. This fix applies equally to any custom map
using its own camera, not just Icebreaker.

## Build paths

Changed default `SptRoot` to the SPT 4.1 installation and searches for game/BepInEx locations
instead of guessing (root → `SPT_Runtime\` → `SPT\`, separately). Missing references
stop the build with a line identifying the folder/file; selected assemblies are printed during build.

Replaced installation `copy /Y` with MSBuild `Copy`, producing a warning
rather than failing the build when a running game locks the DLL.

## Verification

Actual raids: all four patches applied, no failures/warnings. Confirmed raid initialization,
`woods` map overrides, and terrain-detail overrides.

## Reference — assembly-name inspection tool

`tools/AssemblyDump` lives in the **HollywoodFX repository**. It extracts real type/member
names and signatures from game assemblies; all renames in this port were confirmed with it.

**Deobfuscation happens when the game first reaches the main menu, not during installation.**
Building beforehand uses BSG's original names; all SPT-renamed names appear missing,
making the mapping table look incorrect.

