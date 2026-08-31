using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs.Minions;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles.Minions;

namespace everflow.Content.Items
{
    public class AlloyStaff : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DeadlySphereStaff);
            Item.damage = 15;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = ModContent.ProjectileType<AlloyBall>();
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
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
            player.AddBuff(ModContent.BuffType<AlloyBallBuff>(), 2);

            int projectileIndex = Projectile.NewProjectile(
                source,
                Main.MouseWorld,
                Vector2.Zero,
                ModContent.ProjectileType<AlloyBall>(),
                damage,
                knockback,
                player.whoAmI);

            Main.projectile[projectileIndex].originalDamage = Item.damage;
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(15)
                .AddIngredient(ItemID.Emerald, 2)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
