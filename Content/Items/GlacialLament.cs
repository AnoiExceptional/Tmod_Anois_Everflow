using everflow.Common.Players;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class GlacialLament : ModItem
    {
        public override bool AltFunctionUse(Player player) => true;

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.VenusMagnum);
            Item.damage = 150;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.knockBack = 7.5f;
            Item.crit = 9;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.useAmmo = AmmoID.Bullet;
            Item.shoot = ProjectileID.Bullet;
        }

        public override bool CanUseItem(Player player)
        {
            bool alternateFire = player.altFunctionUse == 2;
            Item.damage = alternateFire ? 100 : 150;
            Item.useTime = alternateFire ? 45 : 15;
            Item.useAnimation = alternateFire ? 45 : 15;
            Item.knockBack = alternateFire ? 7f : 7.5f;
            return true;
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
            if (player.altFunctionUse == 2)
            {
                FireAlternateShot(
                    player,
                    source,
                    position,
                    velocity,
                    type,
                    damage,
                    knockback);
                return false;
            }

            bool freezingShot = player
                .GetModPlayer<GlacialLamentPlayer>()
                .AdvanceShotCounter();

            int projectileIndex = Projectile.NewProjectile(
                source,
                position,
                velocity,
                type,
                damage,
                knockback,
                player.whoAmI);

            Projectile projectile = Main.projectile[projectileIndex];
            projectile.penetrate = 1;
            projectile.maxPenetrate = 1;
            projectile
                .GetGlobalProjectile<GlacialLamentProjectile>()
                .Enable(freezingShot);
            projectile.netUpdate = true;

            return false;
        }

        private static void FireAlternateShot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int ammoProjectileType,
            int specialDamage,
            float specialKnockback)
        {
            // 右键特殊弹基础伤害为100；散射弹仍保持左键150基础伤害。
            int pelletDamage = System.Math.Max(1, (int)System.MathF.Round(specialDamage * 1.5f));

            for (int i = 0; i < 6; i++)
            {
                Vector2 pelletVelocity = velocity
                    .RotatedByRandom(MathHelper.ToRadians(8f)) *
                    Main.rand.NextFloat(0.82f, 1.12f);
                int pelletIndex = Projectile.NewProjectile(
                    source,
                    position,
                    pelletVelocity,
                    ammoProjectileType,
                    pelletDamage,
                    specialKnockback,
                    player.whoAmI);

                Projectile pellet = Main.projectile[pelletIndex];
                pellet.penetrate = 1;
                pellet.maxPenetrate = 1;
                pellet.GetGlobalProjectile<GlacialLamentProjectile>().Enable(false);
                pellet.netUpdate = true;
            }

            Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<GlacialLamentSpecialProjectile>(),
                specialDamage,
                specialKnockback,
                player.whoAmI);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.VenusMagnum)
                .AddIngredient(ItemID.FrostCore)
                .AddIngredient(ItemID.FragmentVortex, 15)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
