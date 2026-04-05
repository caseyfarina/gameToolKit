using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single inventory slot with a name, icon, capacity, and change events.
/// Configure slots in the GameInventoryManager Inspector.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    [Tooltip("Name of the item this slot tracks (e.g. 'Keys', 'Coins', 'Arrows')")]
    public string itemName = "Item";

    [Tooltip("Optional icon displayed in the UI card")]
    public Sprite icon;

    [Tooltip("Maximum number of items this slot can hold")]
    public int maxCapacity = 10;

    [Tooltip("Starting count for this slot")]
    public int currentCount = 0;


    [Tooltip("Fires when count reaches maxCapacity")]
    /// <summary>
    /// Fires when the slot becomes full (count reaches maxCapacity)
    /// </summary>
    public UnityEvent onFull;

    [Tooltip("Fires when count reaches zero")]
    /// <summary>
    /// Fires when the slot becomes empty (count reaches zero)
    /// </summary>
    public UnityEvent onEmpty;

    [Tooltip("Fires whenever the count changes, passing the new count as a parameter")]
    /// <summary>
    /// Fires whenever the count changes, passing the new count as an int
    /// </summary>
    public UnityEvent<int> onChanged;

    /// <summary>Runtime reference to the count text in the UI card (set by GameInventoryManager)</summary>
    [System.NonSerialized] public TextMeshProUGUI countText;

    /// <summary>Runtime reference to the icon image in the UI card (set by GameInventoryManager)</summary>
    [System.NonSerialized] public Image iconImage;
}

/// <summary>
/// Manages multiple inventory slots, each with capacity limits and change events.
/// Optionally creates a row of UI cards (one per slot) showing icons and counts.
///
/// MULTI-SCENE SUPPORT: Enable Persist Across Scenes to save all slot counts when loading a
/// new scene. Persistence is automatic for the first 20 slots — no extra setup required.
///
/// Common use: Key collections, ammunition types, multi-resource systems, or collectible sets.
/// </summary>
public class GameInventoryManager : MonoBehaviour
{
    [Header("Scene Persistence")]
    [Tooltip("Save all slot counts when loading a new scene. Each scene can have its own manager — only the counts carry over. Limited to the first 20 slots.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("Inventory Slots")]
    [Tooltip("List of item slots - each tracks a different item type")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot> { new InventorySlot() };

    [Header("UI Cards (Optional)")]
    [Tooltip("Enable to create a self-contained row of UI cards, one per slot")]
    [SerializeField] private bool showUI = false;

    [Tooltip("Show the item count number inside each card")]
    [SerializeField] private bool showCount = true;

    [Tooltip("Position of the card row on screen (anchor position, top-left origin)")]
    [SerializeField] private Vector2 uiPosition = new Vector2(60, -120);

    [Tooltip("Width and height of each card in pixels")]
    [SerializeField] private Vector2 cardSize = new Vector2(70, 70);

    [Tooltip("Spacing between cards in pixels")]
    [SerializeField] private float cardSpacing = 8f;

    [Tooltip("Background color of each card")]
    [SerializeField] private Color cardBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Tooltip("Font size for the count text")]
    [SerializeField] private float fontSize = 22f;

    [Tooltip("Count text color")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Custom font (leave empty for TMP default)")]
    [SerializeField] private TMP_FontAsset customFont;

    // Runtime UI references
    private Canvas uiCanvas;

    private void SyncSlotToGameData(int slotIndex, InventorySlot slot)
    {
        if (!persistAcrossScenes) return;
        if (slotIndex >= GameData.INVENTORY_SLOT_COUNT) return;
        GameData.Instance.SetInt(GameData.INVENTORY_SLOT_START + slotIndex, slot.currentCount);
    }

    private void Start()
    {
        // If persistence is enabled, read carried-over counts (or use Inspector defaults on first load)
        if (persistAcrossScenes)
        {
            if (slots.Count > GameData.INVENTORY_SLOT_COUNT)
                Debug.LogWarning($"[GameInventoryManager] Persistence is limited to the first {GameData.INVENTORY_SLOT_COUNT} slots.", this);

            for (int i = 0; i < Mathf.Min(slots.Count, GameData.INVENTORY_SLOT_COUNT); i++)
            {
                slots[i].currentCount = GameData.Instance.GetInt(
                    GameData.INVENTORY_SLOT_START + i, slots[i].currentCount);
            }
        }

        if (showUI)
        {
            CreateCanvas();
            CreateCards();
        }

        // Fire initial events so any external UI wired up is synchronized
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].onChanged.Invoke(slots[i].currentCount);
        }
    }

    // ──────────────────────────────────────────────
    // Public API — by slot index
    // ──────────────────────────────────────────────

    /// <summary>
    /// Adds items to the slot at the given index, up to its maxCapacity
    /// </summary>
    public void Increment(int slotIndex, int amount = 1)
    {
        if (!IsValidIndex(slotIndex)) return;
        InventorySlot slot = slots[slotIndex];
        int previous = slot.currentCount;
        slot.currentCount = Mathf.Clamp(slot.currentCount + amount, 0, slot.maxCapacity);

        if (slot.currentCount != previous)
        {
            SyncSlotToGameData(slotIndex, slot);
            slot.onChanged.Invoke(slot.currentCount);
            UpdateCardText(slot);

            if (slot.currentCount >= slot.maxCapacity && previous < slot.maxCapacity)
                slot.onFull.Invoke();
        }
    }

    /// <summary>
    /// Removes items from the slot at the given index, down to zero
    /// </summary>
    public void Decrement(int slotIndex, int amount = 1)
    {
        if (!IsValidIndex(slotIndex)) return;
        InventorySlot slot = slots[slotIndex];
        int previous = slot.currentCount;
        slot.currentCount = Mathf.Max(0, slot.currentCount - amount);

        if (slot.currentCount != previous)
        {
            SyncSlotToGameData(slotIndex, slot);
            slot.onChanged.Invoke(slot.currentCount);
            UpdateCardText(slot);

            if (slot.currentCount <= 0 && previous > 0)
                slot.onEmpty.Invoke();
        }
    }

    /// <summary>
    /// Uses the specified number of items from the slot if available.
    /// Decrements the count and fires onChanged. Does nothing if not enough items.
    /// </summary>
    public void UseItem(int slotIndex, int amount = 1)
    {
        if (!IsValidIndex(slotIndex)) return;
        if (slots[slotIndex].currentCount >= amount)
            Decrement(slotIndex, amount);
    }

    /// <summary>
    /// Returns the current count of the slot at the given index, or -1 if the index is invalid
    /// </summary>
    public int GetCount(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return -1;
        return slots[slotIndex].currentCount;
    }

    // ──────────────────────────────────────────────
    // Public API — by item name
    // ──────────────────────────────────────────────

    /// <summary>
    /// Adds items to the first slot whose itemName matches, up to its maxCapacity
    /// </summary>
    public void IncrementByName(string itemName, int amount = 1)
    {
        int index = FindSlotIndex(itemName);
        if (index >= 0) Increment(index, amount);
    }

    /// <summary>
    /// Removes items from the first slot whose itemName matches, down to zero
    /// </summary>
    public void DecrementByName(string itemName, int amount = 1)
    {
        int index = FindSlotIndex(itemName);
        if (index >= 0) Decrement(index, amount);
    }

    /// <summary>
    /// Returns the current count of the first slot matching the given itemName, or -1 if not found
    /// </summary>
    public int GetCountByName(string itemName)
    {
        int index = FindSlotIndex(itemName);
        return index >= 0 ? slots[index].currentCount : -1;
    }

    // ──────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Count;
    }

    private int FindSlotIndex(string itemName)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].itemName == itemName)
                return i;
        }
        Debug.LogWarning($"[GameInventoryManager] No slot found with itemName '{itemName}'", this);
        return -1;
    }

    private void UpdateCardText(InventorySlot slot)
    {
        if (slot.countText != null)
            slot.countText.text = slot.currentCount.ToString();
    }

    // ──────────────────────────────────────────────
    // UI creation
    // ──────────────────────────────────────────────

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("InventoryUI_Canvas");
        canvasObj.transform.SetParent(transform);

        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateCards()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            float xOffset = i * (cardSize.x + cardSpacing);
            CreateCard(slots[i], new Vector2(uiPosition.x + xOffset, uiPosition.y));
        }
    }

    private void CreateCard(InventorySlot slot, Vector2 position)
    {
        // Card root
        GameObject cardObj = new GameObject($"Card_{slot.itemName}");
        cardObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0, 1);
        cardRect.anchorMax = new Vector2(0, 1);
        cardRect.pivot = new Vector2(0, 1);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = cardSize;

        Image bg = cardObj.AddComponent<Image>();
        bg.color = cardBackgroundColor;

        // Icon (takes top portion of card, leaving room for count text at bottom)
        float countHeight = showCount ? fontSize + 4f : 0f;

        if (slot.icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = new Vector2(4, countHeight + 2);
            iconRect.offsetMax = new Vector2(-4, -4);

            slot.iconImage = iconObj.AddComponent<Image>();
            slot.iconImage.sprite = slot.icon;
            slot.iconImage.preserveAspect = true;
        }

        // Count text (sits at the bottom of the card)
        if (showCount)
        {
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(cardObj.transform, false);

            RectTransform countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0, 0);
            countRect.anchorMax = new Vector2(1, 0);
            countRect.pivot = new Vector2(0.5f, 0);
            countRect.anchoredPosition = Vector2.zero;
            countRect.sizeDelta = new Vector2(0, countHeight);

            slot.countText = countObj.AddComponent<TextMeshProUGUI>();
            slot.countText.text = slot.currentCount.ToString();
            slot.countText.fontSize = fontSize;
            slot.countText.color = textColor;
            slot.countText.alignment = TextAlignmentOptions.Center;
            slot.countText.enableWordWrapping = false;
            slot.countText.overflowMode = TextOverflowModes.Overflow;

            if (customFont != null)
                slot.countText.font = customFont;
        }
    }

    private void OnDestroy()
    {
        // Nothing to clean up besides Unity's normal object teardown
    }

#if UNITY_EDITOR
    /// <summary>
    /// Creates a preview Canvas in the editor for positioning the UI cards
    /// </summary>
    public void CreatePreviewUI()
    {
        DestroyPreviewUI();
        if (!showUI) return;

        GameObject canvasObj = new GameObject("InventoryUI_PREVIEW");
        canvasObj.transform.SetParent(transform);
        canvasObj.hideFlags = HideFlags.DontSave;

        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        CreateCards();

        // Tag all children as DontSave
        foreach (Transform child in uiCanvas.transform.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.DontSave;
    }

    /// <summary>
    /// Updates the preview to reflect current Inspector values
    /// </summary>
    public void UpdatePreviewUI()
    {
        DestroyPreviewUI();
        CreatePreviewUI();
    }

    /// <summary>
    /// Destroys the preview Canvas
    /// </summary>
    public void DestroyPreviewUI()
    {
        if (uiCanvas != null && uiCanvas.gameObject.name.Contains("PREVIEW"))
        {
            Object.DestroyImmediate(uiCanvas.gameObject);
        }
        uiCanvas = null;
        if (slots != null)
        {
            foreach (var s in slots)
            {
                s.countText = null;
                s.iconImage = null;
            }
        }
    }
#endif
}
