//
// Represents a group of skills and handles their initialization and UI updates
//

using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class SkillGroup
{
    public string groupName;
    public Skill[] skills;
    public GameObject lineParentObject;

    //
    // Initializes all skills in the group and sets up dependency lines
    //
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

    //
    // Updates UI for all skills in the group based on affordability
    //
    public void UpdateUI(bool canAfford)
    {
        foreach (Skill skill in skills)
        {
            skill.UpdateUI(canAfford, skills);
        }
    }
}
