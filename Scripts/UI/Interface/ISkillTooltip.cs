using UnityEngine;

public interface ISkillTooltip
{
    void ShowTooltip(Skill skill, Vector3 mousePosition);
    void UpdatePosition(Vector3 mousePosition);
    bool IsOpen { get; }
}