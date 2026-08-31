using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class BurgerKingDropGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            if (item.type != ItemID.Burger || !Main.rand.NextBool(10))
                return;

            player.QuickSpawnItem(
                player.GetSource_ItemUse(item),
                ModContent.ItemType<BurgerKing>(),
                1);
        }
    }
}
