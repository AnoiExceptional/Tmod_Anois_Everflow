using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public class AlloySwordStab : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Items/AlloySword";


        // 刺击最短距离
        private const float MinReach = 10f;


        // ai[0] = Item传过来的最大刺击距离
        private float MaxReach =>
            Projectile.ai[0] > 0f
                ? Projectile.ai[0]
                : 34f;


        public float CollisionWidth =>
            10f * Projectile.scale;


        public override void SetDefaults()
        {
            Projectile.Size =
                new Vector2(18f);

            Projectile.aiStyle = -1;

            Projectile.friendly = true;

            Projectile.penetrate = -1;

            Projectile.tileCollide = false;

            Projectile.DamageType =
                DamageClass.Melee;

            Projectile.ownerHitCheck = true;

            // 我们自己控制位置
            Projectile.timeLeft = 2;

            Projectile.hide = true;
        }


        public override void AI()
        {
            Player player =
                Main.player[Projectile.owner];


            // ==========================
            // 判断本次刺击是否应该结束
            // ==========================

            if (!player.active ||
                player.dead ||
                player.HeldItem.type !=
                    ModContent.ItemType<AlloySword>() ||
                player.itemAnimation <= 0 ||
                player.HeldItem.ModItem is not AlloySword sword ||
                !sword.RightAttackActive)
            {
                Projectile.Kill();
                return;
            }


            // 只要攻击动画没结束就继续存在
            Projectile.timeLeft = 2;

            player.heldProj =
                Projectile.whoAmI;


            // ==========================
            // 固定刺击方向
            // ==========================

            Vector2 direction =
                Projectile.velocity.SafeNormalize(
                    Vector2.UnitX * player.direction
                );


            // ==========================
            // 当前攻击进度
            //
            // 0 ----------------> 1
            // 开始               结束
            // ==========================

            float progress =
                1f -
                player.itemAnimation /
                (float)player.itemAnimationMax;

            progress =
                MathHelper.Clamp(
                    progress,
                    0f,
                    1f
                );


            /*
             * sin(0)   = 0
             * sin(π/2) = 1
             * sin(π)   = 0
             *
             * 所以：
             *
             * 开始：收回
             * 中间：伸到最远
             * 结束：收回
             */

            float thrustProgress =
                (float)System.Math.Sin(
                    progress * MathHelper.Pi
                );


            float currentReach =
                MathHelper.Lerp(
                    MinReach,
                    MaxReach,
                    thrustProgress
                );


            Vector2 playerCenter =
                player.RotatedRelativePoint(
                    player.MountedCenter,
                    reverseRotation: false,
                    addGfxOffY: false
                );


            Projectile.Center =
                playerCenter +
                direction * currentReach;


            // ==========================
            // 贴图朝向
            // ==========================

            Projectile.spriteDirection =
                direction.X >= 0f
                    ? 1
                    : -1;


            Projectile.rotation =
                direction.ToRotation()
                + MathHelper.PiOver2
                - MathHelper.PiOver4
                  * Projectile.spriteDirection;


            SetVisualOffsets();
        }


        private void SetVisualOffsets()
        {
            // 你的贴图是56x56
            const int HalfSpriteWidth =
                56 / 2;

            const int HalfSpriteHeight =
                56 / 2;


            int halfProjectileWidth =
                Projectile.width / 2;

            int halfProjectileHeight =
                Projectile.height / 2;


            DrawOriginOffsetX = 0;

            DrawOffsetX =
                -(HalfSpriteWidth -
                  halfProjectileWidth);

            DrawOriginOffsetY =
                -(HalfSpriteHeight -
                  halfProjectileHeight);
        }


        public override bool ShouldUpdatePosition()
        {
            // AI()里面已经自己设置Center
            return false;
        }


        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 =
                TileCuttingContext.AttackProjectile;


            Vector2 direction =
                Projectile.velocity.SafeNormalize(
                    Vector2.UnitX
                );


            Vector2 start =
                Projectile.Center;

            Vector2 end =
                start +
                direction * 28f;


            Utils.PlotTileLine(
                start,
                end,
                CollisionWidth,
                DelegateMethods.CutTiles
            );
        }


        public override bool? Colliding(
            Rectangle projHitbox,
            Rectangle targetHitbox)
        {
            Vector2 direction =
                Projectile.velocity.SafeNormalize(
                    Vector2.UnitX
                );


            Vector2 start =
                Projectile.Center;

            // 剑刃从Projectile中心继续向前
            Vector2 end =
                start +
                direction * 28f;


            float collisionPoint = 0f;


            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                CollisionWidth,
                ref collisionPoint
            );
        }
    }
}