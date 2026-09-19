using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Tiles;

namespace everflow.Content.Items.Materials
{
    public sealed class WyrmBladeBar : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<AlloyBarPlaced>());
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.material = true;
            Item.rare = ItemRarityID.Orange;
            Item.placeStyle = 1;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeSharp>(3)
                // 精金熔炉与钛金熔炉共用此制作站ID。
                .AddTile(TileID.AdamantiteForge)
                .Register();
        }
    }
}
