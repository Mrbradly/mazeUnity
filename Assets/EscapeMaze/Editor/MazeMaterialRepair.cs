using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EscapeMaze.Editor
{
    // Soft URP integration: no URP package dependency and no render-pipeline setting changes.
    [InitializeOnLoad]
    public static class MazeMaterialRepair
    {
        private const string MaterialFolder = "Assets/EscapeMaze/Materials";
        private static bool scheduled;
        // Keep the utility loadable in older Editors as well. On supported Editors this
        // saves the changed material alone, without saving unrelated dirty project assets.
        private static readonly System.Reflection.MethodInfo SaveOneMaterial = typeof(AssetDatabase)
            .GetMethod("SaveAssetIfDirty", new[] { typeof(UnityEngine.Object) });

        static MazeMaterialRepair()
        {
            EditorApplication.projectChanged += Schedule;
            Schedule();
        }

        private static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += RepairAfterImport;
        }

        private static void RepairAfterImport()
        {
            scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            Repair(false);
        }

        [MenuItem("LUMEN/Fix Pink Materials")]
        public static void RepairFromMenu() { Repair(true); }

        private static void Repair(bool force)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) return;
            RenderPipelineAsset pipeline = QualitySettings.renderPipeline;
            if (pipeline == null) pipeline = GraphicsSettings.currentRenderPipeline;
            bool universal = pipeline != null && pipeline.GetType().FullName.IndexOf("UniversalRenderPipeline", StringComparison.Ordinal) >= 0;
            if (pipeline != null && !universal)
            {
                if (force) Debug.LogWarning("LUMEN material repair supports Built-in and URP. Active pipeline: " + pipeline.GetType().FullName + ". No settings or materials were changed.");
                return;
            }
            string shaderName = universal ? "Universal Render Pipeline/Simple Lit" : "Standard";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                if (force) Debug.LogError("LUMEN: Shader not found: " + shaderName + ". Check that your project's renderer package has finished importing.");
                return;
            }
            int repaired = 0;
            foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                if (material == null || (!force && material.shader == shader)) continue;
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                    : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                Color emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                Undo.RecordObject(material, "Repair LUMEN material shader");
                material.shader = shader;
                material.shaderKeywords = Array.Empty<string>();
                SetColor(material, "_Color", color);
                SetColor(material, "_BaseColor", color);
                SetColor(material, "_EmissionColor", emission);
                SetColor(material, "_SpecColor", Color.black);
                SetFloat(material, "_Glossiness", 0f);
                SetFloat(material, "_Smoothness", 0f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_SpecularHighlights", 0f);
                SetFloat(material, "_GlossyReflections", 0f);
                SetFloat(material, "_Surface", 0f);
                SetFloat(material, "_AlphaClip", 0f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
                SetFloat(material, "_ZWrite", 1f);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                if (!universal) material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                bool emitting = emission.maxColorComponent > .0001f;
                if (emitting) material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = emitting ? MaterialGlobalIlluminationFlags.None : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                material.renderQueue = -1;
                EditorUtility.SetDirty(material);
                if (SaveOneMaterial != null) SaveOneMaterial.Invoke(null, new object[] { material });
                repaired++;
            }
            if (repaired > 0)
            {
                SceneView.RepaintAll();
                Debug.Log("LUMEN: repaired " + repaired + " maze materials for " + (universal ? "URP" : "Built-in") + " using " + shaderName + ". Colours and emission preserved. Render-pipeline settings were not changed.");
                if (SaveOneMaterial == null) Debug.LogWarning("LUMEN: this older Editor does not expose per-asset saving. Choose File > Save Project to persist the repaired materials.");
            }
            else if (force) Debug.Log("LUMEN: no maze materials required repair. If surfaces remain pink, check the Console for shader compile errors.");
        }

        private static void SetColor(Material material, string name, Color value)
        {
            if (material.HasProperty(name)) material.SetColor(name, value);
        }

        private static void SetFloat(Material material, string name, float value)
        {
            if (material.HasProperty(name)) material.SetFloat(name, value);
        }
    }
}
