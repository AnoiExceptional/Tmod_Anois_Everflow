using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloyPistol : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 30;
            Item.scale = 0.5f;
            Item.damage = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 3f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item11;
            Item.useAmmo = AmmoID.Bullet;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 10f;
            Item.mana = 0;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.damage = 5;
                Item.DamageType = DamageClass.Magic;
                Item.useTime = 6;
                Item.useAnimation = 30;
                Item.UseSound = SoundID.Item34;
                Item.useAmmo = AmmoID.None;
                Item.shoot = ProjectileID.Flames;
                Item.shootSpeed = 21f;
                Item.mana = 10;
            }
            else
            {
                Item.damage = 10;
                Item.DamageType = DamageClass.Ranged;
                Item.useTime = 15;
                Item.useAnimation = 15;
                Item.UseSound = SoundID.Item11;
                Item.useAmmo = AmmoID.Bullet;
                Item.shoot = ProjectileID.Bullet;
                Item.shootSpeed = 10f;
                Item.mana = 0;
            }

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
            // 左键让原版弹药系统决定实际发射的子弹类型。
            if (player.altFunctionUse != 2)
                return true;

            Vector2 flameVelocity = velocity.RotatedByRandom(MathHelper.ToRadians(4f));
            Vector2 muzzleDirection =
                flameVelocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzlePosition = player.MountedCenter + muzzleDirection * 20f;

            int projectileIndex = Projectile.NewProjectile(
                source,
                muzzlePosition,
                flameVelocity,
                ProjectileID.Flames,
                damage,
                knockback,
                player.whoAmI);

            Projectile flame = Main.projectile[projectileIndex];
            flame.DamageType = DamageClass.Magic;
            flame.GetGlobalProjectile<AlloyPistolFlame>().FromAlloyPistol = true;
            flame.netUpdate = true;
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
