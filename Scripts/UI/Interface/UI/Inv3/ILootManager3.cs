using System.Collections.Generic;
public interface ILootManager3
{
    void AddGoblinKill();
    int PendingGoblinKills();
    List<InvItemDatabase3> GetPendingLoot();
    List<InvItemDatabase3> ConsumePendingLoot();
}