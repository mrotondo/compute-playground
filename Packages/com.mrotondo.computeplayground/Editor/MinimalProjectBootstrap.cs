using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// The project-file muckery, expressed as idempotent C# instead of hand-edited YAML.
    /// Safe to run repeatedly, and on a new editor version it fails as a compile error on a
    /// specific line rather than as silent breakage six months later.
    /// </summary>
    public static class MinimalProjectBootstrap
    {
        const string PipelineAssetPath = "Assets/Rendering/TextureBlitRenderPipelineAsset.asset";

        const string BlitShaderPath =
            "Packages/com.mrotondo.computeplayground/Runtime/Shaders/TextureBlit.shader";

        [MenuItem("Tools/Minimal Project/Apply Settings", priority = 0)]
        public static void Apply()
        {
            TextureBlitRenderPipelineAsset pipelineAsset = EnsurePipelineAsset();

            ConfigureGraphicsSettings(pipelineAsset);
            ConfigureQualitySettings();
            ConfigurePlayerSettings();
            ConfigureEditorSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("Minimal Project: settings applied.");
        }

        static TextureBlitRenderPipelineAsset EnsurePipelineAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextureBlitRenderPipelineAsset>(PipelineAssetPath);

            if (asset == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PipelineAssetPath));
                asset = ScriptableObject.CreateInstance<TextureBlitRenderPipelineAsset>();
                AssetDatabase.CreateAsset(asset, PipelineAssetPath);
            }

            if (asset.BlitShader == null)
            {
                asset.BlitShader = AssetDatabase.LoadAssetAtPath<Shader>(BlitShaderPath);
                if (asset.BlitShader == null)
                    Debug.LogError($"Minimal Project: could not find the blit shader at {BlitShaderPath}.");
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        static void ConfigureGraphicsSettings(RenderPipelineAsset pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            var graphics = new SerializedObject(GraphicsSettings.GetGraphicsSettings());

            // The single biggest build-time win: built-in shaders such as Standard and
            // UI/Default carry thousands of variants, and nothing here draws with them.
            ClearArray(graphics, "m_AlwaysIncludedShaders");
            ClearArray(graphics, "m_PreloadedShaders");

            // Stale keys here are what break a project when a pipeline package is removed
            // while its references are still live.
            ClearArray(graphics, "m_RenderPipelineGlobalSettingsMap");

            SetIfPresent(graphics, "m_LightmapStripping", 1);   // Automatic
            SetIfPresent(graphics, "m_FogStripping", 1);
            SetIfPresent(graphics, "m_InstancingStripping", 1);

            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureQualitySettings()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets.Length == 0)
            {
                Debug.LogWarning("Minimal Project: could not open QualitySettings.asset; skipping.");
                return;
            }

            var quality = new SerializedObject(assets[0]);
            SerializedProperty levels = quality.FindProperty("m_QualitySettings");

            if (levels != null && levels.isArray)
            {
                while (levels.arraySize > 1)
                    levels.DeleteArrayElementAtIndex(levels.arraySize - 1);
            }

            SetIfPresent(quality, "m_CurrentQuality", 0);
            quality.ApplyModifiedPropertiesWithoutUndo();

            QualitySettings.SetQualityLevel(0, applyExpensiveChanges: false);
            QualitySettings.renderPipeline = null;          // fall through to the default pipeline
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowCascades = 0;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            QualitySettings.skinWeights = SkinWeights.OneBone;
        }

        static void ConfigurePlayerSettings()
        {
            NamedBuildTarget standalone = NamedBuildTarget.Standalone;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(standalone, ManagedStrippingLevel.High);

            // Mono builds in a fraction of the time IL2CPP does. These are experiments,
            // not shipping products.
            PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);

            // One graphics API means one set of shader variants compiled per build.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D12 });

            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1024;
            PlayerSettings.defaultScreenHeight = 1024;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            // Personal licences cannot disable the splash; ignore the failure if so.
            try
            {
                PlayerSettings.SplashScreen.show = false;
            }
            catch
            {
                // Not available on this licence.
            }
        }

        static void ConfigureEditorSettings()
        {
            // Skipping domain reload is the largest iteration-time win available, and these
            // experiments hold no static state that needs resetting between play sessions.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        static void ClearArray(SerializedObject target, string propertyPath)
        {
            SerializedProperty property = target.FindProperty(propertyPath);
            if (property == null)
            {
                Debug.LogWarning($"Minimal Project: '{propertyPath}' not found; the field may have " +
                                 "been renamed in this editor version.");
                return;
            }

            if (property.isArray)
                property.ClearArray();
        }

        static void SetIfPresent(SerializedObject target, string propertyPath, int value)
        {
            SerializedProperty property = target.FindProperty(propertyPath);
            if (property != null)
                property.intValue = value;
        }
    }
}
