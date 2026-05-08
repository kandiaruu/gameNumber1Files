//
// Panel for equipping and unequipping skills. Displays pools of unlocked active
// and passive skills, and lets the player assign them to equipment slots.
//

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SkillSelectPanel : BasePanel
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1] private ISkillEquipManager equipManager { get; set; }

    [Header("Skill pool (source)")]
    public Transform activePoolContainer;
    public Transform passivePoolContainer;
    public GameObject poolItemPrefab;

    [Header("Equipment slots (destination)")]
    public Button[] activeSlots;
    public Button[] passiveSlots;

    private Skill selectedSkillToEquip;

    // Injects dependencies on awake
    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);
    }

    // Refreshes the UI whenever the panel becomes active
    private void OnEnable()
    {
        if (equipManager != null && skillTreeManager != null)
        {
            RefreshUI();
        }
    }

    // Clears and repopulates skill pools and updates all slot visuals
    public void RefreshUI()
    {
        if (equipManager == null || skillTreeManager == null) return;

        selectedSkillToEquip = null;
        ClearContainer(activePoolContainer);
        ClearContainer(passivePoolContainer);

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

        for (int i = 0; i < 6; i++)
        {
            if (i < activeSlots.Length && activeSlots[i] != null)
                UpdateSlotVisual(activeSlots[i], equipManager.EquippedActives[i], SkillCategory.Active, i);

            if (i < passiveSlots.Length && passiveSlots[i] != null)
                UpdateSlotVisual(passiveSlots[i], equipManager.EquippedPassives[i], SkillCategory.Passive, i);
        }
    }

    // Stores the clicked skill as the one pending placement into a slot
    private void OnPoolSkillClicked(Skill skill)
    {
        selectedSkillToEquip = skill;
        Debug.Log("Skill selected for equip: " + skill.skillName + ". Now click a slot!");
    }

    // Equips the pending skill into the clicked slot, or unequips whatever is already there
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

    // Updates a single slot button to show the equipped skill icon and bound key text
    private void UpdateSlotVisual(Button slotBtn, Skill equippedSkill, SkillCategory category, int slotIndex)
    {
        if (slotBtn == null) return;

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

        Transform keyTextTransform = slotBtn.transform.Find("KeyText");
        if (keyTextTransform != null)
        {
            TextMeshProUGUI keyText = keyTextTransform.GetComponent<TextMeshProUGUI>();
            if (keyText != null)
            {
                if (category == SkillCategory.Active)
                {
                    keyText.text = equipManager.ActiveSkillKeys[slotIndex].ToString();
                }
                else
                {
                    keyText.text = "";
                }
            }
        }

        slotBtn.onClick.RemoveAllListeners();
        slotBtn.onClick.AddListener(() => OnSlotClicked(slotIndex, category));
    }

    // Destroys all child objects of the given container
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
    }
}
