//
// Tracks pending loot from goblin kills and chest openings. Each kill adds a fixed
// set of items; each chest open adds items according to per-item random drop chances
// and quantity ranges. Loot accumulates in an internal list until consumed, at which
// point the list and kill/open counters are reset.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootManager3 : MonoBehaviour, ILootManager3
{
    private int goblinKills = 0;
    private int chestOpens = 0;
    private readonly List<InvItemDatabase3> pendingLoot = new();

    // Returns the number of goblin kills that have not yet been consumed
    public int PendingGoblinKills() => goblinKills;

    // Returns the number of chest opens that have not yet been consumed
    public int PendingChestOpens() => chestOpens;

    // Records a goblin kill and queues a fixed loot reward into the pending list
    public void AddGoblinKill()
    {
        goblinKills++;
        AddLoot(0, 24);
        AddLoot(1, 4);
        AddLoot(2, 10);
    }

    // Records a chest open and queues randomly determined loot drops based on per-item chances and quantity ranges
    public void AddChestOpen()
    {
        chestOpens++;

        if (UnityEngine.Random.value <= 0.20f)
        {
            int randomAmount = UnityEngine.Random.Range(1, 11);
            AddLoot(1, randomAmount);
        }

        if (UnityEngine.Random.value <= 0.50f)
        {
            AddLoot(2, 1);
        }

        if (UnityEngine.Random.value <= 0.33f)
        {
            AddLoot(3, 1);
        }
    }

    // Adds the given quantity of an item to the pending loot list, stacking onto an existing entry if one exists
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

    // Returns a snapshot copy of the current pending loot list without clearing it
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

    // Returns the pending loot list and then clears it along with the kill and chest open counters
    public List<InvItemDatabase3> ConsumePendingLoot()
    {
        var result = GetPendingLoot();
        pendingLoot.Clear();
        goblinKills = 0;
        chestOpens = 0;
        return result;
    }
}
