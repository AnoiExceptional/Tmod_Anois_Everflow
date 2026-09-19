using everflow.Content.Projectiles;
using everflow.Content.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class WyrmBladeSpear : ModItem
    {
        private int attackSequence;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.Spears[Type] = true;
            // 与合金长枪相同：原版需要把物品登记进这个集合，
            // 按住替代使用键时才会在一次动作结束后继续触发右键。
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 104;
            Item.height = 104;
            Item.damage = 120;
            Item.DamageType = DamageClass.Melee;
            Item.crit = 4;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;

            // 伤害和绘制均交由随玩家手臂伸缩的长枪射弹负责。
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WyrmBladeSpearProjectile>();
            // 原版长枪AI在25帧动作中的最大位移系数约为38.7；
            // 12.4速度对应约480像素，也就是30格的最大戳刺长度。
            Item.shootSpeed = 12.4f;
        }

        public override bool AltFunctionUse(Player player) => true;

        // Item.autoReuse本身不会可靠地覆盖所有右键替代攻击路径，
        // 明确允许本物品在左右键按住时连续使用。
        public override bool? CanAutoReuseItem(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                int eruptionType =
                    ModContent.ProjectileType<WyrmBladeSpearEruptionProjectile>();
                Item.shoot = eruptionType;
                Item.shootSpeed = 1f;
                return player.ownedProjectileCounts[eruptionType] <= 0;
            }

            int nextAttack = attackSequence % 4 + 1;
            bool useSwing = nextAttack == 2 || nextAttack == 4;
            int projectileType = useSwing
                ? ModContent.ProjectileType<WyrmBladeSpearSwingProjectile>()
                : ModContent.ProjectileType<WyrmBladeSpearProjectile>();

            Item.shoot = projectileType;
            Item.shootSpeed = useSwing ? 1f : 12.4f;
            return player.ownedProjectileCounts[projectileType] <= 0;
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
                Vector2 rightClickAimDirection = velocity.SafeNormalize(
                    Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    source,
                    player.RotatedRelativePoint(player.MountedCenter),
                    rightClickAimDirection,
                    ModContent.ProjectileType<WyrmBladeSpearEruptionProjectile>(),
                    damage,
                    knockback,
                    player.whoAmI);
                return false;
            }

            attackSequence = attackSequence % 4 + 1;
            bool useSwing = attackSequence == 2 || attackSequence == 4;
            if (!useSwing)
                return true;

            Vector2 aimDirection = velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            float aimAngle = aimDirection.ToRotation();
            int swingDirection = attackSequence == 4 ? -1 : 1;
            Projectile.NewProjectile(
                source,
                player.RotatedRelativePoint(player.MountedCenter),
                Vector2.Zero,
                ModContent.ProjectileType<WyrmBladeSpearSwingProjectile>(),
                damage,
                knockback,
                player.whoAmI,
                aimAngle,
                swingDirection);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(15)
                .AddIngredient(ItemID.SoulofNight, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
