using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Systems;
using everflow.Content.Tiles;

namespace everflow.Content.Items.Materials
{
    public class AlloyBar : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<AlloyBarPlaced>());

            Item.width = 20;
            Item.height = 20;

            Item.maxStack = Item.CommonMaxStack;

            Item.value = Item.sellPrice(silver: 3);
            Item.rare = ItemRarityID.Blue;
            Item.placeStyle = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(3)

                // 铁锭 / 铅锭
                .AddRecipeGroup(RecipeGroupID.IronBar, 1)

                // 银锭 / 钨锭
                .AddRecipeGroup(EverflowRecipeGroups.SilverBarGroup, 1)

                // 恶魔锭 / 猩红锭
                .AddRecipeGroup(EverflowRecipeGroups.EvilBarGroup, 1)

                // 熔炉
                .AddTile(TileID.Furnaces)

                .Register();
        }
    }
}
