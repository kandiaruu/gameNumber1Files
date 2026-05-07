using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootManager3 : MonoBehaviour, ILootManager3
{
    private int goblinKills = 0;
    private int chestOpens = 0; // <--- ДОБАВЛЕНО
    private readonly List<InvItemDatabase3> pendingLoot = new();

    public int PendingGoblinKills() => goblinKills;
    public int PendingChestOpens() => chestOpens; // <--- ДОБАВЛЕНО

    public void AddGoblinKill()
    {
        goblinKills++;
        AddLoot(0, 24);
        AddLoot(1, 4);
        AddLoot(2, 10);
    }

    // <--- ДОБАВЛЕНО: Логика сундуков с рандомом --->
    public void AddChestOpen()
    {
        chestOpens++;

        // Предмет ID 1: Шанс 20% (0.2f), количество от 1 до 10
        if (UnityEngine.Random.value <= 0.20f)
        {
            int randomAmount = UnityEngine.Random.Range(1, 11); // Максимум не включителен, поэтому 11
            AddLoot(1, randomAmount);
        }

        // Предмет ID 2: Шанс 50% (0.5f), количество 1
        if (UnityEngine.Random.value <= 0.50f)
        {
            AddLoot(2, 1);
        }

        // Предмет ID 3: Шанс 33% (0.33f), количество 1
        if (UnityEngine.Random.value <= 0.33f)
        {
            AddLoot(3, 1);
        }
    }

    private void AddLoot(int itemId, int count)
    {
        long now = DateTime.UtcNow.Ticks;
        var existing = pendingLoot.FirstOrDefault(i => i.itemId == itemId);
        
        if (existing != null)
        {
            existing.stackSize += count;
            existing.lastAddedTicks = now;
            if (existing.firstAddedTicks == 0) existing.firstAddedTicks = now;
        }
        else
        {
            pendingLoot.Add(new InvItemDatabase3
            {
                itemId = itemId,
                stackSize = count,
                firstAddedTicks = now,
                lastAddedTicks = now
            });
        }
    }

    public List<InvItemDatabase3> GetPendingLoot()
    {
        return pendingLoot.Select(i => new InvItemDatabase3
        {
            itemId = i.itemId,
            stackSize = i.stackSize,
            firstAddedTicks = i.firstAddedTicks,
            lastAddedTicks = i.lastAddedTicks
        }).ToList();
    }

    public List<InvItemDatabase3> ConsumePendingLoot()
    {
        var result = GetPendingLoot();
        pendingLoot.Clear();
        goblinKills = 0;
        chestOpens = 0; // Сбрасываем сундуки тоже
        return result;
    }
}