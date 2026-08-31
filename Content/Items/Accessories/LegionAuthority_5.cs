using everflow.Common.Items;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class LegionAuthority_5 : LegionAuthorityAccessory
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 15);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions += 5;
            player.GetModPlayer<LegionAuthorityPlayer>().NonSummonDamageMultiplier = 0f;
        }
    }
}
