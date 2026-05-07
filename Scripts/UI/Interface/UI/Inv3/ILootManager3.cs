using System.Collections.Generic;
public interface ILootManager3
{
    int PendingGoblinKills();
    int PendingChestOpens(); // <--- ДОБАВЛЕНО
    void AddGoblinKill();
    void AddChestOpen(); // <--- ДОБАВЛЕНО
    List<InvItemDatabase3> GetPendingLoot();
    List<InvItemDatabase3> ConsumePendingLoot();
}