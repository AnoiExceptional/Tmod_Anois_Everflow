using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items
{
    public class AlloyAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 34;

            Item.damage = 15;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            // 原版斧力字段以5%为一个单位，20对应100%斧力。
            Item.axe = 20;

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
