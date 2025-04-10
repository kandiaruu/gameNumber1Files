public interface ISkillPanelUI : IPanel
{
    void UpdateSkillsButtonText(); // Метод для обновления текста кнопки
    void UnlockPanel(string panelName);
}