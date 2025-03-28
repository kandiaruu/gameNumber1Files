using UnityEngine;
public interface ITooltipManager
{
    void ShowTooltip(Skill skill, Vector3 mousePosition);
    void HideTooltip();
    void UpdatePosition(Vector3 mousePosition);
    bool IsOpen { get; }
}