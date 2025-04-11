using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillGuiPanel : BasePanel, ISkillGuiPanel
{
    [SerializeField] private TextMeshProUGUI skillInfoText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    [InjectAttribute1]
    private ISkillTreeManager skillTreeManager { get; set; }

    private Skill currentSkill;

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (skillInfoText == null) Debug.LogError("skillInfoText не назначен!");
        if (closeButton == null) Debug.LogError("closeButton не назначен!");
        if (resetButton == null) Debug.LogError("resetButton не назначен!");

        closeButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetSkill);
    }

    public void ShowSkillGui(Skill skill)
    {
        Debug.Log($"ShowSkillGui вызван для {skill.skillName}");
        currentSkill = skill;

        Open();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentSkill == null) return;

        skillInfoText.text = $"Навык: {currentSkill.skillName}";
        resetButton.gameObject.SetActive(currentSkill.isUnlocked && currentSkill.canBeReset); // Условие для кнопки
    }

    private void ResetSkill()
    {
        if (currentSkill != null && skillTreeManager != null)
        {
            skillTreeManager.ResetSkill(currentSkill.groupName, currentSkill.skillIndex);
            Close();
        }
        else
        {
            Debug.LogError("SkillTreeManager или currentSkill не инициализированы!");
        }
    }

    protected override void OnClose()
    {
        currentSkill = null;
        skillInfoText.text = "";
    }
}