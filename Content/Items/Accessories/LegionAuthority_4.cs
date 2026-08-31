using everflow.Common.Items;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class LegionAuthority_4 : LegionAuthorityAccessory
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 8);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions += 4;
            player.GetModPlayer<LegionAuthorityPlayer>().NonSummonDamageMultiplier *= 0.50f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<LegionAuthority_3>()
                .AddIngredient(ItemID.FragmentStardust, 30)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
