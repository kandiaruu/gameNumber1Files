using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootManager3 : MonoBehaviour, ILootManager3
{
    private int goblinKills = 0;
    private readonly List<InvItemDatabase3> pendingLoot = new();

    public int PendingGoblinKills() => goblinKills;

    public void AddGoblinKill()
    {
        goblinKills++;

        // фиксированный лут за одного гоблина
        AddLoot(0, 24);
        AddLoot(1, 4);
        AddLoot(2, 10);
    }

    private void AddLoot(int itemId, int count)
    {
        long now = DateTime.UtcNow.Ticks;

        var existing = pendingLoot.FirstOrDefault(i => i.itemId == itemId);
        if (existing != null)
        {
            existing.stackSize += count;
            existing.lastAddedTicks = now;
            if (existing.firstAddedTicks == 0)
                existing.firstAddedTicks = now;
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
        return pendingLoot
            .Select(i => new InvItemDatabase3
            {
                itemId = i.itemId,
                stackSize = i.stackSize,
                firstAddedTicks = i.firstAddedTicks,
                lastAddedTicks = i.lastAddedTicks
            })
            .ToList();
    }

    public List<InvItemDatabase3> ConsumePendingLoot()
    {
        var result = GetPendingLoot();
        pendingLoot.Clear();
        goblinKills = 0;
        return result;
    }
}