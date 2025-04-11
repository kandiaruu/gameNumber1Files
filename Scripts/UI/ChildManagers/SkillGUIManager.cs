using UnityEngine;

public class SkillGuiManager : MonoBehaviour, ISkillGuiManager
{
    [InjectAttribute1]
    private ISkillTreeManager skillTreeManager { get; set; }
    [InjectAttribute1]
    private ISkillGuiPanel skillGuiPanel { get; set; }
    [InjectAttribute1]
    private IUIManager uiManager { get; set; }

    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
        InitializeSkillGuiPanel();
    }

    private void InitializeSkillGuiPanel()
    {
        GameObject skillGuiPanelObject = uiManager.GetPanel(UIManager.PanelType.SkillGui);
        if (skillGuiPanelObject != null)
        {
            skillGuiPanel = skillGuiPanelObject.GetComponent<SkillGuiPanel>();
            if (skillGuiPanel == null)
            {
                Debug.LogError($"Панель {skillGuiPanelObject.name} (SkillGui) не реализует ISkillGuiPanel!");
            }
        }
        else
        {
            Debug.LogError("Панель типа SkillGui не найдена в кэше UIManager!");
        }
    }

    public void ShowSkillGui(Skill skill)
    {
        if (skillGuiPanel != null)
        {
            skillGuiPanel.ShowSkillGui(skill);
        }
        else
        {
            Debug.LogWarning("SkillGuiPanel не инициализирован!");
        }
    }

    public bool IsSkillGuiActive()
    {
        return skillGuiPanel != null && skillGuiPanel.IsOpen;
    }

    public void CloseSkillGui()
    {
        if (skillGuiPanel != null && skillGuiPanel.IsOpen)
        {
            skillGuiPanel.Close();
        }
    }
}