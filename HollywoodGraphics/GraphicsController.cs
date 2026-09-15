using System;
using EFT;
using HollywoodGraphics.Components;
using UnityEngine;
using AmbientOcclusion = HollywoodGraphics.Components.AmbientOcclusion;
using Bloom = HollywoodGraphics.Components.Bloom;

namespace HollywoodGraphics;

public class GraphicsController : MonoBehaviour
{
    private Bloom _bloom;
    private AmbientOcclusion _ambientOcclusion;

    // How many frames to keep offering Bloom a chance to finish configuring itself before
    // giving up and saying so. UltimateBloom's Start() lands on the frame after we add the
    // component, so one or two is the realistic answer; this is slack, not a poll budget.
    private const int MaxBloomConfigureFrames = 120;
    private int _bloomConfigureFrames;

    public void Start()
    {
        // Independent try/catch per stage, on purpose. Before this, Start() was a straight
        // line: the 2026-09-15 Icebreaker log has Bloom's constructor throwing an NRE, and
        // because nothing caught it, ambient occlusion, motion blur and the per-map
        // settings below never ran either - one bad array took the whole mod out for the
        // raid. Bloom itself no longer throws there, but a partial failure should never
        // again be able to cancel the stages that have nothing to do with it.
        try
        {
            _bloom = new Bloom();
            Plugin.Log.LogInfo("Bloom initialized");
        }
        catch (Exception error)
        {
            Plugin.Log.LogError($"Bloom failed to initialize, continuing without it: {error}");
        }

        try
        {
            _ambientOcclusion = new AmbientOcclusion();
            Plugin.Log.LogInfo("Ambient Occlusion initialized");
        }
        catch (Exception error)
        {
            Plugin.Log.LogError($"Ambient Occlusion failed to initialize, continuing without it: {error}");
        }

        UpdateMotionBlurSettings();
        UpdateMapSettings();
        Plugin.Log.LogInfo("Updated all settings");
    }

    public void UpdateMapSettings()
    {
        // Apply per map bloom stuff — same null-Bloom guard as Update(): a failed
        // Start() leaves this null, and these are all externally-callable (config UI,
        // map/weather change hooks), not just our own Update loop.
        _bloom?.UpdateSettings();
    }

    public void UpdateAmbientOcclusionSettings()
    {
        _ambientOcclusion?.UpdateSettings();
    }

    public void UpdateBloomSettings()
    {
        _bloom?.UpdateSettings();
    }

    public void UpdateLensDust()
    {
        _bloom?.UpdateLensDust();
    }

    public void UpdateMotionBlurSettings()
    {
        HfxMotionBlur.UpdateSettings();
    }

    private void Update()
    {
        // final safety net: if Start()'s `_bloom = new Bloom()` threw partway through
        // construction (e.g. ResetIntensities hitting a still-null intensities array
        // on a camera without a fully pre-configured UltimateBloom), _bloom is left
        // null and this NREd every single frame for the rest of the raid - a real,
        // continuous cost on top of whatever caused the constructor to fail in the
        // first place. Doesn't matter WHY the constructor failed; just don't run on a
        // null Bloom.
        // independent of each other: a failed Bloom construction (e.g. on a camera
        // without a fully pre-configured UltimateBloom) must not also skip the AO/NVG
        // guard below - they don't share any state.
        // Finish Bloom's setup as soon as UltimateBloom's own Start() has populated the
        // arrays it indexes. On every camera that ships a serialized UltimateBloom this is
        // already done in the constructor and the first check below ends it immediately.
        if (_bloom is { Configured: false, HasBloom: true } &&
            _bloomConfigureFrames < MaxBloomConfigureFrames)
        {
            _bloomConfigureFrames++;

            if (_bloom.TryConfigure())
            {
                Plugin.Log.LogInfo(
                    $"Bloom: configured on frame {_bloomConfigureFrames} after the camera's "
                    + "UltimateBloom finished starting");
                UpdateMapSettings();
            }
            else if (_bloomConfigureFrames == MaxBloomConfigureFrames)
            {
                if (_bloom.Parked)
                {
                    Plugin.Log.LogInfo(
                        "Bloom: this camera's UltimateBloom is present but disabled, so it never "
                        + "starts and never fills its arrays. That is the map deliberately opting "
                        + "out of the effect - bloom is off here by design. Ambient occlusion and "
                        + "motion blur are unaffected.");
                }
                else
                {
                    Plugin.Log.LogError(
                        $"Bloom: the camera's UltimateBloom still has no usable intensity/usage "
                        + $"arrays after {MaxBloomConfigureFrames} frames - bloom stays at its "
                        + "defaults for this raid. Ambient occlusion and motion blur are "
                        + "unaffected.");
                }
            }
        }

        _bloom?.Update();
        _ambientOcclusion?.Update();
    }

    private void OnDestroy()
    {
        Bloom.Destroy();
    }
}