using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items
{
    public class AlloyHammer : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;

            Item.damage = 15;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.hammer = 65;

            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 30);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
