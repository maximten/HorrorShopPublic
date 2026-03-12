using UnityEditor;
using UnityEngine;

namespace HorrorShop.Editor
{
    public static class SwapJpgToPng
    {
        [MenuItem("Horror Shop/Swap JPG Textures to PNG in Materials")]
        public static void Run()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Maxim Ten/Horror Burger Shop/Materials" });
            int swapped = 0;

            foreach (string matGuid in matGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) continue;

                bool dirty = false;

                int propCount = mat.shader != null ? mat.shader.GetPropertyCount() : 0;
                for (int i = 0; i < propCount; i++)
                {
                    if (mat.shader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                        continue;

                    string propName = mat.shader.GetPropertyName(i);
                    Texture tex = mat.GetTexture(propName);
                    if (tex == null) continue;

                    string texPath = AssetDatabase.GetAssetPath(tex);
                    if (!texPath.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    string pngPath = texPath.Substring(0, texPath.Length - 4) + ".png";
                    Texture pngTex = AssetDatabase.LoadAssetAtPath<Texture>(pngPath);
                    if (pngTex == null)
                    {
                        Debug.LogWarning($"PNG not found for {texPath}, skipping.");
                        continue;
                    }

                    mat.SetTexture(propName, pngTex);
                    dirty = true;
                    swapped++;
                    Debug.Log($"[{mat.name}] {propName}: {texPath} -> {pngPath}");
                }

                if (dirty)
                    EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Done. Swapped {swapped} texture slot(s) across {matGuids.Length} material(s).");
        }
    }
}
