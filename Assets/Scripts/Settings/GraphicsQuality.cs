using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Horror.Settings
{
    /// <summary>
    /// Low / Medium / High graphics presets.
    ///
    /// The project only has one quality level for PC, and in URP most of the expensive options
    /// (shadows, anti-aliasing, render scale) live on the URP asset, not in Quality Settings.
    /// So at runtime we make a temporary copy of the URP asset and change the copy. The real
    /// asset in Assets/Settings is never modified, and the original is put back when play stops.
    ///
    /// Tweak the numbers in Apply() to taste.
    /// </summary>
    public static class GraphicsQuality
    {
        private static RenderPipelineAsset originalQualityPipeline;
        private static UniversalRenderPipelineAsset runtimeCopy;
        private static bool restoreHooked;
        private static QualityPreset? lastApplied;

        public static void Apply(QualityPreset preset)
        {
            // Settings are re-applied whenever any slider moves; only do the work when the preset changed.
            if (lastApplied == preset && runtimeCopy != null) return;
            lastApplied = preset;

            UniversalRenderPipelineAsset urp = GetRuntimeCopy();

            switch (preset)
            {
                case QualityPreset.Low:
                    if (urp != null)
                    {
                        urp.renderScale = 0.75f;
                        urp.msaaSampleCount = 1;
                        urp.shadowDistance = 20f;
                        urp.shadowCascadeCount = 1;
                    }
                    QualitySettings.lodBias = 0.7f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    QualitySettings.globalTextureMipmapLimit = 1; // half-resolution textures
                    break;

                case QualityPreset.Medium:
                    if (urp != null)
                    {
                        urp.renderScale = 1f;
                        urp.msaaSampleCount = 2;
                        urp.shadowDistance = 35f;
                        urp.shadowCascadeCount = 2;
                    }
                    QualitySettings.lodBias = 1f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    break;

                default: // High
                    if (urp != null)
                    {
                        urp.renderScale = 1f;
                        urp.msaaSampleCount = 4;
                        urp.shadowDistance = 50f;
                        urp.shadowCascadeCount = 4;
                    }
                    QualitySettings.lodBias = 1.5f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    break;
            }
        }

        private static UniversalRenderPipelineAsset GetRuntimeCopy()
        {
            if (runtimeCopy != null) return runtimeCopy;

            originalQualityPipeline = QualitySettings.renderPipeline;
            RenderPipelineAsset active = originalQualityPipeline != null
                ? originalQualityPipeline
                : GraphicsSettings.defaultRenderPipeline;

            if (!(active is UniversalRenderPipelineAsset source))
            {
                Debug.LogWarning("[Settings] No URP asset found, so quality presets only change texture/LOD settings.");
                return null;
            }

            runtimeCopy = Object.Instantiate(source);
            runtimeCopy.name = source.name + " (runtime copy)";
            runtimeCopy.hideFlags = HideFlags.DontSave;
            QualitySettings.renderPipeline = runtimeCopy;

            if (!restoreHooked)
            {
                Application.quitting += Restore;
                restoreHooked = true;
            }

            return runtimeCopy;
        }

        // Runs when the game closes and when you press Stop in the editor, so the project's
        // Quality Settings never end up pointing at the temporary copy.
        private static void Restore()
        {
            if (runtimeCopy == null) return;
            QualitySettings.renderPipeline = originalQualityPipeline;
            Object.DestroyImmediate(runtimeCopy);
            runtimeCopy = null;
            lastApplied = null;
        }
    }
}
