using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ImportedAssetMaterialUpgrader
{
    private const string SessionKey = "ImportedAssetMaterialUpgrader.HasRun";

    private static readonly string[] MaterialRoots =
    {
        "Assets/Stylized Nature Environment",
        "Assets/Low_Poly_Mini_Village",
        "Assets/LowpolyStreetPack",
        "Assets/Free_Building_01",
        "Assets/ALP_Assets",
        "Assets/Cozy Mountain Cabin",
        "Assets/ModularHousePack1",
        "Assets/UrbanBuilding",
        "Assets/AssetsStore",
    };

    [InitializeOnLoadMethod]
    private static void RunOnceAfterReload()
    {
        EditorApplication.delayCall += TryUpgradeOnce;
    }

    [MenuItem("Tools/Infinite World/Upgrade Imported Materials To URP")]
    public static void UpgradeImportedMaterialsToUrp()
    {
        int changedCount = UpgradeMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Imported material upgrade finished. Changed {changedCount} materials.");
    }

    private static void TryUpgradeOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        UpgradeImportedMaterialsToUrp();
    }

    private static int UpgradeMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (litShader == null || unlitShader == null)
        {
            Debug.LogWarning("URP shaders not found. Imported materials were not upgraded.");
            return 0;
        }

        HashSet<string> materialPaths = new();
        foreach (string root in MaterialRoots)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { root }))
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(materialPath))
                {
                    materialPaths.Add(materialPath);
                }
            }
        }

        int changedCount = 0;
        foreach (string materialPath in materialPaths)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                continue;
            }

            if (!ApplyUrpSettings(material, materialPath, litShader, unlitShader))
            {
                continue;
            }

            EditorUtility.SetDirty(material);
            changedCount++;
        }

        return changedCount;
    }

    private static bool ApplyUrpSettings(Material material, string materialPath, Shader litShader, Shader unlitShader)
    {
        string normalizedPath = materialPath.Replace('\\', '/');
        string lowerPath = normalizedPath.ToLowerInvariant();
        string materialName = material.name.ToLowerInvariant();

        if (lowerPath.Contains("/sky"))
        {
            return false;
        }

        bool isFoliage = lowerPath.Contains("leaves")
            || lowerPath.Contains("grass")
            || lowerPath.Contains("branch")
            || materialName.Contains("leaves")
            || materialName.Contains("grass")
            || materialName.Contains("branch");
        bool isTransparentCutout = isFoliage || lowerPath.Contains("fence");
        bool isRoadMark = lowerPath.Contains("roadmark") || lowerPath.Contains("sign");
        bool isLightShape = lowerPath.Contains("lightshape");

        Texture mainTexture = GetFirstTexture(material, "_BaseMap", "_MainTex");
        Texture normalTexture = GetFirstTexture(material, "_BumpMap", "_Normal");
        Texture metallicTexture = GetFirstTexture(material, "_MetallicGlossMap");
        Texture occlusionTexture = GetFirstTexture(material, "_OcclusionMap");
        Texture emissionTexture = GetFirstTexture(material, "_EmissionMap");
        Color baseColor = GetFirstColor(material, "_BaseColor", "_Color");

        Shader targetShader = isLightShape ? unlitShader : litShader;
        bool changed = material.shader != targetShader;
        if (changed)
        {
            material.shader = targetShader;
        }

        changed |= SetTexture(material, "_BaseMap", mainTexture);
        changed |= SetColor(material, "_BaseColor", baseColor);
        changed |= SetTexture(material, "_BumpMap", normalTexture);
        changed |= SetTexture(material, "_MetallicGlossMap", metallicTexture);
        changed |= SetTexture(material, "_OcclusionMap", occlusionTexture);
        changed |= SetTexture(material, "_EmissionMap", emissionTexture);
        changed |= ApplyHeuristicColor(material, lowerPath, mainTexture);

        if (targetShader == litShader)
        {
            changed |= SetFloat(material, "_Metallic", 0f);
            changed |= SetFloat(material, "_Smoothness", lowerPath.Contains("ground") ? 0.05f : isRoadMark ? 0.05f : 0.18f);
            changed |= SetFloat(material, "_Surface", 0f);
            changed |= SetFloat(material, "_BumpScale", normalTexture != null ? 1f : 0f);
            changed |= SetFloat(material, "_OcclusionStrength", occlusionTexture != null ? 1f : 0f);
            changed |= SetFloat(material, "_WorkflowMode", 1f);
            changed |= SetFloat(material, "_SpecularHighlights", 1f);
            changed |= SetFloat(material, "_EnvironmentReflections", 1f);

            if (isTransparentCutout)
            {
                changed |= SetFloat(material, "_AlphaClip", 1f);
                changed |= SetFloat(material, "_Cutoff", 0.35f);
                changed |= SetFloat(material, "_Cull", 0f);
            }
            else
            {
                changed |= SetFloat(material, "_AlphaClip", 0f);
                changed |= SetFloat(material, "_Cull", 2f);
            }

            if (emissionTexture != null)
            {
                changed |= SetColor(material, "_EmissionColor", Color.white);
            }
        }
        else
        {
            changed |= SetColor(material, "_BaseColor", Color.white);
        }

        return changed;
    }

    private static bool ApplyHeuristicColor(Material material, string lowerPath, Texture mainTexture)
    {
        if (!material.HasProperty("_BaseColor"))
        {
            return false;
        }

        bool hasTexture = mainTexture != null;

        if (lowerPath.Contains("/nature/materials/default"))
        {
            return SetColor(material, "_BaseColor", new Color(0.45f, 0.72f, 0.34f, 1f));
        }

        if (lowerPath.Contains("branch_tex"))
        {
            return SetColor(material, "_BaseColor", new Color(0.45f, 0.3f, 0.17f, 1f));
        }

        if (lowerPath.Contains("leaves") || lowerPath.Contains("grass"))
        {
            Color foliageTint = hasTexture
                ? new Color(0.92f, 0.97f, 0.9f, 1f)
                : new Color(0.5f, 0.76f, 0.35f, 1f);
            return SetColor(material, "_BaseColor", foliageTint);
        }

        if (lowerPath.Contains("fence") || lowerPath.Contains("tree_dif"))
        {
            return SetColor(material, "_BaseColor", new Color(0.47f, 0.32f, 0.2f, 1f));
        }

        if (lowerPath.Contains("/art/meshes/materials/texture.mat"))
        {
            return SetColor(material, "_BaseColor", Color.white);
        }

        if (lowerPath.Contains("/art/meshes/materials/gnd.mat"))
        {
            return SetColor(material, "_BaseColor", new Color(0.88f, 0.82f, 0.68f, 1f));
        }

        if (lowerPath.Contains("/art/materials/1.mat"))
        {
            return SetColor(material, "_BaseColor", new Color(0.82f, 0.45f, 0.28f, 1f));
        }

        if (lowerPath.Contains("/art/materials/2.mat"))
        {
            return SetColor(material, "_BaseColor", new Color(0.83f, 0.77f, 0.62f, 1f));
        }

        if (lowerPath.Contains("/art/materials/3.mat"))
        {
            return SetColor(material, "_BaseColor", new Color(0.58f, 0.74f, 0.32f, 1f));
        }

        return false;
    }

    private static bool SetFloat(Material material, string propertyName, float value)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        float currentValue = material.GetFloat(propertyName);
        if (Mathf.Approximately(currentValue, value))
        {
            return false;
        }

        material.SetFloat(propertyName, value);
        return true;
    }

    private static bool SetColor(Material material, string propertyName, Color value)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Color currentValue = material.GetColor(propertyName);
        if (currentValue == value)
        {
            return false;
        }

        material.SetColor(propertyName, value);
        return true;
    }

    private static bool SetTexture(Material material, string propertyName, Texture value)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Texture currentValue = material.GetTexture(propertyName);
        if (currentValue == value)
        {
            return false;
        }

        material.SetTexture(propertyName, value);
        return true;
    }

    private static Texture GetFirstTexture(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!material.HasProperty(propertyName))
            {
                continue;
            }

            Texture texture = material.GetTexture(propertyName);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Color GetFirstColor(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return Color.white;
    }
}
