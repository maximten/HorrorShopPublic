using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HorrorShop.Editor
{
    public class HorrorShopSetupWizard : EditorWindow
    {
        [MenuItem("Tools/Horror Burger Shop/Setup Wizard")]
        public static void ShowWizard()
        {
            var window = GetWindow<HorrorShopSetupWizard>(true, "Horror Shop Setup", true);
            window.minSize = new Vector2(420, 320);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Horror Shop — Setup Wizard", headerStyle);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Apply recommended URP settings required by Horror Shop assets.",
                MessageType.Info);
            EditorGUILayout.Space(14);

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (urpAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "No URP Pipeline Asset detected. Please assign a URP asset under Edit > Project Settings > Graphics.",
                    MessageType.Error);
                DrawCloseButton();
                return;
            }

            DrawStatusRow("URP Active", true);
            DrawStatusRow("Decal Renderer Feature", HasDecalFeature(urpAsset));
            DrawStatusRow("Adaptive Probe Volumes", HasAPV(urpAsset));
            DrawStatusRow("AO Method: Interleaved Gradient", HasInterleavedGradientAO(urpAsset));

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Apply URP Graphics Settings", GUILayout.Height(42)))
                ApplySettings(urpAsset);

            DrawCloseButton();
        }

        private void DrawStatusRow(string label, bool ok)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(true));
                var prev = GUI.color;
                GUI.color = ok ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.85f, 0.2f);
                EditorGUILayout.LabelField(ok ? "✓  Ready" : "○  Not Applied",
                    new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold },
                    GUILayout.Width(100));
                GUI.color = prev;
            }
        }

        private void DrawCloseButton()
        {
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Close"))
                Close();
        }

        // -------------------------------------------------------------------------
        // Status checks
        // -------------------------------------------------------------------------

        private bool HasDecalFeature(UniversalRenderPipelineAsset urpAsset)
        {
            var renderer = GetActiveRenderer(urpAsset);
            return renderer != null && renderer.rendererFeatures.Any(f => f is DecalRendererFeature);
        }

        private bool HasAPV(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            var prop = so.FindProperty("m_LightProbeSystem");
            return prop != null && prop.intValue == 1;
        }

        private bool HasInterleavedGradientAO(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            var prop = so.FindProperty("m_AOMethod");
            // InterleavedGradient = 1
            return prop != null && prop.intValue == 1;
        }

        // -------------------------------------------------------------------------
        // Apply
        // -------------------------------------------------------------------------

        private void ApplySettings(UniversalRenderPipelineAsset urpAsset)
        {
            ApplyDecalFeature(urpAsset);
            ApplyAPV(urpAsset);
            ApplyInterleavedGradientAO(urpAsset);
            AssetDatabase.SaveAssets();
            Repaint();
            Debug.Log("[Horror Shop] URP settings applied successfully.");
        }

        private void ApplyDecalFeature(UniversalRenderPipelineAsset urpAsset)
        {
            var renderer = GetActiveRenderer(urpAsset);
            if (renderer == null)
            {
                Debug.LogWarning("[Horror Shop] Could not find active URP renderer data.");
                return;
            }

            if (renderer.rendererFeatures.Any(f => f is DecalRendererFeature))
                return;

            var decal = ScriptableObject.CreateInstance<DecalRendererFeature>();
            decal.name = "DecalRendererFeature";
            AssetDatabase.AddObjectToAsset(decal, renderer);

            var so = new SerializedObject(renderer);
            var featuresArray = so.FindProperty("m_RendererFeatures");
            featuresArray.arraySize++;
            featuresArray.GetArrayElementAtIndex(featuresArray.arraySize - 1).objectReferenceValue = decal;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            Debug.Log("[Horror Shop] Added DecalRendererFeature to renderer.");
        }

        private void ApplyInterleavedGradientAO(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            var prop = so.FindProperty("m_AOMethod");
            if (prop == null)
            {
                Debug.LogWarning("[Horror Shop] Could not find m_AOMethod on URP asset.");
                return;
            }

            if (prop.intValue == 1) return;

            prop.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urpAsset);
            Debug.Log("[Horror Shop] AO Method set to Interleaved Gradient.");
        }

        private void ApplyAPV(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            var prop = so.FindProperty("m_LightProbeSystem");
            if (prop == null)
            {
                Debug.LogWarning("[Horror Shop] Could not find m_LightProbeSystem on URP asset. APV may not be supported in this URP version.");
                return;
            }

            if (prop.intValue == 1) return;

            prop.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urpAsset);
            Debug.Log("[Horror Shop] Adaptive Probe Volumes enabled.");
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private ScriptableRendererData GetActiveRenderer(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            var listProp = so.FindProperty("m_RendererDataList");
            var indexProp = so.FindProperty("m_DefaultRendererIndex");
            if (listProp == null || indexProp == null) return null;

            int idx = indexProp.intValue;
            if (idx < 0 || idx >= listProp.arraySize) return null;

            return listProp.GetArrayElementAtIndex(idx).objectReferenceValue as ScriptableRendererData;
        }
    }
}
