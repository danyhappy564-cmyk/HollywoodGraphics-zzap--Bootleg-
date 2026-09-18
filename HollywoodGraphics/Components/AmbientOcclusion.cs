using EFT.CameraControl;
using UnityEngine;

namespace HollywoodGraphics.Components;

public class AmbientOcclusion
{
    private readonly Camera _camera;
    private readonly HBAO _hbao;
    private readonly HBAO_Core.AOSettings _defaultAOSettings;
    private readonly HBAO_Core.ColorBleedingSettings _defaultColorBleedingSettings;

    // (field report: night vision reads darker than it should) image effects run in
    // component order, and HBAO sits ahead of NightVision on the camera - so ambient
    // occlusion darkens crevices/shadows in the frame BEFORE night vision amplifies
    // it, leaving the goggles with less to work with than intended. no safe runtime
    // API exists to reorder components (that's editor-only, and would touch every
    // other effect after HBAO too), so instead: suppress AO outright while NVG is
    // actually on, and hand it straight back the moment it's off.
    //
    // NightVision.enabled is NOT the right signal - confirmed by two field reports
    // (2026-09) where it fired on toggle-ON but never on toggle-OFF, leaving HBAO
    // suppressed for the rest of the raid after one NVG use. A bool-field reflection
    // dump (logging every field that actually flips when the player hits the NVG
    // toggle key) identified the real one: the private field _on tracks whether the
    // goggles are actively switched on right now, not just equipped.
    private BSG.CameraEffects.NightVision _nightVision;
    private bool _nightVisionSearched;
    private bool _suppressedForNvg;

    public AmbientOcclusion()
    {
        var camera = CameraManager.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("AmbientOcclusion: No camera found!");
            return;
        }

        _camera = camera;
        _hbao = camera.GetComponent<HBAO>();

        // same class of bug as Bloom's missing check: a camera without a
        // pre-configured HBAO (ours doesn't ship one) leaves this null, and every
        // line below unconditionally dereferences it.
        if (_hbao == null)
        {
            Plugin.Log.LogError("AmbientOcclusion: No HBAO component on the camera!");
            return;
        }

        _defaultAOSettings = _hbao.aoSettings;
        _defaultColorBleedingSettings = _hbao.colorBleedingSettings;

        UpdateSettings();
    }

    public void UpdateSettings()
    {
        if (_hbao == null) return;
        if (Plugin.GraphicsConfig.AOEnabled.Value)
        {
            var settings = _hbao.aoSettings;
            settings.intensity = Plugin.GraphicsConfig.AOIntensity.Value;
            settings.radius = Plugin.GraphicsConfig.AORadius.Value;
            settings.bias = Plugin.GraphicsConfig.AOBias.Value;
            settings.useMultiBounce = Plugin.GraphicsConfig.AOMultiBounceEnabled.Value;
            settings.multiBounceInfluence = Plugin.GraphicsConfig.AOMultiBounceInfluence.Value;
            _hbao.aoSettings = settings;

            var colorbleedSettings = _hbao.colorBleedingSettings;
            colorbleedSettings.enabled = Plugin.GraphicsConfig.AOColorBleedEnabled.Value;
            colorbleedSettings.saturation = Plugin.GraphicsConfig.AOColorBleedSaturation.Value;
            colorbleedSettings.albedoMultiplier = Plugin.GraphicsConfig.AOColorBleedAlbedoMul.Value;
            _hbao.colorBleedingSettings = colorbleedSettings;
        }
        else
        {
            _hbao.aoSettings = _defaultAOSettings;
            _hbao.colorBleedingSettings = _defaultColorBleedingSettings;
        }
    }

    // called every frame from GraphicsController.Update() alongside Bloom's own
    // Update() - cheap (one bool read, a component enabled-flag write only on an actual
    // on/off transition).
    public void Update()
    {
        if (_hbao == null || _camera == null) return;

        if (!_nightVisionSearched)
        {
            _nightVisionSearched = true;
            _nightVision = _camera.GetComponent<BSG.CameraEffects.NightVision>();

            if (_nightVision == null)
            {
                // Not the same as the field being missing, and it used to look the same
                // in the log: nothing. Without the component there is nothing to track,
                // so the guard is off and AO stays on, which is worth saying out loud.
                Plugin.Log.LogWarning(
                    "[NvgAoDiag] no NightVision component on the camera — AO/NVG guard "
                    + "inactive, AO stays on.");
            }
        }

        // Read directly. This was reflection because _on was private in 4.0, and the
        // reflection is what quietly stopped working on 4.1 -- not because the field was
        // renamed, but because it turned public, which a NonPublic-only lookup misses in
        // a way indistinguishable from it being gone.
        //
        // It is public now, so there is nothing left to look up. If BSG ever takes that
        // away it is a build error on this machine rather than a guard that silently
        // stops guarding, which is the whole reason the 4.1 deobfuscation is worth
        // leaning on.
        //
        // Still _on rather than the On property: _on is what was verified to track the
        // key in both directions, and On has its own getter that may not.
        var nvOn = _nightVision != null && _nightVision._on;

        if (nvOn && !_suppressedForNvg)
        {
            _suppressedForNvg = true;
            _hbao.enabled = false;
        }
        else if (!nvOn && _suppressedForNvg)
        {
            _suppressedForNvg = false;
            _hbao.enabled = true;
        }
    }
}
