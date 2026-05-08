using System.Collections.Generic;
public interface ILootManager3
{
    int PendingGoblinKills();
    int PendingChestOpens();
    void AddGoblinKill();
    void AddChestOpen();
    List<InvItemDatabase3> GetPendingLoot();
    List<InvItemDatabase3> ConsumePendingLoot();
}