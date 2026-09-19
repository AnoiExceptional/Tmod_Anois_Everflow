using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class WyrmBladePow : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 32;
            Item.damage = 200;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 9f;
            Item.crit = 4;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<WyrmBladePowProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool CanUseItem(Player player) =>
            player.ownedProjectileCounts[
                ModContent.ProjectileType<WyrmBladePowProjectile>()] == 0;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(15)
                .AddIngredient(ItemID.SoulofLight, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
