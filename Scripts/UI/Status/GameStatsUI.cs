using UnityEngine;
using TMPro;

//
// Displays the player's current and maximum HP and MP in the HUD
// by reading from IPlayerStats every frame.
//

public class GameStatsUI : MonoBehaviour
{
    [InjectAttribute1] public IPlayerStats PlayerStats { get; set; }

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    // Injects the IPlayerStats dependency
    void Start()
    {
        DependencyContainer1.InjectDependencies(this);
    }

    // Updates the HP and MP text labels with the latest values from PlayerStats
    public void Update()
    {
        if (PlayerStats == null) return;

        hpText.text = $"HP: {PlayerStats.CurrentHP}/{PlayerStats.MaxHP}";
        mpText.text = $"MP: {PlayerStats.CurrentMP}/{PlayerStats.MaxMP}";
    }
}
