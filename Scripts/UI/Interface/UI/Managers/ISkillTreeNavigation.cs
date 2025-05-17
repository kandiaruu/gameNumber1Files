using UnityEngine;
public interface ISkillTreeNavigation
{
    bool isDragging { get; }
    void ResetNavigation();
    void CenterOnSkill(Skill skill);
    public Skill getLastSkill();
    void inputLastSkill(Skill skill);
}