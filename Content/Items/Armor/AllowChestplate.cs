using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class AlloyChestplate : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 20;

            Item.defense = 5;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 30);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(25)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}