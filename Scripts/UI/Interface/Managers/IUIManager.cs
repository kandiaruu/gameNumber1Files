using UnityEngine;
using System.Collections.Generic;
public interface IUIManager
{
    GameObject GetPanel(UIManager.PanelType panelType);
    List<UIManager.PanelConfig> GetPanelConfigs();
    UIManager.PanelConfig FindActivePanelConfig();
    bool ShouldAllowNavigation(); // Новый метод
    bool ShouldAllowSkillButtonInteraction(); // Новый метод
}
