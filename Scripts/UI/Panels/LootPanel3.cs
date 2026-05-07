using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootPanel3 : BasePanel, IPanel
{
    [InjectAttribute1] private ILootManager3 lootManager3 { get; set; }
    [InjectAttribute1] private IInventoryPanel3 playerInventoryPanel { get; set; }
    [InjectAttribute1] private ILootInventoryPanel3 lootInventoryPanel { get; set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI killedText;
    [SerializeField] private Button confirmButton;

    public override void Awake()
    {
        base.Awake();
        DependencyContainer1.InjectDependencies(this);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
    }

    public override void Open()
    {
        int kills = lootManager3.PendingGoblinKills();
        int chests = lootManager3.PendingChestOpens();

        // Если лута нет вообще
        if (kills <= 0 && chests <= 0) return;

        base.Open();

        if (killedText != null)
        {
            // Формируем текст в зависимости от того, что было собрано
            if (chests > 0 && kills == 0)
            {
                killedText.text = $"You opened {chests} chests. Lets see what have you got";
            }
            else if (kills > 0 && chests == 0)
            {
                killedText.text = $"You have killed: {kills} goblins";
            }
            else
            {
                // Если игрок собрал и сундуки, и гоблинов одновременно
                killedText.text = $"You have killed: {kills} goblins, You opened: {chests} chests. Lets see what have you got";
            }
        }

        if (lootInventoryPanel != null)
            lootInventoryPanel.SetItemsRaw(lootManager3.GetPendingLoot());
    }

    private void Confirm()
    {
        List<InvItemDatabase3> loot = lootManager3.ConsumePendingLoot();

        if (playerInventoryPanel != null)
            playerInventoryPanel.AddItemsFromList(loot);

        Close();
    }
}