using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class AquaVirtueManaStarGlobalItem : GlobalItem
    {
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (IsManaStar(item.type) && player.GetModPlayer<AquaVirtuePlayer>().ActiveForPickup)
            {
                // manaMagnet 已把原版范围从 100px 提到 300px；再加 180px，合计正好增加 30 格。
                grabRange += 180;
            }
        }

        public override bool ItemSpace(Item item, Player player)
        {
            // 原版满魔时拒绝拾取回魔星；水德允许继续吸收，以记录溢出的恢复量。
            return IsManaStar(item.type) && player.GetModPlayer<AquaVirtuePlayer>().ActiveForPickup;
        }

        public override bool OnPickup(Item item, Player player)
        {
            if (IsManaStar(item.type))
            {
                AquaVirtuePlayer virtuePlayer = player.GetModPlayer<AquaVirtuePlayer>();
                // Do not inspect the transient Equipped flag here. Vanilla can execute this
                // hook while ResetEffects has cleared it but before UpdateAccessory restores it.
                int nominalRestoration = item.type == ItemID.ManaCloakStar ? 50 : 100;
                virtuePlayer.QueueManaStarPickup(nominalRestoration);
            }

            return true;
        }

        private static bool IsManaStar(int type) =>
            type == ItemID.Star || type == ItemID.SoulCake || type == ItemID.SugarPlum ||
            type == ItemID.ManaCloakStar;
    }
}
