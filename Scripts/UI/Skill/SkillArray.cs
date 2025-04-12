using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class SkillGroup
{
    public string groupName;
    public Skill[] skills;
    public GameObject lineParentObject;
    public void Initialize()
    {
        foreach (Skill skill in skills)
        {
            skill.groupName = groupName;
            skill.Initialize();
        }

        foreach (Skill skill in skills)
        {
            skill.SetupDependencyLines(skills, lineParentObject);
        }
    }

    public void UpdateUI(bool canAfford)
    {
        foreach (Skill skill in skills)
        {
            skill.UpdateUI(canAfford, skills);
        }
    }
}