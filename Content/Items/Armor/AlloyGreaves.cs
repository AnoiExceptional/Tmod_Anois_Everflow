using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class AlloyGreaves : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;

            Item.defense = 5;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 24);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(20)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}