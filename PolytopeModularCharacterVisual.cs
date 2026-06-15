using UnityEngine;

public class PolytopeModularCharacterVisual : MonoBehaviour
{
    [Header("Default visible parts")]
    public string nakedPartToken = "naked_00";
    public string defaultClothToken = "cloth_00";
    public string defaultHeadToken = "head_01";
    public string defaultHairToken = "hair_01";

    [Header("Default clothing colors")]
    public Color shirtColor = new Color(0.34f, 0.17f, 0.07f, 1f);
    public Color trousersColor = new Color(0.18f, 0.09f, 0.04f, 1f);

    private void Awake()
    {
        ApplyDefaultPartVisibility();
    }

    private void OnEnable()
    {
        ApplyDefaultPartVisibility();
    }

    public void ApplyDefaultPartVisibility()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer == null || IsEquippedVisual(renderer.transform))
            {
                continue;
            }

            string lowerName = renderer.name.ToLowerInvariant();
            if (!lowerName.StartsWith("pt_male_armor_"))
            {
                continue;
            }

            renderer.gameObject.SetActive(ShouldShowByDefault(lowerName));
            ApplyDefaultClothingColor(renderer, lowerName);
        }
    }

    private bool ShouldShowByDefault(string lowerName)
    {
        bool isBodyOrLegs = lowerName.Contains("_body") || lowerName.Contains("_legs");
        bool isDefaultClothing = ContainsToken(lowerName, defaultClothToken) && isBodyOrLegs;
        bool isVisibleNakedPart = ContainsToken(lowerName, nakedPartToken) && !isBodyOrLegs;
        return isVisibleNakedPart
            || isDefaultClothing
            || ContainsToken(lowerName, defaultHeadToken)
            || ContainsToken(lowerName, defaultHairToken);
    }

    private void ApplyDefaultClothingColor(SkinnedMeshRenderer renderer, string lowerName)
    {
        if (!ContainsToken(lowerName, defaultClothToken))
        {
            return;
        }

        bool isBody = lowerName.Contains("_body");
        bool isLegs = lowerName.Contains("_legs");
        if (!isBody && !isLegs)
        {
            return;
        }

        Color targetColor = isBody ? shirtColor : trousersColor;
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", targetColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", targetColor);
            }
        }
    }

    private static bool ContainsToken(string lowerName, string token)
    {
        return !string.IsNullOrEmpty(token) && lowerName.Contains(token.ToLowerInvariant());
    }

    private static bool IsEquippedVisual(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.name == "EquippedHelmetVisual" || candidate.name == "EquippedArmorVisual")
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }
}

