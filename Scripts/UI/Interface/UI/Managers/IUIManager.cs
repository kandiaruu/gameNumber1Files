using UnityEngine;
using System.Collections.Generic;
public interface IUIManager
{
    GameObject GetPanel(UIManager.PanelType panelType);
    List<UIManager.PanelConfig> GetPanelConfigs();
    UIManager.PanelConfig FindActivePanelConfig();
    bool ShouldAllowNavigation();
    bool ShouldAllowSkillButtonInteraction();
    void OpenPanel(UIManager.PanelType panelType);
    void CloseCurrentPanel();
    void CloseAllChildren(UIManager.PanelType parentPanelType);
}
