public interface ISkillTreeNavigation
{
    bool isDragging { get; }
    void ResetNavigation();
    void SetCurrentGroup(string groupName); // Уже добавлен ранее
    string CurrentGroupName { get; } // Добавляем свойство
}