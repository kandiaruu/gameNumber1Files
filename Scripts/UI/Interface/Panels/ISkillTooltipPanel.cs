using UnityEngine;
public interface ISkillTooltipPanel : IPanel
{
    void ShowTooltip(Skill skill, Vector3 mousePosition);
    void ShowTooltip(string content, Vector3 mousePosition);
    void UpdatePosition(Vector3 mousePosition);
}