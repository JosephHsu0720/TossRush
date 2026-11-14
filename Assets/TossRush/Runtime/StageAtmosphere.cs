using UnityEngine;
using UnityEngine.Rendering;

namespace TossRush
{
    // Owns the temporary lighting/camera changes for this stage only.
    public sealed class StageAtmosphere : MonoBehaviour
    {
        public Material sky;
        public Light sunlight;
        public Color ambient = new Color(.7f, .76f, .68f);
        public float viewingDistance = 180;
        Camera cameraView;
        Material previousSky;
        Light previousSun;
        Color previousAmbient, previousSkyAmbient, previousEquator, previousGround;
        AmbientMode previousMode;
        CameraClearFlags previousFlags;
        float previousDistance;
        bool previousSunEnabled, applied;

        void OnEnable()
        {
            var stage = GetComponent<StageView>();
            var thrower = stage && stage.Game ? stage.Game.thrower : FindFirstObjectByType<ThrowController>();
            if (!thrower || !thrower.view || !sky || !sunlight) return;
            cameraView = thrower.view;
            previousSky = RenderSettings.skybox;
            previousSun = RenderSettings.sun;
            previousSunEnabled = previousSun && previousSun.enabled;
            previousMode = RenderSettings.ambientMode;
            previousAmbient = RenderSettings.ambientLight;
            previousSkyAmbient = RenderSettings.ambientSkyColor;
            previousEquator = RenderSettings.ambientEquatorColor;
            previousGround = RenderSettings.ambientGroundColor;
            previousFlags = cameraView.clearFlags;
            previousDistance = cameraView.farClipPlane;
            applied = true;
            if (previousSun && previousSun != sunlight) previousSun.enabled = false;
            RenderSettings.skybox = sky;
            RenderSettings.sun = sunlight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            cameraView.clearFlags = CameraClearFlags.Skybox;
            cameraView.farClipPlane = viewingDistance;
        }

        void OnDisable()
        {
            if (!applied) return;
            applied = false;
            RenderSettings.skybox = previousSky;
            RenderSettings.sun = previousSun;
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousAmbient;
            RenderSettings.ambientSkyColor = previousSkyAmbient;
            RenderSettings.ambientEquatorColor = previousEquator;
            RenderSettings.ambientGroundColor = previousGround;
            if (previousSun) previousSun.enabled = previousSunEnabled;
            if (cameraView)
            {
                cameraView.clearFlags = previousFlags;
                cameraView.farClipPlane = previousDistance;
            }
        }
    }
}
