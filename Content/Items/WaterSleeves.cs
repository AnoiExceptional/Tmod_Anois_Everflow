using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class WaterSleeves : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 15;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<WaterSleevesProjectile>();
            Item.shootSpeed = 1f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? CanAutoReuseItem(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                int spinType = ModContent.ProjectileType<WaterSleevesSpinProjectile>();
                Item.damage = 10;
                Item.knockBack = 8f;
                Item.crit = 4;
                Item.useTime = 60;
                Item.useAnimation = 60;
                Item.shoot = spinType;
                Item.shootSpeed = 1f;
                return player.ownedProjectileCounts[spinType] <= 0;
            }

            int thrustType = ModContent.ProjectileType<WaterSleevesProjectile>();
            Item.damage = 15;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.shoot = thrustType;
            Item.shootSpeed = 1f;
            return player.ownedProjectileCounts[thrustType] <= 0;
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            if (player.altFunctionUse != 2)
                return true;

            float startAngle = velocity.SafeNormalize(
                Vector2.UnitX * player.direction).ToRotation();

            Projectile.NewProjectile(
                source,
                player.RotatedRelativePoint(player.MountedCenter),
                Vector2.Zero,
                ModContent.ProjectileType<WaterSleevesSpinProjectile>(),
                damage,
                knockback,
                player.whoAmI,
                startAngle,
                player.direction);
            Projectile.NewProjectile(
                source,
                player.RotatedRelativePoint(player.MountedCenter),
                Vector2.UnitX,
                ModContent.ProjectileType<WaterSleevesSpinProjectile>(),
                damage,
                knockback,
                player.whoAmI,
                startAngle + MathHelper.Pi,
                player.direction);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Silk, 20)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}
