using UnityEngine;

// polytope modular character visual script that owns this feature's runtime behavior.
public class PolytopeModularCharacterVisual : MonoBehaviour
{
    [Header("Default visible parts")]
    // Important runtime data or configuration used by this component: nakedPartToken.
    public string nakedPartToken = "naked_00";
    // Important runtime data or configuration used by this component: defaultClothToken.
    public string defaultClothToken = "cloth_00";
    // Important runtime data or configuration used by this component: defaultHeadToken.
    public string defaultHeadToken = "head_01";
    // Important runtime data or configuration used by this component: defaultHairToken.
    public string defaultHairToken = "hair_01";

    [Header("Default clothing colors")]
    public Color shirtColor = new Color(0.34f, 0.17f, 0.07f, 1f);
    public Color trousersColor = new Color(0.18f, 0.09f, 0.04f, 1f);

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        ApplyDefaultPartVisibility();
    }

    // Unity lifecycle: restores runtime configuration or subscriptions when the component is enabled.
    private void OnEnable()
    {
        ApplyDefaultPartVisibility();
    }

    // Refreshes and applies configuration or runtime state for apply default part visibility.
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

    // Calculates and returns the result for should show by default.
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

    // Refreshes and applies configuration or runtime state for apply default clothing color.
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

    // Handles the contains token workflow.
    private static bool ContainsToken(string lowerName, string token)
    {
        return !string.IsNullOrEmpty(token) && lowerName.Contains(token.ToLowerInvariant());
    }

    // Calculates and returns the result for is equipped visual.
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

