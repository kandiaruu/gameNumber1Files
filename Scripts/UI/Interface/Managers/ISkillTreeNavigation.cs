public interface ISkillTreeNavigation
{
    bool isDragging { get; }
    void ResetNavigation();
    void SavePanelState(SkillPanelSwitcher.SkillPanelState state); // Новый метод
    void LoadPanelState(SkillPanelSwitcher.SkillPanelState state); // Новый метод
    void SetCurrentGroup(string groupName); // Уже добавлен ранее
    string CurrentGroupName { get; } // Добавляем свойство
}