using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // <--- Обязательная библиотека для TextMeshPro

public class SkillSelectPanel : BasePanel
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }

    [Header("Пул навыков (Откуда берем)")]
    public Transform activePoolContainer;
    public Transform passivePoolContainer;
    public GameObject poolItemPrefab;

    [Header("Ячейки экипировки (Куда ставим)")]
    public Button[] activeSlots;  
    public Button[] passiveSlots; 

    private Skill selectedSkillToEquip; 

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);
    }

    private void OnEnable()
    {
        if (equipManager != null && skillTreeManager != null)
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (equipManager == null || skillTreeManager == null) return;

        selectedSkillToEquip = null;
        ClearContainer(activePoolContainer);
        ClearContainer(passivePoolContainer);

        // 1. Спавним навыки в пулы
        var allSkills = skillTreeManager.GetAllSkills();
        if (allSkills != null)
        {
            foreach (var skill in allSkills)
            {
                if (skill.isUnlocked && !equipManager.IsSkillEquipped(skill))
                {
                    Transform container = skill.category == SkillCategory.Active ? activePoolContainer : passivePoolContainer;
                    
                    if (container != null && poolItemPrefab != null)
                    {
                        GameObject itemObj = Instantiate(poolItemPrefab, container);
                        Image icon = itemObj.GetComponent<Image>();
                        if (skill.skillIcon != null && icon != null) icon.sprite = skill.skillIcon;

                        Button btn = itemObj.GetComponent<Button>();
                        if (btn != null) btn.onClick.AddListener(() => OnPoolSkillClicked(skill));
                    }
                }
            }
        }

        // 2. Обновляем иконки и текст в слотах
        for (int i = 0; i < 6; i++)
        {
            if (i < activeSlots.Length && activeSlots[i] != null)
                UpdateSlotVisual(activeSlots[i], equipManager.EquippedActives[i], SkillCategory.Active, i);

            if (i < passiveSlots.Length && passiveSlots[i] != null)
                UpdateSlotVisual(passiveSlots[i], equipManager.EquippedPassives[i], SkillCategory.Passive, i);
        }
    }

    private void OnPoolSkillClicked(Skill skill)
    {
        selectedSkillToEquip = skill;
        Debug.Log("Выбран навык для экипировки: " + skill.skillName + ". Теперь кликните на ячейку!");
    }

    private void OnSlotClicked(int slotIndex, SkillCategory category)
    {
        if (selectedSkillToEquip != null && selectedSkillToEquip.category == category)
        {
            equipManager.EquipSkill(selectedSkillToEquip, slotIndex, category);
            selectedSkillToEquip = null;
            RefreshUI(); 
        }
        else
        {
            equipManager.UnequipSkill(slotIndex, category);
            RefreshUI();
        }
    }

    private void UpdateSlotVisual(Button slotBtn, Skill equippedSkill, SkillCategory category, int slotIndex)
    {
        if (slotBtn == null) return;
        
        // 1. Ищем иконку
        Transform iconTransform = slotBtn.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image icon = iconTransform.GetComponent<Image>();
            if (icon != null)
            {
                if (equippedSkill != null)
                {
                    icon.sprite = equippedSkill.skillIcon;
                    icon.enabled = true;
                }
                else
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
        }

        // 2. Ищем текст для отображения клавиши (TextMeshPro)
        Transform keyTextTransform = slotBtn.transform.Find("KeyText");
        if (keyTextTransform != null)
        {
            // Берем компонент TextMeshProUGUI
            TextMeshProUGUI keyText = keyTextTransform.GetComponent<TextMeshProUGUI>();
            if (keyText != null)
            {
                if (category == SkillCategory.Active)
                {
                    // Вписываем клавишу из менеджера экипировки (F, Z, X...)
                    keyText.text = equipManager.ActiveSkillKeys[slotIndex].ToString();
                }
                else
                {
                    keyText.text = ""; // У пассивок нет клавиш
                }
            }
        }

        slotBtn.onClick.RemoveAllListeners();
        slotBtn.onClick.AddListener(() => OnSlotClicked(slotIndex, category));
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
    }
}