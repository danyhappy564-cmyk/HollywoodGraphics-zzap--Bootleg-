using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using EFT.CameraControl;
using EFT.Weather;
using UnityEngine;
using EFT.CameraControl;

namespace HollywoodGraphics.Components;

public class Bloom
{
    private float _sunLightFactor = 10000f;

    private readonly UltimateBloom _ultimateBloom;
    private readonly WeatherController _weatherController;

    // Configuration is split out of the constructor because it cannot always run in
    // the constructor's frame. See TryConfigure.
    private bool _configured;
    internal bool Configured => _configured;
    internal bool HasBloom => _ultimateBloom != null;

    // A map mod can graft an UltimateBloom purely so our constructor has something to find
    // and then disable it, because it does not want the effect rendering on its own camera
    // (Icebreaker does exactly this). A disabled MonoBehaviour never gets its Start called,
    // so its serialized arrays stay null forever and TryConfigure can never finish. That is
    // a deliberate arrangement, not a fault, and should not be reported as one.
    internal bool Parked => _ultimateBloom != null && !_ultimateBloom.enabled;

    public Bloom()
    {
        // Find the main camera
        var camera = CameraManager.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("Bloom: No camera found!");
            return;
        }

        // Check if Ultimate Bloom is already on the camera
        _ultimateBloom = camera.GetComponent<UltimateBloom>();

        if (_ultimateBloom == null)
        {
            // Add Ultimate Bloom component to camera
            _ultimateBloom = camera.gameObject.AddComponent<UltimateBloom>();
            Plugin.Log.LogInfo("Bloom: Added Ultimate Bloom component to camera");
        }

        var weather = GameObject.Find("Weather");

        if (weather != null)
            _weatherController = weather.GetComponent<WeatherController>();

        TryConfigure(camera.name);
    }

    // Apply the one-off setup that needs UltimateBloom's own serialized arrays, and say
    // whether it actually happened.
    //
    // On a camera that already ships a fully-serialized UltimateBloom (every retail map)
    // this succeeds on the first call from the constructor and nothing below ever runs
    // again. On a camera that does NOT - a custom map that builds its own camera, where
    // we AddComponent the effect ourselves - the arrays are still null in this frame,
    // because AddComponent runs Awake but NOT Start, and Start is where UltimateBloom
    // fills them. So there is nothing to configure yet.
    //
    // 2026-09-15 field log (Icebreaker) is what this is for. The three ResetIntensities
    // calls logged their headers and returned on their own null guard, and then the very
    // next statement - m_BloomUsages[0], which had no such guard - threw:
    //
    //   FIRST Exception: NullReferenceException
    //     HollywoodGraphics.Components.Bloom..ctor ()
    //     HollywoodGraphics.GraphicsController.Start ()
    //
    // That is not just bloom being lost. The throw escapes GraphicsController.Start(),
    // so ambient occlusion, motion blur and the per-map settings never initialise
    // either: one null array takes out the whole mod for the raid. The same raid's
    // Lighthouse log shows the intended path for comparison ("Bloom: Ultimate Bloom
    // effect applied to camera FPS Camera" / "Bloom initialized" / "Updated all
    // settings"), which is exactly what the Icebreaker log is missing.
    //
    // So: report not-ready instead of throwing, and let GraphicsController call back on
    // later frames until UltimateBloom has started.
    internal bool TryConfigure(string cameraName = null)
    {
        if (_configured) return true;
        if (_ultimateBloom == null) return false;

        // These are assigned by value, not indexed, so they are safe whenever the
        // component exists. Re-applying them on a retry is harmless.
        _ultimateBloom.m_IntensityManagement = UltimateBloom.BloomIntensityManagement.FilmicCurve;
        _ultimateBloom.m_SamplingMode = UltimateBloom.SamplingMode.HeightRelative;
        _ultimateBloom.m_SamplingMinHeight = 384;
        // Reduces flicker
        _ultimateBloom.m_AnamorphicSmallVerticalBlur = true;

        // Everything from here down indexes into UltimateBloom's own arrays. Bail out
        // as a whole rather than half-configuring: a retry re-runs all of it.
        if (!Populated(_ultimateBloom.m_BloomIntensities) ||
            !Populated(_ultimateBloom.m_AnamorphicBloomIntensities) ||
            !Populated(_ultimateBloom.m_StarBloomIntensities) ||
            !Populated(_ultimateBloom.m_BloomUsages, 2) ||
            !Populated(_ultimateBloom.m_AnamorphicBloomUsages, 2) ||
            !Populated(_ultimateBloom.m_StarBloomUsages))
            return false;

        Plugin.Log.LogInfo("Resetting Main Bloom intensities");
        ResetIntensities(_ultimateBloom.m_BloomIntensities);
        Plugin.Log.LogInfo("Resetting Anamorphic Bloom intensities");
        ResetIntensities(_ultimateBloom.m_AnamorphicBloomIntensities);
        Plugin.Log.LogInfo("Resetting Star Bloom intensities");
        ResetIntensities(_ultimateBloom.m_StarBloomIntensities);

        // Turn these off as they form the "blob" part of the bloom and can oversaturate things.
        _ultimateBloom.m_BloomUsages[0] = _ultimateBloom.m_BloomUsages[1] = false;
        _ultimateBloom.m_AnamorphicBloomUsages[0] = false;
        _ultimateBloom.m_AnamorphicBloomUsages[1] = true;
        _ultimateBloom.m_StarBloomUsages[0] = false;

        // Disable high order star blooms because they end up applying everywhere on the screen
        for (var i = 3; i < _ultimateBloom.m_StarBloomUsages.Length; i++)
        {
            _ultimateBloom.m_StarBloomUsages[i] = false;
        }

        _configured = true;

        UpdateSettings();
        UpdateLensDust();
        Plugin.Log.LogInfo(
            $"Bloom: Ultimate Bloom effect applied to camera {cameraName ?? _ultimateBloom.name}");
        return true;
    }

    private static bool Populated(float[] array, int minimumLength = 1)
    {
        return array != null && array.Length >= minimumLength;
    }

    private static bool Populated(bool[] array, int minimumLength = 1)
    {
        return array != null && array.Length >= minimumLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (_weatherController == null || _ultimateBloom == null)
            return;

        var nightFactor = Mathf.InverseLerp(0f, -0.1f, _weatherController.SunHeight);
        
        if (Mathf.Abs(nightFactor - _sunLightFactor) < 0.05f)
            return;

        var bloomConfig = Plugin.GraphicsConfig.Bloom;

        var bias = 0.5f * nightFactor;
        _ultimateBloom.m_DirtLightIntensity = bloomConfig.DirtLightIntensity.Value * Plugin.GraphicsConfig.Current.BloomMultiplier.Value + bias;
        _ultimateBloom.m_StarFlareIntensity = bloomConfig.StarFlareIntensity.Value + bias;
        _ultimateBloom.m_StarScale = Mathf.Max(bloomConfig.StarScale.Value - 2f * nightFactor, 0.5f);

        var highlightScaling = 1f + 0.1f * nightFactor;
        _ultimateBloom.SetFilmicCurveParameters(
            bloomConfig.BloomMid.Value,
            bloomConfig.BloomDark.Value,
            bloomConfig.BloomBright.Value,
            bloomConfig.BloomHighlight.Value * highlightScaling
        );

        _sunLightFactor = nightFactor;
    }
    
    public static void Destroy()
    {
    }
    
    public void UpdateSettings()
    {
        // Same guard AmbientOcclusion.UpdateSettings already had. This is reachable with
        // no component at all: the constructor returns early when there is no camera, and
        // GraphicsController's `_bloom?.` null-conditionals only cover a null Bloom, not a
        // Bloom holding a null UltimateBloom. Config-UI handlers call in here too.
        if (_ultimateBloom == null) return;

        var config = Plugin.GraphicsConfig.Bloom;
        
        _ultimateBloom.m_BloomIntensity = config.BloomIntensity.Value;
        _ultimateBloom.SetFilmicCurveParameters(config.BloomMid.Value, config.BloomDark.Value, config.BloomBright.Value, config.BloomHighlight.Value);

        _ultimateBloom.m_UseLensDust = config.UseLensDust.Value;
        _ultimateBloom.m_DustIntensity = config.DustIntensity.Value;
        _ultimateBloom.m_DirtLightIntensity = config.DirtLightIntensity.Value * Plugin.GraphicsConfig.Current.BloomMultiplier.Value;

        _ultimateBloom.m_UseAnamorphicFlare = config.UseAnamorphicFlare.Value;
        _ultimateBloom.m_AnamorphicFlareIntensity = config.AnamorphicFlareIntensity.Value;
        _ultimateBloom.m_AnamorphicScale = config.AnamorphicScale.Value;
        _ultimateBloom.m_AnamorphicBlurPass = config.AnamorphicBlurPass.Value;

        _ultimateBloom.m_UseStarFlare = config.UseStarFlare.Value;
        _ultimateBloom.m_StarFlareIntensity = config.StarFlareIntensity.Value;
        _ultimateBloom.m_StarScale = config.StarScale.Value;
        _ultimateBloom.m_StarBlurPass = config.StarBlurPass.Value;
        
        // Force the recalculation of the sunlight factor
        _sunLightFactor = 10000f;
    }

    public void UpdateLensDust()
    {
        if (_ultimateBloom == null) return;

        var config = Plugin.GraphicsConfig.Bloom;
        
        if (config.LensDust.Value == null)
            return;

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (assemblyDirectory == null)
            return;

        var path = Path.Combine(assemblyDirectory, "bloom", config.LensDust.Value);

        if (!File.Exists(path))
            return;

        var data = File.ReadAllBytes(path);
        var tex2D = new Texture2D(1920, 1080, TextureFormat.RGBA32, true);

        tex2D.LoadImage(data);
        _ultimateBloom.m_DustTexture = tex2D;
    }
    
    private static void ResetIntensities(float[] intensities)
    {
        // TryConfigure gates on this now, so a null here would be a caller bug rather
        // than the normal not-started-yet case it used to absorb. Kept anyway - it is
        // one branch, and silently doing nothing beats an NRE from a static helper.
        if (intensities == null) return;
        for (var i = 0; i < intensities.Length; i++)
        {
            Plugin.Log.LogInfo($"Intensity: {intensities[i]}");
            intensities[i] = 1f;
        }
    }
}