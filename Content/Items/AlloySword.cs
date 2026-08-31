using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles;
using everflow.Common.Players;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items
{


    public class AlloySword : ModItem
    {
        // 锁定“当前这一次攻击”是不是右键
        private bool rightAttackActive = false;

        public bool RightAttackActive => rightAttackActive;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.DamageType = DamageClass.Melee;

            Item.width = 56;
            Item.height = 56;

            // 永久基础状态 = 普通长剑
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;

            Item.knockBack = 5f;
            Item.value = Item.buyPrice(silver: 1);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;

            Item.autoReuse = true;

            // 永远不要在 CanUseItem 里改这两个
            Item.noMelee = false;
            Item.noUseGraphic = false;

            // 永久指定右键可能使用的 Projectile
            // 左键会在 Shoot() 中阻止它生成
            Item.shoot = ModContent.ProjectileType<AlloySwordStab>();

            // 现在我们明确规定：
            // 每 1 shootSpeed = 16 像素刺击距离
            Item.shootSpeed = 3.6f;
        }


        public override bool AltFunctionUse(Player player)
        {
            return true;
        }


        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();


            // 没有输入
            if (input.Mode ==
                AlloySwordInputPlayer.SwordInputMode.None)
            {
                return false;
            }


            // 右键刺击
            if (input.Mode ==
                AlloySwordInputPlayer.SwordInputMode.Right)
            {
                int stabType =
                    ModContent.ProjectileType<AlloySwordStab>();

                // 同一时刻最多存在一把刺击剑
                return player.ownedProjectileCounts[stabType] == 0;
            }


            // 左键普通挥砍
            return true;
        }


        public override void UseAnimation(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();


            rightAttackActive =
                input.Mode ==
                AlloySwordInputPlayer.SwordInputMode.Right;


            if (rightAttackActive)
            {
                // =========================
                // 右键：戳刺
                // =========================

                Item.useStyle =
                    ItemUseStyleID.Rapier;

                Item.noUseGraphic = true;
            }
            else
            {
                // =========================
                // 左键：普通挥舞
                // =========================

                Item.useStyle =
                    ItemUseStyleID.Swing;

                Item.noUseGraphic = false;
            }
        }


        public override void HoldItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();


            // 右键已经松开，并且当前攻击已经结束
            if (rightAttackActive &&
                input.Mode !=
                    AlloySwordInputPlayer.SwordInputMode.Right &&
                player.itemAnimation <= 0)
            {
                rightAttackActive = false;

                Item.useStyle =
                    ItemUseStyleID.Swing;

                Item.noUseGraphic = false;
            }
        }


        private void ResetToNormalSword()
        {
            rightAttackActive = false;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.noUseGraphic = false;
        }


        // 右键的时候禁止 Item 本体产生近战碰撞箱
        public override void UseItemHitbox(
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            if (rightAttackActive)
            {
                noHitbox = true;
            }
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


            // =========================
            // 左键
            // 不生成刺击Projectile
            // =========================

            if (input.Mode !=
                AlloySwordInputPlayer.SwordInputMode.Right)
            {
                return false;
            }


            // =========================
            // 右键
            // =========================

            Vector2 direction =
                velocity.SafeNormalize(
                    Vector2.UnitX * player.direction
                );


            float stabReach =
                Item.shootSpeed * 16f;


            int projectileIndex =
                Projectile.NewProjectile(
                    source,
                    player.MountedCenter,
                    direction,
                    type,
                    damage,
                    knockback,
                    player.whoAmI
                );


            if (projectileIndex >= 0 &&
                projectileIndex < Main.maxProjectiles)
            {
                Projectile projectile =
                    Main.projectile[projectileIndex];

                projectile.ai[0] = stabReach;

                projectile.netUpdate = true;
            }


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