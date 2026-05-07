using UnityEngine;
using System.Collections.Generic;

public interface ISkillEquipManager
{
    Skill[] EquippedActives { get; }
    Skill[] EquippedPassives { get; }
    KeyCode[] ActiveSkillKeys { get; } 
    
    void EquipSkill(Skill skill, int slotIndex, SkillCategory category);
    void UnequipSkill(int slotIndex, SkillCategory category);
    bool IsSkillEquipped(Skill skill);
    void SetKey(int slotIndex, KeyCode newKey); // <--- ДОБАВЛЕНО
}

public class SkillEquipManager : MonoBehaviour, ISkillEquipManager
{
    [InjectAttribute1] private ISkillTreeManager skillTreeManager { get; set; }

    public Skill[] EquippedActives { get; private set; } = new Skill[6];
    public Skill[] EquippedPassives { get; private set; } = new Skill[6];

    public KeyCode[] ActiveSkillKeys { get; private set; } = new KeyCode[6];

    // Кнопки по умолчанию, если игрок зашел в первый раз
    private KeyCode[] defaultKeys = { KeyCode.F, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.Q, KeyCode.E };

    private void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
        LoadKeys(); // Загружаем сохраненные кнопки при старте
    }

    private void Start()
    {
        if (skillTreeManager != null) skillTreeManager.OnSkillsUpdated += ValidateEquippedSkills;
    }

    private void OnDestroy()
    {
        if (skillTreeManager != null) skillTreeManager.OnSkillsUpdated -= ValidateEquippedSkills;
    }

    private void ValidateEquippedSkills()
    {
        for (int i = 0; i < 6; i++)
        {
            if (EquippedActives[i] != null && !EquippedActives[i].isUnlocked) EquippedActives[i] = null;
            if (EquippedPassives[i] != null && !EquippedPassives[i].isUnlocked) EquippedPassives[i] = null;
        }
    }

    // <--- СОХРАНЕНИЕ И ЗАГРУЗКА КНОПОК --->
    private void LoadKeys()
    {
        for (int i = 0; i < 6; i++)
        {
            string savedKey = PlayerPrefs.GetString("SkillKey_" + i, defaultKeys[i].ToString());
            if (System.Enum.TryParse(savedKey, out KeyCode parsedKey))
            {
                ActiveSkillKeys[i] = parsedKey;
            }
            else
            {
                ActiveSkillKeys[i] = defaultKeys[i];
            }
        }
    }

    public void SetKey(int slotIndex, KeyCode newKey)
    {
        ActiveSkillKeys[slotIndex] = newKey;
        PlayerPrefs.SetString("SkillKey_" + slotIndex, newKey.ToString());
        PlayerPrefs.Save();
    }

    public void EquipSkill(Skill skill, int slotIndex, SkillCategory category)
    {
        if (skill == null) return;
        for (int i = 0; i < 6; i++)
        {
            if (category == SkillCategory.Active && EquippedActives[i] == skill) EquippedActives[i] = null;
            if (category == SkillCategory.Passive && EquippedPassives[i] == skill) EquippedPassives[i] = null;
        }
        if (category == SkillCategory.Active) EquippedActives[slotIndex] = skill;
        else if (category == SkillCategory.Passive) EquippedPassives[slotIndex] = skill;
    }

    public void UnequipSkill(int slotIndex, SkillCategory category)
    {
        if (category == SkillCategory.Active) EquippedActives[slotIndex] = null;
        else if (category == SkillCategory.Passive) EquippedPassives[slotIndex] = null;
    }

    public bool IsSkillEquipped(Skill skill)
    {
        foreach (var s in EquippedActives) if (s == skill) return true;
        foreach (var s in EquippedPassives) if (s == skill) return true;
        return false;
    }
}