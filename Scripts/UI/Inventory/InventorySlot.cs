using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Interactions;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IScrollHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image progressBorderImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image realbackgroundImage;
    [SerializeField] private TMP_Text stackText;
    public InventoryPanel inventoryPanel;
    private Item item;
    private static InventorySlot heldSlot;
    private static Item heldItem;
    private static GameObject heldIcon;
    private static TextMeshProUGUI heldStackText;
    private Canvas canvas;
    private static InventorySlot hoveredSlot;
    [InjectAttribute1] private IItemInfoManager itemInfoManager { get; set; }
    [InjectAttribute1] private ITooltipManager tooltipManager { get; set; } // Добавляем инъекцию TooltipManager
    [InjectAttribute1] private IChestUIController chestUIController { get; set; }
    private float lastClickTime;
    private float itemPickupTime = -1f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;
    private bool isCursorInSlot;
    private Color initialBorderColor = Color.white;
    private Color progressBorderColor = Color.yellow;
    private bool hasPickedUp;
    private float fHoldTime;
    private const float F_HOLD_THRESHOLD = 0.5f; // 500 мс
    private Color modifiableBorderColor = new Color(0.83f, 0.83f, 0.83f);  // Light Gray (#D3D3D3)
    private Color greenBackgroundColor = new Color(94f / 255f, 156f / 255f, 110f / 255f, 150f/255f);
    private Color redBackgroundColor = new Color(184f / 255f, 80f / 255f, 80f / 255f, 150f/255f);
    private Color blueBackgroundColor = new Color(86f / 255f, 142f / 255f, 198f / 255f, 83f/255f);
    private Color yellowBackgroundColor = new Color(255f/255f, 192f/255f, 0f/255f, 150f/255f); // Прозрачный белый цвет
    private bool shiftPressed;
    private bool fPressed;
    private float shiftPressTime;
    private float fPressTime;
    private const float COMBO_WINDOW = 0.2f; // 200 мс
    private float leftClickHoldStartTime = -1f;
    private const float HOLD_THRESHOLD = 0.2f;
    private bool isHoldingLeftClick = false;
    private bool hasTriedDropAfterHold = false;

    private float rightClickHoldStartTime = -1f;
    private const float RIGHT_HOLD_THRESHOLD = 0.2f;

    private string inputNumber = "";
    private static InventorySlot inputSlot;
    private bool Deleted = true;
    private static bool isIllusion = false;

    private float rightClickHoldDelay; // Новая переменная для задержки
    private  InventorySlot rightDragStartSlot = null;
    private  bool isRightDragging = false;
    private  HashSet<InventorySlot> rightDragUsedSlots = new HashSet<InventorySlot>();
    private static List<InventorySlot> leftDragUsedSlots = new List<InventorySlot>();
    private static bool LKMstart = true;
    private static bool LKMraztagivanie = false;
    private static bool CanRaztagivanie = false;
    private static InventorySlot realHoveredSlot;
    private bool canSwap = true;
    private static string searchInput = "";
    private static bool searchActive = false;
    public static bool isSearchMode = false;
    public bool isYellow = false;
    public static bool isEscape = false;
    public static bool isRealEscape = false;
    static bool isSearchLocked = false;
    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        if (itemImage == null || backgroundImage == null || progressBorderImage == null || stackText == null)
        {
            Debug.LogError("Один из компонентов не назначен для слота инвентаря!");
        }

        backgroundImage.enabled = true;
        itemImage.enabled = false;
        progressBorderImage.enabled = false;
        stackText.enabled = false;
        realbackgroundImage.enabled = false;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas не найден!");
        }

        // backgroundImage.color = initialBorderColor;
        backgroundImage.color = modifiableBorderColor;
        progressBorderImage.color = progressBorderColor;

        progressBorderImage.type = Image.Type.Filled;
        progressBorderImage.fillMethod = Image.FillMethod.Radial360;
        progressBorderImage.fillOrigin = (int)Image.Origin360.Top;
        progressBorderImage.fillClockwise = true;
        progressBorderImage.fillAmount = 0f;

        DependencyContainer1.InjectDependencies(this);
    }

    private void Start()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryPanel не назначен для слота!");
        }
    }

    private void Update()
    {
        // if (isCursorInSlot && Input.GetKeyDown(KeyCode.C) && item != null)
        // {
        //     ClearSlot();
        // }
        if (heldItem == null && isCursorInSlot && item != null && realHoveredSlot != null)
        {
            if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                realHoveredSlot.TryMoveToOtherPanel();
                return;
            }
        }
        if (heldItem != null && Input.GetMouseButton(1))
        {
            if (!isRightDragging)
            {
                // начало правого драг-дропа
                isRightDragging = true;
                rightDragStartSlot = this;
                rightDragUsedSlots.Clear();
            }
        }
        else if (heldItem != null && Input.GetMouseButton(0))
        {
            if (isCursorInSlot && realHoveredSlot != null && !leftDragUsedSlots.Contains(realHoveredSlot) && !LKMstart && CanRaztagivanie)
            {
                if (item == null || item.id == heldItem.id)
                {
                    LKMraztagivanie = true;
                    int maxStackSize = heldItem.maxStackSize;
                    leftDragUsedSlots.Add(realHoveredSlot);

                    int slotsCount = leftDragUsedSlots.Count;

                    // Считаем общее количество предметов: в руке + в слотах
                    int totalHeld = heldItem.stackSize;

                    // Всё кладём заново, так что обнуляем слоты
                    foreach (var slot in leftDragUsedSlots)
                    {
                        if (slot.item != null && slot.item.id == heldItem.id)
                        {
                            totalHeld += slot.item.stackSize;
                        }
                    }

                    if (totalHeld < slotsCount)
                    {
                        // Недостаточно предметов — полностью отменяем растягивание
                        CanRaztagivanie = false;
                        leftDragUsedSlots.Clear();
                        ClearHeldItem();
                        return;
                    }

                    // Считаем равномерное распределение 
                    int amountPerSlot = totalHeld / slotsCount;
                    int remainder = totalHeld % slotsCount; // Остаток остаётся в руке

                    if (amountPerSlot > maxStackSize)
                    {
                        remainder = amountPerSlot - maxStackSize;
                        amountPerSlot = maxStackSize;
                    }

                    if (item != null && item.stackSize < maxStackSize)
                    {
                        canSwap = false;
                    }
                    else
                    {
                        canSwap = true;
                    }

                    foreach (var slot in leftDragUsedSlots)
                    {
                        if (slot.item == null)
                        {
                            Item newItem = new Item(
                                heldItem.id,
                                heldItem.itemName,
                                heldItem.description,
                                heldItem.icon,
                                amountPerSlot,
                                heldItem.maxStackSize,
                                heldItem.isModifiable,
                                heldItem.isRecipe,
                                heldItem.recipeUsesLeft
                            );
                            slot.SetItem(newItem);
                        }
                        else
                        {
                            slot.item.stackSize = amountPerSlot;
                            slot.SetItem(slot.item);
                        }
                    }

                    // Остаток предметов остаётся в руке
                    heldItem.stackSize = remainder;
                    UpdateHeldIcon();
                }
            }
        }
        else
        {
            isRightDragging = false;
            rightDragStartSlot = null;
            rightDragUsedSlots.Clear();
            leftDragUsedSlots.Clear();
            LKMraztagivanie = false;

            if (heldItem != null && heldItem.stackSize == 0 && !isIllusion)
            {
                ClearHeldItem();
            }
        }
        HandleItemCountInput(); // добавим вызов метода ниже
        if (isHoldingLeftClick && !hasTriedDropAfterHold)
        {
            float holdDuration = Time.unscaledTime - leftClickHoldStartTime;
            if (holdDuration > HOLD_THRESHOLD && Input.GetMouseButtonUp(0))
            {
                TryPlaceHeldItem();
                isHoldingLeftClick = false;
                hasTriedDropAfterHold = true;
            }
        }

        if (heldIcon != null)
        {
            tooltipManager?.HideTooltip();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out localPoint
            );
            heldIcon.GetComponent<RectTransform>().anchoredPosition = localPoint + new Vector2(10, -10);
        }

        if (item != null && isCursorInSlot && heldItem == null)
        {
            frameImage.enabled = true;
        }
        else
        {
            frameImage.enabled = false;
        }
        if (heldItem != null && (item == null || item.id == heldItem.id) && isCursorInSlot && !isIllusion)
        {
            realbackgroundImage.enabled = true;
            realbackgroundImage.color = greenBackgroundColor;
        }
        else if (heldItem != null && item != null && item.id != heldItem.id && isCursorInSlot && !isIllusion)
        {
            realbackgroundImage.enabled = true;
            realbackgroundImage.color = redBackgroundColor;
        }
        else if (isYellow && item != null)
        {
            realbackgroundImage.enabled = true;
            realbackgroundImage.color = yellowBackgroundColor;
        }
        else if (item != null)
        {
            realbackgroundImage.enabled = true;
            realbackgroundImage.color = blueBackgroundColor;
        }
        else
        {
            realbackgroundImage.enabled = false;
        }

        if (item != null && item.isModifiable && !isIllusion)
        {
            if (isCursorInSlot && heldItem == null)
            {
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                {
                    shiftPressed = true;
                    shiftPressTime = Time.unscaledTime;
                }

                // Мгновенное открытие панели при Shift+ПКМ
                if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButtonDown(1))
                {
                    itemInfoManager?.ShowItemInfo(item);
                    shiftPressed = false;
                    fPressed = false;
                }
                // Основная логика удержания ПКМ без Shift
                else if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    rightClickHoldDelay += Time.unscaledDeltaTime;

                    if (rightClickHoldDelay >= HOLD_THRESHOLD)
                    {
                        fPressed = true;
                        fPressTime = Time.unscaledTime;

                        fHoldTime += Time.unscaledDeltaTime;

                        progressBorderImage.enabled = true;
                        progressBorderImage.fillAmount = fHoldTime / F_HOLD_THRESHOLD;

                        if (fHoldTime >= F_HOLD_THRESHOLD)
                        {
                            itemInfoManager?.ShowItemInfo(item);
                            ResetFInteraction();
                            rightClickHoldDelay = 0f;
                        }
                    }
                }
                else
                {
                    ResetFInteraction();
                    rightClickHoldDelay = 0f;
                }

                if (shiftPressed && fPressed)
                {
                    if (Mathf.Abs(shiftPressTime - fPressTime) <= COMBO_WINDOW)
                    {
                        itemInfoManager?.ShowItemInfo(item);
                    }

                    shiftPressed = false;
                    fPressed = false;
                }

                if (shiftPressed && Time.unscaledTime - shiftPressTime > COMBO_WINDOW)
                    shiftPressed = false;

                if (fPressed && Time.unscaledTime - fPressTime > COMBO_WINDOW)
                    fPressed = false;
            }
        }
        else
        {
            backgroundImage.color = modifiableBorderColor;
            ResetFInteraction();
            rightClickHoldDelay = 0f;
        }
    }

    //backgroundImage.color = initialBorderColor;
    //backgroundImage.color = modifiableBorderColor;

    public static void HandleSlotNameSearch(List<InventorySlot> allSlots)
{
    isEscape = isSearchMode || isRealEscape;

    // Подсветка слотов по совпадению с поиском
    foreach (var slot in allSlots)
    {
        if (searchActive && searchInput.Length > 0
            && slot.item != null
            && slot.item.itemName.IndexOf(searchInput, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            slot.isYellow = true;
        }
        else
        {
            slot.isYellow = false;
        }
    }

    // Shift + Enter фиксирует ввод
    if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
        (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
    {
        if (isSearchMode)
        {
            searchInput = "";
            isSearchLocked = false;
            searchActive = false;
        }
        return;
    }

    // Enter без Shift
    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
    {
        if (!isSearchMode)
        {
            isSearchMode = true;
            return;
        }
        else if (!isSearchLocked)
        {
            isSearchLocked = true;
            return;
        }
        else
        {
            isSearchLocked = false; // Разблокировка для продолжения ввода
            return;
        }
    }

    // Если не в режиме поиска — выход
    if (!isSearchMode) return;

    // Escape — сброс всего
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        searchInput = "";
        searchActive = false;
        isSearchMode = false;
        isSearchLocked = false;

        foreach (var slot in allSlots)
            slot.realbackgroundImage.enabled = false;

        return;
    }

    // Если ввод зафиксирован — блокируем изменения
    if (isSearchLocked) return;

    // Backspace
    if (Input.GetKeyDown(KeyCode.Backspace) && searchInput.Length > 0)
    {
        searchInput = searchInput.Substring(0, searchInput.Length - 1);
        searchActive = searchInput.Length > 0;
    }

    // Ввод латинских букв
    for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
    {
        if (Input.GetKeyDown(k))
        {
            char c = k.ToString()[0];
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                c = Char.ToLower(c);

            searchInput += c;
            searchActive = true;
            break;
        }
    }
}

    public static void typeRealEscape(bool input)
    {
        isRealEscape = input;
    }
    public static bool returnEscape()
    {
        return isEscape;
    }
    public static bool getSearchMode()
    {
        return isSearchMode;
    }

    public static bool getSearchLocked()
    {
        return isSearchLocked;
    }

    public static string getSearchInput()
    {
        return searchInput;
    } 

    public static void resetSearchMod()
    {
        isSearchMode = false;
        searchInput = "";
        searchActive = false;
    }

    private void HandleItemCountInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftShift)) || (Input.GetKeyDown(KeyCode.Backspace) && Input.GetKey(KeyCode.LeftShift)))
        {
            if (inputSlot != null && heldItem != null)
            {
                ReturnHeldItem();
            }
            inputNumber = "";
            isIllusion = false;
            return;
        }

        if ((!isCursorInSlot || item == null) && inputNumber == "" && Deleted)
        {
            // Debug.LogWarning($"isCursorInSlot: {isCursorInSlot} item: {item} inputNumber: {inputNumber} Deleted: {Deleted}");
            return;
        }
        // else
        // {
        //     Debug.Log($"isCursorInSlot: {isCursorInSlot} item: {item} inputNumber: {inputNumber} Deleted: {Deleted}");
        // }

        if (inputSlot != null && inputSlot != this)
        {
            return;
        }

        // Обработка Backspace или D
        if ((Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.D)) && inputNumber.Length > 0)
        {
            inputNumber = inputNumber.Substring(0, inputNumber.Length - 1);
            if (inputNumber == "")
            {
                if (heldItem != null)
                {
                    // Если item == null, восстанавливаем его временно из heldItem
                    if (item == null)
                    {
                        item = new Item(
                            heldItem.id,
                            heldItem.itemName,
                            heldItem.description,
                            heldItem.icon,
                            0,
                            heldItem.maxStackSize,
                            heldItem.isModifiable,
                            heldItem.isRecipe,
                            heldItem.recipeUsesLeft
                        );
                        SetItem(item);
                    }

                    item.AddToStack(heldItem.stackSize);
                    heldItem.stackSize = 0;

                    isIllusion = true;
                    SetItem(item);
                    UpdateHeldIcon();
                }
                return;
            }
            if (int.TryParse(inputNumber, out int desiredAmount))
            {
                if (item == null && heldItem != null && heldSlot == this)
                {
                    item = new Item(
                        heldItem.id,
                        heldItem.itemName,
                        heldItem.description,
                        heldItem.icon,
                        0,
                        heldItem.maxStackSize,
                        heldItem.isModifiable,
                        heldItem.isRecipe,
                        heldItem.recipeUsesLeft
                    );
                    SetItem(item);
                }

                int available = (heldItem != null) ? item.stackSize + heldItem.stackSize : item.stackSize;
                int amountToPick = Mathf.Min(desiredAmount, available);

                if (heldItem != null && heldSlot == this && heldItem.id == item.id)
                {
                    int newAmount = Mathf.Min(amountToPick, heldItem.maxStackSize);
                    int diff = newAmount - heldItem.stackSize;

                    if (diff > 0)
                    {
                        item.RemoveFromStack(diff);
                    }
                    else if (diff < 0)
                    {
                        item.AddToStack(-diff);
                    }

                    heldItem.stackSize = newAmount;

                    if (item.stackSize <= 0)
                        ClearSlot();
                    else
                        SetItem(item);

                    UpdateHeldIcon();
                }
            }

            return;
        }
        else if ((Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.D)) && inputNumber.Length == 0)
        {
            if (isIllusion)
            {
                Deleted = true;
                isIllusion = false;
                heldItem = null;
                inputSlot = null;
                Destroy(heldIcon);
                inputNumber = "";
                return;
            }
            else if (heldItem != null && heldItem.stackSize == 1)
            {
                item.AddToStack(1);
                inputSlot.Deleted = true;
                inputSlot = null;
                heldItem = null;
                Destroy(heldIcon);
                SetItem(item);
            }
        }


        // Обработка ввода цифр
        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
        {
            if (item == null)
            {
                return;
            }

            if (Input.GetKeyDown(key))
            {
                if (isIllusion)
                {
                    isIllusion = false;
                    heldItem = null;
                    Destroy(heldIcon);
                }

                if (heldIcon != null)
                {
                    Image img = heldIcon.GetComponent<Image>();
                    if (img != null)
                    {
                        Color color = img.color;
                        color.a = 1f;
                        img.color = color;
                    }
                }

                Deleted = false;
                string digit = key.ToString().Replace("Alpha", "");
                inputNumber += digit;

                if (!int.TryParse(inputNumber, out int desiredAmount))
                {
                    inputNumber = "";
                    return;
                }

                int available = (heldItem != null) ? item.stackSize + heldItem.stackSize : item.stackSize;
                int amountToPick = Mathf.Min(desiredAmount, available);

                if (amountToPick > 0)
                {
                    if (heldItem != null && heldSlot == this && heldItem.id == item.id)
                    {
                        if (inputSlot == null)
                        {
                            inputNumber = "";
                            return;
                        }

                        int newAmount = Mathf.Min(amountToPick, heldItem.maxStackSize);
                        int amountToAdd = newAmount - heldItem.stackSize;

                        if (amountToAdd != 0)
                        {
                            heldItem.stackSize = newAmount;
                            item.RemoveFromStack(amountToAdd);

                            if (item.stackSize <= 0)
                                ClearSlot();
                            else
                                SetItem(item);

                            UpdateHeldIcon();
                        }
                    }
                    else if (heldItem == null)
                    {
                        inputSlot = this;
                        heldItem = new Item(item.id, item.itemName, item.description, item.icon, amountToPick, item.maxStackSize, item.isModifiable, item.isRecipe, item.recipeUsesLeft);
                        heldSlot = this;

                        item.RemoveFromStack(amountToPick);

                        if (item.stackSize <= 0)
                            ClearSlot();
                        else
                            SetItem(item);

                        CreateHeldIcon();
                        UpdateHeldIcon();
                    }
                }
                else
                {
                    inputNumber = "";
                    Deleted = true;
                    inputSlot = null;
                }

                break;
            }
        }
    }

public bool HasItem()
{
    return item != null;
}

public Item GetItem()
{
    return item;
}

    public void SetItem(Item newItem)
    {
        item = newItem;

        if (isCursorInSlot) ShowItemTooltip();

        if (item != null)
        {
            itemImage.sprite = item.icon;
            itemImage.enabled = true;
            stackText.text = item.stackSize > 1 ? item.stackSize.ToString() : "";
            stackText.enabled = item.stackSize > 1;
        }
        else
        {
            Debug.Log(123);
            ClearSlot();
        }
        chestUIController?.UpdateCraftingUI(); // Обновляем UI крафта, если есть
    }

    public void ClearSlot()
    {
        item = null;
        itemImage.sprite = null;
        itemImage.enabled = false;
        stackText.text = "";
        stackText.enabled = false;
        itemInfoManager?.HideItemInfo();
        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
        tooltipManager?.HideTooltip(); // Скрываем тултип при очистке слота
    }
public void OnScroll(PointerEventData eventData)
{
    if (eventData.scrollDelta.y < 0) // скролл вниз
    {
        if (item == null) return;

        if (heldItem == null && item.stackSize > 0)
        {
            heldItem = new Item(item.id, item.itemName, item.description, item.icon, 1, item.maxStackSize, item.isModifiable, item.isRecipe, item.recipeUsesLeft);
            heldSlot = this;
            item.RemoveFromStack(1);

            if (item.stackSize <= 0)
            {
                ClearSlot();
            }
            else
            {
                SetItem(item);
            }

            CreateHeldIcon();
            inputSlot = this;
            inputSlot.inputNumber = heldItem.stackSize.ToString(); // <-- добавлено
            inputSlot.Deleted = false; // <-- добавлено
        }
        else if (heldItem != null && heldItem.id == item.id && item.stackSize > 0 && heldItem.stackSize < heldItem.maxStackSize)
        {
            heldItem.AddToStack(1);
            item.RemoveFromStack(1);

            if (item.stackSize <= 0)
            {
                ClearSlot();
            }
            else
            {
                SetItem(item);
            }

            UpdateHeldIcon();

            inputSlot = this;
            inputSlot.inputNumber = heldItem.stackSize.ToString();
            inputSlot.Deleted = false;
        }
    }
    else if (eventData.scrollDelta.y > 0) // скролл вверх — старое поведение
    {
        if (heldItem != null)
        {
            HandleRightClick();
        }
    }
}

public void OnPointerDown(PointerEventData eventData)
{   
    if (isIllusion) return; // Игнорируем нажатия по иллюзии
    if (eventData.button == PointerEventData.InputButton.Left && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            isCursorInSlot = true;
            hasPickedUp = false;
            isHoldingLeftClick = true;
            leftClickHoldStartTime = Time.unscaledTime;
            hasTriedDropAfterHold = false;

            if (item != null && heldItem == null)
            {
                LKMstart = true;
                heldItem = new Item(item.id, item.itemName, item.description, item.icon, item.stackSize, item.maxStackSize, item.isModifiable, item.isRecipe, item.recipeUsesLeft);
                heldSlot = this;
                ClearSlot();
                CreateHeldIcon();
                hasPickedUp = true;
                itemPickupTime = Time.unscaledTime;
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            isCursorInSlot = true;
            rightClickHoldStartTime = Time.unscaledTime;
        }
}

public void OnPointerUp(PointerEventData eventData)
{
    CanRaztagivanie = true;
    bool creatingRClicked = false;
    if (isIllusion)
    {
        inputSlot.Deleted = true;
        isIllusion = false;
        heldItem = null;
        inputSlot = null;
        inputNumber = "";
        if (heldIcon != null)
        {
            Destroy(heldIcon);
            heldIcon = null;
        }
        return;
    }

    if (eventData.button == PointerEventData.InputButton.Left)
    {
        float currentTime = Time.unscaledTime;
        bool clickedOnOccupiedSlot = item == null;
        isHoldingLeftClick = false;

        if (!hasTriedDropAfterHold)
        {
            float holdDuration = Time.unscaledTime - leftClickHoldStartTime;
            if (holdDuration > HOLD_THRESHOLD)
            {
                if (hoveredSlot != null && !LKMraztagivanie) 
                {
                    hoveredSlot.TryPlaceHeldItem();
                }
                else 
                {
                    // TryPlaceHeldItem();
                }
            }
            else if (currentTime - lastClickTime <= DOUBLE_CLICK_THRESHOLD && heldItem != null && lastClickTime >= itemPickupTime)
            {
                if (heldItem != null && item != null && heldItem.id != item.id) 
                {
                    LKMstart = false;
                    return;
                }
                realHoveredSlot.HandleDoubleClick();
            }
            else if (!hasPickedUp)
            {
                HandleLeftClick();
            }
            lastClickTime = currentTime;
        }
        LKMstart = false;
        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
    }
    else if (eventData.button == PointerEventData.InputButton.Right)
    {
        float holdDuration = Time.unscaledTime - rightClickHoldStartTime;

        if (holdDuration <= RIGHT_HOLD_THRESHOLD && item != null && heldItem == null && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            creatingRClicked = true;
            // Создаём heldItem для быстрого клика (менее 0.2 секунд)
            int amountToPick = Mathf.CeilToInt(item.stackSize / 2f);
            heldItem = new Item(item.id, item.itemName, item.description, item.icon, amountToPick, item.maxStackSize, item.isModifiable, item.isRecipe, item.recipeUsesLeft);
            item.RemoveFromStack(amountToPick);
            heldSlot = this;

            inputSlot = this;
            inputSlot.inputNumber = heldItem.stackSize.ToString(); // Добавлено
            inputSlot.Deleted = false; // Добавлено

            if (item.stackSize <= 0)
            {
                ClearSlot();
            }
            else
            {
                SetItem(item);
            }

            CreateHeldIcon();
        }
       else if (heldItem != null && isCursorInSlot && !rightDragUsedSlots.Contains(this))
        {
            HandleRightClick(); // один предмет при отпускании
            rightDragUsedSlots.Add(this);
        }

        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
    }

    if (!creatingRClicked)
    {
        if (inputSlot != null)
        {
            inputSlot.resetInput();
            inputSlot.Deleted = true;
        }
        inputSlot = null;
    }
}

private void TryPlaceHeldItem()
{
    if (heldItem == null) return;

    if (item == null)
    {
        SetItem(heldItem);
        ClearHeldItem();
    }
    else if (item.id == heldItem.id && item.stackSize < item.maxStackSize)
    {
        int amountToMove = Mathf.Min(heldItem.stackSize, item.maxStackSize - item.stackSize);
        item.AddToStack(amountToMove);
        heldItem.RemoveFromStack(amountToMove);
        SetItem(item);
        UpdateHeldIcon();

        if (heldItem.stackSize <= 0)
        {
            ClearHeldItem();
        }
    }
}


    public void OnPointerEnter(PointerEventData eventData)
    {
        realHoveredSlot = this;
        hoveredSlot = this;
        isCursorInSlot = true;
    if (heldItem != null && Input.GetMouseButton(1) && !rightDragUsedSlots.Contains(this))
    {
        if (item == null || item.id == heldItem.id)
        {
            // если это первый переход с начального слота
            if (isRightDragging && rightDragStartSlot != null && rightDragStartSlot != this)
            {
                if (!rightDragUsedSlots.Contains(rightDragStartSlot))
                {
                    rightDragStartSlot.HandleRightClick();
                    rightDragUsedSlots.Add(rightDragStartSlot);
                }
            }

            HandleRightClick(); // текущий слот
            rightDragUsedSlots.Add(this);
        }
    }

    if (item != null && heldItem == null)
    {
        ShowItemTooltip();
    }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoveredSlot == this)
        {
            hoveredSlot = null;
        }
        if (realHoveredSlot == this)
        {
            realHoveredSlot = null;
        }
        isCursorInSlot = false;
        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
        tooltipManager?.HideTooltip(); // Скрываем тултип при уходе курсора
        if (isRightDragging && rightDragStartSlot == this && !rightDragUsedSlots.Contains(this))
        {
            HandleRightClick();
            rightDragUsedSlots.Add(this);
        }
    }

    private void ShowItemTooltip()
    {
        if (item != null && tooltipManager != null)
        {
            string content = $"Предмет: {item.itemName}\n" +
                           $"Описание: {item.description}\n" +
                           $"Количество: {item.stackSize}/{item.maxStackSize}" +
                            (item.isRecipe ? $"\nИспользований рецепта: {item.recipeUsesLeft}" : "") +
                           (item.isModifiable ? "\nМодифицируемый: Да" : "");
            tooltipManager.ShowTooltip(content, Input.mousePosition);
        }
    }

    private void HandleLeftClick()
    {
        if (heldItem == null && item == null)
        {
            return;
        }

        if (heldItem == null)
        {
            heldItem = new Item(item.id, item.itemName, item.description, item.icon, item.stackSize, item.maxStackSize, item.isModifiable, item.isRecipe, item.recipeUsesLeft);
            heldSlot = this;
            ClearSlot();
            CreateHeldIcon();
        }
        else if (item == null)
        {
            SetItem(heldItem);
            ClearHeldItem();
        }
        else if (item.id == heldItem.id && item.stackSize < item.maxStackSize)
        {
            int amountToMove = Mathf.Min(heldItem.stackSize, item.maxStackSize - item.stackSize);
            item.AddToStack(amountToMove);
            heldItem.RemoveFromStack(amountToMove);
            SetItem(item);
            UpdateHeldIcon();

            if (heldItem.stackSize <= 0)
            {
                ClearHeldItem();
            }
        }
        else
        {
            if (heldItem.stackSize == 0 || !canSwap) return;
            Item temp = item;
            SetItem(heldItem);
            heldItem = temp;
            UpdateHeldIcon();
        }
    }

    private void HandleRightClick()
    {
        if (heldItem == null && item == null)
        {
            return;
        }

        if (item == null)
        {
            Item newItem = new Item(heldItem.id, heldItem.itemName, heldItem.description, heldItem.icon, 1, heldItem.maxStackSize, heldItem.isModifiable, heldItem.isRecipe, heldItem.recipeUsesLeft);
            SetItem(newItem);
            heldItem.RemoveFromStack(1);
            UpdateHeldIcon();
            if (heldItem.stackSize <= 0)
            {
                ClearHeldItem();
            }
        }
        else if (item.id == heldItem.id && item.CanAddToStack(1))
        {
            item.AddToStack(1);
            heldItem.RemoveFromStack(1);
            SetItem(item);
            UpdateHeldIcon();
            if (heldItem.stackSize <= 0)
            {
                ClearHeldItem();
            }
        }
    }

    private void HandleDoubleClick()
    {
        if (heldItem == null)
            return;
        var panels = InventoryPanelsManager.Instance.GetPanelsForDoubleClick();
        foreach (var panel in panels)
        {
            int remainingToFill = heldItem.maxStackSize - heldItem.stackSize;
            Debug.Log($"Double click on {item.itemName}. Remaining to fill: {remainingToFill}");
            if (remainingToFill <= 0)
                continue;

            var slotsWithSameItem = panel.slots
                .Where(slot => slot.item != null && slot.item.id == heldItem.id)
                .OrderBy(slot => slot.item.stackSize)
                .ToList();

            foreach (var slot in slotsWithSameItem)
            {
                if (remainingToFill <= 0)
                    break;

                int amountToMove = Mathf.Min(slot.item.stackSize, remainingToFill);

                if (amountToMove > 0)
                {
                    heldItem.AddToStack(amountToMove);
                    slot.item.RemoveFromStack(amountToMove);
                    remainingToFill -= amountToMove;

                    if (slot.item.stackSize <= 0)
                        slot.ClearSlot();
                    else
                        slot.SetItem(slot.item);
                }
            }
        }

        UpdateHeldIcon();
    }

    public void TryMoveToOtherPanel()
    {
        if (item == null) return;

        var panels = InventoryPanelsManager.Instance.GetPanelsForDoubleClick()
            .Where(p => p != this.inventoryPanel)
            .ToList();

        if (panels.Count == 0) return;

        foreach (var panel in panels)
        {
            // Сначала добавляем в существующие стеки
            foreach (var targetSlot in panel.slots)
            {
                if (item == null) break;

                if (targetSlot.item != null &&
                    targetSlot.item.id == item.id &&
                    targetSlot.item.stackSize < targetSlot.item.maxStackSize)
                {
                    int canMove = Mathf.Min(item.stackSize, targetSlot.item.maxStackSize - targetSlot.item.stackSize);
                    if (canMove > 0)
                    {
                        targetSlot.item.AddToStack(canMove);
                        item.RemoveFromStack(canMove);
                        targetSlot.SetItem(targetSlot.item);

                        if (item.stackSize <= 0)
                        {
                            ClearSlot();
                            return; // Все предметы перемещены
                        }
                        else
                        {
                            SetItem(item);
                        }
                    }
                }
            }

            // Затем кладём остатки в пустые слоты
            foreach (var targetSlot in panel.slots)
            {
                if (item == null) break;

                if (targetSlot.item == null)
                {
                    targetSlot.SetItem(item);
                    ClearSlot();
                    return; // все предметы перемещены
                }
            }
        }
    }

    private void CreateHeldIcon()
    {
        if (heldIcon != null)
        {
            Destroy(heldIcon);
        }

        heldIcon = new GameObject("HeldIcon");
        heldIcon.transform.SetParent(canvas.transform, false);
        heldIcon.transform.SetAsLastSibling();

        Image dragImage = heldIcon.AddComponent<Image>();
        dragImage.sprite = heldItem.icon;
        dragImage.rectTransform.sizeDelta = itemImage.rectTransform.sizeDelta;
        dragImage.color = new Color(1f, 1f, 1f, 1f);
        dragImage.raycastTarget = false;

        GameObject textObject = new GameObject("HeldText");
        textObject.transform.SetParent(heldIcon.transform, false);
        heldStackText = textObject.AddComponent<TextMeshProUGUI>();
        heldStackText.text = heldItem.stackSize > 1 ? heldItem.stackSize.ToString() : "";
        heldStackText.font = stackText.font;
        heldStackText.fontSize = stackText.fontSize;
        heldStackText.alignment = stackText.alignment;
        heldStackText.color = stackText.color;
        heldStackText.rectTransform.sizeDelta = stackText.rectTransform.sizeDelta;
        heldStackText.rectTransform.anchoredPosition = stackText.rectTransform.anchoredPosition;
        heldStackText.fontStyle = stackText.fontStyle;
        heldStackText.margin = stackText.margin;
    }

    private void UpdateHeldIcon()
    {
        if (heldIcon == null || heldStackText == null) return;

        heldStackText.text = heldItem.stackSize > 1 ? heldItem.stackSize.ToString() : "";

        Image dragImage = heldIcon.GetComponent<Image>();
        if (dragImage != null)
        {
            dragImage.sprite = heldItem?.icon;
    
            // Если количество предметов равно 0, делаем иконку полностью прозрачной
            if (heldItem.stackSize == 0)
            {
                Color color = dragImage.color;
                if (isIllusion)
                {
                    color.a = 0.5f; // Возвращаем прозрачность в 100%, если количество предметов больше 0
                }
                else
                {
                    color.a = 0f; // Устанавливаем прозрачность в 0%
                }
                dragImage.color = color;
            }
            else
            {
                Color color = dragImage.color;
                color.a = 1f; // Возвращаем прозрачность в 100%, если количество предметов больше 0
                dragImage.color = color;
            }
        }
    }

    private void ClearHeldItem()
    {
        if (heldIcon != null)
        {
            Destroy(heldIcon);
            heldIcon = null;
            heldStackText = null;
        }
        heldItem = null;
        heldSlot = null;
        hoveredSlot = null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public static void ReturnHeldItem()
    {
        if (inputSlot != null)
        {
            inputSlot.inputNumber = "";
            inputSlot.Deleted = true;
        }

        inputSlot = null;
        if (heldItem != null && heldSlot != null)
        {
            bool itemReturned = false;

            // Сначала попробуем вернуть в оригинальный слот, если он пустой или предмет тот же
            if (heldSlot.item == null)
            {
                heldSlot.SetItem(heldItem);
                itemReturned = true;
            }
            else if (heldSlot.item.id == heldItem.id && heldSlot.item.CanAddToStack(heldItem.stackSize))
            {
                heldSlot.item.AddToStack(heldItem.stackSize);
                heldSlot.SetItem(heldSlot.item);
                itemReturned = true;
            }

            // Если не удалось вернуть в оригинальный слот — ищем подходящие стаки
            if (!itemReturned && heldSlot.inventoryPanel != null)
            {
                foreach (var slot in heldSlot.inventoryPanel.slots)
                {
                    if (slot.item != null && slot.item.id == heldItem.id && slot.item.CanAddToStack(heldItem.stackSize))
                    {
                        slot.item.AddToStack(heldItem.stackSize);
                        slot.SetItem(slot.item);
                        itemReturned = true;
                        break;
                    }
                }
            }

            // Если не удалось найти подходящие стаки — ищем пустой слот
            if (!itemReturned && heldSlot.inventoryPanel != null)
            {
                foreach (var slot in heldSlot.inventoryPanel.slots)
                {
                    if (slot.item == null)
                    {
                        slot.SetItem(heldItem);
                        itemReturned = true;
                        break;
                    }
                }
            }

            if (!itemReturned)
            {
                Debug.LogWarning("Не удалось вернуть предмет обратно в слот, нет подходящих стеков и пустых слотов.");
            }

            // Очистка "в руках"
            heldItem = null;
            hoveredSlot = null;
            heldSlot = null;

            if (heldIcon != null)
            {
                GameObject.Destroy(heldIcon);
                heldIcon = null;
                heldStackText = null;
            }
        }
    }

    public void resetCursorInSlot()
    {
        isCursorInSlot = false;
    }

    public void resetInput()
    {
        inputNumber = "";
    }

    private void ResetFInteraction()
    {
        fHoldTime = 0f;
        progressBorderImage.enabled = false;
        progressBorderImage.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (heldIcon != null && heldSlot == this)
        {
            Destroy(heldIcon);
            heldIcon = null;
            heldStackText = null;
        }
    }
}