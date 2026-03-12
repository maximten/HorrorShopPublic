using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorrorShop.Editor
{
    public static class FixNormalMapImportSettings
    {
        static readonly string[] NormalPropNames =
        {
            "_BumpMap", "_NormalMap", "_DetailNormalMap",
            "_NormalMapOS", "_ClearCoatNormalMap"
        };

        [MenuItem("Horror Shop/Fix Normal Map Import Settings")]
        public static void Run()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Maxim Ten/Horror Burger Shop/Materials" });
            int fixed_ = 0;

            foreach (string matGuid in matGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || mat.shader == null) continue;

                int propCount = mat.shader.GetPropertyCount();
                for (int i = 0; i < propCount; i++)
                {
                    if (mat.shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                        continue;

                    string propName = mat.shader.GetPropertyName(i);
                    if (!IsNormalProp(propName))
                        continue;

                    Texture tex = mat.GetTexture(propName);
                    if (tex == null) continue;

                    string texPath = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(texPath)) continue;

                    var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                    if (importer == null) continue;

                    if (importer.textureType == TextureImporterType.NormalMap)
                        continue;

                    importer.textureType = TextureImporterType.NormalMap;
                    AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                    fixed_++;
                    Debug.Log($"[{mat.name}] {propName}: set NormalMap import type on {texPath}");
                }
            }

            Debug.Log($"Done. Fixed {fixed_} texture(s).");
        }

        static bool IsNormalProp(string name)
        {
            foreach (var n in NormalPropNames)
                if (name == n) return true;
            return false;
        }
    }
}
