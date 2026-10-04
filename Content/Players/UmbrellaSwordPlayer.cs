using Microsoft.Xna.Framework;
using global::everflow.Content.Items;
using global::everflow.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class UmbrellaSwordPlayer : ModPlayer
    {
        internal int BlockTime;
        internal int ParryCooldown;
        internal Vector2 BlockDirection = Vector2.UnitX;
        internal bool BlockConsumed { get; private set; }
        internal bool RightClickConsumed { get; private set; }

        public override void PreUpdate()
        {
            if (!Player.controlUseTile)
                RightClickConsumed = false;

            if (!Player.controlUseTile || RightClickConsumed || ParryCooldown > 0 || Player.itemAnimation <= 0 ||
                Player.HeldItem.type != ModContent.ItemType<UmbrellaSword>() ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<UmbrellaSwordBlockProjectile>()] > 0 ||
                Player.HeldItem.noMelee)
                return;

            // 右键优先级高于正在进行的左键挥舞：先结束左键动作，随后同一帧
            // 的原版 ItemCheck 就能正常开始右键格挡。左右键同时按住时还要
            // 屏蔽本帧左键，否则原版会优先重新启动左键，表现为卡在同一帧。
            Player.itemAnimation = 0;
            Player.itemTime = 0;
            Player.reuseDelay = 0;
            Player.controlUseItem = false;

            // 原版不会保证在取消左键后重新处理同一帧的副使用输入，
            // 因此这里直接开始格挡，而不是继续等待 ItemCheck。
            Item item = Player.HeldItem;
            item.useStyle = ItemUseStyleID.Thrust;
            item.useTime = 20;
            item.useAnimation = 20;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.shoot = ModContent.ProjectileType<UmbrellaSwordBlockProjectile>();
            item.shootSpeed = 1f;

            Vector2 direction = (Main.MouseWorld - Player.MountedCenter)
                .SafeNormalize(new Vector2(Player.direction, 0f));
            BeginBlockAttempt();
            Projectile.NewProjectile(
                Player.GetSource_ItemUse(item),
                Player.MountedCenter,
                direction,
                ModContent.ProjectileType<UmbrellaSwordBlockProjectile>(),
                0,
                0f,
                Player.whoAmI);

            RightClickConsumed = true;
            Player.itemAnimationMax = 20;
            Player.itemAnimation = 20;
            Player.itemTime = 20;
            Player.altFunctionUse = 2;
            Player.releaseUseTile = false;
        }

        internal void ConsumeRightClick() => RightClickConsumed = true;

        internal void BeginBlockAttempt()
        {
            BlockTime = 0;
            BlockConsumed = false;
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (Player.HeldItem.type == ModContent.ItemType<UmbrellaSword>() &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<UmbrellaSwordBlockProjectile>()] > 0)
            {
                // 清除左键挥舞已缓存到玩家绘制数据中的物品贴图；
                // 打开的雨伞由格挡射弹独立绘制。
                PlayerDrawLayers.HeldItem.Hide();
            }
        }

        public override void PostUpdate()
        {
            ApplyUmbrellaFallSlow();

            if (BlockTime > 0)
                BlockTime--;
            if (ParryCooldown > 0)
                ParryCooldown--;
        }

        private void ApplyUmbrellaFallSlow()
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<UmbrellaSwordBlockProjectile>()] <= 0)
                return;

            // “角色上方”同时兼容重力翻转。
            Vector2 characterUp = new Vector2(0f, -Player.gravDir);
            Vector2 umbrellaDirection = BlockDirection.SafeNormalize(characterUp);
            float upwardDot = Vector2.Dot(umbrellaDirection, characterUp);
            const float Cos45Degrees = 0.70710677f;
            if (upwardDot <= Cos45Degrees)
                return;

            // 原版雨伞每帧都会把坠落起点刷新到玩家当前高度。这使玩家可以
            // 在临近地面时开伞，通过时机操作避免此前累积的摔落伤害。
            Player.fallStart = (int)(Player.position.Y / 16f);

            // 与原版一致：按下“下”只绕过减速，仍保留雨伞的免摔刷新。
            if (Player.controlDown)
                return;

            float angle = System.MathF.Acos(MathHelper.Clamp(upwardDot, -1f, 1f));
            float strength = 1f - MathHelper.Clamp(angle / MathHelper.PiOver4, 0f, 1f);

            float fallingSpeed = Player.velocity.Y * Player.gravDir;
            if (fallingSpeed <= 2f)
                return;

            // 原版雨伞最终将下落速度限制为2。这里按角度在玩家当前的
            // maxFallSpeed 与原版雨伞上限2之间插值；因此正上方与原版完全
            // 一致，45度处回到无效果，并保留羽落药水原有的叠加顺序。
            float angledFallSpeedCap = MathHelper.Lerp(Player.maxFallSpeed, 2f, strength);
            if (fallingSpeed > angledFallSpeedCap)
                Player.velocity.Y = angledFallSpeedCap * Player.gravDir;
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            // 地狱烙印只格挡敌怪的接触攻击；摔落、岩浆、陷阱等不触发。
            int sourceNpcIndex = info.DamageSource.SourceNPCIndex;
            if (BlockTime <= 0 || ParryCooldown > 0 ||
                sourceNpcIndex < 0 || sourceNpcIndex >= Main.maxNPCs)
                return false;

            NPC sourceNpc = Main.npc[sourceNpcIndex];
            if (!sourceNpc.active)
                return false;

            // 只格挡撑伞方向左右各45度，即正前方90度锥形范围内的接触攻击。
            Vector2 facing = BlockDirection.SafeNormalize(new Vector2(Player.direction, 0f));
            Vector2 toAttacker = (sourceNpc.Center - Player.Center).SafeNormalize(facing);
            if (Vector2.Dot(facing, toAttacker) < 0.70710677f)
                return false;

            BlockTime = 0;
            BlockConsumed = true;

            // 不给予惊人一刻，也不增加防御；仅给予固定1秒无敌。
            Player.immune = true;
            Player.immuneTime = System.Math.Max(Player.immuneTime, 60);
            for (int i = 0; i < Player.hurtCooldowns.Length; i++)
                Player.hurtCooldowns[i] = System.Math.Max(Player.hurtCooldowns[i], 60);

            Player.ClearBuff(BuffID.ParryDamageBuff);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(22f, 30f),
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(2.5f, 2.5f),
                    100,
                    Color.LightGray,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }

            return true;
        }
    }
}
