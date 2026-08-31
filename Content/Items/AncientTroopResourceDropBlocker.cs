using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.NPCs.Bosses;

namespace everflow.Content.Items
{
    public sealed class AncientTroopResourceDropBlocker : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (source is not EntitySource_Loot lootSource ||
                lootSource.Entity is not NPC sourceNPC ||
                sourceNPC.type != ModContent.NPCType<AncientTroop>())
            {
                return;
            }

            if (item.type == ItemID.Heart ||
                item.type == ItemID.CandyApple ||
                item.type == ItemID.CandyCane)
            {
                item.TurnToAir();
            }
        }
    }
}
