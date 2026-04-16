using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// A single item available for purchase in the store.
/// </summary>
[System.Serializable]
public class StoreItem
{
    [Tooltip("Display name shown in the store")]
    public string itemName = "Item";

    [Tooltip("Optional icon shown on the left of the item row")]
    public Sprite icon;

    [Tooltip("Cost in the linked GameCollectionManager currency")]
    public int price = 10;

    [Tooltip("Maximum number of times this item can be purchased. 0 = unlimited, 1 = one-time unlock, 3 = up to three times.")]
    public int maxPurchases = 0;

    [Tooltip("Remember this purchase across scene loads. Keep on for permanent unlocks and upgrades. Turn off for consumables like health packs.")]
    public bool persistPurchase = true;

    /// <summary>
    /// Fires when this item is successfully purchased. Wire to game-world effects only
    /// (enable a spawner, increase jump height, unlock an ability) — sound and UI feedback
    /// are handled automatically by the store. This event also re-fires on scene load for
    /// already-purchased persistent items, so game state is correctly restored.
    /// </summary>
    public UnityEvent onPurchased;

    /// <summary>Fires when the player tries to buy but cannot afford the item</summary>
    public UnityEvent onCannotAfford;

    // Runtime UI references — set by GameStoreManager, not serialized
    [System.NonSerialized] public int purchaseCount;
    [System.NonSerialized] public Button buyButton;
    [System.NonSerialized] public Image rowBackground;
    [System.NonSerialized] public TextMeshProUGUI nameText;
    [System.NonSerialized] public TextMeshProUGUI priceText;
    [System.NonSerialized] public Image iconImage;
}

/// <summary>
/// Controls how the store is opened and closed.
/// </summary>
public enum StoreOpenMode
{
    /// <summary>Press a key (default: B) to toggle the store from anywhere.</summary>
    Key,
    /// <summary>Open and close via UnityEvents. Use with InputTriggerZone for walk-up shopkeepers.</summary>
    EventOnly
}

/// <summary>
/// In-game store that reads currency from a GameCollectionManager, displays purchasable
/// items in a scrollable panel, pauses the game while open, and fires per-item events
/// on purchase. Open mode can be a key press or triggered via UnityEvents.
/// Common use: upgrade shops, unlock screens, item vendors, ability stores.
/// </summary>
public class GameStoreManager : MonoBehaviour
{
    [Header("Currency Source")]
    [Tooltip("The GameCollectionManager that tracks the player's currency")]
    [SerializeField] private GameCollectionManager currencySource;

    [Header("Store Items")]
    [SerializeField] private List<StoreItem> items = new List<StoreItem> { new StoreItem() };

    [Header("Open Mode")]
    [Tooltip("Key: press a key to toggle the store. EventOnly: open/close via UnityEvents (e.g. walk up to a shopkeeper).")]
    [SerializeField] private StoreOpenMode openMode = StoreOpenMode.Key;

    [Tooltip("Key that opens and closes the store (only used when Open Mode is Key)")]
    [SerializeField] private KeyCode storeKey = KeyCode.B;

    [Header("Character Controller (Optional)")]
    [Tooltip("Assign a CharacterControllerFP to unlock the cursor and disable look input while the store is open")]
    [SerializeField] private CharacterControllerFP fpController;

    [Tooltip("Assign a CharacterControllerCC to disable movement while the store is open")]
    [SerializeField] private CharacterControllerCC ccController;

    [Header("Purchase Limit")]
    [Tooltip("If enabled, each item can only be purchased once per store visit, regardless of its Max Purchases setting")]
    [SerializeField] private bool limitOnePurchasePerOpen = false;

    [Tooltip("What happens to persistent item purchases when the scene is loaded via RestartScene. Keep Value means upgrades survive death — recommended for permanent unlocks.")]
    [SerializeField] private RestartBehavior onRestartBehavior = RestartBehavior.KeepValue;

    [Header("Audio (Optional)")]
    [Tooltip("AudioManager to use for music switching")]
    [SerializeField] private GameAudioManager audioManager;

    [Tooltip("Music to play when the store opens. Leave empty to keep the current music.")]
    [SerializeField] private AudioClip storeMusic;

    [Tooltip("Music to restore when the store closes. Leave empty to leave the music as-is.")]
    [SerializeField] private AudioClip previousMusic;

    [Header("Sound Effects (Optional)")]
    [Tooltip("AudioSource used to play purchase sound effects. Falls back to GetComponent if not assigned.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sound played when an item is successfully purchased")]
    [SerializeField] private AudioClip purchaseSound;

    [Tooltip("Sound played when the player tries to buy but cannot afford an item")]
    [SerializeField] private AudioClip cannotAffordSound;

    [Header("Store UI")]
    [Tooltip("Create a self-contained store panel at runtime")]
    [SerializeField] private bool showUI = true;

    // Panel
    [Tooltip("Title text shown at the top of the store panel")]
    [SerializeField] private string storeTitle = "STORE";

    [Tooltip("Position of the panel center relative to screen center (0,0 = centered)")]
    [SerializeField] private Vector2 panelPosition = Vector2.zero;

    [Tooltip("Width and height of the store panel in pixels (at 1920×1080)")]
    [SerializeField] private Vector2 panelSize = new Vector2(520f, 620f);

    [Tooltip("Optional sprite for the panel background. Leave empty to use a solid color.")]
    [SerializeField] private Sprite panelBackgroundSprite;

    [SerializeField] private Color panelBackgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.97f);

    // Rows
    [Tooltip("Height of each item row in pixels")]
    [SerializeField] private float rowHeight = 72f;

    [Tooltip("Vertical gap between rows in pixels")]
    [SerializeField] private float rowSpacing = 6f;

    [Tooltip("Horizontal padding inside each row")]
    [SerializeField] private float rowPadding = 10f;

    [Tooltip("Optional sprite for row backgrounds. Leave empty to use a solid color.")]
    [SerializeField] private Sprite rowBackgroundSprite;

    [SerializeField] private Color rowBackgroundColor = new Color(0.16f, 0.16f, 0.22f, 0.9f);

    [SerializeField] private Color rowDisabledColor = new Color(0.22f, 0.22f, 0.22f, 0.65f);

    // Icons
    [Tooltip("Size of item icons in pixels (width and height)")]
    [SerializeField] private float iconSize = 52f;

    // Button
    [Tooltip("Optional sprite for the Buy button. Leave empty to use a solid color.")]
    [SerializeField] private Sprite buttonSprite;

    [SerializeField] private Color buttonColor = new Color(0.18f, 0.72f, 0.32f, 1f);

    [SerializeField] private Color buttonDisabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Tooltip("Width and height of the Buy button")]
    [SerializeField] private Vector2 buttonSize = new Vector2(80f, 44f);

    // Typography
    [Tooltip("Font size for item names and title")]
    [SerializeField] private float fontSize = 22f;

    [Tooltip("Font size for price and Buy button labels")]
    [SerializeField] private float priceFontSize = 17f;

    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Color used for price values and the balance display")]
    [SerializeField] private Color priceTextColor = new Color(1f, 0.85f, 0.25f, 1f);

    [Tooltip("Optional custom font. Leave empty to use the TMP default.")]
    [SerializeField] private TMP_FontAsset customFont;

    [Tooltip("Label prefix for the balance display (e.g. 'Coins' shows 'Coins: 150')")]
    [SerializeField] private string currencyLabel = "Balance";

    [Tooltip("Optional symbol shown before currency values (e.g. '$' shows '$10'). Leave empty for none.")]
    [SerializeField] private string currencySymbol = "";

    [Header("Events")]
    /// <summary>Fires when the store is opened</summary>
    public UnityEvent onStoreOpened;

    /// <summary>Fires when the store is closed</summary>
    public UnityEvent onStoreClosed;

    /// <summary>Fires whenever any item is successfully purchased</summary>
    public UnityEvent onAnyPurchase;

    // ── Runtime state ──────────────────────────────────────────────────────────
    private Canvas storeCanvas;
    private GameObject storePanel;
    private CanvasGroup storePanelGroup;
    private RectTransform scrollContent;
    private TextMeshProUGUI balanceText;
    private bool isOpen = false;
    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;
    private readonly HashSet<int> purchasedThisOpen = new HashSet<int>();

    /// <summary>Whether the store panel is currently open</summary>
    public bool IsOpen => isOpen;

    private void OnDestroy()
    {
        if (isOpen)
        {
            Time.timeScale = 1f;
            if (ccController != null) ccController.enabled = true;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (currencySource == null)
            Debug.LogWarning("[GameStoreManager] Currency Source is not assigned. Purchases will not work.", this);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (showUI)
            CreateStoreUI();

        RestorePersistedPurchases();
    }

    private void RestorePersistedPurchases()
    {
        bool isRestart = GameData.IsRestart;

        for (int i = 0; i < items.Count; i++)
        {
            StoreItem item = items[i];
            if (!item.persistPurchase || item.maxPurchases == 0) continue;

            if (i >= GameData.STORE_SLOT_COUNT)
            {
                Debug.LogWarning($"[GameStoreManager] Item '{item.itemName}' at index {i} exceeds the {GameData.STORE_SLOT_COUNT}-item persistence limit and will not be persisted.", this);
                continue;
            }

            if (isRestart && onRestartBehavior == RestartBehavior.ResetToDefault)
            {
                GameData.Instance.ClearInt(GameData.STORE_SLOT_START + i);
                continue;
            }

            int stored = GameData.Instance.GetInt(GameData.STORE_SLOT_START + i, 0);
            if (stored <= 0) continue;

            item.purchaseCount = stored;

            // Re-fire onPurchased once per purchase to restore game state
            // (e.g. two speed upgrades bought → fires twice → both applied)
            for (int p = 0; p < stored; p++)
                item.onPurchased.Invoke();
        }
    }

    private void Update()
    {
        if (openMode == StoreOpenMode.Key && Input.GetKeyDown(storeKey))
            ToggleStore();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles the store open or closed.
    /// </summary>
    public void ToggleStore()
    {
        if (isOpen) CloseStore();
        else OpenStore();
    }

    /// <summary>
    /// Opens the store, pauses the game, refreshes affordability, and fires onStoreOpened.
    /// </summary>
    public void OpenStore()
    {
        if (isOpen) return;
        isOpen = true;
        purchasedThisOpen.Clear();

        if (ccController != null) ccController.enabled = false;

        // Save cursor state and release it so clicks reach the UI buttons
        savedCursorLockMode = Cursor.lockState;
        savedCursorVisible  = Cursor.visible;

        if (fpController != null)
        {
            fpController.UnlockCursor();
            fpController.SetInputEnabled(false);
        }
        else if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // Show panel — CanvasGroup keeps it always active so layout is already valid
        if (storePanel != null && storePanelGroup != null)
        {
            storePanelGroup.alpha          = 1f;
            storePanelGroup.interactable   = true;
            storePanelGroup.blocksRaycasts = true;
            storePanel.transform.localScale = Vector3.one * 0.9f;
            storePanel.transform.DOScale(Vector3.one, 0.2f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        RefreshAllRows();

        // Switch music before pausing so the crossfade tween fires
        if (audioManager != null && storeMusic != null)
            audioManager.PlayMusic(storeMusic, true);

        Time.timeScale = 0f;
        onStoreOpened.Invoke();
    }

    /// <summary>
    /// Closes the store, resumes the game, restores the cursor, and fires onStoreClosed.
    /// </summary>
    public void CloseStore()
    {
        if (!isOpen) return;
        isOpen = false;

        if (storePanelGroup != null)
        {
            storePanelGroup.alpha          = 0f;
            storePanelGroup.interactable   = false;
            storePanelGroup.blocksRaycasts = false;
        }

        // Resume time before restoring music so crossfade tween works
        Time.timeScale = 1f;

        if (ccController != null) ccController.enabled = true;

        // Restore cursor
        if (fpController != null)
        {
            if (savedCursorLockMode != CursorLockMode.None)
                fpController.LockCursor();
            fpController.SetInputEnabled(true);
        }
        else
        {
            Cursor.lockState = savedCursorLockMode;
            Cursor.visible   = savedCursorVisible;
        }

        if (audioManager != null && previousMusic != null)
            audioManager.PlayMusic(previousMusic, true);

        onStoreClosed.Invoke();
    }

    /// <summary>
    /// Attempts to purchase the item at the given index.
    /// Called automatically by Buy buttons; can also be called from UnityEvents.
    /// </summary>
    public void PurchaseItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        StoreItem item = items[index];

        // Max purchases reached
        if (item.maxPurchases > 0 && item.purchaseCount >= item.maxPurchases) return;

        // Per-open limit
        if (limitOnePurchasePerOpen && purchasedThisOpen.Contains(index)) return;

        int balance = currencySource != null ? currencySource.GetCurrentValue() : 0;

        if (balance >= item.price)
        {
            if (currencySource != null)
                currencySource.Decrement(item.price);

            item.purchaseCount++;

            if (item.persistPurchase && item.maxPurchases > 0 && index < GameData.STORE_SLOT_COUNT)
                GameData.Instance.SetInt(GameData.STORE_SLOT_START + index, item.purchaseCount);

            if (limitOnePurchasePerOpen)
                purchasedThisOpen.Add(index);

            if (audioSource != null && purchaseSound != null)
                audioSource.PlayOneShot(purchaseSound);

            item.onPurchased.Invoke();
            onAnyPurchase.Invoke();
            RefreshAllRows();
        }
        else
        {
            if (audioSource != null && cannotAffordSound != null)
                audioSource.PlayOneShot(cannotAffordSound);

            item.onCannotAfford.Invoke();
        }
    }

    /// <summary>
    /// Updates affordability visuals and the balance display for all rows.
    /// Called automatically on open and after each purchase.
    /// </summary>
    public void RefreshAllRows()
    {
        int balance = currencySource != null ? currencySource.GetCurrentValue() : 0;

        if (balanceText != null)
            balanceText.text = $"{currencyLabel}: {currencySymbol}{balance}";

        for (int i = 0; i < items.Count; i++)
        {
            StoreItem item = items[i];
            bool maxReached    = item.maxPurchases > 0 && item.purchaseCount >= item.maxPurchases;
            bool usedThisOpen  = limitOnePurchasePerOpen && purchasedThisOpen.Contains(i);
            bool canAfford     = balance >= item.price;
            bool enabled       = !maxReached && !usedThisOpen && canAfford;

            if (item.rowBackground != null)
                item.rowBackground.color = enabled ? rowBackgroundColor : rowDisabledColor;

            if (item.priceText != null)
            {
                if (maxReached)
                {
                    item.priceText.text = "SOLD";
                }
                else if (usedThisOpen)
                {
                    item.priceText.text = "Come back later";
                }
                else if (item.maxPurchases > 1)
                {
                    int remaining = item.maxPurchases - item.purchaseCount;
                    item.priceText.text = $"{currencySymbol}{item.price}  ({remaining} left)";
                }
                else
                {
                    item.priceText.text = $"{currencySymbol}{item.price}";
                }
            }

            if (item.buyButton != null)
            {
                item.buyButton.interactable = enabled;
                Image btnImg = item.buyButton.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = enabled ? buttonColor : buttonDisabledColor;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UI Creation
    // ──────────────────────────────────────────────────────────────────────────

    private void CreateStoreUI()
    {
        BuildCanvas("StoreUI_Canvas", out storeCanvas);

        storePanel = BuildPanel(storeCanvas.transform);
        // Hide via CanvasGroup so the panel stays active and Unity's layout system
        // sizes all rows correctly on the first frame.
        storePanelGroup = storePanel.AddComponent<CanvasGroup>();
        storePanelGroup.alpha          = 0f;
        storePanelGroup.interactable   = false;
        storePanelGroup.blocksRaycasts = false;
    }

    private void BuildCanvas(string name, out Canvas canvas)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);

        canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution   = new Vector2(1920, 1080);
        scaler.screenMatchMode       = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight    = 0f;

        obj.AddComponent<GraphicRaycaster>();
    }

    private GameObject BuildPanel(Transform canvasTransform)
    {
        const float titleHeight   = 52f;
        const float balanceHeight = 44f;
        float scrollHeight = panelSize.y - titleHeight - balanceHeight;

        // Root panel
        GameObject panel = new GameObject("StorePanel");
        panel.transform.SetParent(canvasTransform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = panelPosition;
        rt.sizeDelta        = panelSize;

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = panelBackgroundColor;
        if (panelBackgroundSprite != null)
        {
            panelImg.sprite = panelBackgroundSprite;
            panelImg.type   = Image.Type.Sliced;
        }

        BuildTitleBar(panel, titleHeight);
        BuildScrollView(panel, titleHeight, scrollHeight);
        BuildBalanceDisplay(panel, balanceHeight);

        return panel;
    }

    private void BuildTitleBar(GameObject parent, float height)
    {
        GameObject bar = new GameObject("TitleBar");
        bar.transform.SetParent(parent.transform, false);

        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, height);

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(bar.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin  = Vector2.zero;
        titleRt.anchorMax  = Vector2.one;
        titleRt.offsetMin  = new Vector2(16, 0);
        titleRt.offsetMax  = new Vector2(-52, 0);

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text               = storeTitle;
        titleTmp.fontSize           = fontSize + 4f;
        titleTmp.color              = textColor;
        titleTmp.fontStyle          = FontStyles.Bold;
        titleTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        titleTmp.enableWordWrapping = false;
        if (customFont != null) titleTmp.font = customFont;

        // Close button
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(bar.transform, false);
        RectTransform closeRt = closeObj.AddComponent<RectTransform>();
        closeRt.anchorMin        = new Vector2(1, 0.5f);
        closeRt.anchorMax        = new Vector2(1, 0.5f);
        closeRt.pivot            = new Vector2(1, 0.5f);
        closeRt.anchoredPosition = new Vector2(-8, 0);
        closeRt.sizeDelta        = new Vector2(40, 40);

        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.7f, 0.2f, 0.2f, 0.85f);

        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseStore);

        GameObject closeLabelObj = new GameObject("Label");
        closeLabelObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeLabelRt = closeLabelObj.AddComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI closeTmp = closeLabelObj.AddComponent<TextMeshProUGUI>();
        closeTmp.text      = "X";
        closeTmp.fontSize  = fontSize;
        closeTmp.color     = Color.white;
        closeTmp.alignment = TextAlignmentOptions.Center;
        if (customFont != null) closeTmp.font = customFont;
    }

    private void BuildScrollView(GameObject parent, float topOffset, float height)
    {
        // ScrollView root
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(parent.transform, false);
        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin        = new Vector2(0, 1);
        scrollRt.anchorMax        = new Vector2(1, 1);
        scrollRt.pivot            = new Vector2(0.5f, 1);
        scrollRt.anchoredPosition = new Vector2(0, -topOffset);
        scrollRt.sizeDelta        = new Vector2(0, height);

        ScrollRect scroll   = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal   = false;
        scroll.inertia      = false; // inertia freezes at timeScale=0

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        Image viewportImg = viewportObj.AddComponent<Image>();
        viewportImg.color = Color.white;
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        scroll.viewport = viewportRt;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta        = Vector2.zero;
        scrollContent              = contentRt;

        VerticalLayoutGroup vlg   = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = rowSpacing;
        vlg.padding               = new RectOffset(
            Mathf.RoundToInt(rowPadding), Mathf.RoundToInt(rowPadding),
            Mathf.RoundToInt(rowPadding), Mathf.RoundToInt(rowPadding));
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight= false;

        ContentSizeFitter csf  = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit        = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRt;

        // Item rows
        for (int i = 0; i < items.Count; i++)
            BuildItemRow(items[i], i, contentObj);
    }

    private void BuildItemRow(StoreItem item, int index, GameObject parent)
    {
        GameObject rowObj = new GameObject($"Row_{item.itemName}");
        rowObj.transform.SetParent(parent.transform, false);

        RectTransform rowRt = rowObj.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, rowHeight);

        item.rowBackground       = rowObj.AddComponent<Image>();
        item.rowBackground.color = rowBackgroundColor;
        if (rowBackgroundSprite != null)
        {
            item.rowBackground.sprite = rowBackgroundSprite;
            item.rowBackground.type   = Image.Type.Sliced;
        }

        LayoutElement rowLE     = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight   = rowHeight;
        rowLE.flexibleWidth     = 1;

        HorizontalLayoutGroup hlg  = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = rowPadding;
        hlg.padding                = new RectOffset(Mathf.RoundToInt(rowPadding), Mathf.RoundToInt(rowPadding), 0, 0);
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Icon
        if (item.icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(rowObj.transform, false);

            item.iconImage              = iconObj.AddComponent<Image>();
            item.iconImage.sprite       = item.icon;
            item.iconImage.preserveAspect = true;

            LayoutElement iconLE  = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = iconSize;
            iconLE.minWidth       = iconSize;
        }

        // Info group — name + price
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(rowObj.transform, false);

        VerticalLayoutGroup infoVlg   = infoObj.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment        = TextAnchor.MiddleLeft;
        infoVlg.childControlWidth     = true;
        infoVlg.childControlHeight    = true;
        infoVlg.childForceExpandWidth = true;
        infoVlg.childForceExpandHeight= true;

        LayoutElement infoLE  = infoObj.AddComponent<LayoutElement>();
        infoLE.flexibleWidth  = 1;

        item.nameText  = BuildLabel(infoObj, item.itemName,                      fontSize,      textColor,      TextAlignmentOptions.MidlineLeft);
        item.priceText = BuildLabel(infoObj, $"{currencySymbol}{item.price}",   priceFontSize, priceTextColor, TextAlignmentOptions.MidlineLeft);

        // Buy button
        GameObject btnObj = new GameObject("BuyButton");
        btnObj.transform.SetParent(rowObj.transform, false);

        Image btnImg   = btnObj.AddComponent<Image>();
        btnImg.color   = buttonColor;
        if (buttonSprite != null)
        {
            btnImg.sprite = buttonSprite;
            btnImg.type   = Image.Type.Sliced;
        }

        item.buyButton = btnObj.AddComponent<Button>();
        int capturedIndex = index;
        item.buyButton.onClick.AddListener(() => PurchaseItem(capturedIndex));

        LayoutElement btnLE  = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredWidth = buttonSize.x;
        btnLE.minWidth       = buttonSize.x;

        GameObject btnLabelObj = new GameObject("Label");
        btnLabelObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnLabelRt = btnLabelObj.AddComponent<RectTransform>();
        btnLabelRt.anchorMin = Vector2.zero;
        btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = Vector2.zero;
        btnLabelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTmp = btnLabelObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text      = "BUY";
        btnTmp.fontSize  = priceFontSize;
        btnTmp.color     = Color.white;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        if (customFont != null) btnTmp.font = customFont;
    }

    private void BuildBalanceDisplay(GameObject parent, float height)
    {
        GameObject obj = new GameObject("BalanceText");
        obj.transform.SetParent(parent.transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0);
        rt.anchorMax        = new Vector2(1, 0);
        rt.pivot            = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, height);

        balanceText               = obj.AddComponent<TextMeshProUGUI>();
        balanceText.text          = $"{currencyLabel}: --";
        balanceText.fontSize      = fontSize;
        balanceText.color         = priceTextColor;
        balanceText.alignment     = TextAlignmentOptions.Center;
        balanceText.enableWordWrapping = false;
        if (customFont != null) balanceText.font = customFont;
    }

    private TextMeshProUGUI BuildLabel(GameObject parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp        = obj.AddComponent<TextMeshProUGUI>();
        tmp.text                   = text;
        tmp.fontSize               = size;
        tmp.color                  = color;
        tmp.alignment              = align;
        tmp.enableWordWrapping     = false;
        tmp.overflowMode           = TextOverflowModes.Ellipsis;
        if (customFont != null) tmp.font = customFont;

        return tmp;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Editor Preview
    // ──────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    /// <summary>
    /// Creates an editor-only preview canvas so you can see the store layout in the Scene view.
    /// </summary>
    public void CreatePreviewUI()
    {
        DestroyPreviewUI();
        if (!showUI) return;

        GameObject canvasObj = new GameObject("StoreUI_PREVIEW");
        canvasObj.transform.SetParent(transform);
        canvasObj.hideFlags = HideFlags.DontSave;

        storeCanvas              = canvasObj.AddComponent<Canvas>();
        storeCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        storeCanvas.sortingOrder = 20;

        CanvasScaler scaler          = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution   = new Vector2(1920, 1080);
        scaler.screenMatchMode       = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight    = 0f;

        storePanel = BuildPanel(storeCanvas.transform);
        storePanel.SetActive(true);

        // Show a dummy balance in preview
        if (balanceText != null)
            balanceText.text = $"{currencyLabel}: 999";

        // Show all rows as available
        for (int i = 0; i < items.Count; i++)
        {
            StoreItem item = items[i];
            if (item.rowBackground != null) item.rowBackground.color = rowBackgroundColor;
            if (item.priceText     != null) item.priceText.text      = item.price.ToString();
            if (item.buyButton     != null)
            {
                item.buyButton.interactable = true;
                Image btnImg = item.buyButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = buttonColor;
            }
        }

        foreach (Transform t in canvasObj.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave;
    }

    /// <summary>Refreshes the preview to reflect current Inspector values.</summary>
    public void UpdatePreviewUI()
    {
        DestroyPreviewUI();
        CreatePreviewUI();
    }

    /// <summary>Destroys the editor preview canvas.</summary>
    public void DestroyPreviewUI()
    {
        if (storeCanvas != null && storeCanvas.gameObject.name.Contains("PREVIEW"))
            UnityEngine.Object.DestroyImmediate(storeCanvas.gameObject);

        storeCanvas = null;
        storePanel  = null;
        balanceText = null;

        if (items != null)
        {
            foreach (var item in items)
            {
                item.buyButton     = null;
                item.rowBackground = null;
                item.nameText      = null;
                item.priceText     = null;
                item.iconImage     = null;
                item.purchaseCount = 0;
            }
        }
    }
#endif
}
