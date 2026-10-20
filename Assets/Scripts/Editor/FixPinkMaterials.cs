using UnityEngine;
using UnityEditor;

public static class FixPinkMaterials
{
    [MenuItem("Tools/Fix Pink Materials (Convert to URP)")]
    [InitializeOnLoadMethod]
    public static void Execute()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (urpLit == null)
        {
            Debug.LogWarning("[FixPinkMaterials] Shader Universal Render Pipeline/Lit tidak ditemukan.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            bool isStandardOrError = mat.shader == null || 
                                     mat.shader.name == "Standard" || 
                                     mat.shader.name == "Standard (Specular setup)" ||
                                     mat.shader.name == "Hidden/InternalErrorShader" ||
                                     mat.shader.name.Contains("Error");

            if (isStandardOrError)
            {
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Vector2 mainScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                Vector2 mainOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0f;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

                mat.shader = urpLit;

                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                    mat.SetTextureScale("_BaseMap", mainScale);
                    mat.SetTextureOffset("_BaseMap", mainOffset);
                }
                mat.SetColor("_BaseColor", color);

                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                }

                if (emissionMap != null)
                {
                    mat.SetTexture("_EmissionMap", emissionMap);
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.EnableKeyword("_EMISSION");
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", smoothness);
                }
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", metallic);
                }

                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[FixPinkMaterials] Berhasil mengonversi material: {path} ke {urpLit.name}");
            }
        }

        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
