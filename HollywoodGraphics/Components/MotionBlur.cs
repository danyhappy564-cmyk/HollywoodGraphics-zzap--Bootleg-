using EFT.CameraControl;
using UnityEngine.Rendering.PostProcessing;

namespace HollywoodGraphics.Components;

public static class HfxMotionBlur
{
    public static void UpdateSettings()
    {
        var camera = CameraManager.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("HFXMotionBlur: No camera found!");
            return;
        }

        var postProcessVolume = camera.GetComponent<PostProcessVolume>();
        if (postProcessVolume == null) return;

        if (!postProcessVolume.profile.TryGetSettings<MotionBlur>(out var motionBlur))
        {
            postProcessVolume.profile.AddSettings<MotionBlur>();
            postProcessVolume.profile.TryGetSettings(out motionBlur);
        }

        motionBlur.enabled.value = Plugin.GraphicsConfig.MotionBlurEnabled.Value;
        motionBlur.shutterAngle.value = Plugin.GraphicsConfig.MotionBlurShutterAngle.Value;
        motionBlur.sampleCount.value = Plugin.GraphicsConfig.MotionBlurSampleCount.Value;
    }
}