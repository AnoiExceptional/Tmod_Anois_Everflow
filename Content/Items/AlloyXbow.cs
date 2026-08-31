using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Players;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloyXbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.CobaltRepeater);
            Item.damage = 30;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useAmmo = AmmoID.Arrow;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            // 与骸骨弓及其专属骨箭的发射速度一致。
            Item.shootSpeed = 11f;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
            Item.autoReuse = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            bool alternate =
                input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;

            Item.damage = alternate ? 45 : 30;
            Item.useTime = alternate ? 50 : 30;
            Item.useAnimation = Item.useTime;
            Item.shoot = alternate
                ? ModContent.ProjectileType<AlloyArrow>()
                : ProjectileID.WoodenArrowFriendly;

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
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            int projectileType =
                input.Mode == AlloySwordInputPlayer.SwordInputMode.Right
                    ? ModContent.ProjectileType<AlloyArrow>()
                    : type;

            int projectileIndex = Projectile.NewProjectile(
                source,
                position,
                velocity,
                projectileType,
                damage,
                knockback,
                player.whoAmI);

            Projectile projectile = Main.projectile[projectileIndex];
            // 左键不穿透，只能命中一个敌人；右键合金箭可命中五个敌人。
            projectile.penetrate =
                input.Mode == AlloySwordInputPlayer.SwordInputMode.Right ? 5 : 1;
            projectile.originalDamage = Item.damage;
            projectile.netUpdate = true;

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
