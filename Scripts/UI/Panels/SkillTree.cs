using UnityEngine;

public class SkillTree : BasePanel, ISkillTree
{
    [InjectAttribute1] private ISkillPanelManager skillPanelManager { get; set; }
    public override void Awake()
    {
        DependencyContainer1.InjectDependencies(this);
    }
    public override void Close()
    {
        base.Close(); // Вызываем базовый метод, чтобы панель стала активной
        skillPanelManager.SetInitialPanelState(); // Устанавливаем Normal как начальную панель
    }
}
