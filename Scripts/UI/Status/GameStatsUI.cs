using UnityEngine;
using TMPro;

public class GameStatsUI : MonoBehaviour
{
    [InjectAttribute1] public IPlayerStats PlayerStats { get; set; }

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    public void Update()
    {
        if (PlayerStats == null) return;

        hpText.text = $"HP: {PlayerStats.CurrentHP}/{PlayerStats.MaxHP}"; // hpText.text = $"HP: {PlayerStats.CurrentHP:F1}/{PlayerStats.MaxHP:F1}";
        mpText.text = $"MP: {PlayerStats.CurrentMP}/{PlayerStats.MaxMP}";
    }
}