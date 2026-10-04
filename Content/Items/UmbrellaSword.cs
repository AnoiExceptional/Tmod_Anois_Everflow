using everflow.Content.Players;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class UmbrellaSword : ModItem
    {
        public override bool AltFunctionUse(Player player) => true;

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.damage = 15;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 20);
        }

        public override bool CanUseItem(Player player)
        {
            bool blocking = player.altFunctionUse == 2;
            int blockProjectileType = ModContent.ProjectileType<UmbrellaSwordBlockProjectile>();

            // 右键格挡动画尚未结束时完全禁止左键进入使用流程。
            // 持续按住左键时，原版自动挥舞会在格挡结束后重新尝试使用。
            if (!blocking && player.ownedProjectileCounts[blockProjectileType] > 0)
                return false;

            if (blocking)
            {
                UmbrellaSwordPlayer blockPlayer = player.GetModPlayer<UmbrellaSwordPlayer>();
                if (blockPlayer.RightClickConsumed || blockPlayer.ParryCooldown > 0 ||
                    player.ownedProjectileCounts[blockProjectileType] > 0)
                    return false;

                // 原版雨伞使用 Thrust 姿势；实际打开的伞由手持射弹绘制，
                // 以便让格挡判定与伸出动作保持一致。
                Item.useStyle = ItemUseStyleID.Thrust;
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.shoot = ModContent.ProjectileType<UmbrellaSwordBlockProjectile>();
                Item.shootSpeed = 1f;
                Item.UseSound = SoundID.Item1;
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 22;
                Item.useAnimation = 22;
                Item.noUseGraphic = false;
                Item.noMelee = false;
                Item.shoot = ProjectileID.None;
                Item.shootSpeed = 0f;
                Item.UseSound = SoundID.Item1;
            }

            return base.CanUseItem(player);
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

            UmbrellaSwordPlayer blockPlayer = player.GetModPlayer<UmbrellaSwordPlayer>();
            blockPlayer.ConsumeRightClick();
            blockPlayer.BeginBlockAttempt();
            Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0f));
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                direction,
                type,
                0,
                0f,
                player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Umbrella)
                .AddIngredient(ItemID.IronBroadsword)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.Umbrella)
                .AddIngredient(ItemID.LeadBroadsword)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
