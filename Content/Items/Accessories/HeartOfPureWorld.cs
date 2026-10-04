using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class HeartOfPureWorld : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 15);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<HeartOfTheWorldPlayer>().PureHeartEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HeartOfTheWorld>()
                .AddIngredient(ItemID.FragmentSolar, 10)
                .AddIngredient(ItemID.FragmentVortex, 10)
                .AddIngredient(ItemID.FragmentNebula, 10)
                .AddIngredient(ItemID.FragmentStardust, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
