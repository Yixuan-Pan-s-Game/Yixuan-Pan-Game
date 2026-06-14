using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    public Color normalColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);
    public Color selectedColor = new Color(1f, 0.84f, 0.25f, 0.9f);

    private Image[] slotBackgrounds;
    private Text[] slotLabels;

    public int SlotCount => slotBackgrounds != null ? slotBackgrounds.Length : 0;

    public void Build(int slotCount)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyChild(transform.GetChild(i).gameObject);
        }

        slotBackgrounds = new Image[slotCount];
        slotLabels = new Text[slotCount];

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 24f);
        root.sizeDelta = new Vector2(slotCount * 76f, 72f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = new GameObject("Slot_" + (i + 1));
            slot.transform.SetParent(transform, false);

            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(68f, 68f);
            rect.anchoredPosition = new Vector2((i - (slotCount - 1) * 0.5f) * 76f, 0f);

            Image background = slot.AddComponent<Image>();
            background.color = normalColor;
            slotBackgrounds[i] = background;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(slot.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            Text label = labelObject.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.font = font;
            label.fontSize = 13;
            label.color = Color.white;
            label.raycastTarget = false;
            slotLabels[i] = label;
        }
    }

    public void Refresh(PlayerToolController controller)
    {
        if (controller == null || slotBackgrounds == null)
        {
            return;
        }

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            bool selected = i == controller.SelectedIndex;
            slotBackgrounds[i].color = selected ? selectedColor : normalColor;

            ToolSlot slot = controller.GetHotbarSlot(i);
            if (slotLabels[i] != null && slot != null)
            {
                string countText = slot.stackCount > 1 ? " x" + slot.stackCount : string.Empty;
                slotLabels[i].text = (i + 1) + "\n" + slot.displayName + countText;
            }
            else if (slotLabels[i] != null)
            {
                slotLabels[i].text = (i + 1) + "\nEmpty";
            }
        }
    }

    private static void DestroyChild(GameObject child)
    {
        if (Application.isPlaying)
        {
            Destroy(child);
        }
        else
        {
            DestroyImmediate(child);
        }
    }
}



