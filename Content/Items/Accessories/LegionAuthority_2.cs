using everflow.Common.Items;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class LegionAuthority_2 : LegionAuthorityAccessory
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions += 2;
            player.GetModPlayer<LegionAuthorityPlayer>().NonSummonDamageMultiplier *= 0.90f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<LegionAuthority_1>()
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
