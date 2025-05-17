public interface ISkillPanelUI : IPanel
{
    void UpdateSkillsButtonText(); // Метод для обновления текста кнопки
    void UnlockPanel(string panelName);
    void ClearPanelButtons(); // Метод для очистки старых кнопок
    void CreatePanelButtons(); // Метод для пересоздания кнопок
    void TogglePanelSelection(); // Метод для переключения панели выбора
}